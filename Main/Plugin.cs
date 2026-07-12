using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CreatureManager;
using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using LocationManager;
using PieceManager;
using ServerSync;
using StatusEffectManager;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ItemDrop;
using Range = LocationManager.Range;
using System.Runtime.CompilerServices;
using LocalizationManager;

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
            string folder = Path.GetDirectoryName(Info.Location);
            string bundlePath = Path.Combine(folder, "FirelinkShrine");

            if (asset == null)
            {
                Logger.LogError("Failed to load asset bundle: " + bundlePath);
                return;
            }

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
            Debug.Log("[PungusSouls] AFTER CONFIG");
            #region Drops
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

            DullEmber_Item.DropsFrom.Add("Elder", 1f, 1, 1);
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
                        ForestThresholdMin = 0.2f,
                        MinDistanceFromSame = 1000f,

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
                        ForestThresholdMin = 0.2f,
                        MinDistanceFromSame = 1000f,

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
                        ForestThresholdMin = 0.2f,
                        MinDistanceFromSame = 1000f,

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
                        ForestThresholdMin = 0.2f,
                        MinDistanceFromSame = 1000f,

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
                        InForest = true,
                        ForestThresholdMin = 0.2f,
                        MinDistanceFromSame = 1000f,

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
                SpawnDistance = new Range(1250, 3550),
                SpawnAltitude = new Range(16, 220),
                GroupName = "AvoidAnyLocationGroup",
                MinimumDistanceFromGroup = 400f,
                Count = 1,
                Prioritize = true,
                Unique = true

            };

            LocationManager.Location newlondo = new("souls", "newlondo")
            {
                Rotation = Rotation.Fixed,
                ShowMapIcon = ShowIcon.Explored,
                Biome = Heightmap.Biome.Mountain,
                SpawnArea = Heightmap.BiomeArea.Median,
                HeightDelta = new Range(75, 120),
                SpawnDistance = new Range(2550, 999999),
                SpawnAltitude = new Range(24, 84),
                Count = 1,
                Prioritize = true,
                Unique = true
            };

            LocationManager.Location Sif_Loc = new("souls", "Sif_Loc")
            {
                ShowMapIcon = ShowIcon.Explored,
                Biome = Heightmap.Biome.BlackForest,
                SpawnArea = Heightmap.BiomeArea.Median,
                HeightDelta = new Range(0, 16),
                SpawnDistance = new Range(5500, 6500),
                SpawnAltitude = new Range(0, 18),
                GroupName = "AvoidAnyLocationGroup",
                MinimumDistanceFromGroup = 1000f,
                Count = 1,
                Prioritize = true,
                Unique = true
            };

            LocationManager.Location PS_Bonfire_Loc = new("souls", "PS_Bonfire_Loc")
            {
                ShowMapIcon = ShowIcon.Never,
                Biome = Heightmap.Biome.Meadows,
                SpawnArea = Heightmap.BiomeArea.Everything,
                HeightDelta = new Range(0, 99999),
                SpawnDistance = new Range(0, 30),
                SpawnAltitude = new Range(0, 99999),
                Count = 1,
                Prioritize = true,
                Unique = true
            };

            LocationManager.Location ForestTower = new("souls", "ForestTower")
            {
                ShowMapIcon = ShowIcon.Never,
                Biome = Heightmap.Biome.BlackForest,
                SpawnArea = Heightmap.BiomeArea.Everything,
                SpawnAltitude = new Range(0, 99999),
                SpawnDistance = new Range(1500, 9999),
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

            LocationManager.Location BoarCave = new("souls", "BoarCave")
            {
                ShowMapIcon = ShowIcon.Never,
                Biome = Heightmap.Biome.Swamp,
                SpawnArea = Heightmap.BiomeArea.Median,
                SpawnAltitude = new Range(0, 100),
                SpawnDistance = new Range(2500, 5500),
                GroupName = "AvoidAnyLocationGroup",
                MinimumDistanceFromGroup = 500f,
                Count = 1,
                Prioritize = true,
                Unique = true,
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
           Debug.Log("[PungusSouls] AFTER SE");
            #endregion StatusEffectManager

            #region ItemManager
            #region Armor

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
            Debug.Log("[PungusSouls] AFTER ARM");
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

                Item SunlightSword = new("souls", "SunlightSword", "assets");
            SunlightSword.Name.English("Sunlight StraightSword"); // You can use this to fix the display name in code
            SunlightSword.Description.English("This standard longsword, belonging to Solaire of Astora, is of high quality, is well-forged, and has been kept in good repair. Easy to use and dependable, but unlikely to live up to its grandiose name.");
            SunlightSword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            SunlightSword.RequiredItems.Add("TitaniteShard", 10);
            SunlightSword.RequiredItems.Add("FineWood", 10);
            SunlightSword.RequiredItems.Add("TrophyGreydwarf", 5);
            SunlightSword.RequiredItems.Add("Bronze", 25);
            SunlightSword.RequiredUpgradeItems.Add("TitaniteShard", 51);
            SunlightSword.ApplyUpgradeMap("Standard");

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

            Item SunlightSeal = new("souls", "SunlightSeal1", "assets");
            SunlightSeal.Name.English("Sunlight Seal"); // You can use this to fix the display name in code
            SunlightSeal.Description.English("Seal of Solaire of Astora, Knight of Sunlight. Decorated with a holy symbol, but Solaire illustrated it himself, and it has no divine powers of its own. As it turns out, Solaire's incredible prowess is a product of his own training, and nothing else.");
            SunlightSeal.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            SunlightSeal.RequiredItems.Add("Bronze", 25);
            SunlightSeal.RequiredItems.Add("Amber", 10);
            SunlightSeal.RequiredItems.Add("DeerHide", 20);
            SunlightSeal.RequiredItems.Add("LargeTitaniteShard", 20);
                SunlightSeal.RequiredUpgradeItems.Add("TitaniteShard", 1);
                SunlightSeal.ApplyUpgradeMap("Tier2");

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

                Item GoldTracer = new("souls", "GoldTracer", "assets");
            GoldTracer.Name.English("Gold Tracer"); // You can use this to fix the display name in code
            GoldTracer.Description.English("Curved sword used by the Lord's Blade Ciaran, one of Gwyn's Four Knights. Ciaran brandishes her sword in a mesmerizing dance, etching the darkness with dire streaks of gold.");
            GoldTracer.Crafting.Add("BlacksmithAltar", 2); // Custom crafting stations can be specified as a string
            GoldTracer.RequiredItems.Add("Iron", 40);
            GoldTracer.RequiredItems.Add("Coins", 99);
            GoldTracer.RequiredItems.Add("Ruby", 40);
            GoldTracer.RequiredItems.Add("LargeTitaniteShard", 20);
            GoldTracer.RequiredUpgradeItems.Add("TitaniteShard", 1);
            GoldTracer.ApplyUpgradeMap("Tier2");

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
            Debug.Log("[PungusSouls] AFTER Tier2");
            #endregion Tier 2 (Swamps)
            #region Tier 3 (Mountains)

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

            Item AbyssGreatsword = new("souls", "AbyssGreatsword", "assets");
            AbyssGreatsword.Name.English("Abyss Greatsword"); // You can use this to fix the display name in code
            AbyssGreatsword.Description.English("This greatsword belonged to Lord Gwyn's Knight Artorias, who fell to the Abyss. Swallowed by the Dark with its master, this sword is tainted by the Abyss, and now its strength reflects its wielder's humanity.");
            AbyssGreatsword.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            AbyssGreatsword.RequiredItems.Add("Silver", 40);
            AbyssGreatsword.RequiredItems.Add("BlackMetal", 20);
            AbyssGreatsword.RequiredItems.Add("TitaniteSlab", 20);
            AbyssGreatsword.RequiredItems.Add("TrophyWolf", 1);
            DarkSilverTracer.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DarkSilverTracer.ApplyUpgradeMap("Tier4");

            Item TinDarkmoonCatalyst = new("souls", "TinDarkmoonCatalyst", "assets");
            TinDarkmoonCatalyst.Description.English("This greatsword belonged to Lord Gwyn's Knight Artorias, who fell to the Abyss. Swallowed by the Dark with its master, this sword is tainted by the Abyss, and now its strength reflects its wielder's humanity.");
            TinDarkmoonCatalyst.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            TinDarkmoonCatalyst.RequiredItems.Add("Silver", 40);
            TinDarkmoonCatalyst.RequiredItems.Add("BlackMetal", 20);
            TinDarkmoonCatalyst.RequiredItems.Add("TitaniteSlab", 20);
            TinDarkmoonCatalyst.RequiredItems.Add("TrophyWolf", 1);
            TinDarkmoonCatalyst.RequiredUpgradeItems.Add("TitaniteShard", 1);
            TinDarkmoonCatalyst.ApplyUpgradeMap("Tier4");

            Item dragontooth = new("souls", "dragontooth", "assets");
            dragontooth.Name.English("Dragon Tooth"); // You can use this to fix the display name in code
            dragontooth.Description.English("Created from an everlasting dragon tooth. Legendary great hammer of Havel the Rock. The dragon tooth will never break as it is harder than stone, and it grants its wielder resistance to magic and flame");
            dragontooth.Crafting.Add("BlacksmithAltar", 4); // Custom crafting stations can be specified as a string
            dragontooth.RequiredItems.Add("TitaniteSlab", 20);
            dragontooth.RequiredItems.Add("Stone", 120);
            dragontooth.RequiredItems.Add("YmirRemains", 25);
            dragontooth.RequiredItems.Add("BoneFragments", 50);
            DarkSilverTracer.RequiredUpgradeItems.Add("TitaniteShard", 1);
            DarkSilverTracer.ApplyUpgradeMap("Tier4");
            Debug.Log("[PungusSouls] AFTER Tier4");
            #endregion Tier 4
            #region Tier 5

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
                Item BlackIronShield = new("souls", "BlackIronShield", "assets");
            BlackIronShield.Name.English("Black Iron GreatShield"); // You can use this to fix the display name in code
            BlackIronShield.Description.English("Greatshield of the might knight Tarkus. Built of special black iron and even heavier than Knight Berenike's tower shield. Especially resistant to fire attacks and effective for shield bashing.");
            BlackIronShield.Crafting.Add("BlacksmithAltar", 5); // Custom crafting stations can be specified as a string
            BlackIronShield.RequiredItems.Add("BlackMetal", 40);
            BlackIronShield.RequiredItems.Add("Eitr", 10);
            BlackIronShield.RequiredItems.Add("Wood", 40);
            BlackIronShield.RequiredItems.Add("TitaniteSlab", 10);
            BlackIronShield.RequiredUpgradeItems.Add("TitaniteShard", 1);
                BlackIronShield.ApplyUpgradeMap("Tier5");

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

            Item RingedKnightPairedGreatswords = new("souls", "RingedKnightPairedGreatswords", "assets");
            RingedKnightPairedGreatswords.Name.English("Great Lord GreatSword"); // You can use this to fix the display name in code
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
            GameObject gameObject17 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_crystalgolem_hurt");
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
            GameObject gameObject50 = ItemManager.PrefabManager.RegisterPrefab("souls", "hellkitedrakeattack");
            GameObject gameObject51 = ItemManager.PrefabManager.RegisterPrefab("souls", "hellkitedrakeattack1");
            GameObject gameObject52 = ItemManager.PrefabManager.RegisterPrefab("souls", "hellkitedrakeattack2");
            GameObject gameObject53 = ItemManager.PrefabManager.RegisterPrefab("souls", "hellkite_breath_fly");
            GameObject gameObject54 = ItemManager.PrefabManager.RegisterPrefab("souls", "hellkitebreath");
            GameObject gameObject55 = ItemManager.PrefabManager.RegisterPrefab("souls", "hellkitedrakeattack3");
            GameObject BoarCompanionSpawn = ItemManager.PrefabManager.RegisterPrefab("souls", "BoarCompanionSpawn");

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
            /*            Item spawn_marikameteors = new("souls", "spawn_marikameteors", "assets");
                        spawn_marikameteors.Configurable = Configurability.Disabled;
                        Item MarikaMeteors = new("souls", "MarikaMeteors", "assets");
                        MarikaMeteors.Configurable = Configurability.Disabled;
                        Item projectile_marikameteor = new ("souls", "projectile_marikameteor", "assets");
                        projectile_marikameteor.Configurable = Configurability.Disabled;*/
            //Item charred_magestaff_fire_marika = new("souls", "charred_magestaff_fire_marika", "assets");
            //charred_magestaff_fire_marika.Configurable = Configurability.Disabled;
            /*  Item GreatLordGreatSword1 = new("souls", "GreatLordGreatSword1", "assets");
            GreatLordGreatSword1.Configurable = Configurability.Disabled;
            Item Gwyn_Kick = new("souls", "Gwyn_Kick", "assets");
            Gwyn_Kick.Configurable = Configurability.Disabled;
            Item GreatLordGreatSword2 = new("souls", "GreatLordGreatSword2", "assets");
            GreatLordGreatSword2.Configurable = Configurability.Disabled;
            Item GreatLordGreatSword3 = new("souls", "GreatLordGreatSword3", "assets");
            GreatLordGreatSword3.Configurable = Configurability.Disabled;
            Item GreatLordGreatSword4 = new("souls", "GreatLordGreatSword4", "assets");
            GreatLordGreatSword4.Configurable = Configurability.Disabled;
            Item DragonSlayerSpear1 = new("souls", "DragonSlayerSpear1", "assets");
            DragonSlayerSpear1.Configurable = Configurability.Disabled;
            Item DragonSlayerSpear2 = new("souls", "DragonSlayerSpear2", "assets");
            DragonSlayerSpear2.Configurable = Configurability.Disabled;
            Item DragonSlayerSpear3 = new("souls", "DragonSlayerSpear3", "assets");
            DragonSlayerSpear3.Configurable = Configurability.Disabled;
            Item SilverKnightSpear1 = new("souls", "SilverKnightSpear1", "assets");
            SilverKnightSpear1.Configurable = Configurability.Disabled;
            Item SilverKnightSpear2 = new("souls", "SilverKnightSpear2", "assets");
            SilverKnightSpear2.Configurable = Configurability.Disabled;
            Item SilverKnightSword1 = new("souls", "SilverKnightSword1", "assets");
            SilverKnightSword1.Configurable = Configurability.Disabled;
            Item SilverKnightShield1 = new("souls", "SilverKnightShield1", "assets");
            SilverKnightShield1.Configurable = Configurability.Disabled;
            Item Seath_AOE = new("souls", "Seath_AOE", "assets");
            Seath_AOE.Configurable = Configurability.Disabled;
            Item Seath_Beam = new("souls", "Seath_Beam", "assets");
            Seath_Beam.Configurable = Configurability.Disabled;
            Item Seath_crystal_spawn = new("souls", "Seath_crystal_spawn", "assets");
            Seath_crystal_spawn.Configurable = Configurability.Disabled;
            Item Seath_Slash = new("souls", "Seath_Slash", "assets");
            Seath_Slash.Configurable = Configurability.Disabled;
            PiecePrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "SK_Spawner", false);
            PiecePrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "OrnsteinSpawner", false);
            PiecePrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "BK_Spawner", false);
            PiecePrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "BlackKnight_Spawn", false);
            PiecePrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "Spawner_AsylumDemon", false);*/
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
            ItemManager.PrefabManager.RegisterPrefab("souls", "projectile_beam_magic");
            ItemManager.PrefabManager.RegisterPrefab("souls", "fx_lightningbolts");
            ItemManager.PrefabManager.RegisterPrefab("souls", "buff_lightning");
            ItemManager.PrefabManager.RegisterPrefab("souls", "buff_fire");
            ItemManager.PrefabManager.RegisterPrefab("souls", "buff_frost");
            ItemManager.PrefabManager.RegisterPrefab("souls", "buff_spirit");
            ItemManager.PrefabManager.RegisterPrefab("souls", "buff_poison");
            Debug.Log("[PungusSouls] AFTER Projectiles and AOEs");
            #endregion Projectiles and AOEs


            #endregion Items and Prefabs
            #region CreatureManager Example Code

            /*   Creature Artorias = new("souls", "Artorias")
            {
            Biome = Heightmap.Biome.None,
            GroupSize = new CreatureManager.Range(1, 2),
            CheckSpawnInterval = 600,
            RequiredWeather = Weather.Rain | Weather.Fog,
            Maximum = 0
            };
            Artorias.Localize().English("Artorias");
            Artorias.Drops["Wood"].Amount = new CreatureManager.Range(1, 2);
            Artorias.Drops["Wood"].DropChance = 100f;
           

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
                Biome = Heightmap.Biome.AshLands,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 900,
                Maximum = 3
            };

            CrystalGolem.Localize().English("Crystal Golem");
            CrystalGolem.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(1, 3);
            CrystalGolem.Drops["TwinklingTitanite"].DropChance = 75f;

            Creature BlackKnight = new("souls", "BlackKnight")

            {
                Biome = Heightmap.Biome.AshLands,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 900,
                Maximum = 3
            };

            BlackKnight.Localize().English("BlackKnight");
            BlackKnight.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(1, 3);
            BlackKnight.Drops["TwinklingTitanite"].DropChance = 75f;

            Creature SilverKnight = new("souls", "SilverKnight")
            {
                Biome = Heightmap.Biome.Mountain,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 600,
                RequiredWeather = Weather.Rain | Weather.Fog,
                Maximum = 0,
            };

            SilverKnight.Localize().English("Silver Knight");
            SilverKnight.Drops["Wood"].Amount = new CreatureManager.Range(1, 2);
            SilverKnight.Drops["Wood"].DropChance = 100f;

            /*            
            Creature DragonSlayerOrnstein = new("souls", "DragonSlayerOrnstein")
            {
            Biome = Heightmap.Biome.None,
            GroupSize = new CreatureManager.Range(1, 2),
            CheckSpawnInterval = 600,
            RequiredWeather = Weather.Rain | Weather.Fog,
            Maximum = 0
            };
            DragonSlayerOrnstein.Localize().English("DragonSlayerOrnstein");
            DragonSlayerOrnstein.Drops["Wood"].Amount = new CreatureManager.Range(1, 2);
            DragonSlayerOrnstein.Drops["Wood"].DropChance = 100f;

            Creature Gwyn = new("souls", "Gwyn")
            {
            Biome = Heightmap.Biome.None,
            GroupSize = new CreatureManager.Range(1, 2),
            CheckSpawnInterval = 600,
            RequiredWeather = Weather.Rain | Weather.Fog,
            Maximum = 0
            };
            Gwyn.Localize().English("Gwyn");
            Gwyn.Drops["Wood"].Amount = new CreatureManager.Range(1, 2);
            Gwyn.Drops["Wood"].DropChance = 100f;
            */


            Creature FangBoar = new("souls", "FangBoar")

            {
                RequiredGlobalKey = GlobalKey.KilledBonemass,
                Biome = Heightmap.Biome.Meadows,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 12000,
                Maximum = 1
            };

            FangBoar.Localize().English("Fang Boar");
            FangBoar.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(2, 4);
            FangBoar.Drops["TwinklingTitanite"].DropChance = 75f;

            Creature FangBoarCompanion = new("souls", "FangBoarCompanion")

            {
                SpawnChance = 0f,
                CanSpawn = false
            };

            FangBoarCompanion.Localize().English("Fang Boar");



            Creature GiantDad = new("souls", "GiantDad")

            {
                RequiredGlobalKey = GlobalKey.KilledBonemass,
                Biome = Heightmap.Biome.Meadows,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 12000,
                Maximum = 1
            };

            GiantDad.Localize().English("GiantDad");
            GiantDad.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(2, 4);
            GiantDad.Drops["TwinklingTitanite"].DropChance = 75f;


            Creature GiantMushroom = new("souls", "GiantMushroom")

            {
                RequiredGlobalKey = GlobalKey.KilledBonemass,
                Biome = Heightmap.Biome.BlackForest | Heightmap.Biome.Meadows,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 1600,
                Maximum = 2
            };

            GiantMushroom.Localize().English("Giant Mushroom");
            GiantMushroom.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(1, 3);
            GiantMushroom.Drops["TwinklingTitanite"].DropChance = 75f;
            GiantMushroom.Drops["Mushroom"].Amount = new CreatureManager.Range(1, 3);
            GiantMushroom.Drops["Mushroom"].DropChance = 75f;

            Creature GiantMushroomBro = new("souls", "GiantMushroomBro")
            {
                SpawnChance = 0f,
                CanSpawn = false
            };

            GiantMushroomBro.Localize().English("Giant Mushroom Bro");
            Creature BabyMushroom = new("souls", "BabyMushroom")
            {
                SpawnChance = 0f,
                CanSpawn = false
            };
            BabyMushroom.Localize().English("Baby Mushroom Bro");


            Creature TaurusDemon = new("souls", "TaurusDemon")

            {
                RequiredGlobalKey = GlobalKey.KilledElder,
                Biome = Heightmap.Biome.Meadows | Heightmap.Biome.BlackForest | Heightmap.Biome.AshLands,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 3500,
                Maximum = 1
            };

            TaurusDemon.Localize().English("TaurusDemon");
            TaurusDemon.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(2, 3);
            TaurusDemon.Drops["TwinklingTitanite"].DropChance = 75f;


            Creature Wyvern = new("souls", "Wyvern")

            {
                RequiredGlobalKey = GlobalKey.KilledModer,
                Biome = Heightmap.Biome.Mountain,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 600,
                Maximum = 3
            };
            Wyvern.Localize().English("Wyvern");
            Wyvern.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(2, 3);
            Wyvern.Drops["TwinklingTitanite"].DropChance = 100f;

            Creature Sif = new Creature("souls", "Sif")
            {
                Biome = Heightmap.Biome.None,
                Maximum = 0,
                FoodItems = "RawMeat"
            };
            Sif.Localize().English("Sif");

            Creature SweetShalquoir = new Creature("souls", "SweetShalquoir")
            {
            CanSpawn = false,

            };
            SweetShalquoir.Localize().English("Sweet Shalquoir");
            SweetShalquoir.Drops["Wood"].Amount = new CreatureManager.Range(1f, 2f);
            SweetShalquoir.Drops["Wood"].DropChance = 100f;
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
            CanSpawn = false,
            };
            HellkiteDrake.Localize().English("Hellkite Drake");
                Creature Marika = new Creature("souls", "queenmarikacompanion")
                {
                    Biome = Heightmap.Biome.None,
                    CanSpawn = false,
                    ConfigurationEnabled = false,
                };
                Marika.Localize().English("Queen Marika");
            Creature FangBoarBoss = new Creature("souls", "FangBoarBoss");
            Debug.Log("[PungusSouls] AFTER Creatures");
            #endregion
            Debug.Log("[PungusSouls] AWAKE COMPLETE");

            Animations.LoadAssets();
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
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

                Character[] characters = UnityEngine.Object.FindObjectsOfType<Character>();
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