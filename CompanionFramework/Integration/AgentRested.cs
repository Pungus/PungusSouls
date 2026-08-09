using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AgentRested : MonoBehaviour
{
    private const string ZDO_RestedRemaining = "agent_rested_remaining";
    private const string ZDO_RestProgress = "agent_rest_progress";
    private const string ZDO_ComfortLevel = "agent_comfort_level";
    private const float SaveInterval = 5f;
    private const float ScanInterval = 20f;
    private const float RestProgressRequired = 20f;
    private const float ComfortSearchRadius = 10f;
    private const float FireSearchRadius = 12f;
    private const float SafeEnemyRange = 18f;
    private const float HealthRegenInterval = 2f;
    private const float BaseRestedSeconds = 420f;
    private const float SecondsPerComfortLevel = 60f;
    private const float MaxRestedDuration = 1800f;
    private const float RestedHealthRegenMultiplierBonus = 0.5f;

    private AgentComponent _agent;
    private ZNetView _zNetView;
    private Character _character;
    private MonsterAI _monsterAI;
    private AgentStamina _stamina;
    private AgentSkills _skills;
    private float _saveTimer;
    private float _scanTimer;
    private float _healthRegenTimer;
    private bool _initialized;
    private bool _safe;
    private bool _nearActiveFire;
    private bool _shelteredOrHome;
    private bool _dirty;

    public float RestedRemaining { get; private set; }
    public float RestProgress { get; private set; }
    public bool IsRested { get { return RestedRemaining > 0f; } }
    public bool IsRestingNow { get; private set; }
    public int ComfortLevel { get; private set; }
    public float CurrentRestedDuration { get; private set; }

    private void Awake()
    {
        RefreshReferences();
        _scanTimer = Random.Range(2f, 8f);
    }

    private void Start()
    {
        _scanTimer = UnityEngine.Random.Range(8f, 20f);
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
            LoadFromZDO();
            return;
        }

        float dt = Time.deltaTime;
        _scanTimer -= dt;

        if (_scanTimer <= 0f)
        {
            _scanTimer = ScanInterval + Random.Range(0f, 2f);
            UpdateComfortAndRestingState();
        }

        if (IsRestingNow)
        {
            RestProgress = Mathf.Min(RestProgressRequired, RestProgress + dt);

            if (RestProgress >= RestProgressRequired)
                GrantRested(CurrentRestedDuration);
        }
        else
        {
            RestProgress = Mathf.Max(0f, RestProgress - dt * 2f);
        }

        if (RestedRemaining > 0f)
        {
            RestedRemaining = Mathf.Max(0f, RestedRemaining - dt);
            _dirty = true;
        }

        ApplyStaminaRestedState();
        ApplyRestedHealthRegen(dt);
        _saveTimer += dt;

        if (_dirty && _saveTimer >= SaveInterval)
            SaveToZDO();
    }

    private void OnDestroy()
    {
        SaveToZDO();
    }

    public void GrantRested(float duration)
    {
        float clamped = Mathf.Clamp(duration, BaseRestedSeconds, MaxRestedDuration);

        if (RestedRemaining < clamped)
        {
            RestedRemaining = clamped;
            _dirty = true;

            if (_skills != null)
                _skills.RaiseSkill("resting", Mathf.Max(1, ComfortLevel));
        }
    }

    public string GetStatusText()
    {
        if (IsRested)
            return "Rested " + Mathf.CeilToInt(RestedRemaining) + "s  Comfort " + ComfortLevel;

        if (IsRestingNow)
            return "Resting " + Mathf.RoundToInt((RestProgress / RestProgressRequired) * 100f) + "%  Comfort " + ComfortLevel;

        return "Comfort " + ComfortLevel;
    }

    private void RefreshReferences()
    {
        if (_agent == null)
            _agent = GetComponent<AgentComponent>();

        if (_zNetView == null)
            _zNetView = GetComponent<ZNetView>();

        if (_character == null)
            _character = GetComponent<Character>();

        if (_monsterAI == null)
            _monsterAI = GetComponent<MonsterAI>();

        if (_stamina == null)
            _stamina = GetComponent<AgentStamina>();

        if (_skills == null)
            _skills = GetComponent<AgentSkills>();
    }

    private void TryInit()
    {
        if (_initialized)
            return;

        if (_zNetView == null || !_zNetView.IsValid())
            return;

        LoadFromZDO();
        _initialized = true;
    }

    private void UpdateComfortAndRestingState()
    {
        _safe = IsSafeToRest();
        _nearActiveFire = IsNearActiveFire();
        _shelteredOrHome = IsShelteredOrAtHome();
        ComfortLevel = CalculateComfortLevel();
        CurrentRestedDuration = CalculateRestedDuration(ComfortLevel);
        bool notMoving = _character == null || _character.GetVelocity().magnitude < 0.35f;
        bool notSwimming = _character == null || !_character.IsSwimming();
        bool notAlerted = !GetBoolField(_monsterAI, "m_alerted", false);
        IsRestingNow = _safe && notMoving && notSwimming && notAlerted && _nearActiveFire && _shelteredOrHome && ComfortLevel > 0;
    }

    private int CalculateComfortLevel()
    {
        Dictionary<string, int> bestByGroup = new Dictionary<string, int>();
        Piece[] pieces = UnityEngine.Object.FindObjectsByType<Piece>(FindObjectsSortMode.None);

        foreach (Piece piece in pieces)
        {
            if (piece == null)
                continue;

            if (Vector3.Distance(transform.position, piece.transform.position) > ComfortSearchRadius)
                continue;

            int comfort = GetPieceComfort(piece);

            if (comfort <= 0)
                continue;

            string group = GetPieceComfortGroup(piece);

            if (string.IsNullOrEmpty(group))
                group = piece.name;

            if (group.IndexOf("Fire", System.StringComparison.OrdinalIgnoreCase) >= 0 && !PieceHasActiveFire(piece))
                continue;

            if (!bestByGroup.TryGetValue(group, out int existing) || comfort > existing)
                bestByGroup[group] = comfort;
        }

        int total = 0;

        foreach (KeyValuePair<string, int> entry in bestByGroup)
            total += Mathf.Max(0, entry.Value);

        if (_nearActiveFire && total < 1)
            total = 1;

        return Mathf.Max(0, total);
    }

    private float CalculateRestedDuration(int comfortLevel)
    {
        if (comfortLevel <= 0)
            return 0f;

        return Mathf.Clamp(BaseRestedSeconds + comfortLevel * SecondsPerComfortLevel, BaseRestedSeconds, MaxRestedDuration);
    }

    private int GetPieceComfort(Piece piece)
    {
        FieldInfo field = FindField(piece.GetType(), "m_comfort");

        if (field == null)
            return 0;

        object value = field.GetValue(piece);

        if (value is int intValue)
            return intValue;

        if (value is float floatValue)
            return Mathf.RoundToInt(floatValue);

        return 0;
    }

    private string GetPieceComfortGroup(Piece piece)
    {
        FieldInfo field = FindField(piece.GetType(), "m_comfortGroup");

        if (field == null)
            return piece.name;

        object value = field.GetValue(piece);
        string group = value != null ? value.ToString() : string.Empty;
        return string.IsNullOrEmpty(group) || group == "None" ? piece.name : group;
    }

    private bool PieceHasActiveFire(Piece piece)
    {
        Fireplace fireplace = piece.GetComponent<Fireplace>() ?? piece.GetComponentInChildren<Fireplace>();
        return fireplace != null && IsFireplaceActive(fireplace);
    }

    private bool IsNearActiveFire()
    {
        Fireplace[] fireplaces = UnityEngine.Object.FindObjectsByType<Fireplace>(FindObjectsSortMode.None);

        foreach (Fireplace fireplace in fireplaces)
        {
            if (fireplace != null && Vector3.Distance(transform.position, fireplace.transform.position) <= FireSearchRadius && IsFireplaceActive(fireplace))
                return true;
        }

        return false;
    }

    private bool IsFireplaceActive(Fireplace fireplace)
    {
        if (fireplace == null || fireplace.gameObject == null)
            return false;

        try
        {
            MethodInfo method = FindMethod(fireplace.GetType(), "IsBurning");

            if (method != null && method.ReturnType == typeof(bool))
                return (bool)method.Invoke(fireplace, null);
        }
        catch
        {
            return false;
        }

        ZNetView view = fireplace.GetComponent<ZNetView>();

        if (view != null && view.IsValid())
        {
            ZDO zdo = view.GetZDO();

            if (zdo != null)
            {
                if (zdo.GetFloat("fuel", 0f) > 0f)
                    return true;

                if (zdo.GetBool("burning", false))
                    return true;
            }
        }

        return false;
    }

    private bool IsShelteredOrAtHome()
    {
        if (IsAtHome())
            return true;

        if (_character != null)
        {
            MethodInfo method = FindMethod(_character.GetType(), "InShelter");

            if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
                return (bool)method.Invoke(_character, null);
        }

        return IsNearBed();
    }

    private bool IsAtHome()
    {
        if (_agent == null || _agent.Context == null || _agent.Context.HomeZone == null || !_agent.Context.HomeZone.IsSet)
            return false;

        return DistanceXZ(transform.position, _agent.Context.HomeZone.Center) <= Mathf.Max(1f, _agent.Context.HomeZone.Radius);
    }

    private bool IsNearBed()
    {
        Bed[] beds = UnityEngine.Object.FindObjectsByType<Bed>(FindObjectsSortMode.None);

        foreach (Bed bed in beds)
        {
            if (bed != null && Vector3.Distance(transform.position, bed.transform.position) <= ComfortSearchRadius)
                return true;
        }

        return false;
    }

    private bool IsSafeToRest()
    {
        if (_character == null)
            return false;

        foreach (Character candidate in Character.GetAllCharacters())
        {
            if (candidate == null || candidate == _character || candidate.IsDead() || candidate.IsPlayer())
                continue;

            if (!BaseAI.IsEnemy(_character, candidate))
                continue;

            if (Vector3.Distance(transform.position, candidate.transform.position) <= SafeEnemyRange)
                return false;
        }

        return true;
    }

    private void ApplyStaminaRestedState()
    {
        if (_stamina != null)
            _stamina.IsResting = IsRested || IsRestingNow;
    }

    private void ApplyRestedHealthRegen(float dt)
    {
        if (!IsRested || _character == null || _character.IsDead() || !_safe)
            return;

        _healthRegenTimer += dt;

        if (_healthRegenTimer < HealthRegenInterval)
            return;

        _healthRegenTimer = 0f;

        if (_character.GetHealth() < _character.GetMaxHealth())
            _character.Heal(RestedHealthRegenMultiplierBonus * HealthRegenInterval);
    }

    private void SaveToZDO()
    {
        if (!_dirty)
            return;

        if (_zNetView == null || !_zNetView.IsValid() || !_zNetView.IsOwner())
            return;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null)
            return;

        zdo.Set(ZDO_RestedRemaining, RestedRemaining);
        zdo.Set(ZDO_RestProgress, RestProgress);
        zdo.Set(ZDO_ComfortLevel, ComfortLevel);
        _dirty = false;
        _saveTimer = 0f;
    }

    private void LoadFromZDO()
    {
        if (_zNetView == null || !_zNetView.IsValid())
            return;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null)
            return;

        RestedRemaining = Mathf.Max(0f, zdo.GetFloat(ZDO_RestedRemaining, RestedRemaining));
        RestProgress = Mathf.Clamp(zdo.GetFloat(ZDO_RestProgress, RestProgress), 0f, RestProgressRequired);
        ComfortLevel = Mathf.Max(0, zdo.GetInt(ZDO_ComfortLevel, ComfortLevel));
        CurrentRestedDuration = CalculateRestedDuration(ComfortLevel);
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return Mathf.Sqrt(x * x + z * z);
    }

    private static bool GetBoolField(object instance, string name, bool fallback)
    {
        if (instance == null)
            return fallback;

        FieldInfo field = FindField(instance.GetType(), name);
        return field != null && field.FieldType == typeof(bool) ? (bool)field.GetValue(instance) : fallback;
    }

    private static bool TryGetBool(object instance, string name, out bool value)
    {
        value = false;

        if (instance == null)
            return false;

        FieldInfo field = FindField(instance.GetType(), name);

        if (field == null || field.FieldType != typeof(bool))
            return false;

        value = (bool)field.GetValue(instance);
        return true;
    }

    private static MethodInfo FindMethod(System.Type type, string name)
    {
        while (type != null)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method != null)
                return method;

            type = type.BaseType;
        }

        return null;
    }

    private static FieldInfo FindField(System.Type type, string name)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null)
                return field;

            type = type.BaseType;
        }

        return null;
    }
}
