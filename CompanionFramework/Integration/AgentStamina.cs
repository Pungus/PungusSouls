using UnityEngine;

public class AgentStamina : MonoBehaviour
{
    private const string ZDO_Stamina = "agent_stamina";

    private const float SaveInterval = 5f;
    private const float RemoteSyncInterval = 0.5f;

    private const float BaseStamina = 100f;
    private const float BaseRegenPerSecond = 8f;
    private const float RestedRegenMultiplier = 1.5f;

    private const float RunDrainPerSecond = 1.5f;
    private const float SwimDrainPerSecond = 7f;
    private const float SneakDrainPerSecond = 1f;

    private const float RegenDelayAfterUse = 0.35f;
    private const float LowRunThreshold = 0.15f;

    private ZNetView _zNetView;
    private Character _character;
    private Humanoid _humanoid;
    private AgentFood _food;

    private float _saveTimer;
    private float _regenDelayTimer;
    private float _remoteSyncTimer;
    private bool _initialized;
    private bool _dirty;

    public float Stamina { get; private set; }
    public bool IsResting { get; set; }
    public bool IsCrouching { get; set; }

    public float MaxStamina
    {
        get
        {
            RefreshReferences();
            float foodBonus = _food != null ? _food.TotalStaminaBonus : 0f;
            return Mathf.Max(1f, BaseStamina + foodBonus);
        }
    }

    private void Awake()
    {
        RefreshReferences();
    }

    private void Start()
    {
        TryInit();
    }

    private void Update()
    {
        RefreshReferences();

        if (!_initialized)
        {
            TryInit();
            return;
        }

        if (_zNetView == null || !_zNetView.IsValid())
            return;

        if (!_zNetView.IsOwner())
        {
            _remoteSyncTimer -= Time.deltaTime;

            if (_remoteSyncTimer <= 0f)
            {
                _remoteSyncTimer = RemoteSyncInterval;
                LoadFromZDO();
            }

            return;
        }

        UpdateStamina(Time.deltaTime);

        _saveTimer += Time.deltaTime;

        if (_dirty && _saveTimer >= SaveInterval)
        {
            _saveTimer = 0f;
            _dirty = false;
            SaveToZDO();
        }
    }

    private void OnDestroy()
    {
        if (_initialized)
            SaveToZDO();
    }

    public bool CanRun()
    {
        return Stamina > LowRunThreshold;
    }

    public bool CanAttack(float minimum = 8f)
    {
        return Stamina >= minimum;
    }

    public bool UseStamina(float amount)
    {
        if (float.IsNaN(amount) || amount <= 0f)
            return true;

        if (Stamina < amount)
            return false;

        Stamina = Mathf.Max(0f, Stamina - amount);
        _regenDelayTimer = RegenDelayAfterUse;
        _dirty = true;
        return true;
    }

    public void Drain(float amount)
    {
        if (float.IsNaN(amount) || amount <= 0f)
            return;

        float old = Stamina;
        Stamina = Mathf.Max(0f, Stamina - amount);
        _regenDelayTimer = RegenDelayAfterUse;

        if (!Mathf.Approximately(old, Stamina))
            _dirty = true;
    }

    public void Restore(float amount)
    {
        if (float.IsNaN(amount) || amount <= 0f)
            return;

        float old = Stamina;
        Stamina = Mathf.Min(MaxStamina, Stamina + amount);

        if (!Mathf.Approximately(old, Stamina))
            _dirty = true;
    }

    public void ClampToMax()
    {
        float old = Stamina;
        Stamina = Mathf.Clamp(Stamina, 0f, MaxStamina);

        if (!Mathf.Approximately(old, Stamina))
            _dirty = true;
    }

    public float GetStaminaPercentage()
    {
        float max = MaxStamina;
        return max <= 0f ? 0f : Stamina / max;
    }

    private void RefreshReferences()
    {
        if (_zNetView == null)
            _zNetView = GetComponent<ZNetView>();

        if (_character == null)
            _character = GetComponent<Character>();

        if (_humanoid == null)
            _humanoid = GetComponent<Humanoid>();

        if (_food == null)
            _food = GetComponent<AgentFood>();
    }

    private void TryInit()
    {
        RefreshReferences();

        if (_initialized)
            return;

        if (_zNetView == null || !_zNetView.IsValid())
            return;

        LoadFromZDO(MaxStamina);

        if (float.IsNaN(Stamina) || Stamina <= 0f)
            Stamina = MaxStamina;

        _initialized = true;
    }

    private void UpdateStamina(float dt)
    {
        float max = MaxStamina;

        if (float.IsNaN(Stamina))
            Stamina = max;

        if (Stamina > max)
        {
            Stamina = max;
            _dirty = true;
        }

        bool swimming = _character != null && _character.IsSwimming();
        bool running = _character != null && _character.IsRunning();
        bool sneaking = IsCrouching && !running && !swimming;

        if (swimming)
        {
            DrainContinuous(SwimDrainPerSecond * dt);
            return;
        }

        if (running)
        {
            DrainContinuous(RunDrainPerSecond * dt);
            return;
        }

        if (sneaking)
        {
            DrainContinuous(SneakDrainPerSecond * dt);
            return;
        }

        if (_regenDelayTimer > 0f)
            _regenDelayTimer = Mathf.Max(0f, _regenDelayTimer - dt);

        if (_regenDelayTimer > 0f || Stamina >= max)
            return;

        float multiplier = IsResting ? RestedRegenMultiplier : 1f;
        float old = Stamina;
        Stamina = Mathf.Min(max, Stamina + BaseRegenPerSecond * multiplier * dt);

        if (!Mathf.Approximately(old, Stamina))
            _dirty = true;
    }

    private void DrainContinuous(float amount)
    {
        if (amount <= 0f || float.IsNaN(amount))
            return;

        float old = Stamina;
        Stamina = Mathf.Max(0f, Stamina - amount);

        if (!Mathf.Approximately(old, Stamina))
            _dirty = true;
    }

    private void SaveToZDO()
    {
        if (_zNetView == null || !_zNetView.IsValid() || !_zNetView.IsOwner())
            return;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null)
            return;

        zdo.Set(ZDO_Stamina, float.IsNaN(Stamina) ? MaxStamina : Stamina);
    }

    private void LoadFromZDO(float defaultValue = -1f)
    {
        if (_zNetView == null || !_zNetView.IsValid())
            return;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null)
            return;

        float fallback = defaultValue >= 0f ? defaultValue : MaxStamina;
        float value = zdo.GetFloat(ZDO_Stamina, fallback);
        Stamina = Mathf.Clamp(float.IsNaN(value) ? fallback : value, 0f, MaxStamina);
    }
}
