using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using SkillManager;
using StatusEffectManager;
using UnityEngine;
using LocalizationManager;
using PrefabManager = ItemManager.PrefabManager;
using Range = LocationManager.Range;
using System.Runtime.Remoting.Contexts;
using static Heightmap;
using static MeleeWeaponTrail;
using BlacksmithTools;
using UnityEngine.Audio;

namespace PungusSouls
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class PungusSoulsPlugin : BaseUnityPlugin
    {
        internal const string ModName = "PungusSouls";
        internal const string ModVersion = "0.0.2";
        internal const string Author = "Pungus";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource PungusSoulsLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        /*public static PungusSoulsPlugin context;*/
        private static readonly ConfigSync ConfigSync = new(ModGUID)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        // Location Manager variables
        public Texture2D tex;
        private Sprite mySprite;
        private SpriteRenderer sr;

        public enum Toggle
        {
            On = 1,
            Off = 0
        }
        public void Awake()
        {
            /*context = this;*/
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            /*            Animations.AnimationAwake();
                        _harmony.PatchAll();*/

            #region PieceManager Example Code

            // Globally turn off configuration options for your pieces, omit if you don't want to do this.
            BuildPiece.ConfigurationEnabled = false;

            // Format: new("AssetBundleName", "PrefabName", "FolderName");

            PiecePrefabManager.RegisterPrefab("wep", "BlacksmithAltar");
            

/*            BuildPiece CrystalAnvil = new("wep", "CrystalAnvil");

            CrystalAnvil.Name.English("Arcane Anvil"); // Localize the name and description for the building piece for a language.
            CrystalAnvil.Description.English("Blacksmith Altar Extension");
            CrystalAnvil.RequiredItems.Add("Crystal", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            CrystalAnvil.RequiredItems.Add("SurtlingCore", 10, true);
            CrystalAnvil.RequiredItems.Add("GreydwarfEye", 10, true);
            CrystalAnvil.Category.Add(PieceManager.BuildPieceCategory.Crafting);
            CrystalAnvil.Crafting.Set("BlacksmithAltar"); // Set a crafting station requirement for the piece.
            CrystalAnvil.Extension.Set("BlacksmithAltar", 2); // Makes this piece a station extension, can change the max station distance by changing the second value. Use strings for custom tables.
            CrystalAnvil.Snapshot();*/


            // Format: new("AssetBundleName", "PrefabName", "FolderName");
            BuildPiece Lantern = new("wep", "Lantern");

            Lantern.Name.English("Arcane Lantern"); // Localize the name and description for the building piece for a language.
            Lantern.Description.English("Blacksmith Altar Extension");
            Lantern.RequiredItems.Add("Resin", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            Lantern.RequiredItems.Add("Iron", 10, true);
            Lantern.RequiredItems.Add("ElderBark", 10, true);
            Lantern.RequiredItems.Add("TwinklingTitanite", 15, true);
            Lantern.Category.Add(PieceManager.BuildPieceCategory.Crafting);
            Lantern.Crafting.Set("BlacksmithAltar"); // Set a crafting station requirement for the piece.
            Lantern.Extension.Set("BlacksmithAltar", 2);
            Lantern.Snapshot();

            BuildPiece Lantern1 = new("wep", "Lantern1");

            Lantern1.Name.English("Hunters Lantern"); // Localize the name and description for the building piece for a language.
            Lantern1.Description.English("Blacksmith Altar Extension");
            Lantern1.RequiredItems.Add("FineWood", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            Lantern1.RequiredItems.Add("Bronze", 10, true);
            Lantern1.RequiredItems.Add("Amber", 10, true);
            Lantern1.RequiredItems.Add("TwinklingTitanite", 10, true);
            Lantern1.Category.Add(PieceManager.BuildPieceCategory.Crafting);
            Lantern1.Crafting.Set("BlacksmithAltar"); // Set a crafting station requirement for the piece.
            Lantern1.Extension.Set("BlacksmithAltar", 2);
            Lantern1.Snapshot();

            // Format: new("AssetBundleName", "PrefabName", "FolderName");
            BuildPiece ArcaneStone = new("wep", "ArcaneStone");

            ArcaneStone.Name.English("Arcane Stone"); // Localize the name and description for the building piece for a language.
            ArcaneStone.Description.English("Blacksmith Altar Extension");
            ArcaneStone.RequiredItems.Add("Stone", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            ArcaneStone.RequiredItems.Add("Flint", 10, true);
            ArcaneStone.RequiredItems.Add("GreydwarfEye", 10, true);
            ArcaneStone.RequiredItems.Add("TwinklingTitanite", 5, true);
            ArcaneStone.Category.Add(PieceManager.BuildPieceCategory.Crafting);
            ArcaneStone.Crafting.Set("BlacksmithAltar"); // Set a crafting station requirement for the piece.
            ArcaneStone.Extension.Set("BlacksmithAltar", 2);
            ArcaneStone.Snapshot();

            BuildPiece NamelessStatue = new("wep", "NamelessStatue");

            NamelessStatue.Name.English("Nameless Statue"); // Localize the name and description for the building piece for a language.
            NamelessStatue.Description.English("Blacksmith Altar Extension");
            NamelessStatue.RequiredItems.Add("Stone", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            NamelessStatue.RequiredItems.Add("Crystal", 10, true);
            NamelessStatue.RequiredItems.Add("BlackMetal", 10, true);
            NamelessStatue.RequiredItems.Add("TwinklingTitanite", 30, true);
            NamelessStatue.Category.Add(PieceManager.BuildPieceCategory.Crafting);
            NamelessStatue.Crafting.Set("BlacksmithAltar"); // Set a crafting station requirement for the piece.
            NamelessStatue.Extension.Set("BlacksmithAltar", 2);
            NamelessStatue.Snapshot();

            BuildPiece Bonefire = new("wep", "Bonefire");

            Bonefire.Name.English("Bonefire"); // Localize the name and description for the building piece for a language.
            Bonefire.Description.English("Blacksmith Altar Extension");
            Bonefire.RequiredItems.Add("BoneFragments", 20, true); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            Bonefire.RequiredItems.Add("Crystal", 10, true);
            Bonefire.RequiredItems.Add("Silver", 10, true);
            Bonefire.RequiredItems.Add("TwinklingTitanite", 20, true);
            Bonefire.Category.Add(PieceManager.BuildPieceCategory.Crafting);
            Bonefire.Crafting.Set("BlacksmithAltar"); // Set a crafting station requirement for the piece.
            Bonefire.Extension.Set("BlacksmithAltar", 2);
            Bonefire.Snapshot();

            #endregion
            /*#region SkillManager Example Code

            Skill
                tenacity = new("Tenacity",
                    "tenacity-icon.png"); // Skill name along with the skill icon. By default the icon is found in the icons folder. Put it there if you wish to load one.

            tenacity.Description.English("Reduces damage taken by 0.2% per level.");
            tenacity.Name.German("Hartnäckigkeit"); // Use this to localize values for the name
            tenacity.Description.German(
                "Reduziert erlittenen Schaden um 0,2% pro Stufe."); // You can do the same for the description
            tenacity.Configurable = true;

            #endregion*/
            #region Location Manager

            _ = new LocationManager.Location("location2", "FireLinkShrine")
            {
                MapIcon = "firelinkicon.png",
                Rotation = Rotation.Fixed,
                ShowMapIcon = ShowIcon.Explored,
                Biome = Heightmap.Biome.Meadows,
                SpawnArea = Heightmap.BiomeArea.Median,
                HeightDelta = new Range(0, 6),
                SpawnDistance = new Range(450, 750),
                SpawnAltitude = new Range(30, 100),
                ForestThreshold = new Range(0, 1),
                Count = 1,
                Prioritize = true,
                Unique = true
            };

            /*_ = new LocationManager.Location("location2", "undeadasylum1")
            {
                MapIcon = "portalicon.png",
                Rotation = Rotation.Fixed,
                ShowMapIcon = ShowIcon.Explored,
                Biome = Heightmap.Biome.Mountain,
                SpawnArea = Heightmap.BiomeArea.Median,
                SpawnDistance = new Range(1000, 9000),
                SpawnAltitude = new Range(300, 1000),
                MinimumDistanceFromGroup = 100,
                HeightDelta = new Range(1, 15),
                ForestThreshold = new Range(0, 3),
                Count = 1,
                Prioritize = true,
                Unique = true
            };

            _ = new LocationManager.Location("location2", "church");
            _ = new LocationManager.Location("location2", "anorlondo")
            {
                MapIcon = "portalicon.png",
                Rotation = Rotation.Fixed,
                ShowMapIcon = ShowIcon.Explored,
                Biome = Heightmap.Biome.DeepNorth,
                SpawnArea = Heightmap.BiomeArea.Median,
                SpawnDistance = new Range(8000, 10000),
                SpawnAltitude = new Range(30, 200),
                MinimumDistanceFromGroup = 100,
                Count = 1,
                Prioritize = true,
                Unique = true
            };
            _ = new LocationManager.Location("location2", "pworld");
            _ = new LocationManager.Location("location2", "FirelinkAltar")
            {
                MapIcon = "portalicon.png",
                Rotation = Rotation.Fixed,
                ShowMapIcon = ShowIcon.Explored,
                Biome = Heightmap.Biome.AshLands,
                SpawnArea = Heightmap.BiomeArea.Median,
                SpawnDistance = new Range(8000, 10000),
                SpawnAltitude = new Range(30, 200),
                HeightDelta = new Range(0, 20),
                MinimumDistanceFromGroup = 100,
                Count = 1,
                Prioritize = true,
                Unique = true
            };

            _ = new LocationManager.Location("location2", "NexusTop")
            {
                MapIcon = "portalicon.png",
                ShowMapIcon = ShowIcon.Explored,
                Biome = Heightmap.Biome.Meadows,
                SpawnDistance = new Range(10, 3000),
                SpawnAltitude = new Range(30, 200),
                MinimumDistanceFromGroup = 100,
                Count = 1,
                Prioritize = true,
                Unique = true


            };

            _ = new LocationManager.Location("location2", "House1")
            {
                MapIcon = "portalicon.png",
                ShowMapIcon = ShowIcon.Explored,
                Biome = Heightmap.Biome.Meadows,
                SpawnDistance = new Range(10, 3000),
                SpawnAltitude = new Range(30, 200),
                MinimumDistanceFromGroup = 100,
                Count = 1,
                Prioritize = true,
                Unique = true


            };*/

            #region Location Notes

            // MapIcon                      Sets the map icon for the location.
            // ShowMapIcon                  When to show the map icon of the location. Requires an icon to be set. Use "Never" to not show a map icon for the location. Use "Always" to always show a map icon for the location. Use "Explored" to start showing a map icon for the location as soon as a player has explored the area.
            // MapIconSprite                Sets the map icon for the location.
            // CanSpawn                     Can the location spawn at all.
            // SpawnArea                    If the location should spawn more towards the edge of the biome or towards the center. Use "Edge" to make it spawn towards the edge. Use "Median" to make it spawn towards the center. Use "Everything" if it doesn't matter.</para>
            // Prioritize                   If set to true, this location will be prioritized over other locations, if they would spawn in the same area.
            // PreferCenter                 If set to true, Valheim will try to spawn your location as close to the center of the map as possible.
            // Rotation                     How to rotate the location. Use "Fixed" to use the rotation of the prefab. Use "Random" to randomize the rotation. Use "Slope" to rotate the location along a possible slope.
            // HeightDelta                  The minimum and maximum height difference of the terrain below the location.
            // SnapToWater                  If the location should spawn near water.
            // ForestThreshold              If the location should spawn in a forest. Everything above 1.15 is considered a forest by Valheim. 2.19 is considered a thick forest by Valheim.
            // Biome
            // SpawnDistance                Minimum and maximum range from the center of the map for the location.
            // SpawnAltitude                Minimum and maximum altitude for the location.
            // MinimumDistanceFromGroup     Locations in the same group will keep at least this much distance between each other.
            // GroupName                    The name of the group of the location, used by the minimum distance from group setting.
            // Count                        Maximum number of locations to spawn in. Does not mean that this many locations will spawn. But Valheim will try its best to spawn this many, if there is space.
            // Unique                       If set to true, all other locations will be deleted, once the first one has been discovered by a player.

            #endregion
            #endregion
            #region StatusEffectManager Example Code

            CustomSE Lifesteal = new("wep", "Lifesteal");
            Lifesteal.Name.English("Lifesteal");
            Lifesteal.Type = EffectType.Attack;
            CustomSE SE_GrassShield = new("wep", "SE_GrassShield");
            SE_GrassShield.Name.English("Stamina Regen");
            CustomSE TridentBuff = new("wep", "TridentBuff");
            TridentBuff.Name.English("TridentBuff");
            /*           Lifesteal.IconSprite = null;
                       Lifesteal.Name.German("Toxizität");
                       Lifesteal.Effect.m_startMessageType = MessageHud.MessageType.TopLeft;
                       Lifesteal.Effect.m_startMessage = "My Cool Status Effect Started";
                       Lifesteal.Effect.m_stopMessageType = MessageHud.MessageType.TopLeft;
                       Lifesteal.Effect.m_stopMessage = "Not cool anymore, ending effect.";
                       Lifesteal.Effect.m_tooltip = "<color=orange>Toxic damage over time</color>";
                       Lifesteal.AddSEToPrefab(Lifesteal, "DaggerPrisc");

                       CustomSE drunkeffect = new("se_drunk", "se_drunk_effect");
                       drunkeffect.Name.English("Drunk"); // You can use this to fix the display name in code
                       drunkeffect.Icon = "DrunkIcon.png"; // Use this to add an icon (64x64) for the status effect. Put your icon in an "icons" folder
                       drunkeffect.Name.German("Betrunken"); // Or add translations for other languages
                       drunkeffect.Effect.m_startMessageType = MessageHud.MessageType.Center; // Specify where the start effect message shows
                       drunkeffect.Effect.m_startMessage = "I'm drunk!"; // What the start message says
                       drunkeffect.Effect.m_stopMessageType = MessageHud.MessageType.Center; // Specify where the stop effect message shows
                       drunkeffect.Effect.m_stopMessage = "Sober...again."; // What the stop message says
                       drunkeffect.Effect.m_tooltip = "<color=red>Your vision is blurry</color>"; // Tooltip that will describe the effect applied to the player
                       drunkeffect.AddSEToPrefab(drunkeffect, "TankardAnniversary"); // Adds the status effect to the Anniversary Tankard. Applies when equipped.

                       // Create a new status effect in code and apply it to a prefab.
                       CustomSE codeSE = new("CodeStatusEffect");
                       codeSE.Name.English("New Effect");
                       codeSE.Type = EffectType.Consume; // Set the type of status effect this should be.
                       codeSE.Icon = "ModDevPower.png";
                       codeSE.Name.German("Betrunken"); // Or add translations for other languages
                       codeSE.Effect.m_startMessageType = MessageHud.MessageType.Center; // Specify where the start effect message shows
                       codeSE.Effect.m_startMessage = "Mod Dev power, granted."; // What the start message says
                       codeSE.Effect.m_stopMessageType = MessageHud.MessageType.Center; // Specify where the stop effect message shows
                       codeSE.Effect.m_stopMessage = "Mod Dev power, removed."; // What the stop message says
                       codeSE.Effect.m_tooltip = "<color=green>You now have Mod Dev POWER!</color>"; // Tooltip that will describe the effect applied to the player
                       codeSE.AddSEToPrefab(codeSE, "SwordCheat"); // Adds the status effect to the Cheat Sword. Applies when equipped.
           */


            #endregion

            #region ItemManager Materials

            Item TwinklingTitanite = new("wep", "TwinklingTitanite", "assets");
            TwinklingTitanite.Name.English("Twinkling Titanite"); // You can use this to fix the display name in code
            TwinklingTitanite.Description.English("This weapon-reinforcing titanite is imbued with a particularly powerful energy. After this titanite was peeled from its Slab, it is said that it received a special power, but its specific nature is not clear.");
            TwinklingTitanite.Snapshot();

            TwinklingTitanite.DropsFrom.Add("Boar", .10f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Deer", .20f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Neck", .10f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Greyling", .15f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Greydwarf", .15f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Greydwarf_Shaman", .15f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Greydwarf_Elite", .20f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Troll", .25f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Skeleton", .20f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Blob", .20f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Surtling", .20f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Leech", .20f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Draugr", .20f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Draugr_Elite", .20f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Wraith", .20f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Abomination", .20f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Wolf", .25f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Hatchling", .25f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Deathsquito", .30f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Goblin", .35f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("GoblinBrute", .40f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("BlobTar", .35f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("GoblinShaman", .40f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Lox", .40f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Seeker", .50f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("SeekerBrute", .50f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Gjall", .50f, 1, 1);
            TwinklingTitanite.DropsFrom.Add("Eikthyr", 100f, 3, 7);
            TwinklingTitanite.DropsFrom.Add("gd_king", 100f, 4, 8);
            TwinklingTitanite.DropsFrom.Add("Bonemass", 100f, 5, 12);
            TwinklingTitanite.DropsFrom.Add("Dragon", 100f, 7, 13);
            TwinklingTitanite.DropsFrom.Add("GoblinKing", 100f, 10, 15);
            TwinklingTitanite.DropsFrom.Add("SeekerQueen", 100f, 12, 16);
            #endregion

            #region ItemManager Items

            Item AbyssGreatsword = new("wep", "AbyssGreatsword", "assets");
            AbyssGreatsword.Name.English("Abyss Greatsword"); // You can use this to fix the display name in code
            AbyssGreatsword.Description.English("This greatsword belonged to Lord Gwyn's Knight Artorias, who fell to the Abyss. Swallowed by the Dark with its master, this sword is tainted by the Abyss, and now its strength reflects its wielder's humanity.");
            AbyssGreatsword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            AbyssGreatsword.RequiredItems.Add("Silver", 40);
            AbyssGreatsword.RequiredItems.Add("Eitr", 20);
            AbyssGreatsword.RequiredItems.Add("BlackCore", 10);
            AbyssGreatsword.RequiredItems.Add("TrophyWolf", 1);
            AbyssGreatsword.RequiredUpgradeItems.Add("Silver", 20);
            AbyssGreatsword.RequiredUpgradeItems.Add("Eitr", 10);
            AbyssGreatsword.RequiredUpgradeItems.Add("BlackCore", 5);
            AbyssGreatsword.RequiredUpgradeItems.Add("TrophyWolf", 1);
            AbyssGreatsword.Snapshot();


            Item ArtGS = new("wep", "ArtGS", "assets");
            ArtGS.Name.English("Greatsword of Artorias"); // You can use this to fix the display name in code
            ArtGS.Description.English("Sword born from the soul of the great grey wolf Sif, guardian of the grave of the Abysswalker Knight Artorias. Sir Artorias hunted the Darkwraiths, and his sword strikes harder against dark servants.");
            ArtGS.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            ArtGS.RequiredItems.Add("Silver", 40);
            ArtGS.RequiredItems.Add("Eitr", 20);
            ArtGS.RequiredItems.Add("BlackCore", 10);
            ArtGS.RequiredItems.Add("TrophyWolf", 1);
            ArtGS.RequiredUpgradeItems.Add("Silver", 20);
            ArtGS.RequiredUpgradeItems.Add("Eitr", 10);
            ArtGS.RequiredUpgradeItems.Add("BlackCore", 5);
            ArtGS.RequiredUpgradeItems.Add("TrophyWolf", 1);
            ArtGS.Snapshot();

            Item ArtoriasGreatshield = new("wep", "ArtoriasGreatshield", "assets");
            ArtoriasGreatshield.Name.English("GreatShield of Artorias"); // You can use this to fix the display name in code
            ArtoriasGreatshield.Description.English("Shield born from the soul of the great grey wolf Sif, guardian of the grave of the Abysswalker Knight Artorias.");
            ArtoriasGreatshield.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            ArtoriasGreatshield.RequiredItems.Add("Silver", 40);
            ArtoriasGreatshield.RequiredItems.Add("Eitr", 20);
            ArtoriasGreatshield.RequiredItems.Add("BlackCore", 10);
            ArtoriasGreatshield.RequiredItems.Add("TrophyWolf", 1);
            ArtoriasGreatshield.RequiredUpgradeItems.Add("Silver", 20);
            ArtoriasGreatshield.RequiredUpgradeItems.Add("Eitr", 10);
            ArtoriasGreatshield.RequiredUpgradeItems.Add("BlackCore", 5);
            ArtoriasGreatshield.RequiredUpgradeItems.Add("TrophyWolf", 1);
            ArtoriasGreatshield.Snapshot();

            Item BerserkGreatsword = new("wep", "BerserkGreatsword", "assets");
            BerserkGreatsword.Name.English("Berserk Greatsword"); // You can use this to fix the display name in code
            BerserkGreatsword.Description.English("A huge hunk of metal");
            BerserkGreatsword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BerserkGreatsword.RequiredItems.Add("Bronze", 20);
            BerserkGreatsword.RequiredItems.Add("RoundLog", 10);
            BerserkGreatsword.RequiredItems.Add("TwinklingTitanite", 5);
            BerserkGreatsword.RequiredItems.Add("TrophyGreydwarfBrute", 20);
            BerserkGreatsword.RequiredUpgradeItems.Add("Bronze", 10);
            BerserkGreatsword.RequiredUpgradeItems.Add("RoundLog", 10);
            BerserkGreatsword.RequiredUpgradeItems.Add("Resin", 5);
            BerserkGreatsword.RequiredUpgradeItems.Add("TrophyGreydwarfBrute", 1);
            BerserkGreatsword.Snapshot();

            Item BlackKnightGreatAxe = new("wep", "BlackKnightGreatAxe", "assets");
            BlackKnightGreatAxe.Name.English("Black Knight GreatAxe"); // You can use this to fix the display name in code
            BlackKnightGreatAxe.Description.English("Greataxe of the Black Knights who wander Lordran. Used to face Chaos demons. The large motion that puts the weight of the body into the attack reflects the great size of their adversaries long ago.");
            BlackKnightGreatAxe.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BlackKnightGreatAxe.RequiredItems.Add("BlackMetal", 20);
            BlackKnightGreatAxe.RequiredItems.Add("Flametal", 10);
            BlackKnightGreatAxe.RequiredItems.Add("Silver", 20);
            BlackKnightGreatAxe.RequiredItems.Add("TwinklingTitanite", 20);
            BlackKnightGreatAxe.RequiredUpgradeItems.Add("BlackMetal", 10);
            BlackKnightGreatAxe.RequiredUpgradeItems.Add("Flametal", 10);
            BlackKnightGreatAxe.RequiredUpgradeItems.Add("Silver", 10);
            BlackKnightGreatAxe.RequiredUpgradeItems.Add("TwinklingTitanite", 10);
            BlackKnightGreatAxe.Snapshot();

            Item BlackKnightHalberd = new("wep", "BlackKnightHalberd", "assets");
            BlackKnightHalberd.Name.English("Black Knight Halberd"); // You can use this to fix the display name in code
            BlackKnightHalberd.Description.English("Halberd of the black knights who wander Lordran. Used to face chaos demons.");
            BlackKnightHalberd.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BlackKnightHalberd.RequiredItems.Add("BlackMetal", 20);
            BlackKnightHalberd.RequiredItems.Add("Flametal", 10);
            BlackKnightHalberd.RequiredItems.Add("Silver", 20);
            BlackKnightHalberd.RequiredItems.Add("TwinklingTitanite", 10);
            BlackKnightHalberd.RequiredUpgradeItems.Add("BlackMetal", 10);
            BlackKnightHalberd.RequiredUpgradeItems.Add("Flametal", 10);
            BlackKnightHalberd.RequiredUpgradeItems.Add("Silver", 10);
            BlackKnightHalberd.RequiredUpgradeItems.Add("TwinklingTitanite", 10);
            BlackKnightHalberd.Snapshot();

            Item BlackKnightSword = new("wep", "BlackKnightSword", "assets");
            BlackKnightSword.Name.English("Black Knight Sword"); // You can use this to fix the display name in code
            BlackKnightSword.Description.English("sword of the Black Knights who wander Lordran. Used to face chaos demons. The Large motion that puts the weight of the body into the attack reflects the great size of their adversaries long ago.");
            BlackKnightSword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BlackKnightSword.RequiredItems.Add("BlackMetal", 20);
            BlackKnightSword.RequiredItems.Add("Flametal", 5);
            BlackKnightSword.RequiredItems.Add("Silver", 20);
            BlackKnightSword.RequiredItems.Add("TwinklingTitanite", 5);
            BlackKnightSword.RequiredUpgradeItems.Add("BlackMetal", 10);
            BlackKnightSword.RequiredUpgradeItems.Add("Flametal", 5);
            BlackKnightSword.RequiredUpgradeItems.Add("Silver", 10);
            BlackKnightSword.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
            BlackKnightSword.Snapshot();

            Item BlackKnightShield = new("wep", "BlackKnightShield", "assets");
            BlackKnightShield.Name.English("Black Knight Shield"); // You can use this to fix the display name in code
            BlackKnightShield.Description.English("Shield of the Black Knights that wander Lordan. A flowing canal is chiseled deeply into its face. Long ago, the black knights faced the chaos demons, and were charred black, but their shields became highly resistant to fire.");
            BlackKnightShield.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BlackKnightShield.RequiredItems.Add("BlackMetal", 20);
            BlackKnightShield.RequiredItems.Add("Flametal", 5);
            BlackKnightShield.RequiredItems.Add("Silver", 20);
            BlackKnightShield.RequiredItems.Add("TwinklingTitanite", 5);
            BlackKnightShield.RequiredUpgradeItems.Add("BlackMetal", 10);
            BlackKnightShield.RequiredUpgradeItems.Add("Flametal", 5);
            BlackKnightShield.RequiredUpgradeItems.Add("Silver", 10);
            BlackKnightShield.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
            BlackKnightShield.Snapshot();

            Item BlackKnightUGS = new("wep", "BlackKnightUGS", "assets");
            BlackKnightUGS.Name.English("Black Knight Greatsword"); // You can use this to fix the display name in code
            BlackKnightUGS.Description.English("Greatsword of the black knights who wander Lordran. Used to face chaos demons. The large motion that puts the weight of the body into the attack reflects the great size of their adversaries long ago.");
            BlackKnightUGS.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BlackKnightUGS.RequiredItems.Add("BlackMetal", 40);
            BlackKnightUGS.RequiredItems.Add("Flametal", 10);
            BlackKnightUGS.RequiredItems.Add("Silver", 40);
            BlackKnightUGS.RequiredItems.Add("TwinklingTitanite", 10);
            BlackKnightUGS.RequiredUpgradeItems.Add("BlackMetal", 20);
            BlackKnightUGS.RequiredUpgradeItems.Add("Flametal", 10);
            BlackKnightUGS.RequiredUpgradeItems.Add("Silver", 10);
            BlackKnightUGS.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
            BlackKnightUGS.Snapshot();

            Item BlackIronShield = new("wep", "BlackIronShield", "assets");
            BlackIronShield.Name.English("Black Iron GreatShield"); // You can use this to fix the display name in code
            BlackIronShield.Description.English("Greatshield of the might knight Tarkus. Built of special black iron and even heavier than Knight Berenike's tower shield. Especially resistant to fire attacks and effective for shield bashing.");
            BlackIronShield.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            BlackIronShield.RequiredItems.Add("Iron", 40);
            BlackIronShield.RequiredItems.Add("Tin", 10);
            BlackIronShield.RequiredItems.Add("Wood", 40);
            BlackIronShield.RequiredItems.Add("Resin", 10);
            BlackIronShield.RequiredUpgradeItems.Add("Iron", 20);
            BlackIronShield.RequiredUpgradeItems.Add("Tin", 10);
            BlackIronShield.RequiredUpgradeItems.Add("Wood", 10);
            BlackIronShield.RequiredUpgradeItems.Add("Resin", 5);
            BlackIronShield.Snapshot();

            Item ChannelerTrident = new("wep", "ChannelerTrident", "assets");
            ChannelerTrident.Name.English("Channeler Trident"); // You can use this to fix the display name in code
            ChannelerTrident.Description.English("Trident of the Six-eyed Channelers, sorcerers who serve Seath the Scaleless in collecting human specimens. Thrusted in circular motions in a unique martial arts dance that stirs nearby allies into a bloodthirsty frenzy.");
            ChannelerTrident.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            ChannelerTrident.RequiredItems.Add("Bronze", 25);
            ChannelerTrident.RequiredItems.Add("TwinklingTitanite", 5);
            ChannelerTrident.RequiredItems.Add("Tin", 20);
            ChannelerTrident.RequiredItems.Add("GreydwarfEye", 20);
            ChannelerTrident.RequiredUpgradeItems.Add("Bronze", 10);
            ChannelerTrident.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            ChannelerTrident.RequiredUpgradeItems.Add("Tin", 10);
            ChannelerTrident.RequiredUpgradeItems.Add("GreydwarfEye", 5);
            ChannelerTrident.Snapshot();

            Item CursedGreatsword = new("wep", "CursedGreatsword", "assets");
            CursedGreatsword.Name.English("Cursed Greatsword"); // You can use this to fix the display name in code
            CursedGreatsword.Description.English("Sword born from the souls of the great grey wolf Sif, guardian of the grave of the Abysswalker Knight Artorias. The sword can damage ghosts, as it was cursed when Artorias joined a covenant with the creatures of the Abyss");
            CursedGreatsword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            CursedGreatsword.RequiredItems.Add("Silver", 25);
            CursedGreatsword.RequiredItems.Add("TrophyWraith", 5);
            CursedGreatsword.RequiredItems.Add("Iron", 20);
            CursedGreatsword.RequiredItems.Add("GreydwarfEye", 20);
            CursedGreatsword.RequiredUpgradeItems.Add("Silver", 10);
            CursedGreatsword.RequiredUpgradeItems.Add("TrophyWraith", 1);
            CursedGreatsword.RequiredUpgradeItems.Add("Iron", 5);
            CursedGreatsword.RequiredUpgradeItems.Add("GreydwarfEye", 5);
            CursedGreatsword.Snapshot();

            Item DaggerPrisc = new("wep", "DaggerPrisc", "assets");
            DaggerPrisc.Name.English("Priscillas Dagger"); // You can use this to fix the display name in code
            DaggerPrisc.Description.English("This sword, one of the rare dragon weapons, came from the tail of Priscilla, the Dragon Crossbreed in the painted world of Ariamis.\r\nPossessing the power of lifehunt, it dances about when wielded, in a fashion reminiscent of the white-robed painting guardians.");
            DaggerPrisc.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DaggerPrisc.RequiredItems.Add("TwinklingTitanite", 12);
            DaggerPrisc.RequiredItems.Add("Bloodbag", 20);
            DaggerPrisc.RequiredItems.Add("KnifeChitin", 1);
            DaggerPrisc.RequiredItems.Add("TrophyLeech", 5);
            DaggerPrisc.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            DaggerPrisc.RequiredUpgradeItems.Add("Bloodbag", 10);
            DaggerPrisc.RequiredUpgradeItems.Add("KnifeChitin", 1);
            DaggerPrisc.RequiredUpgradeItems.Add("TrophyLeech", 5);
            DaggerPrisc.Snapshot();

            Item DarkMoonBow = new("wep", "DarkMoonBow", "assets");
            DarkMoonBow.Name.English("Darkmoon Bow"); // You can use this to fix the display name in code
            DarkMoonBow.Description.English("Bow born from the soul of the Dark Sun Gwyndolin, Darkmoon deity who watches over the abandoned city of the Gods, Anor Londo. This golden bow is imbued with powerful magic and is most impressive with Moonlight Arrows.");
            DarkMoonBow.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DarkMoonBow.RequiredItems.Add("FineWood", 40);
            DarkMoonBow.RequiredItems.Add("Iron", 20);
            DarkMoonBow.RequiredItems.Add("GreydwarfEye", 10);
            DarkMoonBow.RequiredItems.Add("TwinklingTitanite", 12);
            DarkMoonBow.RequiredUpgradeItems.Add("FineWood", 10);
            DarkMoonBow.RequiredUpgradeItems.Add("Iron", 5);
            DarkMoonBow.RequiredUpgradeItems.Add("GreydwarfEye", 5);
            DarkMoonBow.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            DarkMoonBow.Snapshot();

            Item DarkSilverTracer = new("wep", "DarkSilverTracer", "assets");
            DarkSilverTracer.Name.English("Dark Silver Tracer"); // You can use this to fix the display name in code
            DarkSilverTracer.Description.English("A dark silver dagger used by the Lord's Blade Ciaran, of Gwyn's Four Knights. The victim is first distracted by dazzling streaks of the Gold Tracer, then stung by the vicious poison of this dagger");
            DarkSilverTracer.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DarkSilverTracer.RequiredItems.Add("TwinklingTitanite", 10);
            DarkSilverTracer.RequiredItems.Add("Ooze", 20);
            DarkSilverTracer.RequiredItems.Add("Iron", 5);
            DarkSilverTracer.RequiredItems.Add("FineWood", 5);
            DarkSilverTracer.RequiredUpgradeItems.Add("TwinklingTitanite", 1);
            DarkSilverTracer.RequiredUpgradeItems.Add("Ooze", 5);
            DarkSilverTracer.RequiredUpgradeItems.Add("Iron", 1);
            DarkSilverTracer.Snapshot();

            Item DarkSword = new("wep", "DarkSword", "assets");
            DarkSword.Name.English("Dark Sword"); // You can use this to fix the display name in code
            DarkSword.Description.English("The sword of the knights of the Four Kings of New Londo. Its blade is wide and thick and it is wielded in an unusual manner. When the Four Kings were seduced by evil, their knights became Darkwraiths, servants of the Dark who wielded these darkswords.");
            DarkSword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DarkSword.RequiredItems.Add("ElderBark", 30);
            DarkSword.RequiredItems.Add("Coal", 20);
            DarkSword.RequiredItems.Add("Silver", 40);
            DarkSword.RequiredItems.Add("TwinklingTitanite", 15);
            DarkSword.RequiredUpgradeItems.Add("ElderBark", 10);
            DarkSword.RequiredUpgradeItems.Add("Silver", 10);
            DarkSword.RequiredUpgradeItems.Add("TwinklingTitanite", 3);
            DarkSword.Snapshot();

            Item DemonGreatHammer = new("wep", "DemonGreatHammer", "assets");
            DemonGreatHammer.Name.English("Demon Great Hammer"); // You can use this to fix the display name in code
            DemonGreatHammer.Description.English("Demon weapon built from the stone archtrees. Used by lesser demons at North Undead Asylum. This hammer is imbued with no special power, but will merrily beat foes to a pulp, provided you have the strength to wield it.");
            DemonGreatHammer.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DemonGreatHammer.RequiredItems.Add("Stone", 120);
            DemonGreatHammer.RequiredItems.Add("SledgeStagbreaker", 20);
            DemonGreatHammer.RequiredItems.Add("TwinklingTitanite", 8);
            DemonGreatHammer.RequiredItems.Add("Ruby", 20);
            DemonGreatHammer.RequiredUpgradeItems.Add("Stone", 50);
            DemonGreatHammer.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            DemonGreatHammer.RequiredUpgradeItems.Add("Ruby", 3);
            DemonGreatHammer.Snapshot();

            Item DragonGreatSword = new("wep", "DragonGreatSword", "assets");
            DragonGreatSword.Name.English("Dragon GreatSword"); // You can use this to fix the display name in code
            DragonGreatSword.Description.English("This sword, one of the rare dragon weapons, came from the tail of the stone dragon of Ash Lake, descendant of the ancient dragons");
            DragonGreatSword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DragonGreatSword.RequiredItems.Add("Stone", 120);
            DragonGreatSword.RequiredItems.Add("TrophyDragonQueen", 2);
            DragonGreatSword.RequiredItems.Add("Silver", 40);
            DragonGreatSword.RequiredItems.Add("TwinklingTitanite", 20);
            DragonGreatSword.RequiredUpgradeItems.Add("Stone", 60);
            DragonGreatSword.RequiredUpgradeItems.Add("TrophyDragonQueen", 2);
            DragonGreatSword.RequiredUpgradeItems.Add("Silver", 20);
            DragonGreatSword.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
            DragonGreatSword.Snapshot();

            Item DragonKingGreatAxe = new("wep", "DragonKingGreatAxe", "assets");
            DragonKingGreatAxe.Name.English("Dragon King GreatAxe"); // You can use this to fix the display name in code
            DragonKingGreatAxe.Description.English("This axe, one of the rare dragon weapons, is formed by the tail of the Gaping Dragon, a distant, deformed descendant of the everlasting dragons.");
            DragonKingGreatAxe.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DragonKingGreatAxe.RequiredItems.Add("Stone", 120);
            DragonKingGreatAxe.RequiredItems.Add("TwinklingTitanite", 1);
            DragonKingGreatAxe.RequiredItems.Add("Silver", 40);
            DragonKingGreatAxe.RequiredItems.Add("DragonEgg", 4);
            DragonKingGreatAxe.RequiredUpgradeItems.Add("Stone", 60);
            DragonKingGreatAxe.RequiredUpgradeItems.Add("Silver", 20);
            DragonKingGreatAxe.RequiredUpgradeItems.Add("DragonEgg", 1);
            DragonKingGreatAxe.RequiredUpgradeItems.Add("TwinklingTitanite", 1);
            DragonKingGreatAxe.Snapshot();

            Item DragonSlayerGreatBow = new("wep", "DragonSlayerGreatBow", "assets");
            DragonSlayerGreatBow.Name.English("DragonSlayer GreatBow"); // You can use this to fix the display name in code
            DragonSlayerGreatBow.Description.English("Bow of the Dragonslayers, led by Hawkeye Gough, one of Gwyn's Four Knights. This bow's unusual size requires that it be anchored to the ground when fired. Only uses specialized great arrows.");
            DragonSlayerGreatBow.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DragonSlayerGreatBow.RequiredItems.Add("Silver", 50);
            DragonSlayerGreatBow.RequiredItems.Add("TwinklingTitanite", 15);
            DragonSlayerGreatBow.RequiredItems.Add("Chain", 10);
            DragonSlayerGreatBow.RequiredItems.Add("RoundLog", 20);
            DragonSlayerGreatBow.RequiredUpgradeItems.Add("Silver", 10);
            DragonSlayerGreatBow.RequiredUpgradeItems.Add("TwinklingTitanite", 3);
            DragonSlayerGreatBow.RequiredUpgradeItems.Add("Chain", 2);
            DragonSlayerGreatBow.RequiredUpgradeItems.Add("RoundLog", 5);
            DragonSlayerGreatBow.Snapshot();

            Item DragonSlayerSpear = new("wep", "DragonSlayerSpear", "assets");
            DragonSlayerSpear.Name.English("DragonSlayer Spear"); // You can use this to fix the display name in code
            DragonSlayerSpear.Description.English("Cross spear born from the soul of Ornstein, a Dragonslayer guarding Anor Londo cathedral. Inflicts lightning damage; effective against dragons. Two-handed thrust relies on cross and buries deep within a dragon's hide, and sends human foes flying.");
            DragonSlayerSpear.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DragonSlayerSpear.RequiredItems.Add("Iron", 40);
            DragonSlayerSpear.RequiredItems.Add("Thunderstone", 20);
            DragonSlayerSpear.RequiredItems.Add("Silver", 20);
            DragonSlayerSpear.RequiredItems.Add("TwinklingTitanite", 15);
            DragonSlayerSpear.RequiredUpgradeItems.Add("Iron", 20);
            DragonSlayerSpear.RequiredUpgradeItems.Add("Thunderstone", 3);
            DragonSlayerSpear.RequiredUpgradeItems.Add("Silver", 5);
            DragonSlayerSpear.RequiredUpgradeItems.Add("TwinklingTitanite", 1);
            DragonSlayerSpear.Snapshot();

            Item DrakeSword = new("wep", "DrakeSword", "assets");
            DrakeSword.Name.English("Drake Sword"); // You can use this to fix the display name in code
            DrakeSword.Description.English("This sword, one of the rare dragon weapons, is formed by a drake's tail. Drakes are seen as undeveloped imitators of the dragons, but they are likely their distant kin.\r\nThe sword is imbued with a mystical power, to be released when held with both hands.");
            DrakeSword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            DrakeSword.RequiredItems.Add("TwinklingTitanite", 20);
            DrakeSword.RequiredItems.Add("Stone", 20);
            DrakeSword.RequiredItems.Add("Wood", 40);
            DrakeSword.RequiredItems.Add("Flint", 1);
            DrakeSword.RequiredUpgradeItems.Add("TwinklingTitanite", 3);
            DrakeSword.RequiredUpgradeItems.Add("Wood", 10);
            DrakeSword.RequiredUpgradeItems.Add("Flint", 20);
            DrakeSword.Snapshot();

            Item dragontooth = new("wep", "dragontooth", "assets");
            dragontooth.Name.English("Dragon Tooth"); // You can use this to fix the display name in code
            dragontooth.Description.English("Created from an everlasting dragon tooth. Legendary great hammer of Havel the Rock. The dragon tooth will never break as it is harder than stone, and it grants its wielder resistance to magic and flame");
            dragontooth.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            dragontooth.RequiredItems.Add("TwinklingTitanite", 15);
            dragontooth.RequiredItems.Add("Stone", 80);
            dragontooth.RequiredItems.Add("YmirRemains", 10);
            dragontooth.RequiredItems.Add("BoneFragments", 25);
            dragontooth.RequiredUpgradeItems.Add("TwinklingTitanite", 3);
            dragontooth.RequiredUpgradeItems.Add("Stone", 20);
            dragontooth.RequiredUpgradeItems.Add("YmirRemains", 2);
            dragontooth.RequiredUpgradeItems.Add("BoneFragments", 5);
            dragontooth.Snapshot();

            Item FurySword = new("wep", "FurySword", "assets");
            FurySword.Name.English("Quelags Fury Sword"); // You can use this to fix the display name in code
            FurySword.Description.English("A curved sword born from the soul of Quelaag, daughter of the Witch of Izalith, who was transformed into a chaos demon. Like Quelaag's body, the sword features shells, spikes, humanity and a coating of chaos fire.");
            FurySword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            FurySword.RequiredItems.Add("Bronze", 30);
            FurySword.RequiredItems.Add("SurtlingCore", 20);
            FurySword.RequiredItems.Add("Chitin", 40);
            FurySword.RequiredItems.Add("TwinklingTitanite", 15);
            FurySword.RequiredUpgradeItems.Add("Bronze", 10);
            FurySword.RequiredUpgradeItems.Add("SurtlingCore", 5);
            FurySword.RequiredUpgradeItems.Add("Chitin", 20);
            FurySword.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            FurySword.Snapshot();

            Item GargoyleAxe = new("wep", "GargoyleAxe", "assets");
            GargoyleAxe.Name.English("Gargoyle Tail Axe"); // You can use this to fix the display name in code
            GargoyleAxe.Description.English("Sliced tail of the gargoyle guarding the Bell of Awakening in the Undead Church or patrolling in Anor Londo. Can be used as a bronze battle axe. Bends dramatically during large attacks, owing to its nature as a tail.");
            GargoyleAxe.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            GargoyleAxe.RequiredItems.Add("Stone", 40);
            GargoyleAxe.RequiredItems.Add("Flint", 12);
            GargoyleAxe.RequiredItems.Add("GreydwarfEye", 15);
            GargoyleAxe.RequiredItems.Add("TwinklingTitanite", 10);
            GargoyleAxe.RequiredUpgradeItems.Add("Stone", 8);
            GargoyleAxe.RequiredUpgradeItems.Add("Flint", 2);
            GargoyleAxe.RequiredUpgradeItems.Add("GreydwarfEye", 2);
            GargoyleAxe.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            GargoyleAxe.Snapshot();

            Item Glordsword = new("wep", "Glordsword", "assets");
            Glordsword.Name.English("GraveLord Sword"); // You can use this to fix the display name in code
            Glordsword.Description.English("Sword wielded only by servants of Gravelord Nito, the first of the dead. Crafted from the bones of the fallen. The miasma of death exudes from the sword, a veritable toxin to any living being.");
            Glordsword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            Glordsword.RequiredItems.Add("WitheredBone", 30);
            Glordsword.RequiredItems.Add("TwinklingTitanite", 20);
            Glordsword.RequiredItems.Add("Ooze", 20);
            Glordsword.RequiredItems.Add("Guck", 20);
            Glordsword.RequiredUpgradeItems.Add("WitheredBone", 3);
            Glordsword.RequiredUpgradeItems.Add("TwinklingTitanite", 3);
            Glordsword.RequiredUpgradeItems.Add("Ooze", 5);
            Glordsword.RequiredUpgradeItems.Add("Guck", 5);
            Glordsword.Snapshot();

            Item GoldTracer = new("wep", "GoldTracer", "assets");
            GoldTracer.Name.English("Gold Tracer"); // You can use this to fix the display name in code
            GoldTracer.Description.English("Curved sword used by the Lord's Blade Ciaran, one of Gwyn's Four Knights. Ciaran brandishes her sword in a mesmerizing dance, etching the darkness with dire streaks of gold.");
            GoldTracer.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            GoldTracer.RequiredItems.Add("Bronze", 40);
            GoldTracer.RequiredItems.Add("Coins", 100);
            GoldTracer.RequiredItems.Add("Ruby", 40);
            GoldTracer.RequiredItems.Add("TwinklingTitanite", 20);
            GoldTracer.RequiredUpgradeItems.Add("Bronze", 20);
            GoldTracer.RequiredUpgradeItems.Add("Coins", 20);
            GoldTracer.RequiredUpgradeItems.Add("Ruby", 10);
            GoldTracer.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            GoldTracer.Snapshot();

            Item GreatLordGreatSword = new("wep", "GreatLordGreatSword", "assets");
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
            GreatLordGreatSword.Snapshot();

            Item HavelGreatShield = new("wep", "HavelGreatShield", "assets");
            HavelGreatShield.Name.English("Havels GreatShield"); // You can use this to fix the display name in code
            HavelGreatShield.Description.English("Greatshield of the legendary Havel the Rock. Cut straight from a great slab of stone. This greatshield is imbued with the magic of Havel, proves a strong defense, and is incredibly heavy. A true divine heirloom on par with the Dragon tooth");
            HavelGreatShield.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            HavelGreatShield.RequiredItems.Add("Stone", 50);
            HavelGreatShield.RequiredItems.Add("Iron", 20);
            HavelGreatShield.RequiredItems.Add("Wood", 15);
            HavelGreatShield.RequiredItems.Add("TwinklingTitanite", 10);
            HavelGreatShield.RequiredUpgradeItems.Add("Stone", 20);
            HavelGreatShield.RequiredUpgradeItems.Add("Iron", 5);
            HavelGreatShield.RequiredUpgradeItems.Add("Wood", 5);
            HavelGreatShield.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            HavelGreatShield.Snapshot();

            Item GrassCrestShield = new("wep", "GrassCrestShield", "assets");
            GrassCrestShield.Name.English("Grass-Crest Shield"); // You can use this to fix the display name in code
            GrassCrestShield.Description.English("Old medium metal shield of unknown origin. The grass crest is lightly imbued with magic, which slightly speeds stamina recovery.");
            GrassCrestShield.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            GrassCrestShield.RequiredItems.Add("Dandelion", 25);
            GrassCrestShield.RequiredItems.Add("FineWood", 20);
            GrassCrestShield.RequiredItems.Add("Resin", 15);
            GrassCrestShield.RequiredItems.Add("TwinklingTitanite", 5);
            GrassCrestShield.RequiredUpgradeItems.Add("Dandelion", 2);
            GrassCrestShield.RequiredUpgradeItems.Add("FineWood", 5);
            GrassCrestShield.RequiredUpgradeItems.Add("Resin", 5);
            GrassCrestShield.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            GrassCrestShield.Snapshot();

            Item ManusCatalyst = new("wep", "ManusCatalyst", "assets");
            ManusCatalyst.Name.English("Manus Catalyst"); // You can use this to fix the display name in code
            ManusCatalyst.Description.English("A sorcery catalyst born from the soul of Manus, Father of the Abyss. A rough, old wooden catalyst large enough to be used as a strike weapon. Similar to the Tin Crystallization Catalyst, it boosts the strength of sorceries, but limits the number of castings");
            ManusCatalyst.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            ManusCatalyst.RequiredItems.Add("FineWood", 40);
            ManusCatalyst.RequiredItems.Add("TrophyWraith", 5);
            ManusCatalyst.RequiredItems.Add("SurtlingCore", 10);
            ManusCatalyst.RequiredItems.Add("TwinklingTitanite", 20);
            ManusCatalyst.RequiredUpgradeItems.Add("FineWood", 10);
            ManusCatalyst.RequiredUpgradeItems.Add("TrophyWraith", 1);
            ManusCatalyst.RequiredUpgradeItems.Add("SurtlingCore", 2);
            ManusCatalyst.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
            ManusCatalyst.Snapshot();

            Item MaskOfFather = new("wep", "MaskOfFather", "assets");
            MaskOfFather.Name.English("Mask of the Father"); // You can use this to fix the display name in code
            MaskOfFather.Description.English("One of the three masks of the Pinwheel, the necromancer who stole the power of the Gravelord, and reigns over the Catacombs. This mask, belonging to the valiant father, slightly raises equipment load");
            MaskOfFather.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            MaskOfFather.RequiredItems.Add("FineWood", 40);
            MaskOfFather.RequiredItems.Add("GreydwarfEye", 20);
            MaskOfFather.RequiredItems.Add("TwinklingTitanite", 10);
            MaskOfFather.RequiredItems.Add("Bronze", 20);
            MaskOfFather.RequiredUpgradeItems.Add("FineWood", 10);
            MaskOfFather.RequiredUpgradeItems.Add("GreydwarfEye", 2);
            MaskOfFather.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            MaskOfFather.RequiredUpgradeItems.Add("Bronze", 5);
            MaskOfFather.Snapshot();

            Item MLgreatsword = new("wep", "MLgreatsword", "assets");
            MLgreatsword.Name.English("Moonlight Greatsword"); // You can use this to fix the display name in code
            MLgreatsword.Description.English("This sword, one of the rare dragon weapons, came from the tail of Seath the Scaleless, the pale white dragon who betrayed his own. Seath is the grandfather of sorcery, and this sword is imbued with his magic, which shall be unleashed as a wave of moonlight.");
            MLgreatsword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            MLgreatsword.RequiredItems.Add("TwinklingTitanite", 25);
            MLgreatsword.RequiredItems.Add("DragonTear", 10);
            MLgreatsword.RequiredItems.Add("Eitr", 10);
            MLgreatsword.RequiredItems.Add("Crystal", 20);
            MLgreatsword.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
            MLgreatsword.RequiredUpgradeItems.Add("DragonTear", 1);
            MLgreatsword.RequiredUpgradeItems.Add("Eitr", 5);
            MLgreatsword.RequiredUpgradeItems.Add("Crystal", 10);
            MLgreatsword.Snapshot();

            Item Murakumo = new("wep", "Murakumo", "assets");
            Murakumo.Name.English("Murakumo"); // You can use this to fix the display name in code
            Murakumo.Description.English("Giant curved sword forged using special methods in an Eastern Land. This unparalleled weapon cuts like a Katana but is heavier than a Nata machete. Requires extreme strength, dexterity, and stamina to wield");
            Murakumo.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            Murakumo.RequiredItems.Add("TwinklingTitanite", 10);
            Murakumo.RequiredItems.Add("Tin", 10);
            Murakumo.RequiredItems.Add("Wood", 20);
            Murakumo.RequiredItems.Add("Resin", 10);
            Murakumo.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            Murakumo.RequiredUpgradeItems.Add("Tin", 1);
            Murakumo.RequiredUpgradeItems.Add("Wood", 5);
            Murakumo.RequiredUpgradeItems.Add("Resin", 2);
            Murakumo.Snapshot();

            Item MLHorn = new("wep", "MLHorn", "assets");
            MLHorn.Name.English("Moonlight Butterfly Horn"); // You can use this to fix the display name in code
            MLHorn.Description.English("Weapon born from the mystical creature of the Darkroot Garden, the Moonlight Butterfly. The horns of the butterfly, a being created by Seath, are imbued with a pure magic power.");
            MLHorn.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            MLHorn.RequiredItems.Add("AncientSeed", 30);
            MLHorn.RequiredItems.Add("TwinklingTitanite", 10);
            MLHorn.RequiredItems.Add("GreydwarfEye", 20);
            MLHorn.RequiredItems.Add("FineWood", 10);
            MLHorn.RequiredUpgradeItems.Add("AncientSeed", 10);
            MLHorn.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            MLHorn.RequiredUpgradeItems.Add("GreydwarfEye", 10);
            MLHorn.RequiredUpgradeItems.Add("FineWood", 5);
            MLHorn.Snapshot();

            Item Shotel = new("wep", "Shotel", "assets");
            Shotel.Name.English("Shotel"); // You can use this to fix the display name in code
            Shotel.Description.English("Curved sword with sharply curved blade. Created by Arstor, Earl of Carim. Requires great skill to wield, but evades shield defense to sneak in damage.");
            Shotel.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            Shotel.RequiredItems.Add("Tin", 40);
            Shotel.RequiredItems.Add("RoundLog", 20);
            Shotel.RequiredItems.Add("TwinklingTitanite", 10);
            Shotel.RequiredItems.Add("DeerHide", 20);
            Shotel.RequiredUpgradeItems.Add("Bronze", 10);
            Shotel.RequiredUpgradeItems.Add("Amber", 5);
            Shotel.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            Shotel.RequiredUpgradeItems.Add("DeerHide", 5);
            Shotel.Snapshot();

            Item SmoughHammer = new("wep", "SmoughHammer", "assets");
            SmoughHammer.Name.English("Smough's Hammer"); // You can use this to fix the display name in code
            SmoughHammer.Description.English("Great Hammer from the soul of executioner Smough, who guards the cathedral in the forsaken city of Anor Londo. Smough loved his work, and ground the bones of his victims into his own feed, ruining his hopes of being ranked with the Four Knights");
            SmoughHammer.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            SmoughHammer.RequiredItems.Add("Bronze", 40);
            SmoughHammer.RequiredItems.Add("RoundLog", 20);
            SmoughHammer.RequiredItems.Add("TwinklingTitanite", 20);
            SmoughHammer.RequiredItems.Add("FineWood", 20);
            SmoughHammer.RequiredUpgradeItems.Add("Bronze", 10);
            SmoughHammer.RequiredUpgradeItems.Add("Amber", 5);
            SmoughHammer.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
            SmoughHammer.RequiredUpgradeItems.Add("FineWood", 5);
            SmoughHammer.Snapshot();

            Item StaffWood = new("wep", "StaffWood", "assets");
            StaffWood.Name.English("Beatrice's Catalyst"); // You can use this to fix the display name in code
            StaffWood.Description.English("Catalyst belonging to Beatrice, the rogue witch. Contrasts with Vinheim catalysts. This ancient catalyst shows signs of being used for age-old sorceries. It has passed the hands of many generations to get here.");
            StaffWood.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            StaffWood.RequiredItems.Add("Wood", 40);
            StaffWood.RequiredItems.Add("TwinklingTitanite", 10);
            StaffWood.RequiredItems.Add("GreydwarfEye", 20);
            StaffWood.RequiredItems.Add("HardAntler", 5);
            StaffWood.RequiredUpgradeItems.Add("Wood", 10);
            StaffWood.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            StaffWood.RequiredUpgradeItems.Add("GreydwarfEye", 5);
            StaffWood.RequiredUpgradeItems.Add("HardAntler", 5);
            StaffWood.Snapshot();

            Item sunshield1 = new("wep", "sunshield1", "assets");
            sunshield1.Name.English("sunlight shield"); // You can use this to fix the display name in code
            sunshield1.Description.English("Shield of Solaire of Astora, Knight of Sunlight. Decorated with a holy symbol, but Solaire illustrated it himself, and it has no divine powers of its own. As it turns out, Solaire's incredible prowess is a product of his own training, and nothing else.");
            sunshield1.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            sunshield1.RequiredItems.Add("Bronze", 25);
            sunshield1.RequiredItems.Add("Amber", 20);
            sunshield1.RequiredItems.Add("BronzeNails", 20);
            sunshield1.RequiredItems.Add("TwinklingTitanite", 10);
            sunshield1.RequiredUpgradeItems.Add("Bronze", 10);
            sunshield1.RequiredUpgradeItems.Add("Amber", 5);
            sunshield1.RequiredUpgradeItems.Add("BronzeNails", 5);
            sunshield1.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            sunshield1.Snapshot();

            Item SunlightSword = new("wep", "SunlightSword", "assets");
            SunlightSword.Name.English("Sunlight StraightSword"); // You can use this to fix the display name in code
            SunlightSword.Description.English("This standard longsword, belonging to Solaire of Astora, is of high quality, is well-forged, and has been kept in good repair. Easy to use and dependable, but unlikely to live up to its grandiose name.");
            SunlightSword.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            SunlightSword.RequiredItems.Add("TwinklingTitanite", 10);
            SunlightSword.RequiredItems.Add("FineWood", 10);
            SunlightSword.RequiredItems.Add("TrophyGreydwarf", 5);
            SunlightSword.RequiredItems.Add("Tin", 10);
            SunlightSword.RequiredUpgradeItems.Add("Tin", 10);
            SunlightSword.RequiredUpgradeItems.Add("FineWood", 10);
            SunlightSword.RequiredUpgradeItems.Add("TrophyGreydwarf", 1);
            SunlightSword.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            SunlightSword.Snapshot();

            Item washingpole = new("wep", "washingpole", "assets");
            washingpole.Name.English("Washing Pole"); // You can use this to fix the display name in code
            washingpole.Description.English("Katana forged in an Eastern land. Very unusual specimen with a long blade. Has a different move set than the Uchigatana. The blade is extremely long, but as a result, quite easily broken");
            washingpole.Crafting.Add("BlacksmithAltar", 1); // Custom crafting stations can be specified as a string
            washingpole.RequiredItems.Add("TwinklingTitanite", 12);
            washingpole.RequiredItems.Add("Resin", 10);
            washingpole.RequiredItems.Add("FineWood", 10);
            washingpole.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
            washingpole.RequiredUpgradeItems.Add("Resin", 10);
            washingpole.RequiredUpgradeItems.Add("FineWood", 10);
            washingpole.Snapshot();

            #endregion
            #region Boss Weapons
            /* Item AbyssGreatsword1 = new("souls", "AbyssGreatsword1", "assets");
             AbyssGreatsword1.Configurable = Configurability.Disabled;
             Item AbyssGreatsword2 = new("souls", "AbyssGreatsword2", "assets");
             AbyssGreatsword2.Configurable = Configurability.Disabled;
             Item AbyssGreatsword3 = new("souls", "AbyssGreatsword3", "assets");
             AbyssGreatsword3.Configurable = Configurability.Disabled;
             Item DemonGreatHammer1 = new("souls", "DemonGreatHammer1", "assets");
             DemonGreatHammer1.Configurable = Configurability.Disabled;
             Item DemonGreatHammer2 = new("souls", "DemonGreatHammer2", "assets");
             DemonGreatHammer2.Configurable = Configurability.Disabled;
             Item Slam = new("souls", "Slam", "assets");
             Slam.Configurable = Configurability.Disabled;*/
            Item TaurusWep = new("souls", "TaurusWep", "assets");
            TaurusWep.Configurable = Configurability.Disabled;
            Item TaurusWep1 = new("souls", "TaurusWep1", "assets");
            TaurusWep1.Configurable = Configurability.Disabled;
            Item TaurusWep2 = new("souls", "TaurusWep2", "assets");
            TaurusWep2.Configurable = Configurability.Disabled;
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
            /*Item DragonSlayerGreatBow1 = new("souls", "DragonSlayerGreatBow1", "assets");
            DragonSlayerGreatBow1.Configurable = Configurability.Disabled;
            Item SmoughAttack1 = new("souls", "SmoughAttack1", "assets");
            SmoughAttack1.Configurable = Configurability.Disabled;
            Item SmoughAttack2 = new("souls", "SmoughAttack2", "assets");
            SmoughAttack2.Configurable = Configurability.Disabled;
            Item SmoughAttack3 = new("souls", "SmoughAttack3", "assets");
            SmoughAttack3.Configurable = Configurability.Disabled;
            Item GreatLordGreatSword1 = new("souls", "GreatLordGreatSword1", "assets");
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
            Item LightningSpear = new("souls", "LightningSpear", "assets");
            LightningSpear.Configurable = Configurability.Disabled;
            Item SilverKnightSpear = new("souls", "SilverKnightSpear", "assets");
            SilverKnightSpear.Configurable = Configurability.Disabled;
            Item SilverKnightSpear1 = new("souls", "SilverKnightSpear1", "assets");
            SilverKnightSpear1.Configurable = Configurability.Disabled;
            Item SilverKnightSword1 = new("souls", "SilverKnightSword1", "assets");
            SilverKnightSword1.Configurable = Configurability.Disabled;
            Item SilverKnightShield1 = new("souls", "SilverKnightShield1", "assets");
            SilverKnightShield1.Configurable = Configurability.Disabled;*/
/*            Item dragon_spit_shotgun1 = new("souls", "dragon_spit_shotgun1", "assets");
            dragon_spit_shotgun1.Configurable = Configurability.Disabled;*/
            Item ChaosZweihander = new("souls", "ChaosZweihander", "assets");
            ChaosZweihander.Configurable = Configurability.Disabled;
            Item ChaosZweihander1 = new("souls", "ChaosZweihander1", "assets");
            ChaosZweihander1.Configurable = Configurability.Disabled;
            Item BlackFlame = new("souls", "BlackFlame", "assets");
            BlackFlame.Configurable = Configurability.Disabled;
            Item GrassCrestShield1 = new("souls", "GrassCrestShield1", "assets");
            GrassCrestShield1.Configurable = Configurability.Disabled;
            /*Item Seath_AOE = new("souls", "Seath_AOE", "assets");
            Seath_AOE.Configurable = Configurability.Disabled;
            Item Seath_Beam = new("souls", "Seath_Beam", "assets");
            Seath_Beam.Configurable = Configurability.Disabled;
            Item Seath_crystal_spawn = new("souls", "Seath_crystal_spawn", "assets");
            Seath_crystal_spawn.Configurable = Configurability.Disabled;
            Item Seath_Slash = new("souls", "Seath_Slash", "assets");
            Seath_Slash.Configurable = Configurability.Disabled;*/
/*            Item Wyvern_Bite = new("souls", "Wyvern_Bite", "assets");
            Wyvern_Bite.Configurable = Configurability.Disabled;*/

/*            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "Seath_Beam_Projectile", false);
            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "Seath_Fog", false);
            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "Seath_SpawnCrystal", false);*/
            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "BlackFlame_AOE", false);
            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("wep"), "LightningSpear_Projectile", false);
/*            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "dragon_lightning_projectile", false);*/
            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("wep"), "Trident_AOE", false);
/*            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "DragonSlayer_bow_projectile", false);*/
            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("wep"), "sfx_Andre_bye1", false);
            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("wep"), "sfx_Andre_greeting1", false);
            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("wep"), "sfx_Andre_talk1", false);
            ItemManager.PrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "chest1", false);
            /*            PiecePrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "SK_Spawner", false);
                        PiecePrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "OrnsteinSpawner", false);
                        PiecePrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "BK_Spawner", false);*/
            PiecePrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "firelinkshrine_sfx", false);
            /*var firelinkshrine_sfx = PiecePrefabManager.RegisterPrefab(PrefabManager.RegisterAssetBundle("souls"), "firelinkshrine_sfx", false);*/
/*            GameObject firelinkshrine_sfx = ZNetScene.instance.GetPrefab("firelinkshrine_sfx");
            firelinkshrine_sfx.GetComponentInChildren<AudioSource>().outputAudioMixerGroup =
                AudioMan.instance.m_ambientMixer;*/

            #endregion
            #region CreatureManager Example Code

            /*Creature Artorias = new("souls", "Artorias")
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
            AsylumDemon.Drops["Wood"].DropChance = 100f;*/

            Creature BlackKnight = new("souls", "BlackKnight")
            {
                Biome = Heightmap.Biome.AshLands,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 350,
                Maximum = 3
            };
            BlackKnight.Localize().English("BlackKnight");
            BlackKnight.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(1, 3);
            BlackKnight.Drops["TwinklingTitanite"].DropChance = 75f;

           /* Creature SilverKnight = new("souls", "SilverKnight")
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
            Gwyn.Drops["Wood"].DropChance = 100f;*/

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
                CheckSpawnInterval = 1000,
                Maximum = 3
            };
            GiantMushroom.Localize().English("Giant Mushroom");
            GiantMushroom.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(1, 3);
            GiantMushroom.Drops["TwinklingTitanite"].DropChance = 75f;

            Creature HollowSoldier = new("souls", "HollowSoldier")
            {
                RequiredGlobalKey = GlobalKey.KilledEikthyr,
                Biome = Heightmap.Biome.Meadows | Heightmap.Biome.BlackForest | Heightmap.Biome.Swamp,
                GroupSize = new CreatureManager.Range(1, 3),
                CheckSpawnInterval = 900,
                Maximum = 5
            };
            HollowSoldier.Localize().English("Hollow Soldier");
            HollowSoldier.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(1, 2);
            HollowSoldier.Drops["TwinklingTitanite"].DropChance = 75f;

            /*Creature Smough = new("souls", "Smough")
            {
                Biome = Heightmap.Biome.None,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 600,
                RequiredWeather = Weather.Rain | Weather.Fog,
                Maximum = 0
            };
            Smough.Localize().English("Smough");

            Creature SeathScaleless = new("souls", "SeathScaleless")
            {
                Biome = Heightmap.Biome.None,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 600,
                RequiredWeather = Weather.Rain | Weather.Fog,
                Maximum = 0
            };
            SeathScaleless.Localize().English("Seath The Scaleless");
*/
            Creature TaurusDemon = new("souls", "TaurusDemon")
            {
                RequiredGlobalKey = GlobalKey.KilledElder,
                Biome = Heightmap.Biome.Meadows | Heightmap.Biome.BlackForest,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 700,
                Maximum = 1
            };
            TaurusDemon.Localize().English("TaurusDemon");
            TaurusDemon.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(2, 3);
            TaurusDemon.Drops["TwinklingTitanite"].DropChance = 75f;

/*            Creature Wyvern = new("souls", "Wyvern")
            {
                RequiredGlobalKey = GlobalKey.KilledModer,
                Biome = Heightmap.Biome.Mountain,
                GroupSize = new CreatureManager.Range(1, 2),
                CheckSpawnInterval = 600,
                Maximum = 3
            };
            Wyvern.Localize().English("Wyvern");
            Wyvern.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(2, 3);
            Wyvern.Drops["TwinklingTitanite"].DropChance = 100f;*/

            #endregion


            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
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