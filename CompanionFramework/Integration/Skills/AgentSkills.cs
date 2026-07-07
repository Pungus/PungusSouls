using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public sealed class AgentSkillState
{
    public string Id;
    public float Level;
    public float Experience;

    public AgentSkillState(string id)
    {
        Id = AgentSkillRegistry.NormalizeId(id);
        Level = 0f;
        Experience = 0f;
    }
}

public class AgentSkills : MonoBehaviour
{
    private const string ZDO_Skills = "agent_skills_v1";
    private const float SaveInterval = 5f;
    private const float MinLevel = 0f;
    private const float DefaultMaxLevel = 100f;

    private readonly Dictionary<string, AgentSkillState> _skills = new Dictionary<string, AgentSkillState>();
    private ZNetView _zNetView;
    private float _saveTimer;
    private bool _loaded;
    private bool _dirty;

    private void Awake()
    {
        _zNetView = GetComponent<ZNetView>();
    }

    private void Start()
    {
        TryLoad();
    }

    private void Update()
    {
        if (!_loaded)
            TryLoad();

        if (!_dirty)
            return;

        if (_zNetView == null || !_zNetView.IsValid() || !_zNetView.IsOwner())
            return;

        _saveTimer += Time.deltaTime;

        if (_saveTimer >= SaveInterval)
            Save();
    }

    private void OnDestroy()
    {
        Save();
    }

    public float GetLevel(string id)
    {
        TryLoad();
        id = AgentSkillRegistry.NormalizeId(id);
        return _skills.TryGetValue(id, out AgentSkillState state) ? state.Level : 0f;
    }

    public float GetExperience(string id)
    {
        TryLoad();
        id = AgentSkillRegistry.NormalizeId(id);
        return _skills.TryGetValue(id, out AgentSkillState state) ? state.Experience : 0f;
    }

    public bool TryGetSkill(string id, out float level, out float experience)
    {
        TryLoad();
        id = AgentSkillRegistry.NormalizeId(id);
        level = 0f;
        experience = 0f;

        if (!_skills.TryGetValue(id, out AgentSkillState state))
            return false;

        level = state.Level;
        experience = state.Experience;
        return true;
    }

    public IReadOnlyDictionary<string, AgentSkillState> GetAllSkills()
    {
        TryLoad();
        return _skills;
    }

    public float GetSkillFactor(string id)
    {
        AgentSkillDefinition definition = AgentSkillRegistry.GetOrCreate(id);
        float maxLevel = definition != null ? Mathf.Max(1f, definition.MaxLevel) : DefaultMaxLevel;
        return Mathf.Clamp01(GetLevel(id) / maxLevel);
    }

    public float ApplySkillFactor(string id, float baseValue, float maxBonusMultiplier)
    {
        return baseValue * (1f + Mathf.Max(0f, maxBonusMultiplier) * GetSkillFactor(id));
    }

    public bool RaiseSkill(string id, float amount)
    {
        TryLoad();
        id = AgentSkillRegistry.NormalizeId(id);

        if (string.IsNullOrEmpty(id) || float.IsNaN(amount) || amount <= 0f)
            return false;

        AgentSkillDefinition definition = AgentSkillRegistry.GetOrCreate(id);

        if (definition == null)
            return false;

        AgentSkillState state = GetOrCreateState(id);

        if (state == null)
            return false;

        float maxLevel = Mathf.Max(1f, definition.MaxLevel);

        if (state.Level >= maxLevel)
            return false;

        state.Experience += amount * Mathf.Max(0f, definition.GainMultiplier);
        state.Level = Mathf.Clamp(CalculateLevel(state.Experience), MinLevel, maxLevel);
        _dirty = true;
        return true;
    }

    public void SetSkill(string id, float level, float experience = -1f)
    {
        TryLoad();
        id = AgentSkillRegistry.NormalizeId(id);

        if (string.IsNullOrEmpty(id))
            return;

        AgentSkillDefinition definition = AgentSkillRegistry.GetOrCreate(id);
        AgentSkillState state = GetOrCreateState(id);

        if (definition == null || state == null)
            return;

        state.Level = Mathf.Clamp(level, MinLevel, Mathf.Max(1f, definition.MaxLevel));
        state.Experience = experience >= 0f ? Mathf.Max(0f, experience) : CalculateExperienceForLevel(state.Level);
        _dirty = true;
        Save();
    }

    public void SaveNow()
    {
        Save();
    }

    public string SerializeForDebug()
    {
        TryLoad();
        StringBuilder builder = new StringBuilder();

        foreach (KeyValuePair<string, AgentSkillState> entry in _skills)
        {
            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append(entry.Key).Append("=").Append(entry.Value.Level.ToString("0.0"));
        }

        return builder.ToString();
    }

    private AgentSkillState GetOrCreateState(string id)
    {
        id = AgentSkillRegistry.NormalizeId(id);

        if (string.IsNullOrEmpty(id))
            return null;

        if (_skills.TryGetValue(id, out AgentSkillState state))
            return state;

        state = new AgentSkillState(id);
        _skills[id] = state;
        return state;
    }

    private static float CalculateLevel(float experience)
    {
        experience = Mathf.Max(0f, experience);
        return Mathf.Sqrt(experience / 4f);
    }

    private static float CalculateExperienceForLevel(float level)
    {
        level = Mathf.Max(0f, level);
        return level * level * 4f;
    }

    private void TryLoad()
    {
        if (_loaded)
            return;

        if (_zNetView == null)
            _zNetView = GetComponent<ZNetView>();

        if (_zNetView == null || !_zNetView.IsValid())
            return;

        Load();
        _loaded = true;
    }

    private void Save()
    {
        if (!_dirty)
            return;

        if (_zNetView == null || !_zNetView.IsValid() || !_zNetView.IsOwner())
            return;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null)
            return;

        zdo.Set(ZDO_Skills, Serialize());
        _dirty = false;
        _saveTimer = 0f;
    }

    private void Load()
    {
        if (_zNetView == null || !_zNetView.IsValid())
            return;

        ZDO zdo = _zNetView.GetZDO();

        if (zdo == null)
            return;

        Deserialize(zdo.GetString(ZDO_Skills, string.Empty));
    }

    private string Serialize()
    {
        StringBuilder builder = new StringBuilder();

        foreach (KeyValuePair<string, AgentSkillState> entry in _skills)
        {
            AgentSkillState state = entry.Value;

            if (state == null || string.IsNullOrEmpty(state.Id))
                continue;

            if (builder.Length > 0)
                builder.Append(";");

            builder.Append(state.Id)
                .Append("|")
                .Append(state.Level.ToString("F3", CultureInfo.InvariantCulture))
                .Append("|")
                .Append(state.Experience.ToString("F3", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private void Deserialize(string data)
    {
        _skills.Clear();

        if (string.IsNullOrEmpty(data))
            return;

        string[] entries = data.Split(';');

        for (int i = 0; i < entries.Length; i++)
        {
            string[] parts = entries[i].Split('|');

            if (parts.Length < 3)
                continue;

            string id = AgentSkillRegistry.NormalizeId(parts[0]);

            if (string.IsNullOrEmpty(id))
                continue;

            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float level))
                continue;

            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float xp))
                continue;

            AgentSkillDefinition definition = AgentSkillRegistry.GetOrCreate(id);
            AgentSkillState state = GetOrCreateState(id);
            state.Level = Mathf.Clamp(level, MinLevel, definition != null ? definition.MaxLevel : DefaultMaxLevel);
            state.Experience = Mathf.Max(0f, xp);
        }

        _dirty = false;
        _saveTimer = 0f;
    }
}
