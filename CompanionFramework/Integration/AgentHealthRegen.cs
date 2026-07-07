using System.Reflection;
using UnityEngine;

public class AgentHealthRegen : MonoBehaviour
{
    private const float DefaultRegenPerSecond = 1f;
    private const float DefaultRegenInterval = 2f;
    private const float RecentDamageDelay = 8f;
    private const float CombatDelay = 4f;

    private AgentComponent _agent;
    private ZNetView _zNetView;
    private Character _character;
    private MonsterAI _monsterAI;
    private AgentFood _food;
    private float _tickTimer;
    private float _lastHealth;
    private float _recentDamageTimer;
    private float _combatTimer;

    private static readonly string[] RegenFieldNames =
    {
        "m_healthRegen",
        "m_healthRegenPerSecond",
        "m_regenHealth",
        "m_hpRegen"
    };

    private static readonly string[] RegenIntervalFieldNames =
    {
        "m_healthRegenInterval",
        "m_regenInterval",
        "m_hpRegenInterval"
    };

    private void Awake()
    {
        RefreshReferences();
    }

    private void Start()
    {
        RefreshReferences();

        if (_character != null)
            _lastHealth = _character.GetHealth();
    }

    private void Update()
    {
        RefreshReferences();

        if (_zNetView == null || !_zNetView.IsValid() || !_zNetView.IsOwner())
            return;

        if (_character == null || _character.IsDead())
            return;

        float currentHealth = _character.GetHealth();

        if (_lastHealth <= 0f)
            _lastHealth = currentHealth;

        if (currentHealth < _lastHealth - 0.01f)
            _recentDamageTimer = RecentDamageDelay;

        _lastHealth = currentHealth;

        if (_recentDamageTimer > 0f)
            _recentDamageTimer -= Time.deltaTime;

        if (HasNativeTarget())
            _combatTimer = CombatDelay;
        else if (_combatTimer > 0f)
            _combatTimer -= Time.deltaTime;

        if (_recentDamageTimer > 0f || _combatTimer > 0f)
            return;

        float maxHealth = _character.GetMaxHealth();

        if (maxHealth <= 0f || currentHealth >= maxHealth)
            return;

        _tickTimer -= Time.deltaTime;

        if (_tickTimer > 0f)
            return;

        float interval = GetRegenInterval();
        _tickTimer = interval;
        float regenPerSecond = GetRegenPerSecond();

        if (_food != null && _food.HasAnyFood())
            regenPerSecond += GetFoodPassiveRegenBonus();

        if (regenPerSecond <= 0f)
            return;

        float healAmount = regenPerSecond * interval;
        _character.Heal(healAmount);
        _lastHealth = _character.GetHealth();
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

        if (_food == null)
            _food = GetComponent<AgentFood>();
    }

    private float GetRegenPerSecond()
    {
        if (TryGetFloat(_monsterAI, RegenFieldNames, out float monsterValue) && monsterValue > 0f)
            return monsterValue;

        if (TryGetFloat(_character, RegenFieldNames, out float characterValue) && characterValue > 0f)
            return characterValue;

        return DefaultRegenPerSecond;
    }

    private float GetRegenInterval()
    {
        if (TryGetFloat(_monsterAI, RegenIntervalFieldNames, out float monsterValue) && monsterValue > 0f)
            return Mathf.Clamp(monsterValue, 0.25f, 30f);

        if (TryGetFloat(_character, RegenIntervalFieldNames, out float characterValue) && characterValue > 0f)
            return Mathf.Clamp(characterValue, 0.25f, 30f);

        return DefaultRegenInterval;
    }

    private float GetFoodPassiveRegenBonus()
    {
        if (_food == null)
            return 0f;

        float total = 0f;

        for (int i = 0; i < AgentFood.MaxFoodSlots; i++)
        {
            AgentFood.FoodEffect food = _food.GetFood(i);

            if (food.IsActive)
                total += food.FoodRegen * 0.05f;
        }

        return total;
    }

    private bool HasNativeTarget()
    {
        if (_monsterAI == null)
            return false;

        object target = GetFieldValue(_monsterAI, "m_targetCreature");

        if (target is Character character && character != null && !character.IsDead())
            return true;

        object staticTarget = GetFieldValue(_monsterAI, "m_targetStatic");
        return staticTarget != null;
    }

    private static bool TryGetFloat(object instance, string[] names, out float value)
    {
        value = 0f;

        if (instance == null || names == null)
            return false;

        for (int i = 0; i < names.Length; i++)
        {
            FieldInfo field = FindField(instance.GetType(), names[i]);

            if (field == null || field.FieldType != typeof(float))
                continue;

            value = (float)field.GetValue(instance);
            return true;
        }

        return false;
    }

    private static object GetFieldValue(object instance, string name)
    {
        if (instance == null)
            return null;

        FieldInfo field = FindField(instance.GetType(), name);
        return field != null ? field.GetValue(instance) : null;
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
