using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CreatureManager;
using CustomBossEvent;
using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using LocalizationManager;
using LocationManager;
using PieceManager;
using ServerSync;
using StatusEffectManager;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using static ItemDrop;
using Range = LocationManager.Range;

namespace PungusSouls
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]


    public class PungusSoulsPlugin : BaseUnityPlugin
    {
        internal const string ModName = "PungusSouls";
        internal const string ModVersion = "0.0.7";
        internal const string Author = "Pungus";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        public static AssetBundle assetBundle;
        public static AssetBundle asset = ItemManager.PrefabManager.RegisterAssetBundle("souls");
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static GameObject BonfirePrefab;
        public static AudioClip LocationClip;
        private static void LogSunlightSwordStats(string label, GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.Log("[PungusSouls] " + label + " SunlightSword prefab=null");
                return;
            }

            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();

            if (itemDrop == null)
            {
                Debug.Log("[PungusSouls] " + label + " SunlightSword ItemDrop=null prefab=" + prefab.name);
                return;
            }

            Debug.Log(
                "[PungusSouls] " +
                label +
                " prefab=" +
                prefab.name +
                " quality=" +
                itemDrop.m_itemData.m_quality +
                " maxQuality=" +
                itemDrop.m_itemData.m_shared.m_maxQuality +
                " slash=" +
                itemDrop.m_itemData.m_shared.m_damages.m_slash +
                " slashPerLevel=" +
                itemDrop.m_itemData.m_shared.m_damagesPerLevel.m_slash);
        }

        public static GameObject GetBonfirePrefab()
        {
            if (BonfirePrefab != null)
                return BonfirePrefab;

            if (assetBundle == null)
            {
                Debug.LogError("[PungusSouls] AssetBundle is null");
                return null;
            }
            
            BonfirePrefab =
                assetBundle.LoadAsset<GameObject>("PS_Bonfire");


            try
            {
                ItemManager.PrefabManager.RegisterPrefab(
                    "souls",
                    "PS_Bonfire");

                Debug.Log("[Bonfire] Registration succeeded");
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[Bonfire] Registration failed: {ex}");
            }

            if (BonfirePrefab == null)
            {
                Debug.LogError("[PungusSouls] Failed to load PS_Bonfire");
            }
            Debug.LogError("[PungusSouls] Loaded Bonfire PS_Bonfire");
            return BonfirePrefab;
        }


        public static readonly ManualLogSource PungusSoulsLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        public Texture2D tex;
        private Sprite mySprite;
        private SpriteRenderer sr;

        public static ManualLogSource log;

        public static Assembly assembly;

        public static string modFolder;

        public static ConfigFile configFile;

        public static ConfigEntry<bool> reorderEnabled;

        public static ConfigEntry<bool> bodyHidingEnabled;

        public static ConfigEntry<bool> loggingEnabled;
        const string resourceName = "PungusSouls.Assets.firelinkshrine";
        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {

            RuntimeHelpers.RunClassConstructor(typeof(Localizer).TypeHandle);
            UpgradeMaps.Register();
            configFile = base.Config;
            reorderEnabled = configFile.Bind("bone reorder", "enabled", defaultValue: true, new ConfigDescription("", null));
            bodyHidingEnabled = configFile.Bind("bodypart hiding", "enabled", defaultValue: true, new ConfigDescription("", null));
            loggingEnabled = configFile.Bind("logging", "enabled", defaultValue: false, new ConfigDescription("", null));
            if (bodyHidingEnabled.Value)
            {
                BodypartSystem.BindConfigs();
            }

            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            LocationClip = asset.LoadAsset<AudioClip>("FirelinkShrine");

            if (LocationClip == null)
            {
                Logger.LogError("Failed to load AudioClip: FirelinkShrine");
                return;
            }

            if (MusicMan.instance != null)
            { 
                MusicManPatch.AddFirelinkShrineMusic(MusicMan.instance); 
            }

            //Debug.Log("[PungusSouls] AFTER CONFIG");
            #region ItemManager Materials

            Item TwinklingTitanite = new("souls", "TwinklingTitanite", "assets");
            TwinklingTitanite.Name.English("Twinkling Titanite"); // You can use this to fix the display name in code
            TwinklingTitanite.Description.English("This weapon-reinforcing titanite is imbued with a particularly powerful energy. After this titanite was peeled from its Slab, it is said that it received a special power, but its specific nature is not clear.");
            TwinklingTitanite.Snapshot();
            Item TitaniteShard = new("souls", "TitaniteShard", "assets");
            TitaniteShard.Name.English("Titanite Shard"); // You can use this to fix the display name in code
            TitaniteShard.Description.English("This weapon-reinforcing titanite is imbued with a particularly powerful energy. After this titanite was peeled from its Slab, it is said that it received a special power, but its specific nature is not clear.");
            TitaniteShard.Snapshot();
            Item TitaniteChunk = new("souls", "TitaniteChunk", "assets");
            TitaniteChunk.Name.English("Titanite Chunk"); // You can use this to fix the display name in code
            TitaniteChunk.Description.English("This weapon-reinforcing titanite is imbued with a particularly powerful energy. After this titanite was peeled from its Slab, it is said that it received a special power, but its specific nature is not clear.");
            TitaniteChunk.Snapshot();
            Item TitaniteSlab = new("souls", "TitaniteSlab", "assets");
            TitaniteSlab.Name.English("Titanite Slab"); // You can use this to fix the display name in code
            TitaniteSlab.Description.English("This weapon-reinforcing titanite is imbued with a particularly powerful energy. After this titanite was peeled from its Slab, it is said that it received a special power, but its specific nature is not clear.");
            TitaniteSlab.Snapshot();
            Item DemonTitanite = new("souls", "DemonTitanite", "assets");
            DemonTitanite.Name.English("Demon Titanite"); // You can use this to fix the display name in code
            DemonTitanite.Description.English("This weapon-reinforcing titanite is imbued with a particularly powerful energy. After this titanite was peeled from its Slab, it is said that it received a special power, but its specific nature is not clear.");
            DemonTitanite.Snapshot();
            Item RedTitanite = new("souls", "RedTitanite", "assets");
            RedTitanite.Name.English("Red Titanite"); // You can use this to fix the display name in code
            RedTitanite.Description.English("This weapon-reinforcing titanite is imbued with a particularly powerful energy. After this titanite was peeled from its Slab, it is said that it received a special power, but its specific nature is not clear.");
            RedTitanite.Snapshot();
            Item BlueTitanite = new("souls", "BlueTitanite", "assets");
            BlueTitanite.Name.English("Blue Titanite"); // You can use this to fix the display name in code
            BlueTitanite.Description.English("This weapon-reinforcing titanite is imbued with a particularly powerful energy. After this titanite was peeled from its Slab, it is said that it received a special power, but its specific nature is not clear.");
            BlueTitanite.Snapshot();
            Item GreenTitanite = new("souls", "GreenTitanite", "assets");
            GreenTitanite.Name.English("Green Titanite"); // You can use this to fix the display name in code
            GreenTitanite.Description.English("This weapon-reinforcing titanite is imbued with a particularly powerful energy. After this titanite was peeled from its Slab, it is said that it received a special power, but its specific nature is not clear.");
            GreenTitanite.Snapshot();
            Item LargeTitaniteShard = new("souls", "LargeTitaniteShard", "assets");
            LargeTitaniteShard.Name.English("Large Titanite Shard"); // You can use this to fix the display name in code
            LargeTitaniteShard.Description.English("This weapon-reinforcing titanite is imbued with a particularly powerful energy. After this titanite was peeled from its Slab, it is said that it received a special power, but its specific nature is not clear.");
            LargeTitaniteShard.Snapshot();
            Item TitaniteScale = new("souls", "TitaniteScale", "assets");   
            TitaniteScale.Name.English("Titanite Scale"); // You can use this to fix the display name in code
            TitaniteScale.Description.English("This weapon-reinforcing titanite is imbued with a particularly powerful energy. After this titanite was peeled from its Slab, it is said that it received a special power, but its specific nature is not clear.");
            TitaniteScale.Snapshot();

            ItemManager.PrefabManager.RegisterPrefab("souls", "pickable_titanite_tier1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "pickable_titanite_tier2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "pickable_titanite_tier3");
            ItemManager.PrefabManager.RegisterPrefab("souls", "pickable_titanite_tier4");
            ItemManager.PrefabManager.RegisterPrefab("souls", "pickable_titanite_tier5");
            ItemManager.PrefabManager.RegisterPrefab("souls", "pickable_titanite_tier6");
            ItemManager.PrefabManager.RegisterPrefab("souls", "pickable_titanite_tier7");

            GameObject BlacksmithForge = ItemManager.PrefabManager.RegisterPrefab("souls", "BlacksmithForge");
            GameObject DullEmber = ItemManager.PrefabManager.RegisterPrefab("souls", "DullEmber_tier1");
            GameObject LargeEmber = ItemManager.PrefabManager.RegisterPrefab("souls", "LargeEmber_tier2");
            GameObject DivineEmber = ItemManager.PrefabManager.RegisterPrefab("souls", "DivineEmber_tier3");
            GameObject DarkEmber = ItemManager.PrefabManager.RegisterPrefab("souls", "DarkEmber_tier4");
            GameObject FlameEmber = ItemManager.PrefabManager.RegisterPrefab("souls", "FlameEmber_tier5");
            GameObject ChaosEmber = ItemManager.PrefabManager.RegisterPrefab("souls", "ChaosEmber_tier6");
            GameObject CrystalEmber = ItemManager.PrefabManager.RegisterPrefab("souls", "CrystalEmber_tier7");
            GameObject GiantEmber = ItemManager.PrefabManager.RegisterPrefab("souls", "GiantsEmber_tier8");
            GameObject RuneTablet_Andre = ItemManager.PrefabManager.RegisterPrefab("souls", "RuneTablet_Andre");
            GameObject RuneStone_Andre1 = ItemManager.PrefabManager.RegisterPrefab("souls", "RuneStone_Andre1");
            GameObject StarterRunestone1 = ItemManager.PrefabManager.RegisterPrefab("souls", "StarterRunestone1");

            Item DullEmber_Item = new("souls", "DullEmber_item_tier1", "assets");
            DullEmber_Item.Name.English("Dull Ember");
            DullEmber_Item.Description.English("A dull ember, used to upgrade weapons at the blacksmith altar.");
            //  DullEmber_Item.Snapshot();
            Item LargeEmber_Item = new("souls", "LargeEmber_item_tier2", "assets");
            LargeEmber_Item.Name.English("Large Ember");
            LargeEmber_Item.Description.English("A large ember, used to upgrade weapons at the blacksmith altar.");
            //  LargeEmber_Item.Snapshot();
            Item DivineEmber_Item = new("souls", "DivineEmber_item_tier3", "assets");
            DivineEmber_Item.Name.English("Divine Ember");
            DivineEmber_Item.Description.English("A divine ember, used to upgrade weapons at the blacksmith altar.");
            //  DivineEmber_Item.Snapshot();
            Item DarkEmber_Item = new("souls", "DarkEmber_Item_tier4", "assets");
            DarkEmber_Item.Name.English("Dark Ember");
            DarkEmber_Item.Description.English("A dark ember, used to upgrade weapons at the blacksmith altar.");
            //  DarkEmber_Item.Snapshot();
            Item FlameEmber_Item = new("souls", "FlameEmber_Item_tier5", "assets");
            FlameEmber_Item.Name.English("Flame Ember");
            FlameEmber_Item.Description.English("A flame ember, used to upgrade weapons at the blacksmith altar.");
            // FlameEmber_Item.Snapshot();
            Item ChaosEmber_Item = new("souls", "ChaosEmber_Item_tier6", "assets");
            ChaosEmber_Item.Name.English("Chaos Ember");
            ChaosEmber_Item.Description.English("A chaos ember, used to upgrade weapons at the blacksmith altar.");
            //  ChaosEmber_Item.Snapshot();
            Item CrystalEmber_Item = new("souls", "CrystalEmber_Item_tier7", "assets");
            CrystalEmber_Item.Name.English("Crystal Ember");
            CrystalEmber_Item.Description.English("A crystal ember, used to upgrade weapons at the blacksmith altar.");
            // CrystalEmber_Item.Snapshot();
            Item GiantEmber_Item = new("souls", "GiantsEmber_Item_tier8", "assets");
            GiantEmber_Item.Name.English("Giant Ember");
            GiantEmber_Item.Description.English("A giant ember, used to upgrade weapons at the blacksmith altar.");

            Debug.Log("[PungusSouls] AFTER CONFIG");
            #region Drops
            TitaniteShard.DropsFrom.Add("Greydwarf", .6f, 1, 1);
            TitaniteShard.DropsFrom.Add("Greydwarf_Shaman", .15f, 1, 1);
            TitaniteShard.DropsFrom.Add("Greydwarf_Elite", .15f, 1, 1);
            TitaniteShard.DropsFrom.Add("Troll", .10f, 1, 1);
            TitaniteShard.DropsFrom.Add("Skeleton_Poison", .15f, 1, 1);
            TitaniteShard.DropsFrom.Add("Ghost", .15f, 1, 1);
            LargeTitaniteShard.DropsFrom.Add("Skeleton", .17f, 1, 1);
            LargeTitaniteShard.DropsFrom.Add("Blob", .10f, 1, 1);
            LargeTitaniteShard.DropsFrom.Add("BlobElite", .10f, 1, 1);
            LargeTitaniteShard.DropsFrom.Add("Surtling", .10f, 1, 1);
            LargeTitaniteShard.DropsFrom.Add("Leech", .10f, 1, 1);
            LargeTitaniteShard.DropsFrom.Add("Draugr", .10f, 1, 1);
            LargeTitaniteShard.DropsFrom.Add("Draugr_Elite", .10f, 1, 1);
            LargeTitaniteShard.DropsFrom.Add("Wraith", .10f, 1, 1);
            LargeTitaniteShard.DropsFrom.Add("Abomination", .10f, 1, 1);
            TitaniteChunk.DropsFrom.Add("Wolf", .12f, 1, 1);
            TitaniteChunk.DropsFrom.Add("Hatchling", .12f, 1, 1);
            TitaniteChunk.DropsFrom.Add("StoneGolem", .12f, 1, 1);
            TitaniteChunk.DropsFrom.Add("Fenring", .12f, 1, 1);
            TitaniteChunk.DropsFrom.Add("Ulv", .12f, 1, 1);
            TitaniteSlab.DropsFrom.Add("Deathsquito", .30f, 1, 1);
            TitaniteSlab.DropsFrom.Add("Goblin", .13f, 1, 1);
            TitaniteSlab.DropsFrom.Add("GoblinBrute", .20f, 1, 1);
            TitaniteSlab.DropsFrom.Add("BlobTar", .13f, 1, 1);
            TitaniteSlab.DropsFrom.Add("GoblinShaman", .20f, 1, 1);
            TitaniteSlab.DropsFrom.Add("Lox", .20f, 1, 1);
            TitaniteSlab.DropsFrom.Add("GoblinArcher", .13f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Seeker", .25f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("SeekerBrute", .25f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Gjall", .25f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Dverger", .25f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("DvergerMage", .25f, 1, 1);
            DemonTitanite.DropsFrom.Add("Charred_Melee", .25f, 1, 1);
            DemonTitanite.DropsFrom.Add("Charred_Archer", .25f, 1, 1);
            DemonTitanite.DropsFrom.Add("Charred_Twitcher", .25f, 1, 1);
            DemonTitanite.DropsFrom.Add("Volture", .13f, 1, 1);
            DemonTitanite.DropsFrom.Add("Asksvin", .25f, 1, 1);
            DemonTitanite.DropsFrom.Add("Morgen", .75f, 1, 1);
            DemonTitanite.DropsFrom.Add("Fader", 1f, 1, 1);

            DullEmber_Item.DropsFrom.Add("GdKing", 1f, 1, 1);
            LargeEmber_Item.DropsFrom.Add("Skeleton_Hildir", 1f, 1, 1);
            DivineEmber_Item.DropsFrom.Add("BoneMass", 1f, 1, 1);
            DarkEmber_Item.DropsFrom.Add("Fenring_Cultist_Hildir", 1f, 1, 1);
            FlameEmber_Item.DropsFrom.Add("Dragon", 1f, 1, 1);
            ChaosEmber_Item.DropsFrom.Add("GoblinBruteBros", 1f, 1, 1);
            CrystalEmber_Item.DropsFrom.Add("GoblinKing", 1f, 1, 1);
            GiantEmber_Item.DropsFrom.Add("Fader", 1f, 1, 1);

            Debug.Log("[PungusSouls] AFTER ITEM DROP");
            #endregion

            #endregion
            #region Boss Events

            BossEventDefinition manusBossEvent = asset.LoadAsset<BossEventDefinition>("ManusBossEvent");
            BossEventDefinition seathBossEvent = asset.LoadAsset<BossEventDefinition>("SeathBossEvent");
            BossEventDefinition kalameetBossEvent = asset.LoadAsset<BossEventDefinition>("KalameetBossEvent");
            BossEventManager.Register(manusBossEvent);

            #endregion Boss Events

            #region ResourceManager
            GameObject titanite_giant_sword_Tier1 = ItemManager.PrefabManager.RegisterPrefab("souls","titanite_giant_sword_Tier1");
                ResourceSpawnManager.Register(
                    new ResourceSpawnDefinition
                    {
                        Prefab = titanite_giant_sword_Tier1,

                        Biome = Heightmap.Biome.BlackForest,
                        BiomeArea = Heightmap.BiomeArea.Median,

                        MinPerZone = 0f,
                        MaxPerZone = 0.15f,
                        OverrideGroupDensity = true,
                        OverrideGroupPlacement = true,
                        MinTilt = 0f,
                        MaxTilt = 30f,

                        InForest = true,
                        ForestThresholdMin = 0f,
                        MinDistanceFromSame = 2000f,

                        GroupSizeMin = 1,
                        GroupSizeMax = 1,
                        GroupRadius = 0f
                    });
                GameObject titanite_giant_sword_Tier2 = ItemManager.PrefabManager.RegisterPrefab("souls","titanite_giant_sword_Tier2");
                ResourceSpawnManager.Register(
                    new ResourceSpawnDefinition
                    {
                        Prefab = titanite_giant_sword_Tier2,

                        Biome = Heightmap.Biome.Swamp,
                        BiomeArea = Heightmap.BiomeArea.Median,
                        MinAltitude = 10f,

                        MinPerZone = 0f,
                        MaxPerZone = 0.15f,
                        OverrideGroupDensity = true,
                        OverrideGroupPlacement = true,
                        MinTilt = 0f,
                        MaxTilt = 30f,

                        InForest = true,
                        ForestThresholdMin = 0f,
                        MinDistanceFromSame = 2000f,

                        GroupSizeMin = 1,
                        GroupSizeMax = 1,
                        GroupRadius = 0f
                    });
                GameObject titanite_giant_helmet_tier1 = ItemManager.PrefabManager.RegisterPrefab("souls","titanite_giant_helmet_tier1");
                ResourceSpawnManager.Register(
                    new ResourceSpawnDefinition
                    {
                        Prefab = titanite_giant_helmet_tier1,

                        Biome = Heightmap.Biome.BlackForest,
                        BiomeArea = Heightmap.BiomeArea.Median,
                        OverrideGroupDensity = true,
                        OverrideGroupPlacement = true,
                        MinPerZone = 0f,
                        MaxPerZone = 0.15f,
                        GroundOffset = -2f,

                        MinTilt = 0,
                        MaxTilt = 30,

                        InForest = true,
                        ForestThresholdMin = 0f,
                        MinDistanceFromSame = 2000f,

                        GroupSizeMin = 1,
                        GroupSizeMax = 1,
                        GroupRadius = 0f
                    });
                GameObject titanite_giant_helmet_tier2 = ItemManager.PrefabManager.RegisterPrefab("souls","titanite_giant_helmet_tier2");
                ResourceSpawnManager.Register(
                    new ResourceSpawnDefinition
                    {
                        Prefab = titanite_giant_helmet_tier2,

                        Biome = Heightmap.Biome.Swamp,
                        BiomeArea = Heightmap.BiomeArea.Median,

                        MinPerZone = 0f,
                        MaxPerZone = 1f,

                        MinTilt = 0,
                        MaxTilt = 30,
                        GroundOffset = -2f,
                        OverrideGroupDensity = true,
                        OverrideGroupPlacement = true,
                        InForest = true,
                        ForestThresholdMin = 0f,
                        MinDistanceFromSame = 2000f,

                        GroupSizeMin = 1,
                        GroupSizeMax = 1,
                        GroupRadius = 0f
                    });
                GameObject titanite_giant_skull_tier3 = ItemManager.PrefabManager.RegisterPrefab("souls","titanite_giant_skull_tier3");
                ResourceSpawnManager.Register(
                    new ResourceSpawnDefinition
                    {
                        Prefab = titanite_giant_skull_tier3,
                        Biome = Heightmap.Biome.Mountain,
                        BiomeArea = Heightmap.BiomeArea.Median,

                        MinPerZone = 0f,
                        MaxPerZone = 1f,

                        MinTilt = 20,
                        MaxTilt = 90,
                        GroundOffset = -6f,
                        OverrideGroupDensity = true,
                        OverrideGroupPlacement = true,
                        MinDistanceFromSame = 1000f,

                        GroupSizeMin = 1,
                        GroupSizeMax = 1,
                        GroupRadius = 0f
                    });
                GameObject Titanite_Giant_Ribs_tier4 = ItemManager.PrefabManager.RegisterPrefab("souls","Titanite_Giant_Ribs_tier5");
                ResourceSpawnManager.Register(
                    new ResourceSpawnDefinition
                    {
                        Prefab = Titanite_Giant_Ribs_tier4,

                        Biome = Heightmap.Biome.Plains,
                        BiomeArea = Heightmap.BiomeArea.Median,

                        MinPerZone = 0f,
                        MaxPerZone = 0.15f,

                        MinTilt = 20,
                        MaxTilt = 90,
                        GroundOffset = -6f,
                        OverrideGroupDensity = true,
                        OverrideGroupPlacement = true,
                        MinDistanceFromSame = 2000f,

                        GroupSizeMin = 1,
                        GroupSizeMax = 1,
                        GroupRadius = 0f
                    });
                GameObject titanite_vein_1 = ItemManager.PrefabManager.RegisterPrefab("souls","titanite_vein_1");
                ResourceSpawnManager.Register(
                    new ResourceSpawnDefinition
                    {
                        Prefab = titanite_vein_1,

                        Biome = Heightmap.Biome.AshLands,
                        BiomeArea = Heightmap.BiomeArea.Median,

                        MinPerZone = 0f,
                        MaxPerZone = 0.15f,

                        MinTilt = 0,
                        MaxTilt = 60,
                        GroundOffset = -6f,
                        OverrideGroupDensity = true,
                        OverrideGroupPlacement = true,
                        InForest = true,
                        ForestThresholdMin = 0.5f,
                        MinDistanceFromSame = 1000f,
                        
                        GroupSizeMin = 1,
                        GroupSizeMax = 1,
                        GroupRadius = 0f,
                    });
            Debug.Log("After ResourceManager");
            #endregion ResourceManager

            #region PieceManager Example Code

            // Globally turn off configuration options for your pieces, omit if you don't want to do this.
            BuildPiece.ConfigurationEnabled = false;

            PiecePrefabManager.RegisterPrefab("souls", "BlacksmithAltar");
            PiecePrefabManager.RegisterPrefab("souls", "SweetShalquoir_Piece");

            BuildPiece Lanterny = new("souls", "lantern");

            Lanterny.Name.English("Arcane Lantern"); // Localize the name and description for the building piece for a language.
            Lanterny.Description.English("Blacksmith Altar Extension");
            Lanterny.RequiredItems.Add("Resin", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            Lanterny.RequiredItems.Add("Iron", 10, true);
            Lanterny.RequiredItems.Add("ElderBark", 10, true);
            Lanterny.RequiredItems.Add("TwinklingTitanite", 15, true);
            Lanterny.Category.Set(PieceManager.BuildPieceCategory.Crafting);
            Lanterny.Crafting.Set("BlacksmithAltar"); // Set a crafting station requirement for the piece.
            Lanterny.Snapshot();

            BuildPiece Lantern1 = new("souls", "Lantern1");

            Lantern1.Name.English("Hunters Lantern"); // Localize the name and description for the building piece for a language.
            Lantern1.Description.English("Blacksmith Altar Extension");
            Lantern1.RequiredItems.Add("FineWood", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            Lantern1.RequiredItems.Add("Bronze", 10, true);
            Lantern1.RequiredItems.Add("Amber", 10, true);
            Lantern1.RequiredItems.Add("TwinklingTitanite", 10, true);
            Lantern1.Category.Set(PieceManager.BuildPieceCategory.Crafting);
            Lantern1.Crafting.Set("BlacksmithAltar"); // Set a crafting station requirement for the piece.
            Lantern1.Snapshot();

            BuildPiece ArcaneStone = new("souls", "ArcaneStone");

            ArcaneStone.Name.English("Arcane Stone"); // Localize the name and description for the building piece for a language.
            ArcaneStone.Description.English("Blacksmith Altar Extension");
            ArcaneStone.RequiredItems.Add("Stone", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            ArcaneStone.RequiredItems.Add("Flint", 10, true);
            ArcaneStone.RequiredItems.Add("GreydwarfEye", 10, true);
            ArcaneStone.RequiredItems.Add("TwinklingTitanite", 5, true);
            ArcaneStone.Category.Set(PieceManager.BuildPieceCategory.Crafting);
            ArcaneStone.Crafting.Set("BlacksmithAltar"); // Set a crafting station requirement for the piece.
            ArcaneStone.Snapshot();

            BuildPiece NamelessStatue = new("souls", "NamelessStatue");

            NamelessStatue.Name.English("Nameless Statue"); // Localize the name and description for the building piece for a language.
            NamelessStatue.Description.English("Blacksmith Altar Extension");
            NamelessStatue.RequiredItems.Add("Stone", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            NamelessStatue.RequiredItems.Add("Crystal", 10, true);
            NamelessStatue.RequiredItems.Add("BlackMetal", 10, true);
            NamelessStatue.RequiredItems.Add("TwinklingTitanite", 30, true);
            NamelessStatue.Category.Set(PieceManager.BuildPieceCategory.Crafting);
            NamelessStatue.Crafting.Set("BlacksmithAltar"); // Set a crafting station requirement for the piece.
            NamelessStatue.Snapshot();

            BuildPiece Bonefire = new("souls", "Bonefire");

            Bonefire.Name.English("Bonefire"); // Localize the name and description for the building piece for a language.
            Bonefire.Description.English("Blacksmith Altar Extension");
            Bonefire.RequiredItems.Add("BoneFragments", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            Bonefire.RequiredItems.Add("Crystal", 10, true);
            Bonefire.RequiredItems.Add("Silver", 10, true);
            Bonefire.RequiredItems.Add("TwinklingTitanite", 20, true);
            Bonefire.Category.Set(PieceManager.BuildPieceCategory.Crafting);
            Bonefire.Crafting.Set("BlacksmithAltar"); // Set a crafting station requirement for the piece.
            Bonefire.Snapshot();

            BuildPiece piece_chest_mimic = new("souls", "piece_chest_mimic");

            piece_chest_mimic.Name.English("Mimic Chest"); // Localize the name and description for the building piece for a language.
            piece_chest_mimic.Description.English("Blacksmith Altar Extension");
            piece_chest_mimic.RequiredItems.Add("BoneFragments", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            piece_chest_mimic.RequiredItems.Add("Crystal", 10, true);
            piece_chest_mimic.RequiredItems.Add("Silver", 10, true);
            piece_chest_mimic.RequiredItems.Add("TwinklingTitanite", 20, true);
            piece_chest_mimic.Category.Set(PieceManager.BuildPieceCategory.Furniture);
            piece_chest_mimic.Snapshot();

            BuildPiece lordvessel = new("souls", "lordvessel");

            lordvessel.Name.English("lordvessel"); // Localize the name and description for the building piece for a language.
            lordvessel.Description.English("Blacksmith Altar Extension");
            lordvessel.RequiredItems.Add("BoneFragments", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            lordvessel.RequiredItems.Add("Crystal", 10, true);
            lordvessel.RequiredItems.Add("Silver", 10, true);
            lordvessel.RequiredItems.Add("TwinklingTitanite", 20, true);
            lordvessel.Category.Set(PieceManager.BuildPieceCategory.Furniture);
            lordvessel.Snapshot();

            BuildPiece GwynevereStatue = new("souls", "GwynevereStatue");

            GwynevereStatue.Name.English("GwynevereStatue"); // Localize the name and description for the building piece for a language.
            GwynevereStatue.Description.English("Blacksmith Altar Extension");
            GwynevereStatue.RequiredItems.Add("BoneFragments", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            GwynevereStatue.RequiredItems.Add("Crystal", 10, true);
            GwynevereStatue.RequiredItems.Add("Silver", 10, true);
            GwynevereStatue.RequiredItems.Add("TwinklingTitanite", 20, true);
            GwynevereStatue.Category.Set(PieceManager.BuildPieceCategory.Furniture);
            GwynevereStatue.Snapshot();

            BuildPiece Bed01 = new("souls", "Bed01");

            Bed01.Name.English("Bed01"); // Localize the name and description for the building piece for a language.
            Bed01.Description.English("Blacksmith Altar Extension");
            Bed01.RequiredItems.Add("BoneFragments", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            Bed01.RequiredItems.Add("Crystal", 10, true);
            Bed01.RequiredItems.Add("Silver", 10, true);
            Bed01.RequiredItems.Add("TwinklingTitanite", 20, true);
            Bed01.Category.Set(PieceManager.BuildPieceCategory.Furniture);
            Bed01.Snapshot();

            BuildPiece Painting = new("souls", "Painting");

            Painting.Name.English("Painting"); // Localize the name and description for the building piece for a language.
            Painting.Description.English("Blacksmith Altar Extension");
            Painting.RequiredItems.Add("BoneFragments", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            Painting.RequiredItems.Add("Crystal", 10, true);
            Painting.RequiredItems.Add("Silver", 10, true);
            Painting.RequiredItems.Add("TwinklingTitanite", 20, true);
            Painting.Category.Set(PieceManager.BuildPieceCategory.Furniture);
            Painting.Snapshot();
            Debug.Log("After PieceManager");
                #endregion
                #region SkillManager Example Code

                #endregion
            #region Location Manager
             LocationManager.Location FirelinkShrine = new("souls", "FireLinkShrine")
            {
                MapIcon = "firelinkicon.png",
                Rotation = Rotation.Fixed,
                ShowMapIcon = ShowIcon.Explored,
                Biome = Heightmap.Biome.Meadows,
                SpawnArea = Heightmap.BiomeArea.Median,
                HeightDelta = new Range(0, 16),
                SpawnDistance = new Range(1050, 2050),
                SpawnAltitude = new Range(10, 220),
                GroupName = "AvoidAnyLocationGroup",
                MinimumDistanceFromGroup = 400f,
                Count = 1,
                Prioritize = true,
                Unique = true

            };

            LocationManager.Location SleepingDragonLoc = new("souls", "SleepingDragonLoc")
            {
                Rotation = Rotation.Random,
                Biome = Heightmap.Biome.DeepNorth,
                SpawnArea = Heightmap.BiomeArea.Median,
                HeightDelta = new Range(0, 120),
                SpawnDistance = new Range(2550, 99999),
                GroupName = "AvoidAnyLocationGroup",
                SpawnAltitude = new Range(15, 999),
                Count = 55,
                Prioritize = true,
            };

            LocationManager.Location Sif_Loc = new("souls", "Sif_Loc")
            {
                Rotation = Rotation.Fixed,

                ShowMapIcon = ShowIcon.Explored,
                Biome = Heightmap.Biome.Mistlands,
                SpawnArea = Heightmap.BiomeArea.Median,
                HeightDelta = new Range(0, 99),
                SpawnDistance = new Range(3000, 7500),
                SpawnAltitude = new Range(0, 99),
                GroupName = "AvoidAnyLocationGroup",
                MinimumDistanceFromGroup = 500f,
                Count = 1,
                Prioritize = true,
                Unique = true
            };

            LocationManager.Location KalameetArena = new("souls", "KalameetArena")
            {
                Rotation = Rotation.Fixed,

                ShowMapIcon = ShowIcon.Explored,
                Biome = Heightmap.Biome.Plains,
                SpawnArea = Heightmap.BiomeArea.Median,
                HeightDelta = new Range(0, 22),
                SpawnDistance = new Range(4500, 7500),
                SpawnAltitude = new Range(0, 33),
                GroupName = "AvoidAnyLocationGroup",
                MinimumDistanceFromGroup = 500f,
                Count = 1,
                Prioritize = true,
                Unique = true
            };

            LocationManager.Location Dukes = new("souls", "Dukes")
            {
                Rotation = Rotation.Fixed,

                ShowMapIcon = ShowIcon.Explored,
                Biome = Heightmap.Biome.DeepNorth,
                SpawnArea = Heightmap.BiomeArea.Median,
                HeightDelta = new Range(0, 22),
                SpawnDistance = new Range(4500, 99999),
                SpawnAltitude = new Range(0, 33),
                GroupName = "AvoidAnyLocationGroup",
                MinimumDistanceFromGroup = 500f,
                Count = 1,
                Prioritize = true,
                Unique = true
            };

            LocationManager.Location Altar_of_Sunlight = new("souls", "Altar_of_Sunlight")
            {
                Rotation = Rotation.Random,

                ShowMapIcon = ShowIcon.Explored,
                Biome = Heightmap.Biome.BlackForest,
                SpawnArea = Heightmap.BiomeArea.Median,
                HeightDelta = new Range(0, 33),
                SpawnDistance = new Range(1500, 3500),
                SpawnAltitude = new Range(0, 33),
                GroupName = "AvoidAnyLocationGroup",
                MinimumDistanceFromGroup = 1000f,
                Count = 1,
                Prioritize = true,
                Unique = true
            };

            ItemManager.PrefabManager.RegisterPrefab("souls", "PS_Bonfire_Loc");

            
            LocationManager.Location ForestTower = new("souls", "ForestTower")
            {
                ShowMapIcon = ShowIcon.Never,
                Biome = Heightmap.Biome.BlackForest,
                SpawnArea = Heightmap.BiomeArea.Everything,
                SpawnAltitude = new Range(0, 99999),
                SpawnDistance = new Range(150, 5000),
                GroupName = "AvoidAnyLocationGroup",
                MinimumDistanceFromGroup = 1000f,
                Count = 15,
                Prioritize = true,
            };

              LocationManager.Location AnorLondo = new("souls", "anorlondo")
            {
                ShowMapIcon = ShowIcon.Never,
                Biome = Heightmap.Biome.DeepNorth,
                SpawnArea = Heightmap.BiomeArea.Everything,
                SpawnAltitude = new Range(0, 99999),
                SpawnDistance = new Range(1500, 9999),
                Count = 1,
                Prioritize = true,
                Unique = true,
            };

              LocationManager.Location undeadasylum1 = new("souls", "undeadasylum1")
            {
                ShowMapIcon = ShowIcon.Never,
                Rotation = Rotation.Fixed,
                Biome = Heightmap.Biome.Mountain,
                SpawnArea = Heightmap.BiomeArea.Everything,
                SpawnAltitude = new Range(0, 99999),
                SpawnDistance = new Range(1700, 5755),
                Count = 1,
                Prioritize = true,
                Unique = true,
            };

            LocationManager.Location BoarCave = new("souls", "BoarCave")
            {
                ShowMapIcon = ShowIcon.Never,
                Biome = Heightmap.Biome.Swamp,
                SpawnArea = Heightmap.BiomeArea.Median,
                SpawnAltitude = new Range(0, 100),
                SpawnDistance = new Range(2500, 5000),
                GroupName = "AvoidAnyLocationGroup",
                MinimumDistanceFromGroup = 400f,
                Count = 1,
                Prioritize = true,
                Unique = true,
            };
            LocationManager.Location ForestTower2 = new("souls", "ForestTower2")
            {
                ShowMapIcon = ShowIcon.Never,
                Biome = Heightmap.Biome.BlackForest,
                SpawnArea = Heightmap.BiomeArea.Median,
                SpawnAltitude = new Range(0, 50),
                SpawnDistance = new Range(2500, 3500),
                GroupName = "AvoidAnyLocationGroup",
                MinimumDistanceFromGroup = 500f,
                Count = 1,
                Prioritize = true,
                Unique = true,
            };

            LocationManager.Location DrakeCave = new("souls", "DrakeCave")
            {
                ShowMapIcon = ShowIcon.Never,
                Biome = Heightmap.Biome.AshLands,
                SpawnArea = Heightmap.BiomeArea.Median,
                SpawnAltitude = new Range(0, 100),
                SpawnDistance = new Range(2500, 99999),
                GroupName = "AvoidAnyLocationGroup",
                MinimumDistanceFromGroup = 500f,
                Count = 1,
                Prioritize = true,
                Unique = true,
            };
            LocationManager.Location Titanite_Giant = new("souls", "Titanite_Giant")
            {
                ShowMapIcon = ShowIcon.Never,
                Biome = Heightmap.Biome.Plains,
                SpawnArea = Heightmap.BiomeArea.Median,
                SpawnAltitude = new Range(0, 100),
                SpawnDistance = new Range(2500, 7500),
                GroupName = "AvoidAnyLocationGroup",
                MinimumDistanceFromGroup = 500f,
                Count =15,
                Prioritize = true,
            };


            Debug.Log("[PungusSouls] AFTER LOC");
            #endregion Location Manager

            #region StatusEffectManager

            CustomSE Lifesteal = new("souls", "Lifesteal");
            Lifesteal.Name.English("Lifesteal");
            Lifesteal.Type = EffectType.Attack;
            CustomSE SE_GrassShield = new("souls", "SE_GrassShield");
            SE_GrassShield.Name.English("Stamina Regen");
            CustomSE TridentBuff = new("souls", "TridentBuff");
            TridentBuff.Name.English("TridentBuff");
            CustomSE SetEffect_ArtoriasSet = new("souls", "SetEffect_ArtoriasSet");
            SetEffect_ArtoriasSet.Name.English("Artorias Set");
            SetEffect_ArtoriasSet.Effect.m_tooltip = "<color=orange>The Agility of Artorias</color>";
            CustomSE SetEffect_HavelSet = new("souls", "SetEffect_HavelSet");
            SetEffect_HavelSet.Name.English("Havels Set");
            SetEffect_HavelSet.Effect.m_tooltip = "<color=orange>The Strength of Havel the Rock</color>";
            CustomSE lightningbuff = new("souls", "SE_Lightningbuff");
            lightningbuff.Name.English("Sunlight Blade");
            CustomSE se_sunbro = new("souls", "se_sunbro");
            se_sunbro.Name.English("Warrior of Sunlight");
            CustomSE se_wolf = new("souls", "se_wolf");
            se_wolf.Name.English("Wolf Kinship");
            CustomSE se_cat = new("souls", "se_cat");
            se_cat.Name.English("Cat");
            CustomSE se_onion = new("souls", "se_onion");
            se_onion.Name.English("Warrior of Sunlight");
            CustomSE se_mushroom = new("souls", "se_mushroom");


            Debug.Log("[PungusSouls] AFTER SE");
            #endregion StatusEffectManager

            #region ItemManager
            #region Armor
            #region Tier 1 (Black forest)
            //Medium Armor
            Item SunChest = new("souls", "SunChest");
            SunChest.Name.English("Armor of the Sun");
            SunChest.Description.English("Armor of Solaire of Astora, Knight of Sunlight. The large holy symbol of the Sun while powerless, was painted by Solaire himself");
            SunChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            SunChest.RequiredItems.Add("Iron", 25);
            SunChest.RequiredItems.Add("Chain", 2);
            SunChest.RequiredItems.Add("TwinklingTitanite", 10);
            SunChest.RequiredItems.Add("MushroomYellow", 10);
            SunChest.RequiredUpgradeItems.Add("Iron", 5);
            SunChest.RequiredUpgradeItems.Add("Chain", 1);
            SunChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            SunChest.RequiredUpgradeItems.Add("MushroomYellow", 2);
            BodypartSystem.RegisterHiddenBodyParts("SunChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight);

            Item SunLegs = new("souls", "SunLegs");
            SunLegs.Name.English("Leggings of the Sun");
            SunLegs.Description.English("Leggings of Solaire of Astora, Knight of Sunlight. Of high quality, but lacking any particular powers");
            SunLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            SunLegs.RequiredItems.Add("Iron", 25);
            SunLegs.RequiredItems.Add("Chain", 2);
            SunLegs.RequiredItems.Add("TwinklingTitanite", 10);
            SunLegs.RequiredItems.Add("MushroomYellow", 10);
            SunLegs.RequiredUpgradeItems.Add("Iron", 5);
            SunLegs.RequiredUpgradeItems.Add("Chain", 1);
            SunLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            SunLegs.RequiredUpgradeItems.Add("MushroomYellow", 2);
            BodypartSystem.RegisterHiddenBodyParts("SunLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item SunHelm = new("souls", "SunHelm");
            SunHelm.Name.English("Helmet of the Sun");
            SunHelm.Description.English("Helm of Solaire of Astora, Knight of Sunlight. Of high quality, but lacking any particular powers");
            SunHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            SunHelm.RequiredItems.Add("Iron", 25);
            SunHelm.RequiredItems.Add("Chain", 2);
            SunHelm.RequiredItems.Add("TwinklingTitanite", 10);
            SunHelm.RequiredItems.Add("MushroomYellow", 10);
            SunHelm.RequiredUpgradeItems.Add("Iron", 5);
            SunHelm.RequiredUpgradeItems.Add("Chain", 1);
            SunHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            SunHelm.RequiredUpgradeItems.Add("MushroomYellow", 2);
            BodypartSystem.RegisterHiddenBodyParts("SunHelm",
            BodypartSystem.bodyPart.Head);
            
            //Mage Armor
            Item BlackChest = new("souls", "BlackChest");
            BlackChest.Name.English("Black Sorcerer Garb");
            BlackChest.Description.English("Armor of Solaire of Astora, Knight of Sunlight. The large holy symbol of the Sun while powerless, was painted by Solaire himself");
            BlackChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BlackChest.RequiredItems.Add("Iron", 25);
            BlackChest.RequiredItems.Add("Chain", 2);
            BlackChest.RequiredItems.Add("TwinklingTitanite", 10);
            BlackChest.RequiredItems.Add("MushroomYellow", 10);
            BlackChest.RequiredUpgradeItems.Add("Iron", 5);
            BlackChest.RequiredUpgradeItems.Add("Chain", 1);
            BlackChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            BlackChest.RequiredUpgradeItems.Add("MushroomYellow", 2);
            BodypartSystem.RegisterHiddenBodyParts("BlackChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight);

            Item BlackLegs = new("souls", "BlackLegs");
            BlackLegs.Name.English("Black Sorcerer Leggings");
            BlackLegs.Description.English("Leggings of Solaire of Astora, Knight of Blacklight. Of high quality, but lacking any particular powers");
            BlackLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BlackLegs.RequiredItems.Add("Iron", 25);
            BlackLegs.RequiredItems.Add("Chain", 2);
            BlackLegs.RequiredItems.Add("TwinklingTitanite", 10);
            BlackLegs.RequiredItems.Add("MushroomYellow", 10);
            BlackLegs.RequiredUpgradeItems.Add("Iron", 5);
            BlackLegs.RequiredUpgradeItems.Add("Chain", 1);
            BlackLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            BlackLegs.RequiredUpgradeItems.Add("MushroomYellow", 2);
            BodypartSystem.RegisterHiddenBodyParts("BlackLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item BlackHelm = new("souls", "BlackHelmet");
            BlackHelm.Name.English("Black Sorcerer Hat");
            BlackHelm.Description.English("Helm of Solaire of Astora, Knight of Blacklight. Of high quality, but lacking any particular powers");
            BlackHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BlackHelm.RequiredItems.Add("Iron", 25);
            BlackHelm.RequiredItems.Add("Chain", 2);
            BlackHelm.RequiredItems.Add("TwinklingTitanite", 10);
            BlackHelm.RequiredItems.Add("MushroomYellow", 10);
            BlackHelm.RequiredUpgradeItems.Add("Iron", 5);
            BlackHelm.RequiredUpgradeItems.Add("Chain", 1);
            BlackHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            BlackHelm.RequiredUpgradeItems.Add("MushroomYellow", 2);
            
            //Heavy Armor
            Item KnightChest = new("souls", "KnightChest");
            KnightChest.Name.English("Knight Chest");
            KnightChest.Description.English("Armor of Solaire of Astora, Knight of Sunlight. The large holy symbol of the Sun while powerless, was painted by Solaire himself");
            KnightChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            KnightChest.RequiredItems.Add("Iron", 25);
            KnightChest.RequiredItems.Add("Chain", 2);
            KnightChest.RequiredItems.Add("TwinklingTitanite", 10);
            KnightChest.RequiredItems.Add("MushroomYellow", 10);
            KnightChest.RequiredUpgradeItems.Add("Iron", 5);
            KnightChest.RequiredUpgradeItems.Add("Chain", 1);
            KnightChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            KnightChest.RequiredUpgradeItems.Add("MushroomYellow", 2);
            BodypartSystem.RegisterHiddenBodyParts("KnightChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);

            Item KnightLegs = new("souls", "KnightLegs");
            KnightLegs.Name.English("Leggings of the Knight");
            KnightLegs.Description.English("Leggings of Solaire of Astora, Knight of Knightlight. Of high quality, but lacking any particular powers");
            KnightLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            KnightLegs.RequiredItems.Add("Iron", 25);
            KnightLegs.RequiredItems.Add("Chain", 2);
            KnightLegs.RequiredItems.Add("TwinklingTitanite", 10);
            KnightLegs.RequiredItems.Add("MushroomYellow", 10);
            KnightLegs.RequiredUpgradeItems.Add("Iron", 5);
            KnightLegs.RequiredUpgradeItems.Add("Chain", 1);
            KnightLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            KnightLegs.RequiredUpgradeItems.Add("MushroomYellow", 2);
            BodypartSystem.RegisterHiddenBodyParts("KnightLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item KnightHelm = new("souls", "KnightHelmet");
            KnightHelm.Name.English("Helmet of the Knight");
            KnightHelm.Description.English("Helm of Solaire of Astora, Knight of Knightlight. Of high quality, but lacking any particular powers");
            KnightHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            KnightHelm.RequiredItems.Add("Iron", 25);
            KnightHelm.RequiredItems.Add("Chain", 2);
            KnightHelm.RequiredItems.Add("TwinklingTitanite", 10);
            KnightHelm.RequiredItems.Add("MushroomYellow", 10);
            KnightHelm.RequiredUpgradeItems.Add("Iron", 5);
            KnightHelm.RequiredUpgradeItems.Add("Chain", 1);
            KnightHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            KnightHelm.RequiredUpgradeItems.Add("MushroomYellow", 2);
            BodypartSystem.RegisterHiddenBodyParts("KnightHelm",
            BodypartSystem.bodyPart.Head);
            #endregion Tier1

            #region Tier 2 (Swamps)

            //Medium Armor
            Item DarkChest = new("souls", "DarkChest");
            DarkChest.Name.English("Armor of the Dark");
            DarkChest.Description.English("Armor of Solaire of Astora, Knight of Darklight. The large holy symbol of the Dark while powerless, was painted by Solaire himself");
            DarkChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DarkChest.RequiredItems.Add("Iron", 25);
            DarkChest.RequiredItems.Add("Chain", 2);
            DarkChest.RequiredItems.Add("TwinklingTitanite", 10);
            DarkChest.RequiredItems.Add("MushroomYellow", 10);
            DarkChest.RequiredUpgradeItems.Add("Iron", 5);
            DarkChest.RequiredUpgradeItems.Add("Chain", 1);
            DarkChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            DarkChest.RequiredUpgradeItems.Add("MushroomYellow", 2);
            BodypartSystem.RegisterHiddenBodyParts("DarkChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight);

            Item DarkLegs = new("souls", "DarkLegs");
            DarkLegs.Name.English("Leggings of the Dark");
            DarkLegs.Description.English("Leggings of Solaire of Astora, Knight of Darklight. Of high quality, but lacking any particular powers");
            DarkLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DarkLegs.RequiredItems.Add("Iron", 25);
            DarkLegs.RequiredItems.Add("Chain", 2);
            DarkLegs.RequiredItems.Add("TwinklingTitanite", 10);
            DarkLegs.RequiredItems.Add("MushroomYellow", 10);
            DarkLegs.RequiredUpgradeItems.Add("Iron", 5);
            DarkLegs.RequiredUpgradeItems.Add("Chain", 1);
            DarkLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            DarkLegs.RequiredUpgradeItems.Add("MushroomYellow", 2);
            BodypartSystem.RegisterHiddenBodyParts("DarkLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item DarkHelm = new("souls", "DarkHelmet");
            DarkHelm.Name.English("Helmet of the Dark");
            DarkHelm.Description.English("Helm of Solaire of Astora, Knight of Darklight. Of high quality, but lacking any particular powers");
            DarkHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DarkHelm.RequiredItems.Add("Iron", 25);
            DarkHelm.RequiredItems.Add("Chain", 2);
            DarkHelm.RequiredItems.Add("TwinklingTitanite", 10);
            DarkHelm.RequiredItems.Add("MushroomYellow", 10);
            DarkHelm.RequiredUpgradeItems.Add("Iron", 5);
            DarkHelm.RequiredUpgradeItems.Add("Chain", 1);
            DarkHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            DarkHelm.RequiredUpgradeItems.Add("MushroomYellow", 2);
            BodypartSystem.RegisterHiddenBodyParts("DarkHelm",
            BodypartSystem.bodyPart.Head);

            //Mage armor female
            Item WitchLegs = new("souls", "WitchLegs");
            WitchLegs.Name.English("Witchs Leggings");
            WitchLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            WitchLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            WitchLegs.RequiredItems.Add("Stone", 25);
            WitchLegs.RequiredItems.Add("DragonTear", 1);
            WitchLegs.RequiredItems.Add("TwinklingTitanite", 10);
            WitchLegs.RequiredItems.Add("BlackMetal", 10);
            WitchLegs.RequiredUpgradeItems.Add("Stone", 5);
            WitchLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            WitchLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            WitchLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("WitchLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item WitchHelm = new("souls", "WitchHelm");
            WitchHelm.Name.English("Witchs Helm");
            WitchHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            WitchHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            WitchHelm.RequiredItems.Add("Stone", 25);
            WitchHelm.RequiredItems.Add("DragonTear", 1);
            WitchHelm.RequiredItems.Add("TwinklingTitanite", 10);
            WitchHelm.RequiredItems.Add("BlackMetal", 10);
            WitchHelm.RequiredUpgradeItems.Add("Stone", 5);
            WitchHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            WitchHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            WitchHelm.RequiredUpgradeItems.Add("BlackMetal", 2);

            Item WitchChest = new("souls", "WitchChest");
            WitchChest.Name.English("Witchs Chest Piece");
            WitchChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            WitchChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            WitchChest.RequiredItems.Add("Stone", 25);
            WitchChest.RequiredItems.Add("DragonTear", 1);
            WitchChest.RequiredItems.Add("TwinklingTitanite", 10);
            WitchChest.RequiredItems.Add("BlackMetal", 10);
            WitchChest.RequiredUpgradeItems.Add("Stone", 5);
            WitchChest.RequiredUpgradeItems.Add("DragonTear", 2);
            WitchChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            WitchChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("WitchChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);

            //Light Armor
            Item ArtChest = new("souls", "ArtChest");
            ArtChest.Name.English("Abyss Chest Piece");
            ArtChest.Description.English("Armor of Artorias the Abysswalker, one of Gwyn's four knights. The death of the armor's owner can be surmised from the corrosive Dark of the Abyss, and the tattered azure-blue cape, once a symbol of pride and glory.");
            ArtChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            ArtChest.RequiredItems.Add("Silver", 25);
            ArtChest.RequiredItems.Add("WolfHairBundle", 10);
            ArtChest.RequiredItems.Add("TwinklingTitanite", 10);
            ArtChest.RequiredItems.Add("DeerHide", 10);
            ArtChest.RequiredUpgradeItems.Add("Silver", 5);
            ArtChest.RequiredUpgradeItems.Add("TrophyWolf", 1);
            ArtChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            ArtChest.RequiredUpgradeItems.Add("DeerHide", 2);
            BodypartSystem.RegisterHiddenBodyParts("ArtChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);

            Item ArtLegs = new("souls", "ArtLegs");
            ArtLegs.Name.English("Abyss Leggins");
            ArtLegs.Description.English("Leggings of Artorias the Abysswalker, one of Gwyn's four knights. The death of the their owner can be surmised from the corrosive Dark of the Abyss, which has compromised their protective utility.");
            ArtLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            ArtLegs.RequiredItems.Add("Silver", 25);
            ArtLegs.RequiredItems.Add("WolfHairBundle", 10);
            ArtLegs.RequiredItems.Add("TwinklingTitanite", 10);
            ArtLegs.RequiredItems.Add("DeerHide", 10);
            ArtLegs.RequiredUpgradeItems.Add("Silver", 5);
            ArtLegs.RequiredUpgradeItems.Add("WolfHairBundle", 2);
            ArtLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            ArtLegs.RequiredUpgradeItems.Add("DeerHide", 2);
            BodypartSystem.RegisterHiddenBodyParts("ArtLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item ArtHelm = new("souls", "ArtHelm");
            ArtHelm.Name.English("Abyss Helm");
            ArtHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            ArtHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            ArtHelm.RequiredItems.Add("Silver", 25);
            ArtHelm.RequiredItems.Add("TrophyWolf", 1);
            ArtHelm.RequiredItems.Add("TwinklingTitanite", 10);
            ArtHelm.RequiredItems.Add("DeerHide", 10);
            ArtHelm.RequiredUpgradeItems.Add("Silver", 5);
            ArtHelm.RequiredUpgradeItems.Add("WolfHairBundle", 2);
            ArtHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            ArtHelm.RequiredUpgradeItems.Add("DeerHide", 2);
            BodypartSystem.RegisterHiddenBodyParts("ArtHelm",
            BodypartSystem.bodyPart.Head);
            #endregion Tier2 

            #region Tier 3(Mountains)
            //Heavy Armor
            Item EliteLegs = new("souls", "EliteLegs");
            EliteLegs.Name.English("Elites Leggings");
            EliteLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            EliteLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            EliteLegs.RequiredItems.Add("Stone", 25);
            EliteLegs.RequiredItems.Add("DragonTear", 1);
            EliteLegs.RequiredItems.Add("TwinklingTitanite", 10);
            EliteLegs.RequiredItems.Add("BlackMetal", 10);
            EliteLegs.RequiredUpgradeItems.Add("Stone", 5);
            EliteLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            EliteLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            EliteLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("EliteLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item EliteHelm = new("souls", "EliteHelmet");
            EliteHelm.Name.English("Elites Helm");
            EliteHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            EliteHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            EliteHelm.RequiredItems.Add("Stone", 25);
            EliteHelm.RequiredItems.Add("DragonTear", 1);
            EliteHelm.RequiredItems.Add("TwinklingTitanite", 10);
            EliteHelm.RequiredItems.Add("BlackMetal", 10);
            EliteHelm.RequiredUpgradeItems.Add("Stone", 5);
            EliteHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            EliteHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            EliteHelm.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("EliteHelmet",
            BodypartSystem.bodyPart.Head);

            Item EliteChest = new("souls", "EliteChest");
            EliteChest.Name.English("Elites Chest Piece");
            EliteChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            EliteChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            EliteChest.RequiredItems.Add("Stone", 25);
            EliteChest.RequiredItems.Add("DragonTear", 1);
            EliteChest.RequiredItems.Add("TwinklingTitanite", 10);
            EliteChest.RequiredItems.Add("BlackMetal", 10);
            EliteChest.RequiredUpgradeItems.Add("Stone", 5);
            EliteChest.RequiredUpgradeItems.Add("DragonTear", 2);
            EliteChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            EliteChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("EliteChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);

            //Mage Armor unisex
            Item CrimsonLegs = new("souls", "CrimsonLegs");
            CrimsonLegs.Name.English("Crimsons Leggings");
            CrimsonLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            CrimsonLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            CrimsonLegs.RequiredItems.Add("Stone", 25);
            CrimsonLegs.RequiredItems.Add("DragonTear", 1);
            CrimsonLegs.RequiredItems.Add("TwinklingTitanite", 10);
            CrimsonLegs.RequiredItems.Add("BlackMetal", 10);
            CrimsonLegs.RequiredUpgradeItems.Add("Stone", 5);
            CrimsonLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            CrimsonLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            CrimsonLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("CrimsonLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item CrimsonHelm = new("souls", "CrimsonHelm");
            CrimsonHelm.Name.English("Crimsons Helm");
            CrimsonHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            CrimsonHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            CrimsonHelm.RequiredItems.Add("Stone", 25);
            CrimsonHelm.RequiredItems.Add("DragonTear", 1);
            CrimsonHelm.RequiredItems.Add("TwinklingTitanite", 10);
            CrimsonHelm.RequiredItems.Add("BlackMetal", 10);
            CrimsonHelm.RequiredUpgradeItems.Add("Stone", 5);
            CrimsonHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            CrimsonHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            CrimsonHelm.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("CrimsonHelm",
            BodypartSystem.bodyPart.Head);

            Item CrimsonChest = new("souls", "CrimsonChest");
            CrimsonChest.Name.English("Crimsons Chest Piece");
            CrimsonChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            CrimsonChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            CrimsonChest.RequiredItems.Add("Stone", 25);
            CrimsonChest.RequiredItems.Add("DragonTear", 1);
            CrimsonChest.RequiredItems.Add("TwinklingTitanite", 10);
            CrimsonChest.RequiredItems.Add("BlackMetal", 10);
            CrimsonChest.RequiredUpgradeItems.Add("Stone", 5);
            CrimsonChest.RequiredUpgradeItems.Add("DragonTear", 2);
            CrimsonChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            CrimsonChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("CrimsonChest",
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);

            //Medium Armor
            Item FavorLegs = new("souls", "FavorLegs");
            FavorLegs.Name.English("Favors Leggings");
            FavorLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            FavorLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            FavorLegs.RequiredItems.Add("Stone", 25);
            FavorLegs.RequiredItems.Add("DragonTear", 1);
            FavorLegs.RequiredItems.Add("TwinklingTitanite", 10);
            FavorLegs.RequiredItems.Add("BlackMetal", 10);
            FavorLegs.RequiredUpgradeItems.Add("Stone", 5);
            FavorLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            FavorLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            FavorLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("FavorLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item FavorHelm = new("souls", "FavorHelm");
            FavorHelm.Name.English("Favors Helm");
            FavorHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            FavorHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            FavorHelm.RequiredItems.Add("Stone", 25);
            FavorHelm.RequiredItems.Add("DragonTear", 1);
            FavorHelm.RequiredItems.Add("TwinklingTitanite", 10);
            FavorHelm.RequiredItems.Add("BlackMetal", 10);
            FavorHelm.RequiredUpgradeItems.Add("Stone", 5);
            FavorHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            FavorHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            FavorHelm.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("FavorHelm",
            BodypartSystem.bodyPart.Head);

            Item FavorChest = new("souls", "FavorChest");
            FavorChest.Name.English("Favors Chest Piece");
            FavorChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            FavorChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            FavorChest.RequiredItems.Add("Stone", 25);
            FavorChest.RequiredItems.Add("DragonTear", 1);
            FavorChest.RequiredItems.Add("TwinklingTitanite", 10);
            FavorChest.RequiredItems.Add("BlackMetal", 10);
            FavorChest.RequiredUpgradeItems.Add("Stone", 5);
            FavorChest.RequiredUpgradeItems.Add("DragonTear", 2);
            FavorChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            FavorChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("FavorChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);


            //light amor
            Item XanthousChest = new("souls", "XanthousChest");
            XanthousChest.Name.English("Xanthous Chest");
            XanthousChest.Description.English("Armor of Solaire of Astora, Knight of Sunlight. The large holy symbol of the Sun while powerless, was painted by Solaire himself");
            XanthousChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            XanthousChest.RequiredItems.Add("Iron", 25);
            XanthousChest.RequiredItems.Add("Chain", 2);
            XanthousChest.RequiredItems.Add("TwinklingTitanite", 10);
            XanthousChest.RequiredItems.Add("MushroomYellow", 10);
            XanthousChest.RequiredUpgradeItems.Add("Iron", 5);
            XanthousChest.RequiredUpgradeItems.Add("Chain", 1);
            XanthousChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            XanthousChest.RequiredUpgradeItems.Add("MushroomYellow", 2);
            BodypartSystem.RegisterHiddenBodyParts("XanthousChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);

            Item XanthousHelm = new("souls", "XanthousHelmet");
            XanthousHelm.Name.English("Xanthous Helm");
            XanthousHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            XanthousHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            XanthousHelm.RequiredItems.Add("Stone", 25);
            XanthousHelm.RequiredItems.Add("DragonTear", 1);
            XanthousHelm.RequiredItems.Add("TwinklingTitanite", 10);
            XanthousHelm.RequiredItems.Add("BlackMetal", 10);
            XanthousHelm.RequiredUpgradeItems.Add("Stone", 5);
            XanthousHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            XanthousHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            XanthousHelm.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("XanthousHelmet",
            BodypartSystem.bodyPart.Head);

            Item XanthousLegs = new("souls", "XanthousLegs");
            XanthousLegs.Name.English("Xanthous Leggings");
            XanthousLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            XanthousLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            XanthousLegs.RequiredItems.Add("Stone", 25);
            XanthousLegs.RequiredItems.Add("DragonTear", 1);
            XanthousLegs.RequiredItems.Add("TwinklingTitanite", 10);
            XanthousLegs.RequiredItems.Add("BlackMetal", 10);
            XanthousLegs.RequiredUpgradeItems.Add("Stone", 5);
            XanthousLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            XanthousLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            XanthousLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("XanthousLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);
            #endregion Tier 3 (Mountains

            #region Tier 4 (Plains)
            //medium armor
            Item DragonslayerLegs = new("souls", "DragonslayerLegs");
            DragonslayerLegs.Name.English("Dragonslayers Leggings");
            DragonslayerLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            DragonslayerLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DragonslayerLegs.RequiredItems.Add("Stone", 25);
            DragonslayerLegs.RequiredItems.Add("DragonTear", 1);
            DragonslayerLegs.RequiredItems.Add("TwinklingTitanite", 10);
            DragonslayerLegs.RequiredItems.Add("BlackMetal", 10);
            DragonslayerLegs.RequiredUpgradeItems.Add("Stone", 5);
            DragonslayerLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            DragonslayerLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            DragonslayerLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("DragonslayerLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item DragonslayerHelm = new("souls", "DragonslayerHelmet");
            DragonslayerHelm.Name.English("Dragonslayers Helm");
            DragonslayerHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            DragonslayerHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DragonslayerHelm.RequiredItems.Add("Stone", 25);
            DragonslayerHelm.RequiredItems.Add("DragonTear", 1);
            DragonslayerHelm.RequiredItems.Add("TwinklingTitanite", 10);
            DragonslayerHelm.RequiredItems.Add("BlackMetal", 10);
            DragonslayerHelm.RequiredUpgradeItems.Add("Stone", 5);
            DragonslayerHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            DragonslayerHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            DragonslayerHelm.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("DragonslayerHelmet",
            BodypartSystem.bodyPart.Head);

            Item DragonslayerChest = new("souls", "DragonslayerChest");
            DragonslayerChest.Name.English("Dragonslayers Chest Piece");
            DragonslayerChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            DragonslayerChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DragonslayerChest.RequiredItems.Add("Stone", 25);
            DragonslayerChest.RequiredItems.Add("DragonTear", 1);
            DragonslayerChest.RequiredItems.Add("TwinklingTitanite", 10);
            DragonslayerChest.RequiredItems.Add("BlackMetal", 10);
            DragonslayerChest.RequiredUpgradeItems.Add("Stone", 5);
            DragonslayerChest.RequiredUpgradeItems.Add("DragonTear", 2);
            DragonslayerChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            DragonslayerChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("DragonslayerChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);

            //Heavy armor
            //Catarina 

            Item CatarinaChest = new("souls", "CatarinaChest");
            CatarinaChest.Name.English("Catarina Chest Piece");
            CatarinaChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            CatarinaChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            CatarinaChest.RequiredItems.Add("Stone", 25);
            CatarinaChest.RequiredItems.Add("DragonTear", 1);
            CatarinaChest.RequiredItems.Add("TwinklingTitanite", 10);
            CatarinaChest.RequiredItems.Add("BlackMetal", 10);
            CatarinaChest.RequiredUpgradeItems.Add("Stone", 5);
            CatarinaChest.RequiredUpgradeItems.Add("DragonTear", 2);
            CatarinaChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            CatarinaChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("CatarinaChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);

            Item CatarinaLegs = new("souls", "CatarinaLegs");
            CatarinaLegs.Name.English("Catarina Leggings");
            CatarinaLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            CatarinaLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            CatarinaLegs.RequiredItems.Add("Stone", 25);
            CatarinaLegs.RequiredItems.Add("DragonTear", 1);
            CatarinaLegs.RequiredItems.Add("TwinklingTitanite", 10);
            CatarinaLegs.RequiredItems.Add("BlackMetal", 10);
            CatarinaLegs.RequiredUpgradeItems.Add("Stone", 5);
            CatarinaLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            CatarinaLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            CatarinaLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("CatarinaLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item CatarinaHelm = new("souls", "CatarinaHelmet");
            CatarinaHelm.Name.English("Catarina Helm");
            CatarinaHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            CatarinaHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            CatarinaHelm.RequiredItems.Add("Stone", 25);
            CatarinaHelm.RequiredItems.Add("DragonTear", 1);
            CatarinaHelm.RequiredItems.Add("TwinklingTitanite", 10);
            CatarinaHelm.RequiredItems.Add("BlackMetal", 10);
            CatarinaHelm.RequiredUpgradeItems.Add("Stone", 5);
            CatarinaHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            CatarinaHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            CatarinaHelm.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("CatarinaHelmet",
            BodypartSystem.bodyPart.Head);

            //Light Armor
            Item BrassLegs = new("souls", "BrassLegs");
            BrassLegs.Name.English("Brasss Leggings");
            BrassLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            BrassLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BrassLegs.RequiredItems.Add("Stone", 25);
            BrassLegs.RequiredItems.Add("DragonTear", 1);
            BrassLegs.RequiredItems.Add("TwinklingTitanite", 10);
            BrassLegs.RequiredItems.Add("BlackMetal", 10);
            BrassLegs.RequiredUpgradeItems.Add("Stone", 5);
            BrassLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            BrassLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            BrassLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("BrassLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item BrassHelm = new("souls", "BrassHelmet");
            BrassHelm.Name.English("Brasss Helm");
            BrassHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            BrassHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BrassHelm.RequiredItems.Add("Stone", 25);
            BrassHelm.RequiredItems.Add("DragonTear", 1);
            BrassHelm.RequiredItems.Add("TwinklingTitanite", 10);
            BrassHelm.RequiredItems.Add("BlackMetal", 10);
            BrassHelm.RequiredUpgradeItems.Add("Stone", 5);
            BrassHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            BrassHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            BrassHelm.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("BrassHelmet",
            BodypartSystem.bodyPart.Head);

            Item BrassChest = new("souls", "BrassChest");
            BrassChest.Name.English("Brasss Chest Piece");
            BrassChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            BrassChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BrassChest.RequiredItems.Add("Stone", 25);
            BrassChest.RequiredItems.Add("DragonTear", 1);
            BrassChest.RequiredItems.Add("TwinklingTitanite", 10);
            BrassChest.RequiredItems.Add("BlackMetal", 10);
            BrassChest.RequiredUpgradeItems.Add("Stone", 5);
            BrassChest.RequiredUpgradeItems.Add("DragonTear", 2);
            BrassChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            BrassChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("BrassChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);

            //Mage Armor
            Item DarkmoonChest = new("souls", "DarkmoonChest");
            DarkmoonChest.Name.English("Darkmoon Chest Piece");
            DarkmoonChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            DarkmoonChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DarkmoonChest.RequiredItems.Add("Stone", 25);
            DarkmoonChest.RequiredItems.Add("DragonTear", 1);
            DarkmoonChest.RequiredItems.Add("TwinklingTitanite", 10);
            DarkmoonChest.RequiredItems.Add("BlackMetal", 10);
            DarkmoonChest.RequiredUpgradeItems.Add("Stone", 5);
            DarkmoonChest.RequiredUpgradeItems.Add("DragonTear", 2);
            DarkmoonChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            DarkmoonChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("DarkmoonChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight);

            Item DarkmoonLegs = new("souls", "DarkmoonLegs");
            DarkmoonLegs.Name.English("Darkmoon Leggings");
            DarkmoonLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            DarkmoonLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DarkmoonLegs.RequiredItems.Add("Stone", 25);
            DarkmoonLegs.RequiredItems.Add("DragonTear", 1);
            DarkmoonLegs.RequiredItems.Add("TwinklingTitanite", 10);
            DarkmoonLegs.RequiredItems.Add("BlackMetal", 10);
            DarkmoonLegs.RequiredUpgradeItems.Add("Stone", 5);
            DarkmoonLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            DarkmoonLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            DarkmoonLegs.RequiredUpgradeItems.Add("BlackMetal", 2);

            Item DarkmoonHelm = new("souls", "DarkmoonHelmet");
            DarkmoonHelm.Name.English("Darkmoon Helm");
            DarkmoonHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            DarkmoonHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DarkmoonHelm.RequiredItems.Add("Stone", 25);
            DarkmoonHelm.RequiredItems.Add("DragonTear", 1);
            DarkmoonHelm.RequiredItems.Add("TwinklingTitanite", 10);
            DarkmoonHelm.RequiredItems.Add("BlackMetal", 10);
            DarkmoonHelm.RequiredUpgradeItems.Add("Stone", 5);
            DarkmoonHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            DarkmoonHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            DarkmoonHelm.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("DarkmoonHelmet",
            BodypartSystem.bodyPart.Head);
            #endregion Tier 4 (plains)

            #region Tier 5 (Mistlands)

            //Mage Armor
            Item GoldLegs = new("souls", "GoldLegs");
            GoldLegs.Name.English("Gold-Hemmed Leggings");
            GoldLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            GoldLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            GoldLegs.RequiredItems.Add("Stone", 25);
            GoldLegs.RequiredItems.Add("DragonTear", 1);
            GoldLegs.RequiredItems.Add("TwinklingTitanite", 10);
            GoldLegs.RequiredItems.Add("BlackMetal", 10);
            GoldLegs.RequiredUpgradeItems.Add("Stone", 5);
            GoldLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            GoldLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            GoldLegs.RequiredUpgradeItems.Add("BlackMetal", 2);

            Item GoldHelm = new("souls", "GoldHelm");
            GoldHelm.Name.English("Gold-Hemmed Helm");
            GoldHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            GoldHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            GoldHelm.RequiredItems.Add("Stone", 25);
            GoldHelm.RequiredItems.Add("DragonTear", 1);
            GoldHelm.RequiredItems.Add("TwinklingTitanite", 10);
            GoldHelm.RequiredItems.Add("BlackMetal", 10);
            GoldHelm.RequiredUpgradeItems.Add("Stone", 5);
            GoldHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            GoldHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            GoldHelm.RequiredUpgradeItems.Add("BlackMetal", 2);

            Item GoldChest = new("souls", "GoldChest");
            GoldChest.Name.English("Gold-Hemmed Chest Piece");
            GoldChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            GoldChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            GoldChest.RequiredItems.Add("Stone", 25);
            GoldChest.RequiredItems.Add("DragonTear", 1);
            GoldChest.RequiredItems.Add("TwinklingTitanite", 10);
            GoldChest.RequiredItems.Add("BlackMetal", 10);
            GoldChest.RequiredUpgradeItems.Add("Stone", 5);
            GoldChest.RequiredUpgradeItems.Add("DragonTear", 2);
            GoldChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            GoldChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("GoldChest",
            BodypartSystem.bodyPart.Torso);
            //Heavy Armor

            Item GolemLegs = new("souls", "GolemLegs");
            GolemLegs.Name.English("Golems Leggings");
            GolemLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            GolemLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            GolemLegs.RequiredItems.Add("Stone", 25);
            GolemLegs.RequiredItems.Add("DragonTear", 1);
            GolemLegs.RequiredItems.Add("TwinklingTitanite", 10);
            GolemLegs.RequiredItems.Add("BlackMetal", 10);
            GolemLegs.RequiredUpgradeItems.Add("Stone", 5);
            GolemLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            GolemLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            GolemLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("GolemLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item GolemHelm = new("souls", "GolemHelmet");
            GolemHelm.Name.English("Golems Helm");
            GolemHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            GolemHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            GolemHelm.RequiredItems.Add("Stone", 25);
            GolemHelm.RequiredItems.Add("DragonTear", 1);
            GolemHelm.RequiredItems.Add("TwinklingTitanite", 10);
            GolemHelm.RequiredItems.Add("BlackMetal", 10);
            GolemHelm.RequiredUpgradeItems.Add("Stone", 5);
            GolemHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            GolemHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            GolemHelm.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("GolemHelmet",
            BodypartSystem.bodyPart.Head);

            Item GolemChest = new("souls", "GolemChest");
            GolemChest.Name.English("Golems Chest Piece");
            GolemChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            GolemChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            GolemChest.RequiredItems.Add("Stone", 25);
            GolemChest.RequiredItems.Add("DragonTear", 1);
            GolemChest.RequiredItems.Add("TwinklingTitanite", 10);
            GolemChest.RequiredItems.Add("BlackMetal", 10);
            GolemChest.RequiredUpgradeItems.Add("Stone", 5);
            GolemChest.RequiredUpgradeItems.Add("DragonTear", 2);
            GolemChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            GolemChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("GolemChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);

            //Medium Armor
            Item SilverKnightLegs = new("souls", "SilverKnightLegs");
            SilverKnightLegs.Name.English("SilverKnights Leggings");
            SilverKnightLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            SilverKnightLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            SilverKnightLegs.RequiredItems.Add("Stone", 25);
            SilverKnightLegs.RequiredItems.Add("DragonTear", 1);
            SilverKnightLegs.RequiredItems.Add("TwinklingTitanite", 10);
            SilverKnightLegs.RequiredItems.Add("BlackMetal", 10);
            SilverKnightLegs.RequiredUpgradeItems.Add("Stone", 5);
            SilverKnightLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            SilverKnightLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            SilverKnightLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("SilverKnightLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item SilverKnightHelm = new("souls", "SilverKnightHelmet");
            SilverKnightHelm.Name.English("SilverKnights Helm");
            SilverKnightHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            SilverKnightHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            SilverKnightHelm.RequiredItems.Add("Stone", 25);
            SilverKnightHelm.RequiredItems.Add("DragonTear", 1);
            SilverKnightHelm.RequiredItems.Add("TwinklingTitanite", 10);
            SilverKnightHelm.RequiredItems.Add("BlackMetal", 10);
            SilverKnightHelm.RequiredUpgradeItems.Add("Stone", 5);
            SilverKnightHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            SilverKnightHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            SilverKnightHelm.RequiredUpgradeItems.Add("BlackMetal", 2);
/*            BodypartSystem.RegisterHiddenBodyParts("SilverKnightHelmet",
            BodypartSystem.bodyPart.Head);*/

            Item SilverKnightChest = new("souls", "SilverKnightChest");
            SilverKnightChest.Name.English("Dragonslayers Leggings");
            SilverKnightChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            SilverKnightChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            SilverKnightChest.RequiredItems.Add("Stone", 25);
            SilverKnightChest.RequiredItems.Add("DragonTear", 1);
            SilverKnightChest.RequiredItems.Add("TwinklingTitanite", 10);
            SilverKnightChest.RequiredItems.Add("BlackMetal", 10);
            SilverKnightChest.RequiredUpgradeItems.Add("Stone", 5);
            SilverKnightChest.RequiredUpgradeItems.Add("DragonTear", 2);
            SilverKnightChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            SilverKnightChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("SilverKnightChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);
            #endregion Tier 5 (Mistlands)

            #region Tier 6 (Ashlands)
            //Heavy Armor
            Item HavelLegs = new("souls", "HavelLegs");
            HavelLegs.Name.English("Havels Leggings");
            HavelLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            HavelLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            HavelLegs.RequiredItems.Add("Stone", 25);
            HavelLegs.RequiredItems.Add("DragonTear", 1);
            HavelLegs.RequiredItems.Add("TwinklingTitanite", 10);
            HavelLegs.RequiredItems.Add("BlackMetal", 10);
            HavelLegs.RequiredUpgradeItems.Add("Stone", 5);
            HavelLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            HavelLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            HavelLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("HavelLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item HavelHelm = new("souls", "HavelHelm");
            HavelHelm.Name.English("Havels Helm");
            HavelHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            HavelHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            HavelHelm.RequiredItems.Add("Stone", 25);
            HavelHelm.RequiredItems.Add("DragonTear", 1);
            HavelHelm.RequiredItems.Add("TwinklingTitanite", 10);
            HavelHelm.RequiredItems.Add("BlackMetal", 10);
            HavelHelm.RequiredUpgradeItems.Add("Stone", 5);
            HavelHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            HavelHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            HavelHelm.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("HavelHelm",
            BodypartSystem.bodyPart.Head);

            Item HavelChest = new("souls", "HavelChest");
            HavelChest.Name.English("Havels Chest Piece");
            HavelChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            HavelChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            HavelChest.RequiredItems.Add("Stone", 25);
            HavelChest.RequiredItems.Add("DragonTear", 1);
            HavelChest.RequiredItems.Add("TwinklingTitanite", 10);
            HavelChest.RequiredItems.Add("BlackMetal", 10);
            HavelChest.RequiredUpgradeItems.Add("Stone", 5);
            HavelChest.RequiredUpgradeItems.Add("DragonTear", 2);
            HavelChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            HavelChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("HavelChest",
             BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);

            //Medium Armor
            Item BlackKnightChest = new("souls", "BlackKnightChest");
            BlackKnightChest.Name.English("Black KnightChest");
            BlackKnightChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            BlackKnightChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BlackKnightChest.RequiredItems.Add("Stone", 25);
            BlackKnightChest.RequiredItems.Add("DragonTear", 1);
            BlackKnightChest.RequiredItems.Add("TwinklingTitanite", 10);
            BlackKnightChest.RequiredItems.Add("BlackMetal", 10);
            BlackKnightChest.RequiredUpgradeItems.Add("Stone", 5);
            BlackKnightChest.RequiredUpgradeItems.Add("DragonTear", 2);
            BlackKnightChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            BlackKnightChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("BlackKnightChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight,
            BodypartSystem.bodyPart.HandLeft,
            BodypartSystem.bodyPart.HandRight);

            Item BlackKnightLegs = new("souls", "BlackKnightLegs");
            BlackKnightLegs.Name.English("BlackKnights Leggings");
            BlackKnightLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            BlackKnightLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BlackKnightLegs.RequiredItems.Add("Stone", 25);
            BlackKnightLegs.RequiredItems.Add("DragonTear", 1);
            BlackKnightLegs.RequiredItems.Add("TwinklingTitanite", 10);
            BlackKnightLegs.RequiredItems.Add("BlackMetal", 10);
            BlackKnightLegs.RequiredUpgradeItems.Add("Stone", 5);
            BlackKnightLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            BlackKnightLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            BlackKnightLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("BlackKnightLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item BlackKnightHelm = new("souls", "BlackKnightHelmet");
            BlackKnightHelm.Name.English("BlackKnights Helm");
            BlackKnightHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            BlackKnightHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BlackKnightHelm.RequiredItems.Add("Stone", 25);
            BlackKnightHelm.RequiredItems.Add("DragonTear", 1);
            BlackKnightHelm.RequiredItems.Add("TwinklingTitanite", 10);
            BlackKnightHelm.RequiredItems.Add("BlackMetal", 10);
            BlackKnightHelm.RequiredUpgradeItems.Add("Stone", 5);
            BlackKnightHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            BlackKnightHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            BlackKnightHelm.RequiredUpgradeItems.Add("BlackMetal", 2);
/*            BodypartSystem.RegisterHiddenBodyParts("BlackKnightHelmet",
            BodypartSystem.bodyPart.Head);*/


            //Mage armor
            Item LoganLegs = new("souls", "LoganLegs");
            LoganLegs.Name.English("Logans Leggings");
            LoganLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            LoganLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            LoganLegs.RequiredItems.Add("Stone", 25);
            LoganLegs.RequiredItems.Add("DragonTear", 1);
            LoganLegs.RequiredItems.Add("TwinklingTitanite", 10);
            LoganLegs.RequiredItems.Add("BlackMetal", 10);
            LoganLegs.RequiredUpgradeItems.Add("Stone", 5);
            LoganLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            LoganLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            LoganLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("LoganLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item LoganHelm = new("souls", "LoganHelm");
            LoganHelm.Name.English("Logans Helm");
            LoganHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            LoganHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            LoganHelm.RequiredItems.Add("Stone", 25);
            LoganHelm.RequiredItems.Add("DragonTear", 1);
            LoganHelm.RequiredItems.Add("TwinklingTitanite", 10);
            LoganHelm.RequiredItems.Add("BlackMetal", 10);
            LoganHelm.RequiredUpgradeItems.Add("Stone", 5);
            LoganHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            LoganHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            LoganHelm.RequiredUpgradeItems.Add("BlackMetal", 2);

            Item LoganChest = new("souls", "LoganChest");
            LoganChest.Name.English("Logans Chest Piece");
            LoganChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            LoganChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            LoganChest.RequiredItems.Add("Stone", 25);
            LoganChest.RequiredItems.Add("DragonTear", 1);
            LoganChest.RequiredItems.Add("TwinklingTitanite", 10);
            LoganChest.RequiredItems.Add("BlackMetal", 10);
            LoganChest.RequiredUpgradeItems.Add("Stone", 5);
            LoganChest.RequiredUpgradeItems.Add("DragonTear", 2);
            LoganChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            LoganChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("LoganChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight);

            //Mage Armor female
            Item ZulieLegs = new("souls", "ZulieLegs");
            ZulieLegs.Name.English("Zulies Leggings");
            ZulieLegs.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            ZulieLegs.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            ZulieLegs.RequiredItems.Add("Stone", 25);
            ZulieLegs.RequiredItems.Add("DragonTear", 1);
            ZulieLegs.RequiredItems.Add("TwinklingTitanite", 10);
            ZulieLegs.RequiredItems.Add("BlackMetal", 10);
            ZulieLegs.RequiredUpgradeItems.Add("Stone", 5);
            ZulieLegs.RequiredUpgradeItems.Add("DragonTear", 2);
            ZulieLegs.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            ZulieLegs.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("ZulieLegs",
            BodypartSystem.bodyPart.LegUpperRight,
            BodypartSystem.bodyPart.LegUpperLeft,
            BodypartSystem.bodyPart.LegLowerLeft,
            BodypartSystem.bodyPart.LegLowerRight,
            BodypartSystem.bodyPart.FootRight,
            BodypartSystem.bodyPart.FootLeft);

            Item ZulieHelm = new("souls", "ZulieHelm");
            ZulieHelm.Name.English("Zulies Helm");
            ZulieHelm.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            ZulieHelm.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            ZulieHelm.RequiredItems.Add("Stone", 25);
            ZulieHelm.RequiredItems.Add("DragonTear", 1);
            ZulieHelm.RequiredItems.Add("TwinklingTitanite", 10);
            ZulieHelm.RequiredItems.Add("BlackMetal", 10);
            ZulieHelm.RequiredUpgradeItems.Add("Stone", 5);
            ZulieHelm.RequiredUpgradeItems.Add("DragonTear", 2);
            ZulieHelm.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            ZulieHelm.RequiredUpgradeItems.Add("BlackMetal", 2);

            Item ZulieChest = new("souls", "ZulieChest");
            ZulieChest.Name.English("Zulies Chest Piece");
            ZulieChest.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
            ZulieChest.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            ZulieChest.RequiredItems.Add("Stone", 25);
            ZulieChest.RequiredItems.Add("DragonTear", 1);
            ZulieChest.RequiredItems.Add("TwinklingTitanite", 10);
            ZulieChest.RequiredItems.Add("BlackMetal", 10);
            ZulieChest.RequiredUpgradeItems.Add("Stone", 5);
            ZulieChest.RequiredUpgradeItems.Add("DragonTear", 2);
            ZulieChest.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            ZulieChest.RequiredUpgradeItems.Add("BlackMetal", 2);
            BodypartSystem.RegisterHiddenBodyParts("ZulieChest",
            BodypartSystem.bodyPart.Torso,
            BodypartSystem.bodyPart.ArmUpperLeft,
            BodypartSystem.bodyPart.ArmUpperRight,
            BodypartSystem.bodyPart.ArmLowerLeft,
            BodypartSystem.bodyPart.ArmLowerRight);
            Debug.Log("[PungusSouls] AFTER ARMOR");
            #endregion Tier 6 (Ashlands)
            #endregion Armor
            #region Weapons
            #region Tier 1 (Forest)

            Item StaffWood = new("souls", "StaffWood", "assets");
            StaffWood.Name.English("Beatrice's Catalyst"); // You can use this to fix the display name in code
            StaffWood.Description.English("Catalyst belonging to Beatrice, the rogue witch. Contrasts with Vinheim catalysts. This ancient catalyst shows signs of being used for age-old sorceries. It has passed the hands of many generations to get here.");
            StaffWood.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            StaffWood.RequiredItems.Add("Wood", 40);
            StaffWood.RequiredItems.Add("TitaniteShard", 10);
            StaffWood.RequiredItems.Add("GreydwarfEye", 20);
            StaffWood.RequiredItems.Add("HardAntler", 5);
            StaffWood.RequiredUpgradeItems.Add("TitaniteShard", 1);
            StaffWood.ApplyUpgradeMap("Standard");

                Item sunshield1 = new("souls", "sunshield1", "assets");
            sunshield1.Name.English("sunlight shield"); // You can use this to fix the display name in code
            sunshield1.Description.English("Shield of Solaire of Astora, Knight of Sunlight. Decorated with a holy symbol, but Solaire illustrated it himself, and it has no divine powers of its own. As it turns out, Solaire's incredible prowess is a product of his own training, and nothing else.");
            sunshield1.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            sunshield1.RequiredItems.Add("Bronze", 25);
            sunshield1.RequiredItems.Add("Amber", 10);
            sunshield1.RequiredItems.Add("BronzeNails", 20);
            sunshield1.RequiredItems.Add("TitaniteShard", 10);
            sunshield1.RequiredUpgradeItems.Add("TitaniteShard", 1);
            sunshield1.ApplyUpgradeMap("Standard");

            Item DrakeSword = new("souls", "DrakeSword", "assets");
            DrakeSword.Name.English("Drake Sword"); // You can use this to fix the display name in code
            DrakeSword.Description.English("This sword, one of the rare dragon weapons, is formed by a drake's tail. Drakes are seen as undeveloped imitators of the dragons, but they are likely their distant kin.\r\nThe sword is imbued with a mystical power, to be released when held with both hands.");
            DrakeSword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DrakeSword.RequiredItems.Add("TitaniteShard", 20);
            DrakeSword.RequiredItems.Add("Stone", 20);
            DrakeSword.RequiredItems.Add("Wood", 40);
            DrakeSword.RequiredItems.Add("Flint", 1);
            DrakeSword.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DrakeSword.ApplyUpgradeMap("Standard");

            Item GolemAxe = new("souls", "GolemAxe", "assets");
            GolemAxe.Name.English("Golem Axe"); // You can use this to fix the display name in code
            GolemAxe.Description.English("This sword, one of the rare dragon weapons, is formed by a drake's tail. Drakes are seen as undeveloped imitators of the dragons, but they are likely their distant kin.\r\nThe sword is imbued with a mystical power, to be released when held with both hands.");
            GolemAxe.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            GolemAxe.RequiredItems.Add("TitaniteShard", 20);
            GolemAxe.RequiredItems.Add("Stone", 20);
            GolemAxe.RequiredItems.Add("Wood", 40);
            GolemAxe.RequiredItems.Add("Flint", 1);
            GolemAxe.RequiredUpgradeItems.Add("TitaniteShard", 1);
            GolemAxe.ApplyUpgradeMap("Standard");

            Item SunlightSword = new("souls", "SunlightSword", "assets");
            SunlightSword.Name.English("Sunlight StraightSword"); // You can use this to fix the display name in code
            SunlightSword.Description.English("This standard longsword, belonging to Solaire of Astora, is of high quality, is well-forged, and has been kept in good repair. Easy to use and dependable, but unlikely to live up to its grandiose name.");
            SunlightSword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            SunlightSword.RequiredItems.Add("TitaniteShard", 10);
            SunlightSword.RequiredItems.Add("FineWood", 10);
            SunlightSword.RequiredItems.Add("TrophyGreydwarf", 5);
            SunlightSword.RequiredItems.Add("Bronze", 25);
            SunlightSword.ApplyUpgradeMap("Standard");
            GameObject sunlightAssetPrefab = asset.LoadAsset<GameObject>("SunlightSword");
            LogSunlightSwordStats("asset bundle raw", sunlightAssetPrefab);

            Item ChannelerTrident = new("souls", "ChannelerTrident", "assets");
            ChannelerTrident.Name.English("Channeler Trident"); // You can use this to fix the display name in code
            ChannelerTrident.Description.English("Trident of the Six-eyed Channelers, sorcerers who serve Seath the Scaleless in collecting human specimens. Thrusted in circular motions in a unique martial arts dance that stirs nearby allies into a bloodthirsty frenzy.");
            ChannelerTrident.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            ChannelerTrident.RequiredItems.Add("Bronze", 25);
            ChannelerTrident.RequiredItems.Add("TitaniteShard", 5);
            ChannelerTrident.RequiredItems.Add("Tin", 20);
            ChannelerTrident.RequiredItems.Add("GreydwarfEye", 20);
            ChannelerTrident.RequiredUpgradeItems.Add("TitaniteShard", 1);
            ChannelerTrident.ApplyUpgradeMap("Standard");

            Item Murakumo = new("souls", "Murakumo", "assets");
            Murakumo.Name.English("Murakumo"); // You can use this to fix the display name in code
            Murakumo.Description.English("Trident of the Six-eyed Channelers, sorcerers who serve Seath the Scaleless in collecting human specimens. Thrusted in circular motions in a unique martial arts dance that stirs nearby allies into a bloodthirsty frenzy.");
            Murakumo.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            Murakumo.RequiredItems.Add("Bronze", 25);
            Murakumo.RequiredItems.Add("TitaniteShard", 5);
            Murakumo.RequiredItems.Add("Tin", 20);
            Murakumo.RequiredItems.Add("GreydwarfEye", 20);
            Murakumo.RequiredUpgradeItems.Add("TitaniteShard", 1);
            Murakumo.ApplyUpgradeMap("Standard");
            Debug.Log("[PungusSouls] AFTER Tier1");
            #endregion Tier 1 (Forest)
            #region Tier 2 (Swamps)

            Item BerserkGreatsword = new("souls", "BerserkGreatsword", "assets");
            BerserkGreatsword.Name.English("Berserk Greatsword"); // You can use this to fix the display name in code
            BerserkGreatsword.Description.English("A huge hunk of metal");
            BerserkGreatsword.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            BerserkGreatsword.RequiredItems.Add("Iron", 20);
            BerserkGreatsword.RequiredItems.Add("RoundLog", 10);
            BerserkGreatsword.RequiredItems.Add("LargeTitaniteShard", 20);
            BerserkGreatsword.RequiredItems.Add("TrophyGreydwarfBrute", 1);
            BerserkGreatsword.RequiredUpgradeItems.Add("TitaniteShard", 51);
            BerserkGreatsword.ApplyUpgradeMap("Tier2");

            Item Grant = new("souls", "Grant", "assets");
            Grant.Name.English("Berserk Greatsword"); // You can use this to fix the display name in code
            Grant.Description.English("A huge hunk of metal");
            Grant.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            Grant.RequiredItems.Add("Iron", 20);
            Grant.RequiredItems.Add("RoundLog", 10);
            Grant.RequiredItems.Add("LargeTitaniteShard", 20);
            Grant.RequiredItems.Add("TrophyGreydwarfBrute", 1);
            Grant.RequiredUpgradeItems.Add("TitaniteShard", 51);
            Grant.ApplyUpgradeMap("Tier2");

            Item CanvasTalisman = new("souls", "CanvasTalisman");
            CanvasTalisman.Name.English("Canvas Talisman");
            CanvasTalisman.Description.English("CanvasTalisman.");
            CanvasTalisman.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            CanvasTalisman.RequiredItems.Add("GoldTracer", 1);
            CanvasTalisman.RequiredItems.Add("DarkSilverTracer", 1);
            CanvasTalisman.RequiredItems.Add("Eitr", 40);
            CanvasTalisman.RequiredItems.Add("Silver", 20);
            CanvasTalisman.RequiredUpgradeItems.Add("TitaniteShard", 1);
            CanvasTalisman.ApplyUpgradeMap("Tier2");

            Item GrassCrestShield = new("souls", "GrassCrestShield", "assets");
            GrassCrestShield.Name.English("Grass-Crest Shield"); // You can use this to fix the display name in code
            GrassCrestShield.Description.English("Old medium metal shield of unknown origin. The grass crest is lightly imbued with magic, which slightly speeds stamina recovery.");
            GrassCrestShield.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            GrassCrestShield.RequiredItems.Add("Dandelion", 25);
            GrassCrestShield.RequiredItems.Add("FineWood", 20);
            GrassCrestShield.RequiredItems.Add("Resin", 15);
            GrassCrestShield.RequiredItems.Add("LargeTitaniteShard", 20);
            GrassCrestShield.RequiredUpgradeItems.Add("TitaniteShard", 1);
            GrassCrestShield.ApplyUpgradeMap("Tier2");

                Item MLHorn = new("souls", "MLHorn", "assets");
            MLHorn.Name.English("Moonlight Butterfly Horn"); // You can use this to fix the display name in code
            MLHorn.Description.English("Weapon born from the mystical creature of the Darkroot Garden, the Moonlight Butterfly. The horns of the butterfly, a being created by Seath, are imbued with a pure magic power.");
            MLHorn.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            MLHorn.RequiredItems.Add("AncientSeed", 30);
            MLHorn.RequiredItems.Add("LargeTitaniteShard", 10);
            MLHorn.RequiredItems.Add("GreydwarfEye", 20);
            MLHorn.RequiredItems.Add("ElderBark", 10);
            MLHorn.RequiredUpgradeItems.Add("TitaniteShard", 1);
            MLHorn.ApplyUpgradeMap("Tier2");

                Item Shotel = new("souls", "Shotel", "assets");
            Shotel.Name.English("Gold Tracer"); // You can use this to fix the display name in code
            Shotel.Description.English("Curved sword used by the Lord's Blade Ciaran, one of Gwyn's Four Knights. Ciaran brandishes her sword in a mesmerizing dance, etching the darkness with dire streaks of gold.");
            Shotel.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            Shotel.RequiredItems.Add("Iron", 40);
            Shotel.RequiredItems.Add("Coins", 99);
            Shotel.RequiredItems.Add("Ruby", 40);
            Shotel.RequiredItems.Add("LargeTitaniteShard", 20);
            Shotel.RequiredUpgradeItems.Add("TitaniteShard", 1);
            Shotel.ApplyUpgradeMap("Tier2");


            Item DarkMoonBow = new("souls", "DarkMoonBow", "assets");
            DarkMoonBow.Name.English("Darkmoon Bow"); // You can use this to fix the display name in code
            DarkMoonBow.Description.English("Bow born from the soul of the Dark Sun Gwyndolin, Darkmoon deity who watches over the abandoned city of the Gods, Anor Londo. This golden bow is imbued with powerful magic and is most impressive with Moonlight Arrows.");
            DarkMoonBow.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            DarkMoonBow.RequiredItems.Add("FineWood", 20);
            DarkMoonBow.RequiredItems.Add("Iron", 20);
            DarkMoonBow.RequiredItems.Add("Guck", 10);
            DarkMoonBow.RequiredItems.Add("LargeTitaniteShard", 20);
            DarkMoonBow.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DarkMoonBow.ApplyUpgradeMap("Tier2");

            Item MaskOfFather = new("souls", "MaskOfFather", "assets");
            MaskOfFather.Name.English("Mask of the Father"); // You can use this to fix the display name in code
            MaskOfFather.Description.English("One of the three masks of the Pinwheel, the necromancer who stole the power of the Gravelord, and reigns over the Catacombs. This mask, belonging to the valiant father, slightly raises equipment load");
            MaskOfFather.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            MaskOfFather.RequiredItems.Add("FineWood", 10);
            MaskOfFather.RequiredItems.Add("DeerHide", 20);
            MaskOfFather.RequiredItems.Add("LargeTitaniteShard", 20);
            MaskOfFather.RequiredItems.Add("Bronze", 20);
            MaskOfFather.RequiredUpgradeItems.Add("TitaniteShard", 1);
            MaskOfFather.ApplyUpgradeMap("Tier2");

            Item DragonKingGreatAxe = new("souls", "DragonKingGreatAxe", "assets");
            DragonKingGreatAxe.Name.English("Dragon King GreatAxe"); // You can use this to fix the display name in code
            DragonKingGreatAxe.Description.English("This axe, one of the rare dragon weapons, is formed by the tail of the Gaping Dragon, a distant, deformed descendant of the everlasting dragons.");
            DragonKingGreatAxe.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            DragonKingGreatAxe.RequiredItems.Add("Stone", 80);
            DragonKingGreatAxe.RequiredItems.Add("LargeTitaniteShard", 20);
            DragonKingGreatAxe.RequiredItems.Add("Iron", 20);
            DragonKingGreatAxe.RequiredItems.Add("RoundLog", 10);
            DragonKingGreatAxe.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DragonKingGreatAxe.ApplyUpgradeMap("Tier2");

            Item DemonAxe = new("souls", "DemonAxe", "assets");
            DemonAxe.Name.English("Demon Axe"); // You can use this to fix the display name in code
            DemonAxe.Description.English("This axe, one of the rare dragon weapons, is formed by the tail of the Gaping Dragon, a distant, deformed descendant of the everlasting dragons.");
            DemonAxe.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            DemonAxe.RequiredItems.Add("Stone", 80);
            DemonAxe.RequiredItems.Add("LargeTitaniteShard", 20);
            DemonAxe.RequiredItems.Add("Iron", 20);
            DemonAxe.RequiredItems.Add("RoundLog", 10);
            DemonAxe.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DemonAxe.ApplyUpgradeMap("Tier2");

            Item RicardRapier = new("souls", "RicardRapier", "assets");
            RicardRapier.Name.English("Ricards Rapier"); // You can use this to fix the display name in code
            RicardRapier.Description.English("");
            RicardRapier.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            RicardRapier.RequiredItems.Add("Stone", 80);
            RicardRapier.RequiredItems.Add("LargeTitaniteShard", 20);
            RicardRapier.RequiredItems.Add("Iron", 20);
            RicardRapier.RequiredItems.Add("RoundLog", 10);
            RicardRapier.RequiredUpgradeItems.Add("TitaniteShard", 1);
            RicardRapier.ApplyUpgradeMap("Tier2");
            Debug.Log("[PungusSouls] AFTER Tier2");
            #endregion Tier 2 (Swamps)
            #region Tier 3 (Mountains)
            Item BlackIronShield = new("souls", "BlackIronShield", "assets");
            BlackIronShield.Name.English("Black Iron GreatShield"); // You can use this to fix the display name in code
            BlackIronShield.Description.English("Greatshield of the might knight Tarkus. Built of special black iron and even heavier than Knight Berenike's tower shield. Especially resistant to fire attacks and effective for shield bashing.");
            BlackIronShield.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            BlackIronShield.RequiredItems.Add("BlackMetal", 40);
            BlackIronShield.RequiredItems.Add("Eitr", 10);
            BlackIronShield.RequiredItems.Add("Wood", 40);
            BlackIronShield.RequiredItems.Add("TitaniteSlab", 10);
            BlackIronShield.RequiredUpgradeItems.Add("TitaniteShard", 1);
            BlackIronShield.ApplyUpgradeMap("Tier3");

            Item DragonGreatSword = new("souls", "DragonGreatSword", "assets");
            DragonGreatSword.Name.English("Dragon GreatSword"); // You can use this to fix the display name in code
            DragonGreatSword.Description.English("This sword, one of the rare dragon weapons, came from the tail of the stone dragon of Ash Lake, descendant of the ancient dragons");
            DragonGreatSword.Crafting.Add("BlacksmithAltar", 3); // Custom crafting stations can be specified as a string
            DragonGreatSword.RequiredItems.Add("YmirRemains", 30);
            DragonGreatSword.RequiredItems.Add("TrophyDragonQueen", 2);
            DragonGreatSword.RequiredItems.Add("Silver", 40);
            DragonGreatSword.RequiredItems.Add("TitaniteChunk", 20);
            DragonGreatSword.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DragonGreatSword.ApplyUpgradeMap("Tier3");

            Item BowWant = new("souls", "BowWant", "assets");
            BowWant.Name.English("Bow of Want"); // You can use this to fix the display name in code
            BowWant.Description.English("Repeating crossbow cherished by the weapon craftsman Eidas. Its elaborate design makes it closer to a work of art than a weapon. Intricate mechanism makes heavy damage possible through triple-shot firing of bolts. but in fact each bolt inflicts less damage");
            BowWant.Crafting.Add("BlacksmithAltar", 3); // Custom crafting stations can be specified as a string
            BowWant.RequiredItems.Add("Wood", 20);
            BowWant.RequiredItems.Add("Silver", 10);
            BowWant.RequiredItems.Add("TitaniteChunk", 20);
            BowWant.RequiredItems.Add("Root", 8);
            BowWant.RequiredUpgradeItems.Add("TitaniteShard", 1);
            BowWant.ApplyUpgradeMap("Tier3");

            Item Avelyn = new("souls", "Avelyn", "assets");
            Avelyn.Name.English("Avelyn"); // You can use this to fix the display name in code
            Avelyn.Description.English("Repeating crossbow cherished by the weapon craftsman Eidas. Its elaborate design makes it closer to a work of art than a weapon. Intricate mechanism makes heavy damage possible through triple-shot firing of bolts. but in fact each bolt inflicts less damage");
            Avelyn.Crafting.Add("BlacksmithAltar", 3); // Custom crafting stations can be specified as a string
            Avelyn.RequiredItems.Add("Wood", 20);
            Avelyn.RequiredItems.Add("Silver", 10);
            Avelyn.RequiredItems.Add("TitaniteChunk", 20);
            Avelyn.RequiredItems.Add("Root", 8);
             Avelyn.RequiredUpgradeItems.Add("TitaniteShard", 1);
            Avelyn.ApplyUpgradeMap("Tier3");

            Item GoldTracer = new("souls", "GoldTracer", "assets");
            GoldTracer.Name.English("Gold Tracer"); // You can use this to fix the display name in code
            GoldTracer.Description.English("Curved sword used by the Lord's Blade Ciaran, one of Gwyn's Four Knights. Ciaran brandishes her sword in a mesmerizing dance, etching the darkness with dire streaks of gold.");
            GoldTracer.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            GoldTracer.RequiredItems.Add("Iron", 40);
            GoldTracer.RequiredItems.Add("Coins", 99);
            GoldTracer.RequiredItems.Add("Ruby", 40);
            GoldTracer.RequiredItems.Add("TitaniteChunk", 20);
            GoldTracer.RequiredUpgradeItems.Add("TitaniteShard", 1);
            GoldTracer.ApplyUpgradeMap("Tier3");

            Item DaggerPrisc = new("souls", "DaggerPrisc", "assets");
            DaggerPrisc.Name.English("Priscillas Dagger"); // You can use this to fix the display name in code
            DaggerPrisc.Description.English("This sword, one of the rare dragon weapons, came from the tail of Priscilla, the Dragon Crossbreed in the painted world of Ariamis.\r\nPossessing the power of lifehunt, it dances about when wielded, in a fashion reminiscent of the white-robed painting guardians.");
            DaggerPrisc.Crafting.Add("BlacksmithAltar", 3); // Custom crafting stations can be specified as a string
            DaggerPrisc.RequiredItems.Add("TitaniteChunk", 20);
            DaggerPrisc.RequiredItems.Add("Bloodbag", 5);
            DaggerPrisc.RequiredItems.Add("Obsidian", 20);
            DaggerPrisc.RequiredItems.Add("Silver", 20);
            DaggerPrisc.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DaggerPrisc.ApplyUpgradeMap("Tier3");

                Item Glordsword = new("souls", "Glordsword", "assets");
            Glordsword.Name.English("GraveLord Sword"); // You can use this to fix the display name in code
            Glordsword.Description.English("Sword wielded only by servants of Gravelord Nito, the first of the dead. Crafted from the bones of the fallen. The miasma of death exudes from the sword, a veritable toxin to any living being.");
            Glordsword.Crafting.Add("BlacksmithAltar", 3); // Custom crafting stations can be specified as a string
            Glordsword.RequiredItems.Add("WitheredBone", 30);
            Glordsword.RequiredItems.Add("TitaniteChunk", 20);
            Glordsword.RequiredItems.Add("Ooze", 20);
            Glordsword.RequiredItems.Add("WolfClaw", 20);
            Glordsword.RequiredUpgradeItems.Add("TitaniteShard", 1);
            Glordsword.ApplyUpgradeMap("Tier3");

                Item ManusCatalyst = new("souls", "ManusCatalyst", "assets");
            ManusCatalyst.Name.English("Manus Catalyst"); // You can use this to fix the display name in code
            ManusCatalyst.Description.English("A sorcery catalyst born from the soul of Manus, Father of the Abyss. A rough, old wooden catalyst large enough to be used as a strike weapon. Similar to the Tin Crystallization Catalyst, it boosts the strength of sorceries, but limits the number of castings");
            ManusCatalyst.Crafting.Add("BlacksmithAltar", 3); // Custom crafting stations can be specified as a string
            ManusCatalyst.RequiredItems.Add("FineWood", 40);
            ManusCatalyst.RequiredItems.Add("YmirRemains", 5);
            ManusCatalyst.RequiredItems.Add("SurtlingCore", 10);
            ManusCatalyst.RequiredItems.Add("TitaniteChunk", 20);
            ManusCatalyst.RequiredUpgradeItems.Add("TitaniteShard", 1);
            ManusCatalyst.ApplyUpgradeMap("Tier3");

                Item FurySword = new("souls", "FurySword", "assets");
            FurySword.Name.English("Quelags Fury Sword"); // You can use this to fix the display name in code
            FurySword.Description.English("A curved sword born from the soul of Quelaag, daughter of the Witch of Izalith, who was transformed into a chaos demon. Like Quelaag's body, the sword features shells, spikes, humanity and a coating of chaos fire.");
            FurySword.Crafting.Add("BlacksmithAltar", 3); // Custom crafting stations can be specified as a string
            FurySword.RequiredItems.Add("Silver", 20);
            FurySword.RequiredItems.Add("SurtlingCore", 10);
            FurySword.RequiredItems.Add("Obsidian", 40);
            FurySword.RequiredItems.Add("TitaniteChunk", 20);
            FurySword.RequiredUpgradeItems.Add("TitaniteShard", 51);
            FurySword.ApplyUpgradeMap("Tier3");

                Item DemonGreatHammer = new("souls", "DemonGreatHammer", "assets");
            DemonGreatHammer.Name.English("Demon Great Hammer"); // You can use this to fix the display name in code
            DemonGreatHammer.Description.English("Demon weapon built from the stone archtrees. Used by lesser demons at North Undead Asylum. This hammer is imbued with no special power, but will merrily beat foes to a pulp, provided you have the strength to wield it.");
            DemonGreatHammer.Crafting.Add("BlacksmithAltar", 3); // Custom crafting stations can be specified as a string
            DemonGreatHammer.RequiredItems.Add("Stone", 120);
            DemonGreatHammer.RequiredItems.Add("YmirRemains", 20);
            DemonGreatHammer.RequiredItems.Add("TitaniteChunk", 20);
            DemonGreatHammer.RequiredItems.Add("Iron", 20);
            DemonGreatHammer.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DemonGreatHammer.ApplyUpgradeMap("Tier3");

            Item DemonMace = new("souls", "DemonMace", "assets");
            DemonMace.Name.English("Demon King Hammer"); // You can use this to fix the display name in code
            DemonMace.Description.English("Demon weapon built from the stone archtrees. Used by lesser demons at North Undead Asylum. This hammer is imbued with no special power, but will merrily beat foes to a pulp, provided you have the strength to wield it.");
            DemonMace.Crafting.Add("BlacksmithAltar", 3); // Custom crafting stations can be specified as a string
            DemonMace.RequiredItems.Add("Stone", 120);
            DemonMace.RequiredItems.Add("YmirRemains", 20);
            DemonMace.RequiredItems.Add("TitaniteChunk", 20);
            DemonMace.RequiredItems.Add("Iron", 20);
            DemonMace.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DemonMace.ApplyUpgradeMap("Tier3");

            Item AxeEleonora = new("souls", "AxeEleonora", "assets");
            AxeEleonora.Name.English("Eleonora"); // You can use this to fix the display name in code
            AxeEleonora.Description.English("Demon weapon built from the stone archtrees. Used by lesser demons at North Undead Asylum. This hammer is imbued with no special power, but will merrily beat foes to a pulp, provided you have the strength to wield it.");
            AxeEleonora.Crafting.Add("BlacksmithAltar", 3); // Custom crafting stations can be specified as a string
            AxeEleonora.RequiredItems.Add("Stone", 120);
            AxeEleonora.RequiredItems.Add("YmirRemains", 20);
            AxeEleonora.RequiredItems.Add("TitaniteChunk", 20);
            AxeEleonora.RequiredItems.Add("Iron", 20);
            AxeEleonora.RequiredUpgradeItems.Add("TitaniteShard", 1);
            AxeEleonora.ApplyUpgradeMap("Tier3");

            Item SilverKnightShield = new("souls", "SilverKnightShield");
            SilverKnightShield.Name.English("Silver Knight Shield"); // You can use this to fix the display name in code
            SilverKnightShield.Description.English("Demon weapon built from the stone archtrees. Used by lesser demons at North Undead Asylum. This hammer is imbued with no special power, but will merrily beat foes to a pulp, provided you have the strength to wield it.");
            SilverKnightShield.Crafting.Add("BlacksmithAltar", 3); // Custom crafting stations can be specified as a string
            SilverKnightShield.RequiredItems.Add("Stone", 120);
            SilverKnightShield.RequiredItems.Add("YmirRemains", 20);
            SilverKnightShield.RequiredItems.Add("TitaniteChunk", 20);
            SilverKnightShield.RequiredItems.Add("Silver", 20);
            SilverKnightShield.RequiredUpgradeItems.Add("TitaniteShard", 1);
            SilverKnightShield.ApplyUpgradeMap("Tier3");

            Item SilverKnightSpear = new("souls", "SilverKnightSpear");
            SilverKnightSpear.Name.English("Silver Knight Spear"); // You can use this to fix the display name in code
            SilverKnightSpear.Description.English("Demon weapon built from the stone archtrees. Used by lesser demons at North Undead Asylum. This hammer is imbued with no special power, but will merrily beat foes to a pulp, provided you have the strength to wield it.");
            SilverKnightSpear.Crafting.Add("BlacksmithAltar", 3); // Custom crafting stations can be specified as a string
            SilverKnightSpear.RequiredItems.Add("Stone", 120);
            SilverKnightSpear.RequiredItems.Add("YmirRemains", 20);
            SilverKnightSpear.RequiredItems.Add("TitaniteChunk", 20);
            SilverKnightSpear.RequiredItems.Add("Silver", 20);
            SilverKnightSpear.RequiredUpgradeItems.Add("TitaniteShard", 1);
            SilverKnightSpear.ApplyUpgradeMap("Tier3");

            Item SilverKnightSword = new("souls", "SilverKnightSword");
            SilverKnightSword.Name.English("Silver Knight Sword"); // You can use this to fix the display name in code
            SilverKnightSword.Description.English("Demon weapon built from the stone archtrees. Used by lesser demons at North Undead Asylum. This hammer is imbued with no special power, but will merrily beat foes to a pulp, provided you have the strength to wield it.");
            SilverKnightSword.Crafting.Add("BlacksmithAltar", 3); // Custom crafting stations can be specified as a string
            SilverKnightSword.RequiredItems.Add("Stone", 120);
            SilverKnightSword.RequiredItems.Add("YmirRemains", 20);
            SilverKnightSword.RequiredItems.Add("TitaniteChunk", 20);
            SilverKnightSword.RequiredItems.Add("Silver", 20);
            SilverKnightSword.RequiredUpgradeItems.Add("TitaniteShard", 1);
            SilverKnightSword.ApplyUpgradeMap("Tier3");
            Debug.Log("[PungusSouls] AFTER tier3");
            #endregion Tier 3 (Mountains)
            #region Tier 4 (Plains)

            Item HavelGreatShield = new("souls", "HavelGreatShield", "assets");
            HavelGreatShield.Name.English("Havels GreatShield"); // You can use this to fix the display name in code
            HavelGreatShield.Description.English("Greatshield of the legendary Havel the Rock. Cut straight from a great slab of stone. This greatshield is imbued with the magic of Havel, proves a strong defense, and is incredibly heavy. A true divine heirloom on par with the Dragon tooth");
            HavelGreatShield.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            HavelGreatShield.RequiredItems.Add("Stone", 80);
            HavelGreatShield.RequiredItems.Add("BlackMetal", 20);
            HavelGreatShield.RequiredItems.Add("LinenThread", 15);
            HavelGreatShield.RequiredItems.Add("TitaniteSlab", 10);
            HavelGreatShield.RequiredUpgradeItems.Add("TitaniteShard", 1);
             HavelGreatShield.ApplyUpgradeMap("Tier4");

            Item DarkSilverTracer = new("souls", "DarkSilverTracer", "assets");
            DarkSilverTracer.Name.English("Dark Silver Tracer"); // You can use this to fix the display name in code
            DarkSilverTracer.Description.English("A dark silver dagger used by the Lord's Blade Ciaran, of Gwyn's Four Knights. The victim is first distracted by dazzling streaks of the Gold Tracer, then stung by the vicious poison of this dagger");
            DarkSilverTracer.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            DarkSilverTracer.RequiredItems.Add("TitaniteSlab", 10);
            DarkSilverTracer.RequiredItems.Add("Ooze", 20);
            DarkSilverTracer.RequiredItems.Add("BlackMetal", 5);
            DarkSilverTracer.RequiredItems.Add("Needle", 5);
            DarkSilverTracer.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DarkSilverTracer.ApplyUpgradeMap("Tier4");

            Item DarkSword = new("souls", "DarkSword", "assets");
            DarkSword.Name.English("Dark Sword"); // You can use this to fix the display name in code
            DarkSword.Description.English("A dark silver dagger used by the Lord's Blade Ciaran, of Gwyn's Four Knights. The victim is first distracted by dazzling streaks of the Gold Tracer, then stung by the vicious poison of this dagger");
            DarkSword.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            DarkSword.RequiredItems.Add("TitaniteSlab", 10);
            DarkSword.RequiredItems.Add("Ooze", 20);
            DarkSword.RequiredItems.Add("BlackMetal", 5);
            DarkSword.RequiredItems.Add("Needle", 5);
            DarkSword.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DarkSword.ApplyUpgradeMap("Tier4");

            Item DragonSlayerGreatBow = new("souls", "DragonSlayerGreatBow", "assets");
            DragonSlayerGreatBow.Name.English("DragonSlayer GreatBow"); // You can use this to fix the display name in code
            DragonSlayerGreatBow.Description.English("Bow of the Dragonslayers, led by Hawkeye Gough, one of Gwyn's Four Knights. This bow's unusual size requires that it be anchored to the ground when fired. Only uses specialized great arrows.");
            DragonSlayerGreatBow.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            DragonSlayerGreatBow.RequiredItems.Add("BlackMetal", 20);
            DragonSlayerGreatBow.RequiredItems.Add("TitaniteSlab", 20);
            DragonSlayerGreatBow.RequiredItems.Add("LinenThread", 10);
            DragonSlayerGreatBow.RequiredItems.Add("Thunderstone", 10);
            DragonSlayerGreatBow.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DragonSlayerGreatBow.ApplyUpgradeMap("Tier4");

            Item BowDragonrider = new("souls", "BowDragonrider", "assets");
            BowDragonrider.Name.English("Dragonrider Bow"); // You can use this to fix the display name in code
            BowDragonrider.Description.English("Bow of the Dragonslayers, led by Hawkeye Gough, one of Gwyn's Four Knights. This bow's unusual size requires that it be anchored to the ground when fired. Only uses specialized great arrows.");
            BowDragonrider.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            BowDragonrider.RequiredItems.Add("BlackMetal", 20);
            BowDragonrider.RequiredItems.Add("TitaniteSlab", 20);
            BowDragonrider.RequiredItems.Add("LinenThread", 10);
            BowDragonrider.RequiredItems.Add("Thunderstone", 10);
            BowDragonrider.RequiredUpgradeItems.Add("TitaniteShard", 1);
            BowDragonrider.ApplyUpgradeMap("Tier4");

            Item DragonSlayerSpear = new("souls", "DragonSlayerSpear", "assets");
            DragonSlayerSpear.Name.English("DragonSlayer Spear"); // You can use this to fix the display name in code
            DragonSlayerSpear.Description.English("Cross spear born from the soul of Ornstein, a Dragonslayer guarding Anor Londo cathedral. Inflicts lightning damage; effective against dragons. Two-handed thrust relies on cross and buries deep within a dragon's hide, and sends human foes flying.");
            DragonSlayerSpear.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            DragonSlayerSpear.RequiredItems.Add("BlackMetal", 40);
            DragonSlayerSpear.RequiredItems.Add("Thunderstone", 20);
            DragonSlayerSpear.RequiredItems.Add("Silver", 40);
            DragonSlayerSpear.RequiredItems.Add("TitaniteSlab", 20);
            DragonSlayerSpear.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DragonSlayerSpear.ApplyUpgradeMap("Tier4");

            Item AxeDragonslayer = new("souls", "AxeDragonslayer", "assets");
            AxeDragonslayer.Name.English("DragonSlayer Axe"); // You can use this to fix the display name in code
            AxeDragonslayer.Description.English("Cross spear born from the soul of Ornstein, a Dragonslayer guarding Anor Londo cathedral. Inflicts lightning damage; effective against dragons. Two-handed thrust relies on cross and buries deep within a dragon's hide, and sends human foes flying.");
            AxeDragonslayer.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            AxeDragonslayer.RequiredItems.Add("BlackMetal", 40);
            AxeDragonslayer.RequiredItems.Add("Thunderstone", 20);
            AxeDragonslayer.RequiredItems.Add("Silver", 40);
            AxeDragonslayer.RequiredItems.Add("TitaniteSlab", 20);
            AxeDragonslayer.RequiredUpgradeItems.Add("TitaniteShard", 1);
            AxeDragonslayer.ApplyUpgradeMap("Tier4");

            Item AbyssGreatsword = new("souls", "AbyssGreatsword", "assets");
            AbyssGreatsword.Name.English("Abyss Greatsword"); // You can use this to fix the display name in code
            AbyssGreatsword.Description.English("This greatsword belonged to Lord Gwyn's Knight Artorias, who fell to the Abyss. Swallowed by the Dark with its master, this sword is tainted by the Abyss, and now its strength reflects its wiGdKing's humanity.");
            AbyssGreatsword.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            AbyssGreatsword.RequiredItems.Add("Silver", 40);
            AbyssGreatsword.RequiredItems.Add("BlackMetal", 20);
            AbyssGreatsword.RequiredItems.Add("TitaniteSlab", 20);
            AbyssGreatsword.RequiredItems.Add("TrophyWolf", 1);
            DarkSilverTracer.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DarkSilverTracer.ApplyUpgradeMap("Tier4");

            Item TinDarkmoonCatalyst = new("souls", "TinDarkmoonCatalyst", "assets");
            TinDarkmoonCatalyst.Name.English("Tin Darkmoon Catalyst");
            TinDarkmoonCatalyst.Description.English("This greatsword belonged to Lord Gwyn's Knight Artorias, who fell to the Abyss. Swallowed by the Dark with its master, this sword is tainted by the Abyss, and now its strength reflects its wiGdKing's humanity.");
            TinDarkmoonCatalyst.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            TinDarkmoonCatalyst.RequiredItems.Add("Silver", 40);
            TinDarkmoonCatalyst.RequiredItems.Add("BlackMetal", 20);
            TinDarkmoonCatalyst.RequiredItems.Add("TitaniteSlab", 20);
            TinDarkmoonCatalyst.RequiredItems.Add("TrophyWolf", 1);
            TinDarkmoonCatalyst.RequiredUpgradeItems.Add("TitaniteShard", 1);
            TinDarkmoonCatalyst.ApplyUpgradeMap("Tier4");

            Item dragontooth = new("souls", "dragontooth", "assets");
            dragontooth.Name.English("Dragon Tooth"); // You can use this to fix the display name in code
            dragontooth.Description.English("Created from an everlasting dragon tooth. Legendary great hammer of Havel the Rock. The dragon tooth will never break as it is harder than stone, and it grants its wiGdKing resistance to magic and flame");
            dragontooth.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            dragontooth.RequiredItems.Add("TitaniteSlab", 20);
            dragontooth.RequiredItems.Add("Stone", 120);
            dragontooth.RequiredItems.Add("YmirRemains", 25);
            dragontooth.RequiredItems.Add("BoneFragments", 50);
            dragontooth.RequiredUpgradeItems.Add("TitaniteShard", 1);
            dragontooth.ApplyUpgradeMap("Tier4");

            Item MaceLedo = new("souls", "MaceLedo", "assets");
            MaceLedo.Name.English("Ledos Mace"); // You can use this to fix the display name in code
            MaceLedo.Description.English("Created from an everlasting dragon tooth. Legendary great hammer of Havel the Rock. The dragon tooth will never break as it is harder than stone, and it grants its wiGdKing resistance to magic and flame");
            MaceLedo.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            MaceLedo.RequiredItems.Add("TitaniteSlab", 20);
            MaceLedo.RequiredItems.Add("Stone", 120);
            MaceLedo.RequiredItems.Add("YmirRemains", 25);
            MaceLedo.RequiredItems.Add("BoneFragments", 50);
            MaceLedo.RequiredUpgradeItems.Add("TitaniteShard", 1);
            MaceLedo.ApplyUpgradeMap("Tier4");
            Debug.Log("[PungusSouls] AFTER Tier4");
            #endregion Tier 4
            #region Tier 5 (mistlands)

            Item BlackKnightGreatAxe = new("souls", "BlackKnightGreatAxe", "assets");
            BlackKnightGreatAxe.Name.English("Black Knight GreatAxe"); // You can use this to fix the display name in code
            BlackKnightGreatAxe.Description.English("Greataxe of the Black Knights who wander Lordran. Used to face Chaos demons. The large motion that puts the weight of the body into the attack reflects the great size of their adversaries long ago.");
            BlackKnightGreatAxe.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            BlackKnightGreatAxe.RequiredItems.Add("BlackMetal", 20);
            BlackKnightGreatAxe.RequiredItems.Add("Eitr", 10);
            BlackKnightGreatAxe.RequiredItems.Add("Silver", 20);
            BlackKnightGreatAxe.RequiredItems.Add("TitaniteSlab", 20);
            BlackKnightGreatAxe.RequiredUpgradeItems.Add("TitaniteShard", 1);
            BlackKnightGreatAxe.ApplyUpgradeMap("Tier5");

                Item BlackKnightHalberd = new("souls", "BlackKnightHalberd", "assets");
            BlackKnightHalberd.Name.English("Black Knight Halberd"); // You can use this to fix the display name in code
            BlackKnightHalberd.Description.English("Halberd of the black knights who wander Lordran. Used to face chaos demons.");
            BlackKnightHalberd.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            BlackKnightHalberd.RequiredItems.Add("BlackMetal", 20);
            BlackKnightHalberd.RequiredItems.Add("Eitr", 10);
            BlackKnightHalberd.RequiredItems.Add("Silver", 20); 
            BlackKnightHalberd.RequiredItems.Add("TitaniteSlab", 10);
                BlackKnightHalberd.RequiredUpgradeItems.Add("TitaniteShard", 1);
                BlackKnightHalberd.ApplyUpgradeMap("Tier5");

                Item BlackKnightSword = new("souls", "BlackKnightSword", "assets");
            BlackKnightSword.Name.English("Black Knight Sword"); // You can use this to fix the display name in code
            BlackKnightSword.Description.English("sword of the Black Knights who wander Lordran. Used to face chaos demons. The Large motion that puts the weight of the body into the attack reflects the great size of their adversaries long ago.");
            BlackKnightSword.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            BlackKnightSword.RequiredItems.Add("BlackMetal", 20);
            BlackKnightSword.RequiredItems.Add("Eitr", 5);
            BlackKnightSword.RequiredItems.Add("Silver", 20);
            BlackKnightSword.RequiredItems.Add("TitaniteSlab", 5);
            BlackKnightSword.RequiredUpgradeItems.Add("TitaniteShard", 1);
                BlackKnightSword.ApplyUpgradeMap("Tier5");

                Item BlackKnightShield = new("souls", "BlackKnightShield", "assets");
            BlackKnightShield.Name.English("Black Knight Shield"); // You can use this to fix the display name in code
            BlackKnightShield.Description.English("Shield of the Black Knights that wander Lordan. A flowing canal is chiseled deeply into its face. Long ago, the black knights faced the chaos demons, and were charred black, but their shields became highly resistant to fire.");
            BlackKnightShield.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            BlackKnightShield.RequiredItems.Add("BlackMetal", 20);
            BlackKnightShield.RequiredItems.Add("Eitr", 5);
            BlackKnightShield.RequiredItems.Add("Silver", 20);
            BlackKnightShield.RequiredItems.Add("TitaniteSlab", 5);
            BlackKnightShield.RequiredUpgradeItems.Add("TitaniteShard", 1);
                BlackKnightShield.ApplyUpgradeMap("Tier5");

                Item BlackKnightUGS = new("souls", "BlackKnightUGS", "assets");
            BlackKnightUGS.Name.English("Black Knight Greatsword"); // You can use this to fix the display name in code
            BlackKnightUGS.Description.English("Greatsword of the black knights who wander Lordran. Used to face chaos demons. The large motion that puts the weight of the body into the attack reflects the great size of their adversaries long ago.");
            BlackKnightUGS.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            BlackKnightUGS.RequiredItems.Add("BlackMetal", 40);
            BlackKnightUGS.RequiredItems.Add("Eitr", 10);
            BlackKnightUGS.RequiredItems.Add("Silver", 40);
            BlackKnightUGS.RequiredItems.Add("TitaniteSlab", 10);
            BlackKnightUGS.RequiredUpgradeItems.Add("TitaniteShard", 1);
            BlackKnightUGS.ApplyUpgradeMap("Tier5");

            Item GoldSilverTracers = new("souls", "GoldSilverTracers");
            GoldSilverTracers.Name.English("Gold & Silver Tracers"); // You can use this to fix the display name in code
            GoldSilverTracers.Description.English("Dual Weapons used by the Lord's Blade Ciaran, one of Gwyn's Four Knights.");
            GoldSilverTracers.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            GoldSilverTracers.RequiredItems.Add("GoldTracer", 1);
            GoldSilverTracers.RequiredItems.Add("DarkSilverTracer", 1);
            GoldSilverTracers.RequiredItems.Add("Eitr", 40);
            GoldSilverTracers.RequiredItems.Add("Silver", 20);
            GoldSilverTracers.RequiredUpgradeItems.Add("TitaniteShard", 1);
            GoldSilverTracers.ApplyUpgradeMap("Tier5");

                Item HollowSlayer = new("souls", "HollowSlayer");
            HollowSlayer.Name.English("HollowSlayer"); // You can use this to fix the display name in code
            HollowSlayer.Description.English("Dual Weapons used by the Lord's Blade Ciaran, one of Gwyn's Four Knights.");
            HollowSlayer.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            HollowSlayer.RequiredItems.Add("GoldTracer", 1);
            HollowSlayer.RequiredItems.Add("DarkSilverTracer", 1);
            HollowSlayer.RequiredItems.Add("Eitr", 40);
            HollowSlayer.RequiredItems.Add("Silver", 20);
            HollowSlayer.RequiredUpgradeItems.Add("TitaniteShard", 1);
            HollowSlayer.ApplyUpgradeMap("Tier5");

            Item BowGough = new("souls", "BowGough");
            BowGough.Name.English("Goughs Greatbow"); // You can use this to fix the display name in code
            BowGough.Description.English("Dual Weapons used by the Lord's Blade Ciaran, one of Gwyn's Four Knights.");
            BowGough.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            BowGough.RequiredItems.Add("GoldTracer", 1);
            BowGough.RequiredItems.Add("DarkSilverTracer", 1);
            BowGough.RequiredItems.Add("Eitr", 40);
            BowGough.RequiredItems.Add("Silver", 20);
            BowGough.RequiredUpgradeItems.Add("TitaniteShard", 1);
            BowGough.ApplyUpgradeMap("Tier5");

            Item MaceMorne = new("souls", "MaceMorne");
            MaceMorne.Name.English("Mornes Hammer"); // You can use this to fix the display name in code
            MaceMorne.Description.English("Dual Weapons used by the Lord's Blade Ciaran, one of Gwyn's Four Knights.");
            MaceMorne.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            MaceMorne.RequiredItems.Add("GoldTracer", 1);
            MaceMorne.RequiredItems.Add("DarkSilverTracer", 1);
            MaceMorne.RequiredItems.Add("Eitr", 40);
            MaceMorne.RequiredItems.Add("Silver", 20);
            MaceMorne.RequiredUpgradeItems.Add("TitaniteShard", 1);
            MaceMorne.ApplyUpgradeMap("Tier5");

            Item golemaxedual = new("souls", "golemaxedual");
            golemaxedual.Name.English("Dual Golem Axes"); // You can use this to fix the display name in code
            golemaxedual.Description.English("Dual Weapons used by the Lord's Blade Ciaran, one of Gwyn's Four Knights.");
            golemaxedual.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            golemaxedual.RequiredItems.Add("GoldTracer", 1);
            golemaxedual.RequiredItems.Add("DarkSilverTracer", 1);
            golemaxedual.RequiredItems.Add("Eitr", 40);
            golemaxedual.RequiredItems.Add("Silver", 20);
            golemaxedual.RequiredUpgradeItems.Add("TitaniteShard", 1);
            golemaxedual.ApplyUpgradeMap("Tier5");

            Item SunlightSeal = new("souls", "SunlightSeal1", "assets");
            SunlightSeal.Name.English("Sunlight Seal"); // You can use this to fix the display name in code
            SunlightSeal.Description.English("Seal of Solaire of Astora, Knight of Sunlight. Decorated with a holy symbol, but Solaire illustrated it himself, and it has no divine powers of its own. As it turns out, Solaire's incredible prowess is a product of his own training, and nothing else.");
            SunlightSeal.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            SunlightSeal.RequiredItems.Add("Bronze", 25);
            SunlightSeal.RequiredItems.Add("Amber", 10);
            SunlightSeal.RequiredItems.Add("DeerHide", 20);
            SunlightSeal.RequiredItems.Add("LargeTitaniteShard", 20);
            SunlightSeal.RequiredUpgradeItems.Add("TitaniteShard", 1);
            SunlightSeal.ApplyUpgradeMap("Tier5");

            Item olenfordsCatalyst = new("souls", "olenfordsCatalyst");
            olenfordsCatalyst.Name.English("olenford staff");
            olenfordsCatalyst.Description.English("olenfordsCatalyst.");
            olenfordsCatalyst.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            olenfordsCatalyst.RequiredItems.Add("GoldTracer", 1);
            olenfordsCatalyst.RequiredItems.Add("DarkSilverTracer", 1);
            olenfordsCatalyst.RequiredItems.Add("Eitr", 40);
            olenfordsCatalyst.RequiredItems.Add("Silver", 20);
            olenfordsCatalyst.RequiredUpgradeItems.Add("TitaniteShard", 1);
            olenfordsCatalyst.ApplyUpgradeMap("Tier5");

            Debug.Log("[PungusSouls] AFTER tier5");
            #endregion Tier 5
            #region Tier 6
            Item ArtGS = new("souls", "ArtGS", "assets");
            ArtGS.Name.English("Greatsword of Artorias"); // You can use this to fix the display name in code
            ArtGS.Description.English("Sword born from the soul of the great grey wolf Sif, guardian of the grave of the Abysswalker Knight Artorias. Sir Artorias hunted the Darkwraiths, and his sword strikes harder against dark servants.");
            ArtGS.Crafting.Add("BlacksmithAltar", 6); // Custom crafting stations can be specified as a string
            ArtGS.RequiredItems.Add("Silver", 40);
            ArtGS.RequiredItems.Add("Eitr", 20);
            ArtGS.RequiredItems.Add("TwinklingTitanite", 10);
            ArtGS.RequiredItems.Add("TrophyWolf", 1);
            ArtGS.RequiredUpgradeItems.Add("TitaniteShard", 20);
                ArtGS.ApplyUpgradeMap("Tier6");

            Item DancerTwinblades = new("souls", "DancerTwinblades", "assets");
            DancerTwinblades.Name.English("Dancers Twinblades"); // You can use this to fix the display name in code
            DancerTwinblades.Description.English("Sword born from the soul of the great grey wolf Sif, guardian of the grave of the Abysswalker Knight Artorias. Sir Artorias hunted the Darkwraiths, and his sword strikes harder against dark servants.");
            DancerTwinblades.Crafting.Add("BlacksmithAltar", 6); // Custom crafting stations can be specified as a string
            DancerTwinblades.RequiredItems.Add("Silver", 40);
            DancerTwinblades.RequiredItems.Add("Eitr", 20);
            DancerTwinblades.RequiredItems.Add("TwinklingTitanite", 10);
            DancerTwinblades.RequiredItems.Add("TrophyWolf", 1);
            DancerTwinblades.RequiredUpgradeItems.Add("TitaniteShard", 20);
            DancerTwinblades.ApplyUpgradeMap("Tier6");

            Item DrangSpears = new("souls", "DrangSpears", "assets");
            DrangSpears.Name.English("Drang TwinSpears"); // You can use this to fix the display name in code
            DrangSpears.Description.English("Sword born from the soul of the great grey wolf Sif, guardian of the grave of the Abysswalker Knight Artorias. Sir Artorias hunted the Darkwraiths, and his sword strikes harder against dark servants.");
            DrangSpears.Crafting.Add("BlacksmithAltar", 6); // Custom crafting stations can be specified as a string
            DrangSpears.RequiredItems.Add("Silver", 40);
            DrangSpears.RequiredItems.Add("Eitr", 20);
            DrangSpears.RequiredItems.Add("TwinklingTitanite", 10);
            DrangSpears.RequiredItems.Add("TrophyWolf", 1);
            DrangSpears.RequiredUpgradeItems.Add("TitaniteShard", 20);
            DrangSpears.ApplyUpgradeMap("Tier6");

            Item CrystalRapier = new("souls", "CrystalRapier", "assets");
            CrystalRapier.Name.English("Crystal Rapier"); // You can use this to fix the display name in code
            CrystalRapier.Description.English("Sword born from the soul of the great grey wolf Sif, guardian of the grave of the Abysswalker Knight Artorias. Sir Artorias hunted the Darkwraiths, and his sword strikes harder against dark servants.");
            CrystalRapier.Crafting.Add("BlacksmithAltar", 6); // Custom crafting stations can be specified as a string
            CrystalRapier.RequiredItems.Add("Silver", 40);
            CrystalRapier.RequiredItems.Add("Eitr", 20);
            CrystalRapier.RequiredItems.Add("TwinklingTitanite", 10);
            CrystalRapier.RequiredItems.Add("TrophyWolf", 1);
            CrystalRapier.RequiredUpgradeItems.Add("TitaniteShard", 20);
            CrystalRapier.ApplyUpgradeMap("Tier6");

            Item SwordRinged = new("souls", "SwordRinged", "assets");
            SwordRinged.Name.English("Ringed Knight StraightSword"); // You can use this to fix the display name in code
            SwordRinged.Description.English("Sword born from the soul of the great grey wolf Sif, guardian of the grave of the Abysswalker Knight Artorias. Sir Artorias hunted the Darkwraiths, and his sword strikes harder against dark servants.");
            SwordRinged.Crafting.Add("BlacksmithAltar", 6); // Custom crafting stations can be specified as a string
            SwordRinged.RequiredItems.Add("Silver", 40);
            SwordRinged.RequiredItems.Add("Eitr", 20);
            SwordRinged.RequiredItems.Add("TwinklingTitanite", 10);
            SwordRinged.RequiredItems.Add("TrophyWolf", 1);
            SwordRinged.RequiredUpgradeItems.Add("TitaniteShard", 20);
            SwordRinged.ApplyUpgradeMap("Tier6");

            Item ArtoriasGreatshield = new("souls", "ArtoriasGreatshield", "assets");
            ArtoriasGreatshield.Name.English("GreatShield of Artorias"); // You can use this to fix the display name in code
            ArtoriasGreatshield.Description.English("Shield born from the soul of the great grey wolf Sif, guardian of the grave of the Abysswalker Knight Artorias.");
            ArtoriasGreatshield.Crafting.Add("BlacksmithAltar", 6); // Custom crafting stations can be specified as a string
            ArtoriasGreatshield.RequiredItems.Add("Silver", 40);
            ArtoriasGreatshield.RequiredItems.Add("Eitr", 20);
            ArtoriasGreatshield.RequiredItems.Add("TwinklingTitanite", 10);
            ArtoriasGreatshield.RequiredItems.Add("TrophyWolf", 1);
            ArtoriasGreatshield.RequiredUpgradeItems.Add("TitaniteShard", 1);
            ArtoriasGreatshield.ApplyUpgradeMap("Tier6");

            Item MLgreatsword = new("souls", "MLgreatsword", "assets");
            MLgreatsword.Name.English("Moonlight Greatsword"); // You can use this to fix the display name in code
            MLgreatsword.Description.English("This sword, one of the rare dragon weapons, came from the tail of Seath the Scaleless, the pale white dragon who betrayed his own. Seath is the grandfather of sorcery, and this sword is imbued with his magic, which shall be unleashed as a wave of moonlight.");
            MLgreatsword.Crafting.Add("BlacksmithAltar", 6); // Custom crafting stations can be specified as a string
            MLgreatsword.RequiredItems.Add("TwinklingTitanite", 25);
            MLgreatsword.RequiredItems.Add("DragonTear", 10);
            MLgreatsword.RequiredItems.Add("Eitr", 10);
            MLgreatsword.RequiredItems.Add("Crystal", 20);
            MLgreatsword.RequiredUpgradeItems.Add("TitaniteShard", 1);
            MLgreatsword.ApplyUpgradeMap("Tier6");

            Item SmoughHammer = new("souls", "SmoughHammer", "assets");
            SmoughHammer.Name.English("Smough's Hammer"); // You can use this to fix the display name in code
            SmoughHammer.Description.English("Great Hammer from the soul of executioner Smough, who guards the cathedral in the forsaken city of Anor Londo. Smough loved his work, and ground the bones of his victims into his own feed, ruining his hopes of being ranked with the Four Knights");
            SmoughHammer.Crafting.Add("BlacksmithAltar", 6); // Custom crafting stations can be specified as a string
            SmoughHammer.RequiredItems.Add("Bronze", 40);
            SmoughHammer.RequiredItems.Add("RoundLog", 20);
            SmoughHammer.RequiredItems.Add("TwinklingTitanite", 20);
            SmoughHammer.RequiredItems.Add("FineWood", 20);
            SmoughHammer.RequiredUpgradeItems.Add("TitaniteShard", 1);
            SmoughHammer.ApplyUpgradeMap("Tier6");

            Item TinCrystalStaff = new("souls", "TinCrystalStaff", "assets");
            TinCrystalStaff.Name.English("Tin Crystalization Catalyst"); // You can use this to fix the display name in code
            TinCrystalStaff.Description.English("Great Hammer from the soul of executioner Smough, who guards the cathedral in the forsaken city of Anor Londo. Smough loved his work, and ground the bones of his victims into his own feed, ruining his hopes of being ranked with the Four Knights");
            TinCrystalStaff.Crafting.Add("BlacksmithAltar", 6); // Custom crafting stations can be specified as a string
            TinCrystalStaff.RequiredItems.Add("Bronze", 40);
            TinCrystalStaff.RequiredItems.Add("RoundLog", 20);
            TinCrystalStaff.RequiredItems.Add("TwinklingTitanite", 20);
            TinCrystalStaff.RequiredItems.Add("FineWood", 20);
            TinCrystalStaff.RequiredUpgradeItems.Add("TitaniteShard", 1);
            TinCrystalStaff.ApplyUpgradeMap("Tier6");
            Debug.Log("[PungusSouls] AFTER tier6");
            #endregion Tier 6
            #region Tier 7

            Item GreatLordGreatSword = new("souls", "GreatLordGreatSword", "assets");
            GreatLordGreatSword.Name.English("Great Lord GreatSword"); // You can use this to fix the display name in code
            GreatLordGreatSword.Description.English("Greatsword born from the soul of Gwyn, Lord of Cinder. As bearer of the ultimate soul, Gwyn wielded the bolts of the sun, but before linking the fire, divided that power amongst his children, and set off with only this greatsword as his companion.");
            GreatLordGreatSword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            GreatLordGreatSword.RequiredItems.Add("Flametal", 50);
            GreatLordGreatSword.RequiredItems.Add("SurtlingCore", 20);
            GreatLordGreatSword.RequiredItems.Add("TwinklingTitanite", 15);
            GreatLordGreatSword.RequiredItems.Add("BlackCore", 10);
            GreatLordGreatSword.RequiredUpgradeItems.Add("Flametal", 20);
            GreatLordGreatSword.RequiredUpgradeItems.Add("SurtlingCore", 5);
            GreatLordGreatSword.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
            GreatLordGreatSword.RequiredUpgradeItems.Add("BlackCore", 5);

            Item VortdHammer = new("souls", "VortdHammer", "assets");
            VortdHammer.Name.English("Vortds hammer"); // You can use this to fix the display name in code
            VortdHammer.Description.English("Greatsword born from the soul of Gwyn, Lord of Cinder. As bearer of the ultimate soul, Gwyn wielded the bolts of the sun, but before linking the fire, divided that power amongst his children, and set off with only this greatsword as his companion.");
            VortdHammer.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            VortdHammer.RequiredItems.Add("Flametal", 50);
            VortdHammer.RequiredItems.Add("SurtlingCore", 20);
            VortdHammer.RequiredItems.Add("TwinklingTitanite", 15);
            VortdHammer.RequiredItems.Add("BlackCore", 10);
            VortdHammer.RequiredUpgradeItems.Add("Flametal", 20);
            VortdHammer.RequiredUpgradeItems.Add("SurtlingCore", 5);
            VortdHammer.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
            VortdHammer.RequiredUpgradeItems.Add("BlackCore", 5);

            Item RingedKnightPairedGreatswords = new("souls", "RingedKnightPairedGreatswords", "assets");
            RingedKnightPairedGreatswords.Name.English("Ringed Knight Paired Ultra Greatswords"); // You can use this to fix the display name in code
            RingedKnightPairedGreatswords.Description.English("Greatsword born from the soul of Gwyn, Lord of Cinder. As bearer of the ultimate soul, Gwyn wielded the bolts of the sun, but before linking the fire, divided that power amongst his children, and set off with only this greatsword as his companion.");
            RingedKnightPairedGreatswords.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            RingedKnightPairedGreatswords.RequiredItems.Add("Flametal", 50);
            RingedKnightPairedGreatswords.RequiredItems.Add("SurtlingCore", 20);
            RingedKnightPairedGreatswords.RequiredItems.Add("TwinklingTitanite", 15);
            RingedKnightPairedGreatswords.RequiredItems.Add("BlackCore", 10);
            RingedKnightPairedGreatswords.RequiredUpgradeItems.Add("Flametal", 20);
            RingedKnightPairedGreatswords.RequiredUpgradeItems.Add("SurtlingCore", 5);
            RingedKnightPairedGreatswords.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
            RingedKnightPairedGreatswords.RequiredUpgradeItems.Add("BlackCore", 5);


            Item Dranghammers = new("souls", "Dranghammers", "assets");
            Dranghammers.Name.English("Drang Twinhammers"); // You can use this to fix the display name in code
            Dranghammers.Description.English("Sword born from the soul of the great grey wolf Sif, guardian of the grave of the Abysswalker Knight Artorias. Sir Artorias hunted the Darkwraiths, and his sword strikes harder against dark servants.");
            Dranghammers.Crafting.Add("BlacksmithAltar", 6); // Custom crafting stations can be specified as a string
            Dranghammers.RequiredItems.Add("Silver", 40);
            Dranghammers.RequiredItems.Add("Eitr", 20);
            Dranghammers.RequiredItems.Add("TwinklingTitanite", 10);
            Dranghammers.RequiredItems.Add("TrophyWolf", 1);
            Dranghammers.RequiredUpgradeItems.Add("Flametal", 20);
            Dranghammers.RequiredUpgradeItems.Add("SurtlingCore", 5);
            Dranghammers.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
            Dranghammers.RequiredUpgradeItems.Add("BlackCore", 5);
            #endregion Tier 7
            Debug.Log("[PungusSouls] AFTER WEP");
            #endregion Weapons

            #region Items
            Item item2 = new Item("souls", "HelloCarving");
            item2.Name.English("Hello Carving");
            item2.Description.English("Head carved of archtrees by Gough in his imprisonment. Gough imparts an emotion to each and every completed carving, which helps him achieve personal enlightenment. When a head is disturbed, it speaks, reflecting the emotion conferred to it. This head says Hello. Have another look. Do you sense the amicability in its eyes?");
            Item item3 = new Item("souls", "ThankyouCarving");
            item3.Name.English("Thank You Carving");
            item3.Description.English("Head carved of archtrees by gough in his imprisonment. Gough imparts an emotion to each and every completed carving, which helps him achieve personal enlightenment. When a head is disturbed, it speaks, reflecting the emotion conferred to it. This head says Thank you. Have another look. Is this not the face of gratitude?");
            Item item4 = new Item("souls", "ImsorryCarving");
            item4.Name.English("I'm Sorry Carving");
            item4.Description.English("Head carved of archtrees by gough in his imprisonment. Gough imparts an emotion to each and every completed carving, which helps him achieve personal enlightenment. When a head is disturbed, it speaks, reflecting the emotion conferred to it. This head says I'm Sorry. Have another look. Isn't that an expression of atonement?");
            Item item5 = new Item("souls", "VerygoodCarving");
            item5.Name.English("Very Good Carving");
            item5.Description.English("Head carved of archtrees by gough in his imprisonment. Gough imparts an emotion to each and every completed carving, which helps him achieve personal enlightenment. When a head is disturbed, it speaks, reflecting the emotion conferred to it. This head says Very Good!. Have another look. Does it not appear quite jovial?");
            Item item6 = new Item("souls", "HelpmeCarving");
            item6.Name.English("Help Me Carving");
            item6.Description.English("Head carved of archtrees by Gough in his imprisonment. Gough imparts an emotion to each and every completed carving, which helps him achieve personal enlightenment. When a head is disturbed, it speaks, reflecting the emotion conferred to it. This head says Help me!. Have another look. Can you hear the desperate plea?");
            //Item item7 = new Item("souls", "CatRing");
            //item7.Name.English("Silver Cat Ring");
            //item7.Description.English("Silver ring depicting a leaping feline. Prevents damage from falling. In the Age of Gods, or possibily following it, an old cat was said to speak a human tongue, with the voice of an old woman, and the form of a fanciful immortal.");
            Item item8 = new Item("souls", "CatCharm");
            item8.Name.English("Cat Charm");
            item8.Description.English("Summon Sweet Shalquoir");
            Item item9 = new Item("souls", "WolfCharm");
            item9.Name.English("Wolf Charm");
            item9.Description.English("Summon Sif the Great Grey Wolf");
            Item charcoalpineresin = new Item("souls", "CharcoalPineResin");
            Item goldlpineresin = new Item("souls", "GoldPineResin");
            Item rottenpineresin = new Item("souls", "RottenPineResin");
            Item palepineresin = new Item("souls", "PalePineResin");
            Item frozenpineresin = new Item("souls", "FrozenPineResin");
            Item CompanionControlHorn = new Item("souls", "CompanionControlHorn", "assets");
            CompanionControlHorn.Name.English("Companion Control Horn");
            CompanionControlHorn.Description.English("Remotely command or summon a companion.");
            CompanionControlHorn.Snapshot();
            GameObject RuneTablet = ItemManager.PrefabManager.RegisterPrefab("souls", "RuneTablet");
            Debug.Log("[PungusSouls] AFTER Items");

            #endregion Items

            #region SFX
            GameObject sfx_Andre_bye1 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_Andre_bye1");
            GameObject sfx_Andre_greeting1 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_Andre_greeting1");
            GameObject sfx_Andre_talk1 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_Andre_talk1");
            GameObject firelinkshrine_sfx = ItemManager.PrefabManager.RegisterPrefab("souls", "firelinkshrine_sfx");
            GameObject sfx_taurus_attack1 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_taurus_attack1");
            GameObject sfx_taurus_weaponhit1 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_taurus_weaponhit1");
            GameObject FX_Taurus_Dead = ItemManager.PrefabManager.RegisterPrefab("souls", "FX_Taurus_Dead");
            GameObject SFX_Taurus_Hit = ItemManager.PrefabManager.RegisterPrefab("souls", "FX_Taurus_Hit");
            GameObject SFX_Taurus_Alert1 = ItemManager.PrefabManager.RegisterPrefab("souls", "SFX_Taurus_Alert1");
            GameObject SFX_Taurus_Idle1 = ItemManager.PrefabManager.RegisterPrefab("souls", "SFX_Taurus_Idle1");
            GameObject gameObject15 = ItemManager.PrefabManager.RegisterPrefab("souls", "FX_BlackKnight_Death");
            GameObject gameObject16 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_blackknight_hit");
            GameObject sfx_crystalgolem_hurt = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_crystalgolem_hurt");
            GameObject sfx_crystalgolem_death = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_crystalgolem_death");
            GameObject sfx_crystalgolem_swing = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_crystalgolem_swing");
            GameObject sfx_crystalgolem_bomb = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_crystalgolem_bomb");
            GameObject gameObject18 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_fangboar_alert");
            GameObject gameObject19 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_fangboar_attack");
            GameObject gameObject20 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_fangboar_dead");
            GameObject gameObject21 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_fangboar_hit");
            GameObject gameObject22 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_fangboar_idle");
            GameObject gameObject23 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_wyvern_idle");
            GameObject gameObject24 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_wyvern_alert");
            GameObject gameObject25 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_wyvern_attack");
            GameObject gameObject26 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_wyvern_hit");
            GameObject gameObject27 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_wyvern_dead");
            GameObject gameObject28 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_idle");
            GameObject gameObject29 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_alert");
            GameObject gameObject30 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_attack");
            GameObject gameObject31 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_hit");
            GameObject gameObject32 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_damage");
            GameObject gameObject33 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_swing");
            GameObject gameObject34 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_footstep");
            GameObject gameObject35 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_hello");
            GameObject gameObject36 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_thankyou");
            GameObject gameObject37 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_helpme");
            GameObject gameObject38 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_verygood");
            GameObject gameObject39 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_imsorry");
            GameObject gameObject40 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_sif_alert");
            GameObject gameObject41 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_sif_attack");
            GameObject gameObject42 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_sif_hit");
            GameObject gameObject43 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_sif_swing");
            GameObject vfx_drake_breath = ItemManager.PrefabManager.RegisterPrefab("souls", "vfx_drake_breath");
            GameObject vfx_kalameet_breath1 = ItemManager.PrefabManager.RegisterPrefab("souls", "vfx_kalameet_breath1");
            GameObject vfx_kalameet_breath2 = ItemManager.PrefabManager.RegisterPrefab("souls", "vfx_kalameet_breath2");
            GameObject fx_rain_spell = ItemManager.PrefabManager.RegisterPrefab("souls", "fx_rain_spell");
            GameObject sfx_seath_charge = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_seath_charge");
            GameObject sfx_seath_start = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_seath_start");
            GameObject sfx_seath_attack = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_seath_attack");
            GameObject sfx_seath_breath = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_seath_breath");
            GameObject sfx_drake_breath_trailon = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_drake_breath_trailon");
            GameObject fx_fireball_staff_explosion1 = ItemManager.PrefabManager.RegisterPrefab("souls", "fx_fireball_staff_explosion1");
            GameObject sfx_explosion1 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_explosion1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_manus_spell");
            ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_manus_attack1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_manus_swing");
            GameObject SFX_Hollow_Alert = ItemManager.PrefabManager.RegisterPrefab("souls", "SFX_Hollow_Alert");
            GameObject sfx_hollow_attack = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_hollow_attack");
            GameObject SFX_Hollow_death = ItemManager.PrefabManager.RegisterPrefab("souls", "SFX_Hollow_death");
            GameObject SFX_Hollow_Idle = ItemManager.PrefabManager.RegisterPrefab("souls", "SFX_Hollow_Idle");
            GameObject HollowSoldier_Ragdoll = ItemManager.PrefabManager.RegisterPrefab("souls", "HollowSoldier_Ragdoll");
            GameObject sfx_kalameet_attack = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_kalameet_attack");
            GameObject sfx_kalameet_breath = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_kalameet_breath");
            GameObject sfx_kalameet_damage = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_kalameet_damage");
            GameObject sfx_kalameet_growl = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_kalameet_growl");
            GameObject sfx_giant_attack = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_giant_attack");
            GameObject sfx_giant_kick = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_giant_kick");
            GameObject sfx_giant_dead = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_giant_dead");
            GameObject sfx_giant_hurt = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_giant_hurt");
            GameObject sfx_giant_punch = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_giant_punch");
            GameObject sfx_giant_damage = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_giant_damage");
            GameObject sfx_giant_growl = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_giant_growl");
            GameObject sfx_giant_steps = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_giant_steps");
            GameObject sfx_giant_idle = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_giant_idle");
            GameObject sfx_queelag_growl = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_queelag_growl");
            GameObject sfx_queelag_steps = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_queelag_steps");
            GameObject sfx_queelag_idle = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_queelag_idle");
            GameObject sfx_queelag_attack = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_queelag_attack");
            GameObject sfx_queelag_swing = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_queelag_swing");
            GameObject sfx_queelag_lava = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_queelag_lava");
            GameObject sfx_queelag_lava_land = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_queelag_lava_land");
            GameObject sfx_queelag_movement = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_queelag_move");
            GameObject sfx_queelag_land = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_queelag_land");
            GameObject sfx_queelag_dead = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_queelag_dead");
            GameObject sfx_queelag_aoe = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_queelag_aoe");

            Debug.Log("[PungusSouls] AFTER SFX");
            #endregion sfx

            #region Creature Attacks

            Item item110 = new Item("souls", "Sif_attack1_vert");
            item110.Configurable = Configurability.Disabled;
            Item item111 = new Item("souls", "Sif_attack2_vert");
            item111.Configurable = Configurability.Disabled;
            Item item112 = new Item("souls", "Sif_attack_jumpslash");
            item112.Configurable = Configurability.Disabled;
            Item item113 = new Item("souls", "Sif_attack_slash1");
            item113.Configurable = Configurability.Disabled;
            Item item114 = new Item("souls", "Sif_attack_slash2");
            item114.Configurable = Configurability.Disabled;
            Item item115 = new Item("souls", "Sif_attack_slash3");
            item115.Configurable = Configurability.Disabled;
            Item item116 = new Item("souls", "Sif_attack_spin1");
            item116.Configurable = Configurability.Disabled;
            Item item117 = new Item("souls", "Sif_attack_spinjump");
            item117.Configurable = Configurability.Disabled;

            Item DemonGreatHammer1 = new("souls", "DemonGreatHammer1", "assets");
            DemonGreatHammer1.Configurable = Configurability.Disabled;
            Item DemonGreatHammer2 = new("souls", "DemonGreatHammer2", "assets");
            DemonGreatHammer2.Configurable = Configurability.Disabled;
            Item DemonGreatHammerSlam = new("souls", "DemonGreatHammerSlam", "assets");
            DemonGreatHammerSlam.Configurable = Configurability.Disabled;
            Item Tauruswep = new("souls", "TaurusWep", "assets");
            Tauruswep.Configurable = Configurability.Disabled;
            Item Tauruswep1 = new("souls", "TaurusWep1", "assets");
            Tauruswep1.Configurable = Configurability.Disabled;
            Item Tauruswep2 = new("souls", "TaurusWep2", "assets");
            Tauruswep2.Configurable = Configurability.Disabled;
            Item MushroomUnarmed = new("souls", "MushroomUnarmed", "assets");
            MushroomUnarmed.Configurable = Configurability.Disabled;
            Item BlackKnightHalberd1 = new("souls", "BlackKnightHalberd1", "assets");
            BlackKnightHalberd1.Configurable = Configurability.Disabled;
            Item BlackKnightHalberd2 = new("souls", "BlackKnightHalberd2", "assets");
            BlackKnightHalberd2.Configurable = Configurability.Disabled;
            Item BlackKnightHalberd3 = new("souls", "BlackKnightHalberd3", "assets");
            BlackKnightHalberd3.Configurable = Configurability.Disabled;
            Item BlackKnightGreatAxe1 = new("souls", "BlackKnightGreatAxe1", "assets");
            BlackKnightGreatAxe1.Configurable = Configurability.Disabled;
            Item BlackKnightGreatAxe2 = new("souls", "BlackKnightGreatAxe2", "assets");
            BlackKnightGreatAxe2.Configurable = Configurability.Disabled;
            Item BlackKnightUGS1 = new("souls", "BlackKnightUGS1", "assets");
            BlackKnightUGS1.Configurable = Configurability.Disabled;
            Item BlackKnightUGS2 = new("souls", "BlackKnightUGS2", "assets");
            BlackKnightUGS2.Configurable = Configurability.Disabled;
            Item Golem_Unarmed2 = new("souls", "Golem_Unarmed2", "assets");
            Golem_Unarmed2.Configurable = Configurability.Disabled;
            Item Golem_Unarmed3 = new("souls", "Golem_Unarmed3", "assets");
            Golem_Unarmed3.Configurable = Configurability.Disabled;
            Item DragonSlayerGreatBow1 = new("souls", "DragonSlayerGreatBow1", "assets");
            DragonSlayerGreatBow1.Configurable = Configurability.Disabled;
            Item ChaosZweihander = new("souls", "ChaosZweihander", "assets");
            ChaosZweihander.Configurable = Configurability.Disabled;
            Item ChaosZweihander1 = new("souls", "ChaosZweihander1", "assets");
            ChaosZweihander1.Configurable = Configurability.Disabled;
            Item BlackFlame = new("souls", "BlackFlame", "assets");
            BlackFlame.Configurable = Configurability.Disabled;
            Item FangBoarAttack = new("souls", "FangBoar_Attack", "assets");
            FangBoarAttack.Configurable = Configurability.Disabled;
            Item GrassCrestShield1 = new("souls", "GrassCrestShield1", "assets");
            GrassCrestShield1.Configurable = Configurability.Disabled;
            Item Wyvern_Bite = new("souls", "Wyvern_Bite", "assets");
            Wyvern_Bite.Configurable = Configurability.Disabled;
            Item SilverKnightSpear1 = new("souls", "SilverKnightSpear1", "assets");
            SilverKnightSpear1.Configurable = Configurability.Disabled;
            Item SilverKnightSpear2 = new("souls", "SilverKnightSpear2", "assets");
            SilverKnightSpear2.Configurable = Configurability.Disabled;
            Item SilverKnightSword1 = new("souls", "SilverKnightSword1", "assets");
            SilverKnightSword1.Configurable = Configurability.Disabled;
            Item SilverKnightShield1 = new("souls", "SilverKnightShield1", "assets");
            SilverKnightShield1.Configurable = Configurability.Disabled;
            Item Hollow_Bow1 = new("souls", "hollow_bow1", "assets");
            Hollow_Bow1.Configurable = Configurability.Disabled;
            Item Hollow_Sword1 = new("souls", "hollow_sword1", "assets");
            Hollow_Sword1.Configurable = Configurability.Disabled;
            GameObject hellkitedrakeattack = ItemManager.PrefabManager.RegisterPrefab("souls", "hellkitedrakeattack");
            GameObject hellkite_breath_fly = ItemManager.PrefabManager.RegisterPrefab("souls", "hellkite_breath_fly");
            GameObject hellkitebreath = ItemManager.PrefabManager.RegisterPrefab("souls", "hellkitebreath");
            GameObject hellkitedrakeattack3 = ItemManager.PrefabManager.RegisterPrefab("souls", "hellkitedrakeattack3");
            GameObject HellkiteBite = ItemManager.PrefabManager.RegisterPrefab("souls", "hellkitedrakebite");
            GameObject BoarCompanionSpawn = ItemManager.PrefabManager.RegisterPrefab("souls", "BoarCompanionSpawn");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_aoe1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_aoe2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_attack_aoe1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_attack_aoe2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_attack_beam1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_attack_beam2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_attack_punch1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_attack_punch2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_attack_tentacle1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_attack_tentacle2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_attack_summon");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_beam1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_beam2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "seath_summon");
            ItemManager.PrefabManager.RegisterPrefab("souls", "manus_attack1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "manus_attack2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "manus_attack3");
            ItemManager.PrefabManager.RegisterPrefab("souls", "manus_attack4");
            ItemManager.PrefabManager.RegisterPrefab("souls", "manus_attack5");
            ItemManager.PrefabManager.RegisterPrefab("souls", "manus_attack6");
            ItemManager.PrefabManager.RegisterPrefab("souls", "manus_attack7");
            ItemManager.PrefabManager.RegisterPrefab("souls", "manus_spell1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "manus_spell2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "manus_spell3");
            ItemManager.PrefabManager.RegisterPrefab("souls", "manus_spell4");
            ItemManager.PrefabManager.RegisterPrefab("souls", "manus_spell_spawn");
            ItemManager.PrefabManager.RegisterPrefab("souls", "manus_spell_rain");
            ItemManager.PrefabManager.RegisterPrefab("souls", "fx_manus_spell1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "artorias_attack");
            ItemManager.PrefabManager.RegisterPrefab("souls", "artorias_attack1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "artorias_attack2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "artorias_attack3");
            ItemManager.PrefabManager.RegisterPrefab("souls", "artorias_attack_charge");
            ItemManager.PrefabManager.RegisterPrefab("souls", "artorias_attack_jump");
            ItemManager.PrefabManager.RegisterPrefab("souls", "artorias_attack_spin");
            ItemManager.PrefabManager.RegisterPrefab("souls", "artorias_attack_flip");
            ItemManager.PrefabManager.RegisterPrefab("souls", "artorias_buff");
            ItemManager.PrefabManager.RegisterPrefab("souls", "kalameet_attack1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "kalameet_attack2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "kalameet_attack3");
            ItemManager.PrefabManager.RegisterPrefab("souls", "kalameet_attack4");
            ItemManager.PrefabManager.RegisterPrefab("souls", "kalameet_breath2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "kalameet_breath3");
            ItemManager.PrefabManager.RegisterPrefab("souls", "kalameet_breath4");
            ItemManager.PrefabManager.RegisterPrefab("souls", "kalameet_aoe");
            ItemManager.PrefabManager.RegisterPrefab("souls", "BlackKnight_Ragdoll");
            ItemManager.PrefabManager.RegisterPrefab("souls", "CrystalGolem_Attack");
            ItemManager.PrefabManager.RegisterPrefab("souls", "Golem_AOE");
            ItemManager.PrefabManager.RegisterPrefab("souls", "DragonSlayer_bow_projectile");
            ItemManager.PrefabManager.RegisterPrefab("souls", "giant_attack1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "giant_attack2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "giant_attack3");
            ItemManager.PrefabManager.RegisterPrefab("souls", "giant_attack4");
            ItemManager.PrefabManager.RegisterPrefab("souls", "giant_throw");
            ItemManager.PrefabManager.RegisterPrefab("souls", "queelag_attack1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "queelag_attack2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "queelag_attack3");
            ItemManager.PrefabManager.RegisterPrefab("souls", "queelag_attack4");
            ItemManager.PrefabManager.RegisterPrefab("souls", "queelag_attack5");
            ItemManager.PrefabManager.RegisterPrefab("souls", "queelag_attack_aoe");
            ItemManager.PrefabManager.RegisterPrefab("souls", "queelag_lava_1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "queelag_lava_2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "queelag_lava_3");
            ItemManager.PrefabManager.RegisterPrefab("souls", "frog_attack");
            ItemManager.PrefabManager.RegisterPrefab("souls", "frog_attack1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "frog_attack2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "mushroom_heal");

            Debug.Log("[PungusSouls] AFTER Creatureattacks");
            #endregion Creature Attacks


            #region Projectiles and AOEs 

            ItemManager.PrefabManager.RegisterPrefab("souls", "BlackFlame_AOE");
            ItemManager.PrefabManager.RegisterPrefab("souls", "LightningSpear_Projectile");
            ItemManager.PrefabManager.RegisterPrefab("souls", "dragon_lightning_projectile");
            ItemManager.PrefabManager.RegisterPrefab("souls", "MLGS_Projectile_New");
            ItemManager.PrefabManager.RegisterPrefab("souls", "projectile_hello");
            ItemManager.PrefabManager.RegisterPrefab("souls", "projectile_thankyou");
            ItemManager.PrefabManager.RegisterPrefab("souls", "projectile_helpme");
            ItemManager.PrefabManager.RegisterPrefab("souls", "projectile_imsorry");
            ItemManager.PrefabManager.RegisterPrefab("souls", "projectile_verygood");
            ItemManager.PrefabManager.RegisterPrefab("souls", "CatCharm_projectile");
            ItemManager.PrefabManager.RegisterPrefab("souls", "WolfCharm_Projectile");
            ItemManager.PrefabManager.RegisterPrefab("souls", "Soularrow");
            ItemManager.PrefabManager.RegisterPrefab("souls", "SoulSpear");
            ItemManager.PrefabManager.RegisterPrefab("souls", "SoulSpear2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "CrystalSoulSpear");
            ItemManager.PrefabManager.RegisterPrefab("souls", "StarsofRuin");
            ItemManager.PrefabManager.RegisterPrefab("souls", "stars");
            ItemManager.PrefabManager.RegisterPrefab("souls", "projectile_beam_magic");
            ItemManager.PrefabManager.RegisterPrefab("souls", "dragon_fire_projectile");
            ItemManager.PrefabManager.RegisterPrefab("souls", "fx_lightningbolts");
            ItemManager.PrefabManager.RegisterPrefab("souls", "fx_lightningbolts2");
            ItemManager.PrefabManager.RegisterPrefab("souls", "Bolt");
            ItemManager.PrefabManager.RegisterPrefab("souls", "rain_spawn");
            ItemManager.PrefabManager.RegisterPrefab("souls", "spell_rain_projectile");
            ItemManager.PrefabManager.RegisterPrefab("souls", "staff_Dark_projectile");

            ItemManager.PrefabManager.RegisterPrefab("souls", "buff_lightning");
            ItemManager.PrefabManager.RegisterPrefab("souls", "buff_fire");
            ItemManager.PrefabManager.RegisterPrefab("souls", "buff_frost");
            ItemManager.PrefabManager.RegisterPrefab("souls", "buff_spirit");
            ItemManager.PrefabManager.RegisterPrefab("souls", "buff_poison");
            ItemManager.PrefabManager.RegisterPrefab("souls", "Drakesword_Projectile");
            ItemManager.PrefabManager.RegisterPrefab("souls", "Discus_Projectile");
            ItemManager.PrefabManager.RegisterPrefab("souls", "aoe_wrath");
            ItemManager.PrefabManager.RegisterPrefab("souls", "vfx_mushroom_aura");
            ItemManager.PrefabManager.RegisterPrefab("souls", "vfx_Ball_launch");
            ItemManager.PrefabManager.RegisterPrefab("souls", "giant_projectile");
            ItemManager.PrefabManager.RegisterPrefab("souls", "lavabomb_explosion1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "lavabomb_projectile1");
            ItemManager.PrefabManager.RegisterPrefab("souls", "mushroom_heal_aoe");


            Debug.Log("[PungusSouls] AFTER Projectiles and AOEs");
            #endregion Projectiles and AOEs


            #endregion Items and Prefabs
            #region CreatureManager Example Code

            Creature Artorias = new("souls", "Artorias")
            {
                SpawnChance = 0f,
                CanSpawn = false
            };

            Creature Queelag = new("souls", "Queelag")
            {
                SpawnChance = 0f,
                CanSpawn = false
            };

            Creature Frog = new("souls", "Frog")
            {
                SpawnChance = 0f,
                CanSpawn = false
            };

           
            /*
            Creature AsylumDemon = new("souls", "AsylumDemon")
            {
            Biome = Heightmap.Biome.None,
            GroupSize = new CreatureManager.Range(1, 2),
            CheckSpawnInterval = 600,
            RequiredWeather = Weather.Rain | Weather.Fog,
            Maximum = 0
            };
            AsylumDemon.Localize().English("Asylum Demon");
            AsylumDemon.Drops["Wood"].Amount = new CreatureManager.Range(1, 2);
            AsylumDemon.Drops["Wood"].DropChance = 100f;
            */

            Creature CrystalGolem = new("souls", "CrystalGolem")

            {
                Biome = Heightmap.Biome.DeepNorth,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 900,
                Maximum = 3
            };

            Creature BlackKnight = new("souls", "BlackKnight")

            {
                Biome = Heightmap.Biome.AshLands,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 900,
                Maximum = 3
            };

            Creature SilverKnight = new("souls", "SilverKnight")
            {
                Biome = Heightmap.Biome.DeepNorth,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 600,
                RequiredWeather = Weather.Rain | Weather.Fog,
                Maximum = 0,
            };

            Creature FangBoar = new("souls", "FangBoar")

            {
                Biome = Heightmap.Biome.Swamp,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 12000,
                Maximum = 1
            };

            Creature FangBoarCompanion = new("souls", "FangBoarCompanion")

            {
                SpawnChance = 0f,
                CanSpawn = false
            };

            Creature GiantDad = new("souls", "GiantDad")

            {
                RequiredGlobalKey = GlobalKey.KilledBonemass,
                Biome = Heightmap.Biome.All,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 12000,
                Maximum = 1
            };

            Creature GiantMushroom = new("souls", "GiantMushroom")

            {
                Biome = Heightmap.Biome.BlackForest,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 2200,
                Maximum = 2
            };

            Creature GiantMushroomBro = new("souls", "GiantMushroomBro")
            {
                SpawnChance = 0f,
                CanSpawn = false
            };

            Creature BabyMushroom = new("souls", "BabyMushroom")
            {
                SpawnChance = 0f,
                CanSpawn = false
            };

            Creature TaurusDemon = new("souls", "TaurusDemon")

            {
                Biome = Heightmap.Biome.Plains | Heightmap.Biome.AshLands,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 3500,
                Maximum = 1
            };

            Creature Capra = new("souls", "Capra")

            {
                RequiredGlobalKey = GlobalKey.KilledModer,
                Biome = Heightmap.Biome.Mistlands | Heightmap.Biome.AshLands,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 3500,
                Maximum = 1
            };

            Creature HollowSoldier = new("souls", "HollowSoldier")

            {
                RequiredGlobalKey = GlobalKey.KilledBonemass,
                Biome = Heightmap.Biome.Mistlands | Heightmap.Biome.BlackForest | Heightmap.Biome.AshLands,
                GroupSize = new CreatureManager.Range(1, 3),
                CheckSpawnInterval = 7000,
                Maximum = 4
            };

            Creature AncientDragon = new("souls", "AncientDragon")

            {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Creature Wyvern = new("souls", "Wyvern")

            {
                Biome = Heightmap.Biome.DeepNorth,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 600,
                Maximum = 3
            };

            Creature Giant = new("souls", "Giant")

            {
                Biome = Heightmap.Biome.DeepNorth,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 600,
                Maximum = 3
            };

            Creature Sif = new Creature("souls", "Sif")
            {
                Biome = Heightmap.Biome.None,
                Maximum = 0,
                FoodItems = "RawMeat"
            };

            Creature SweetShalquoir = new Creature("souls", "SweetShalquoir")
            {
                CanSpawn = false,

            };
            GameObject gameObject44 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_cat_hit");
            GameObject gameObject45 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_cat_idle");
            GameObject gameObject46 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_cat_alert");
            GameObject gameObject47 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_cat_attack");
            Item item120 = new Item("souls", "Cat_AttackP1");
            item120.Configurable = Configurability.Disabled;
            Item item121 = new Item("souls", "Cat_bap");
            item121.Configurable = Configurability.Disabled;
            Item item122 = new Item("souls", "Cat_bite");
            item122.Configurable = Configurability.Disabled;
            Item item123 = new Item("souls", "Cat_jumpattack");
            item123.Configurable = Configurability.Disabled;

            Creature HellkiteDrake = new Creature("souls", "HellkiteDrake")
            {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Creature Marika = new Creature("souls", "queenmarikacompanion")
            {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Creature Bria = new Creature("souls", "Bria")
            {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Creature FangBoarBoss = new Creature("souls", "FangBoarBoss")
            {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Creature Solaire = new Creature("souls", "Solaire")
            {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Creature Oscar = new Creature("souls", "Oscar")
            {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Creature Havel = new Creature("souls", "Havel")
            {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Creature Firekeeper = new Creature("souls", "FireKeeper")
            {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Creature Ciarin = new Creature("souls", "Ciarin")
            {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Creature Seath = new Creature("souls", "Seath")
            {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Creature Manus = new Creature("souls", "Manus")
                {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Creature Kalameet = new Creature("souls", "Kalameet")
            {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Creature HellkiteDrakeBoss = new Creature("souls", "HellkiteDrakeBoss")
            {
                Biome = Heightmap.Biome.None,
                CanSpawn = false,
                ConfigurationEnabled = false,
            };

            Debug.Log("[PungusSouls] AFTER Creatures");
            #endregion

            Animations.LoadAssets();

            Assembly assembly = typeof(WeaponUpgradeStationRequirementPatch).Assembly;

            BossEventManager.RegisterFromBundle(asset);
            BossEventManager.Patch(_harmony);
            DungeonPickableReplacement.RegisterDefaults();

            _harmony.PatchAll(assembly);

            MethodInfo target = AccessTools.Method(
                typeof(Recipe),
                nameof(Recipe.GetRequiredStationLevel),
                new[] { typeof(int) });

            if (MusicMan.instance != null)
            {
                MusicManPatch.AddFirelinkShrineMusic(MusicMan.instance);
            }

            Debug.Log("[PungusSouls] AWAKE COMPLETE");

            SetupWatcher();
        }
        private static string GetFullPath(Transform transform)
        {
            string path = transform.name;

            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
        private void OnDestroy()
        {
            Config.Save();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                Debug.Log("[PungusSouls] F8 probe fired");

                Character[] characters = UnityEngine.Object.FindObjectsByType<Character>(FindObjectsSortMode.None);
                Debug.Log("[PungusSouls] Character count: " + characters.Length);

                foreach (Character character in characters)
                {
                    if (character == null)
                    {
                        continue;
                    }

                    Debug.Log("[PungusSouls] Character: " + character.name);

                    NpcGiveItemController existing = character.GetComponentInChildren<NpcGiveItemController>(true);
                    if (existing != null)
                    {
                        Debug.Log("[PungusSouls] Found NpcGiveItemController on " + GetPath(existing.transform));
                    }
                }
            }
        }

        private static string GetPath(Transform transform)
        {
            if (transform == null)
            {
                return "";
            }

            string path = transform.name;

            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                PungusSoulsLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                PungusSoulsLogger.LogError($"There was an issue loading your {ConfigFileName}");
                PungusSoulsLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }
        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", KeyboardShortcut.AllKeyCodes);
        }

        #endregion
    }
}