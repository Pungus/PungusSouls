using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
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
using UnityEngine;
using static Heightmap;

namespace PungusSouls;

[_003Cff97f7d9_002D7f48_002D44a2_002Da23f_002D4f039e0cea12_003ENullableContext(1)]
[BepInPlugin("Pungus.PungusSouls", "PungusSouls", "0.0.7")]
[_003C4bc8397d_002D6ffa_002D4fe6_002D9846_002D4d3467239fd6_003ENullable(0)]
public class PungusSoulsPlugin : BaseUnityPlugin
{
    [_003Cff97f7d9_002D7f48_002D44a2_002Da23f_002D4f039e0cea12_003ENullableContext(0)]
    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    [_003Cff97f7d9_002D7f48_002D44a2_002Da23f_002D4f039e0cea12_003ENullableContext(0)]
    private class ConfigurationManagerAttributes
    {
        [UsedImplicitly]
        public int? Order;

        [UsedImplicitly]
        public bool? Browsable;

        [UsedImplicitly]
        [_003C4bc8397d_002D6ffa_002D4fe6_002D9846_002D4d3467239fd6_003ENullable(2)]
        public string Category;

        [_003C4bc8397d_002D6ffa_002D4fe6_002D9846_002D4d3467239fd6_003ENullable(new byte[] { 2, 1 })]
        [UsedImplicitly]
        public Action<ConfigEntryBase> CustomDrawer;
    }

    [_003C4bc8397d_002D6ffa_002D4fe6_002D9846_002D4d3467239fd6_003ENullable(0)]
    private class AcceptableShortcuts : AcceptableValueBase
    {
        public AcceptableShortcuts()
            : base(typeof(KeyboardShortcut))
        {
        }

        public override object Clamp(object value)
        {
            return value;
        }

        public override bool IsValid(object value)
        {
            return true;
        }

        public override string ToDescriptionString()
        {
            return "# Acceptable values: " + string.Join(", ", KeyboardShortcut.AllKeyCodes);
        }
    }

    internal const string ModName = "PungusSouls";

    internal const string ModVersion = "0.0.7";

    internal const string Author = "Pungus";

    private const string ModGUID = "Pungus.PungusSouls";

    private static string ConfigFileName = "Pungus.PungusSouls.cfg";

    private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

    internal static string ConnectionError = "";

    private readonly Harmony _harmony = new Harmony("Pungus.PungusSouls");

    public static readonly ManualLogSource PungusSoulsLogger = Logger.CreateLogSource("PungusSouls");

    private static readonly ConfigSync ConfigSync = new ConfigSync("Pungus.PungusSouls")
    {
        DisplayName = "PungusSouls",
        CurrentVersion = "0.0.7",
        MinimumRequiredVersion = "0.0.7"
    };

    public Texture2D tex;

    private Sprite mySprite;

    private SpriteRenderer sr;

    private static ConfigEntry<Toggle> _serverConfigLocked = null;

    public void Awake()
    {
        //IL_0a67: Unknown result type (might be due to invalid IL or missing references)
        //IL_0a6e: Unknown result type (might be due to invalid IL or missing references)
        //IL_493e: Unknown result type (might be due to invalid IL or missing references)
        //IL_4995: Unknown result type (might be due to invalid IL or missing references)
        //IL_4a32: Unknown result type (might be due to invalid IL or missing references)
        //IL_4acf: Unknown result type (might be due to invalid IL or missing references)
        //IL_4b6d: Unknown result type (might be due to invalid IL or missing references)
        //IL_4c0b: Unknown result type (might be due to invalid IL or missing references)
        //IL_4ce9: Unknown result type (might be due to invalid IL or missing references)
        //IL_4d7f: Unknown result type (might be due to invalid IL or missing references)
        //IL_4dc1: Unknown result type (might be due to invalid IL or missing references)
        //IL_4e58: Unknown result type (might be due to invalid IL or missing references)
        //IL_4ef6: Unknown result type (might be due to invalid IL or missing references)
        //IL_4f8c: Unknown result type (might be due to invalid IL or missing references)
        _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
        ConfigSync.AddLockingConfigEntry<Toggle>(_serverConfigLocked);
        Item item = new Item("shared", "TwinklingTitanite");
        item.Name.English("Twinkling Titanite");
        item.Description.English("This weapon-reinforcing titanite is imbued with a particularly powerful energy. After this titanite was peeled from its Slab, it is said that it received a special power, but its specific nature is not clear.");
        item.Snapshot();
        item.DropsFrom.Add("Boar", 0.5f, 1, 1);
        item.DropsFrom.Add("Deer", 0.15f, 1, 1);
        item.DropsFrom.Add("Neck", 0.7f, 1, 1);
        item.DropsFrom.Add("Greyling", 0.7f, 1, 1);
        item.DropsFrom.Add("Greydwarf", 0.12f, 1, 1);
        item.DropsFrom.Add("Greydwarf_Shaman", 0.15f, 1, 1);
        item.DropsFrom.Add("Greydwarf_Elite", 0.15f, 1, 1);
        item.DropsFrom.Add("Troll", 0.2f, 1, 1);
        item.DropsFrom.Add("Skeleton", 0.17f, 1, 1);
        item.DropsFrom.Add("Blob", 0.2f, 1, 1);
        item.DropsFrom.Add("Surtling", 0.2f, 1, 1);
        item.DropsFrom.Add("Leech", 0.2f, 1, 1);
        item.DropsFrom.Add("Draugr", 0.2f, 1, 1);
        item.DropsFrom.Add("Draugr_Elite", 0.2f, 1, 1);
        item.DropsFrom.Add("Wraith", 0.2f, 1, 1);
        item.DropsFrom.Add("Abomination", 0.2f, 1, 1);
        item.DropsFrom.Add("Wolf", 0.25f, 1, 1);
        item.DropsFrom.Add("Hatchling", 0.25f, 1, 1);
        item.DropsFrom.Add("Deathsquito", 0.3f, 1, 1);
        item.DropsFrom.Add("Goblin", 0.35f, 1, 1);
        item.DropsFrom.Add("GoblinBrute", 0.4f, 1, 1);
        item.DropsFrom.Add("BlobTar", 0.35f, 1, 1);
        item.DropsFrom.Add("GoblinShaman", 0.4f, 1, 1);
        item.DropsFrom.Add("Lox", 0.4f, 1, 1);
        item.DropsFrom.Add("Seeker", 0.5f, 1, 1);
        item.DropsFrom.Add("SeekerBrute", 0.5f, 1, 1);
        item.DropsFrom.Add("Gjall", 0.5f, 1, 1);
        item.DropsFrom.Add("Eikthyr", 100f, 3, 7);
        item.DropsFrom.Add("gd_king", 100f, 4, 8);
        item.DropsFrom.Add("Bonemass", 100f, 5, 12);
        item.DropsFrom.Add("Dragon", 100f, 7, 13);
        item.DropsFrom.Add("GoblinKing", 100f, 10, 15);
        item.DropsFrom.Add("SeekerQueen", 100f, 12, 16);
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
        Item item7 = new Item("souls", "CatRing");
        item7.Name.English("Silver Cat Ring");
        item7.Description.English("Silver ring depicting a leaping feline. Prevents damage from falling. In the Age of Gods, or possibily following it, an old cat was said to speak a human tongue, with the voice of an old woman, and the form of a fanciful immortal.");
        Item item8 = new Item("souls", "CatCharm");
        item8.Name.English("Cat Charm");
        item8.Description.English("Summon Sweet Shalquoir");
        Item item9 = new Item("souls", "WolfCharm");
        item9.Name.English("Wolf Charm");
        item9.Description.English("Summon Sif the Great Grey Wolf");
        BuildPiece.ConfigurationEnabled = false;
        PiecePrefabManager.RegisterPrefab("souls", "BlacksmithAltar");
        PiecePrefabManager.RegisterPrefab("souls", "SweetShalquoir_piece");
        BuildPiece buildPiece = new BuildPiece("souls", "Lantern");
        buildPiece.Name.English("Arcane Lantern");
        buildPiece.Description.English("Blacksmith Altar Extension");
        buildPiece.RequiredItems.Add("Resin", 20, recover: true);
        buildPiece.RequiredItems.Add("Iron", 10, recover: true);
        buildPiece.RequiredItems.Add("ElderBark", 10, recover: true);
        buildPiece.RequiredItems.Add("TwinklingTitanite", 15, recover: true);
        buildPiece.Category.Set(BuildPieceCategory.Crafting);
        buildPiece.Crafting.Set("BlacksmithAltar");
        buildPiece.Snapshot();
        BuildPiece buildPiece2 = new BuildPiece("souls", "Lantern1");
        buildPiece2.Name.English("Hunters Lantern");
        buildPiece2.Description.English("Blacksmith Altar Extension");
        buildPiece2.RequiredItems.Add("FineWood", 20, recover: true);
        buildPiece2.RequiredItems.Add("Bronze", 10, recover: true);
        buildPiece2.RequiredItems.Add("Amber", 10, recover: true);
        buildPiece2.RequiredItems.Add("TwinklingTitanite", 10, recover: true);
        buildPiece2.Category.Set(BuildPieceCategory.Crafting);
        buildPiece2.Crafting.Set("BlacksmithAltar");
        buildPiece2.Snapshot();
        BuildPiece buildPiece3 = new BuildPiece("souls", "ArcaneStone");
        buildPiece3.Name.English("Arcane Stone");
        buildPiece3.Description.English("Blacksmith Altar Extension");
        buildPiece3.RequiredItems.Add("Stone", 20, recover: true);
        buildPiece3.RequiredItems.Add("Flint", 10, recover: true);
        buildPiece3.RequiredItems.Add("GreydwarfEye", 10, recover: true);
        buildPiece3.RequiredItems.Add("TwinklingTitanite", 5, recover: true);
        buildPiece3.Category.Set(BuildPieceCategory.Crafting);
        buildPiece3.Crafting.Set("BlacksmithAltar");
        buildPiece3.Snapshot();
        BuildPiece buildPiece4 = new BuildPiece("souls", "NamelessStatue");
        buildPiece4.Name.English("Nameless Statue");
        buildPiece4.Description.English("Blacksmith Altar Extension");
        buildPiece4.RequiredItems.Add("Stone", 20, recover: true);
        buildPiece4.RequiredItems.Add("Crystal", 10, recover: true);
        buildPiece4.RequiredItems.Add("BlackMetal", 10, recover: true);
        buildPiece4.RequiredItems.Add("TwinklingTitanite", 30, recover: true);
        buildPiece4.Category.Set(BuildPieceCategory.Crafting);
        buildPiece4.Crafting.Set("BlacksmithAltar");
        buildPiece4.Snapshot();
        BuildPiece buildPiece5 = new BuildPiece("souls", "Bonefire");
        buildPiece5.Name.English("Bonefire");
        buildPiece5.Description.English("Blacksmith Altar Extension");
        buildPiece5.RequiredItems.Add("BoneFragments", 20, recover: true);
        buildPiece5.RequiredItems.Add("Crystal", 10, recover: true);
        buildPiece5.RequiredItems.Add("Silver", 10, recover: true);
        buildPiece5.RequiredItems.Add("TwinklingTitanite", 20, recover: true);
        buildPiece5.Category.Set(BuildPieceCategory.Crafting);
        buildPiece5.Crafting.Set("BlacksmithAltar");
        buildPiece5.Snapshot();

         _ = new LocationManager.Location("souls", "FireLinkShrine")
         {
             MapIcon = "firelinkicon.png",
             Rotation = Rotation.Fixed,
             ShowMapIcon = ShowIcon.Explored,
             Biome = (Biome)1,
             SpawnArea = (BiomeArea)2,
             HeightDelta = new LocationManager.Range(0f, 12f),
             SpawnDistance = new LocationManager.Range(450f, 850f),
             SpawnAltitude = new LocationManager.Range(23f, 100f),
             Count = 1,
             Prioritize = true,
             Unique = true
         };
        CustomSE customSE = new CustomSE("souls", "Lifesteal");
        customSE.Name.English("Lifesteal");
        customSE.Type = EffectType.Attack;
        CustomSE customSE2 = new CustomSE("souls", "SE_GrassShield");
        customSE2.Name.English("Stamina Regen");
        CustomSE customSE3 = new CustomSE("souls", "TridentBuff");
        customSE3.Name.English("TridentBuff");
        CustomSE customSE4 = new CustomSE("souls", "SetEffect_ArtoriasSet");
        customSE4.Name.English("Artorias Set");
        customSE4.Effect.m_tooltip = "<color=orange>The Agility of Artorias</color>";
        CustomSE customSE5 = new CustomSE("souls", "SetEffect_HavelSet");
        customSE5.Name.English("Havels Set");
        customSE5.Effect.m_tooltip = "<color=orange>The Strength of Havel the Rock</color>";
        CustomSE customSE6 = new CustomSE("souls", "SE_CatRing");
        customSE6.Name.English("Silver Cat Ring");
        customSE6.Effect.m_tooltip = "<color=orange>You feel light as a feather</color>";
        Item item10 = new Item("souls", "SunChest");
        item10.Name.English("Armor of the Sun");
        item10.Description.English("Armor of Solaire of Astora, Knight of Sunlight. The large holy symbol of the Sun while powerless, was painted by Solaire himself");
        item10.Crafting.Add("BlacksmithAltar", 1);
        item10.RequiredItems.Add("Iron", 25);
        item10.RequiredItems.Add("Chain", 2);
        item10.RequiredItems.Add("TwinklingTitanite", 10);
        item10.RequiredItems.Add("MushroomYellow", 10);
        item10.RequiredUpgradeItems.Add("Iron", 5);
        item10.RequiredUpgradeItems.Add("Chain", 1);
        item10.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item10.RequiredUpgradeItems.Add("MushroomYellow", 2);
        Item item11 = new Item("souls", "SunLegs");
        item11.Name.English("Leggings of the Sun");
        item11.Description.English("Leggings of Solaire of Astora, Knight of Sunlight. Of high quality, but lacking any particular powers");
        item11.Crafting.Add("BlacksmithAltar", 1);
        item11.RequiredItems.Add("Iron", 25);
        item11.RequiredItems.Add("Chain", 2);
        item11.RequiredItems.Add("TwinklingTitanite", 10);
        item11.RequiredItems.Add("MushroomYellow", 10);
        item11.RequiredUpgradeItems.Add("Iron", 5);
        item11.RequiredUpgradeItems.Add("Chain", 1);
        item11.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item11.RequiredUpgradeItems.Add("MushroomYellow", 2);
        Item item12 = new Item("souls", "SunHelm");
        item12.Name.English("Helmet of the Sun");
        item12.Description.English("Helm of Solaire of Astora, Knight of Sunlight. Of high quality, but lacking any particular powers");
        item12.Crafting.Add("BlacksmithAltar", 1);
        item12.RequiredItems.Add("Iron", 25);
        item12.RequiredItems.Add("Chain", 2);
        item12.RequiredItems.Add("TwinklingTitanite", 10);
        item12.RequiredItems.Add("MushroomYellow", 10);
        item12.RequiredUpgradeItems.Add("Iron", 5);
        item12.RequiredUpgradeItems.Add("Chain", 1);
        item12.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item12.RequiredUpgradeItems.Add("MushroomYellow", 2);
        Item item13 = new Item("souls", "ArtChest");
        item13.Name.English("Abyss Chest Piece");
        item13.Description.English("Armor of Artorias the Abysswalker, one of Gwyn's four knights. The death of the armor's owner can be surmised from the corrosive Dark of the Abyss, and the tattered azure-blue cape, once a symbol of pride and glory.");
        item13.Crafting.Add("BlacksmithAltar", 1);
        item13.RequiredItems.Add("Silver", 25);
        item13.RequiredItems.Add("WolfHairBundle", 10);
        item13.RequiredItems.Add("TwinklingTitanite", 10);
        item13.RequiredItems.Add("DeerHide", 10);
        item13.RequiredUpgradeItems.Add("Silver", 5);
        item13.RequiredUpgradeItems.Add("TrophyWolf", 1);
        item13.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item13.RequiredUpgradeItems.Add("DeerHide", 2);
        Item item14 = new Item("souls", "ArtLegs");
        item14.Name.English("Abyss Leggins");
        item14.Description.English("Leggings of Artorias the Abysswalker, one of Gwyn's four knights. The death of the their owner can be surmised from the corrosive Dark of the Abyss, which has compromised their protective utility.");
        item14.Crafting.Add("BlacksmithAltar", 1);
        item14.RequiredItems.Add("Silver", 25);
        item14.RequiredItems.Add("WolfHairBundle", 10);
        item14.RequiredItems.Add("TwinklingTitanite", 10);
        item14.RequiredItems.Add("DeerHide", 10);
        item14.RequiredUpgradeItems.Add("Silver", 5);
        item14.RequiredUpgradeItems.Add("WolfHairBundle", 2);
        item14.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item14.RequiredUpgradeItems.Add("DeerHide", 2);
        Item item15 = new Item("souls", "ArtHelm");
        item15.Name.English("Abyss Helm");
        item15.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
        item15.Crafting.Add("BlacksmithAltar", 1);
        item15.RequiredItems.Add("Silver", 25);
        item15.RequiredItems.Add("TrophyWolf", 1);
        item15.RequiredItems.Add("TwinklingTitanite", 10);
        item15.RequiredItems.Add("DeerHide", 10);
        item15.RequiredUpgradeItems.Add("Silver", 5);
        item15.RequiredUpgradeItems.Add("WolfHairBundle", 2);
        item15.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item15.RequiredUpgradeItems.Add("DeerHide", 2);
        Item item16 = new Item("souls", "HavelLegs");
        item16.Name.English("Havels Leggings");
        item16.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
        item16.Crafting.Add("BlacksmithAltar", 1);
        item16.RequiredItems.Add("Stone", 25);
        item16.RequiredItems.Add("DragonTear", 1);
        item16.RequiredItems.Add("TwinklingTitanite", 10);
        item16.RequiredItems.Add("BlackMetal", 10);
        item16.RequiredUpgradeItems.Add("Stone", 5);
        item16.RequiredUpgradeItems.Add("DragonTear", 2);
        item16.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item16.RequiredUpgradeItems.Add("BlackMetal", 2);
        Item item17 = new Item("souls", "HavelHelm");
        item17.Name.English("Havels Helm");
        item17.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
        item17.Crafting.Add("BlacksmithAltar", 1);
        item17.RequiredItems.Add("Stone", 25);
        item17.RequiredItems.Add("DragonTear", 1);
        item17.RequiredItems.Add("TwinklingTitanite", 10);
        item17.RequiredItems.Add("BlackMetal", 10);
        item17.RequiredUpgradeItems.Add("Stone", 5);
        item17.RequiredUpgradeItems.Add("DragonTear", 2);
        item17.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item17.RequiredUpgradeItems.Add("BlackMetal", 2);
        Item item18 = new Item("souls", "HavelChest");
        item18.Name.English("Havels Chest Piece");
        item18.Description.English("Helm of Artorias the Abysswalker, one of Gwyn's four knights. The death of the helm's owner can be surmised from the corrosive Dark of the Abyss, and the musty azure - blue tassel, once a symbol of pride and glory.");
        item18.Crafting.Add("BlacksmithAltar", 1);
        item18.RequiredItems.Add("Stone", 25);
        item18.RequiredItems.Add("DragonTear", 1);
        item18.RequiredItems.Add("TwinklingTitanite", 10);
        item18.RequiredItems.Add("BlackMetal", 10);
        item18.RequiredUpgradeItems.Add("Stone", 5);
        item18.RequiredUpgradeItems.Add("DragonTear", 2);
        item18.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item18.RequiredUpgradeItems.Add("BlackMetal", 2);
        Item item19 = new Item("souls", "AbyssGreatsword");
        item19.Name.English("Abyss Greatsword");
        item19.Description.English("This greatsword belonged to Lord Gwyn's Knight Artorias, who fell to the Abyss. Swallowed by the Dark with its master, this sword is tainted by the Abyss, and now its strength reflects its wielder's humanity.");
        item19.Crafting.Add("BlacksmithAltar", 1);
        item19.RequiredItems.Add("Silver", 40);
        item19.RequiredItems.Add("Eitr", 20);
        item19.RequiredItems.Add("TwinklingTitanite", 20);
        item19.RequiredItems.Add("TrophyWolf", 1);
        item19.RequiredUpgradeItems.Add("Silver", 20);
        item19.RequiredUpgradeItems.Add("Eitr", 10);
        item19.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        item19.RequiredUpgradeItems.Add("TrophyWolf", 1);
        Item item20 = new Item("souls", "ArtGS");
        item20.Name.English("Greatsword of Artorias");
        item20.Description.English("Sword born from the soul of the great grey wolf Sif, guardian of the grave of the Abysswalker Knight Artorias. Sir Artorias hunted the Darkwraiths, and his sword strikes harder against dark servants.");
        item20.Crafting.Add("BlacksmithAltar", 1);
        item20.RequiredItems.Add("Silver", 40);
        item20.RequiredItems.Add("Eitr", 20);
        item20.RequiredItems.Add("TwinklingTitanite", 10);
        item20.RequiredItems.Add("TrophyWolf", 1);
        item20.RequiredUpgradeItems.Add("Silver", 20);
        item20.RequiredUpgradeItems.Add("Eitr", 10);
        item20.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        item20.RequiredUpgradeItems.Add("TrophyWolf", 1);
        Item item21 = new Item("souls", "ArtoriasGreatshield");
        item21.Name.English("GreatShield of Artorias");
        item21.Description.English("Shield born from the soul of the great grey wolf Sif, guardian of the grave of the Abysswalker Knight Artorias.");
        item21.Crafting.Add("BlacksmithAltar", 1);
        item21.RequiredItems.Add("Silver", 40);
        item21.RequiredItems.Add("Eitr", 20);
        item21.RequiredItems.Add("TwinklingTitanite", 10);
        item21.RequiredItems.Add("TrophyWolf", 1);
        item21.RequiredUpgradeItems.Add("Silver", 20);
        item21.RequiredUpgradeItems.Add("Eitr", 10);
        item21.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        item21.RequiredUpgradeItems.Add("TrophyWolf", 1);
        Item item22 = new Item("souls", "Avelyn");
        item22.Name.English("Avelyn");
        item22.Description.English("Repeating crossbow cherished by the weapon craftsman Eidas. Its elaborate design makes it closer to a work of art than a weapon. Intricate mechanism makes heavy damage possible through triple-shot firing of bolts. but in fact each bolt inflicts less damage");
        item22.Crafting.Add("BlacksmithAltar", 1);
        item22.RequiredItems.Add("Wood", 20);
        item22.RequiredItems.Add("Iron", 10);
        item22.RequiredItems.Add("TwinklingTitanite", 10);
        item22.RequiredItems.Add("Root", 8);
        item22.RequiredUpgradeItems.Add("Wood", 5);
        item22.RequiredUpgradeItems.Add("Iron", 2);
        item22.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item22.RequiredUpgradeItems.Add("Root", 2);
        Item item23 = new Item("souls", "BerserkGreatsword");
        item23.Name.English("Berserk Greatsword");
        item23.Description.English("A huge hunk of metal");
        item23.Crafting.Add("BlacksmithAltar", 1);
        item23.RequiredItems.Add("Bronze", 20);
        item23.RequiredItems.Add("RoundLog", 10);
        item23.RequiredItems.Add("TwinklingTitanite", 5);
        item23.RequiredItems.Add("TrophyGreydwarfBrute", 20);
        item23.RequiredUpgradeItems.Add("Bronze", 10);
        item23.RequiredUpgradeItems.Add("RoundLog", 10);
        item23.RequiredUpgradeItems.Add("Resin", 5);
        item23.RequiredUpgradeItems.Add("TrophyGreydwarfBrute", 1);
        Item item24 = new Item("souls", "BlackKnightGreatAxe");
        item24.Name.English("Black Knight GreatAxe");
        item24.Description.English("Greataxe of the Black Knights who wander Lordran. Used to face Chaos demons. The large motion that puts the weight of the body into the attack reflects the great size of their adversaries long ago.");
        item24.Crafting.Add("BlacksmithAltar", 1);
        item24.RequiredItems.Add("BlackMetal", 20);
        item24.RequiredItems.Add("Flametal", 10);
        item24.RequiredItems.Add("Silver", 20);
        item24.RequiredItems.Add("TwinklingTitanite", 20);
        item24.RequiredUpgradeItems.Add("BlackMetal", 10);
        item24.RequiredUpgradeItems.Add("Flametal", 10);
        item24.RequiredUpgradeItems.Add("Silver", 10);
        item24.RequiredUpgradeItems.Add("TwinklingTitanite", 10);
        Item item25 = new Item("souls", "BlackKnightHalberd");
        item25.Name.English("Black Knight Halberd");
        item25.Description.English("Halberd of the black knights who wander Lordran. Used to face chaos demons.");
        item25.Crafting.Add("BlacksmithAltar", 1);
        item25.RequiredItems.Add("BlackMetal", 20);
        item25.RequiredItems.Add("Flametal", 10);
        item25.RequiredItems.Add("Silver", 20);
        item25.RequiredItems.Add("TwinklingTitanite", 10);
        item25.RequiredUpgradeItems.Add("BlackMetal", 10);
        item25.RequiredUpgradeItems.Add("Flametal", 10);
        item25.RequiredUpgradeItems.Add("Silver", 10);
        item25.RequiredUpgradeItems.Add("TwinklingTitanite", 10);
        Item item26 = new Item("souls", "BlackKnightSword");
        item26.Name.English("Black Knight Sword");
        item26.Description.English("sword of the Black Knights who wander Lordran. Used to face chaos demons. The Large motion that puts the weight of the body into the attack reflects the great size of their adversaries long ago.");
        item26.Crafting.Add("BlacksmithAltar", 1);
        item26.RequiredItems.Add("BlackMetal", 20);
        item26.RequiredItems.Add("Flametal", 5);
        item26.RequiredItems.Add("Silver", 20);
        item26.RequiredItems.Add("TwinklingTitanite", 5);
        item26.RequiredUpgradeItems.Add("BlackMetal", 10);
        item26.RequiredUpgradeItems.Add("Flametal", 5);
        item26.RequiredUpgradeItems.Add("Silver", 10);
        item26.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        Item item27 = new Item("souls", "BlackKnightShield");
        item27.Name.English("Black Knight Shield");
        item27.Description.English("Shield of the Black Knights that wander Lordan. A flowing canal is chiseled deeply into its face. Long ago, the black knights faced the chaos demons, and were charred black, but their shields became highly resistant to fire.");
        item27.Crafting.Add("BlacksmithAltar", 1);
        item27.RequiredItems.Add("BlackMetal", 20);
        item27.RequiredItems.Add("Flametal", 5);
        item27.RequiredItems.Add("Silver", 20);
        item27.RequiredItems.Add("TwinklingTitanite", 5);
        item27.RequiredUpgradeItems.Add("BlackMetal", 10);
        item27.RequiredUpgradeItems.Add("Flametal", 5);
        item27.RequiredUpgradeItems.Add("Silver", 10);
        item27.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        Item item28 = new Item("souls", "BlackKnightUGS");
        item28.Name.English("Black Knight Greatsword");
        item28.Description.English("Greatsword of the black knights who wander Lordran. Used to face chaos demons. The large motion that puts the weight of the body into the attack reflects the great size of their adversaries long ago.");
        item28.Crafting.Add("BlacksmithAltar", 1);
        item28.RequiredItems.Add("BlackMetal", 40);
        item28.RequiredItems.Add("Flametal", 10);
        item28.RequiredItems.Add("Silver", 40);
        item28.RequiredItems.Add("TwinklingTitanite", 10);
        item28.RequiredUpgradeItems.Add("BlackMetal", 20);
        item28.RequiredUpgradeItems.Add("Flametal", 10);
        item28.RequiredUpgradeItems.Add("Silver", 10);
        item28.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        Item item29 = new Item("souls", "BlackIronShield");
        item29.Name.English("Black Iron GreatShield");
        item29.Description.English("Greatshield of the might knight Tarkus. Built of special black iron and even heavier than Knight Berenike's tower shield. Especially resistant to fire attacks and effective for shield bashing.");
        item29.Crafting.Add("BlacksmithAltar", 1);
        item29.RequiredItems.Add("Iron", 40);
        item29.RequiredItems.Add("Tin", 10);
        item29.RequiredItems.Add("Wood", 40);
        item29.RequiredItems.Add("TwinklingTitanite", 10);
        item29.RequiredUpgradeItems.Add("Iron", 20);
        item29.RequiredUpgradeItems.Add("Tin", 10);
        item29.RequiredUpgradeItems.Add("Wood", 10);
        item29.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        Item item30 = new Item("souls", "ChannelerTrident");
        item30.Name.English("Channeler Trident");
        item30.Description.English("Trident of the Six-eyed Channelers, sorcerers who serve Seath the Scaleless in collecting human specimens. Thrusted in circular motions in a unique martial arts dance that stirs nearby allies into a bloodthirsty frenzy.");
        item30.Crafting.Add("BlacksmithAltar", 1);
        item30.RequiredItems.Add("Bronze", 25);
        item30.RequiredItems.Add("TwinklingTitanite", 5);
        item30.RequiredItems.Add("Tin", 20);
        item30.RequiredItems.Add("GreydwarfEye", 20);
        item30.RequiredUpgradeItems.Add("Bronze", 10);
        item30.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item30.RequiredUpgradeItems.Add("Tin", 10);
        item30.RequiredUpgradeItems.Add("GreydwarfEye", 5);
        Item item31 = new Item("souls", "CursedGreatsword");
        item31.Name.English("Cursed Greatsword");
        item31.Description.English("Sword born from the souls of the great grey wolf Sif, guardian of the grave of the Abysswalker Knight Artorias. The sword can damage ghosts, as it was cursed when Artorias joined a covenant with the creatures of the Abyss");
        item31.Crafting.Add("BlacksmithAltar", 1);
        item31.RequiredItems.Add("TwinklingTitanite", 25);
        item31.RequiredItems.Add("TrophyWraith", 5);
        item31.RequiredItems.Add("Iron", 20);
        item31.RequiredItems.Add("GreydwarfEye", 20);
        item31.RequiredUpgradeItems.Add("TwinklingTitanite", 10);
        item31.RequiredUpgradeItems.Add("TrophyWraith", 1);
        item31.RequiredUpgradeItems.Add("Iron", 5);
        item31.RequiredUpgradeItems.Add("GreydwarfEye", 5);
        Item item32 = new Item("souls", "DaggerPrisc");
        item32.Name.English("Priscillas Dagger");
        item32.Description.English("This sword, one of the rare dragon weapons, came from the tail of Priscilla, the Dragon Crossbreed in the painted world of Ariamis.\r\nPossessing the power of lifehunt, it dances about when wielded, in a fashion reminiscent of the white-robed painting guardians.");
        item32.Crafting.Add("BlacksmithAltar", 1);
        item32.RequiredItems.Add("TwinklingTitanite", 12);
        item32.RequiredItems.Add("Bloodbag", 20);
        item32.RequiredItems.Add("KnifeChitin", 1);
        item32.RequiredItems.Add("TrophyLeech", 5);
        item32.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item32.RequiredUpgradeItems.Add("Bloodbag", 10);
        item32.RequiredUpgradeItems.Add("KnifeChitin", 1);
        item32.RequiredUpgradeItems.Add("TrophyLeech", 5);
        Item item33 = new Item("souls", "DarkMoonBow");
        item33.Name.English("Darkmoon Bow");
        item33.Description.English("Bow born from the soul of the Dark Sun Gwyndolin, Darkmoon deity who watches over the abandoned city of the Gods, Anor Londo. This golden bow is imbued with powerful magic and is most impressive with Moonlight Arrows.");
        item33.Crafting.Add("BlacksmithAltar", 1);
        item33.RequiredItems.Add("FineWood", 40);
        item33.RequiredItems.Add("Iron", 20);
        item33.RequiredItems.Add("GreydwarfEye", 10);
        item33.RequiredItems.Add("TwinklingTitanite", 12);
        item33.RequiredUpgradeItems.Add("FineWood", 10);
        item33.RequiredUpgradeItems.Add("Iron", 5);
        item33.RequiredUpgradeItems.Add("GreydwarfEye", 5);
        item33.RequiredUpgradeItems.Add("TwinklingTitanite", 4);
        Item item34 = new Item("souls", "DarkSilverTracer");
        item34.Name.English("Dark Silver Tracer");
        item34.Description.English("A dark silver dagger used by the Lord's Blade Ciaran, of Gwyn's Four Knights. The victim is first distracted by dazzling streaks of the Gold Tracer, then stung by the vicious poison of this dagger");
        item34.Crafting.Add("BlacksmithAltar", 1);
        item34.RequiredItems.Add("TwinklingTitanite", 10);
        item34.RequiredItems.Add("Ooze", 20);
        item34.RequiredItems.Add("Iron", 5);
        item34.RequiredItems.Add("FineWood", 5);
        item34.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item34.RequiredUpgradeItems.Add("Ooze", 5);
        item34.RequiredUpgradeItems.Add("Iron", 1);
        Item item35 = new Item("souls", "DarkSword");
        item35.Name.English("Dark Sword");
        item35.Description.English("The sword of the knights of the Four Kings of New Londo. Its blade is wide and thick and it is wielded in an unusual manner. When the Four Kings were seduced by evil, their knights became Darkwraiths, servants of the Dark who wielded these darkswords.");
        item35.Crafting.Add("BlacksmithAltar", 1);
        item35.RequiredItems.Add("ElderBark", 30);
        item35.RequiredItems.Add("Coal", 20);
        item35.RequiredItems.Add("Silver", 40);
        item35.RequiredItems.Add("TwinklingTitanite", 15);
        item35.RequiredUpgradeItems.Add("ElderBark", 10);
        item35.RequiredUpgradeItems.Add("Silver", 10);
        item35.RequiredUpgradeItems.Add("TwinklingTitanite", 3);
        Item item36 = new Item("souls", "DemonGreatHammer");
        item36.Name.English("Demon Great Hammer");
        item36.Description.English("Demon weapon built from the stone archtrees. Used by lesser demons at North Undead Asylum. This hammer is imbued with no special power, but will merrily beat foes to a pulp, provided you have the strength to wield it.");
        item36.Crafting.Add("BlacksmithAltar", 1);
        item36.RequiredItems.Add("Stone", 120);
        item36.RequiredItems.Add("SledgeStagbreaker", 1);
        item36.RequiredItems.Add("TwinklingTitanite", 8);
        item36.RequiredItems.Add("Ruby", 20);
        item36.RequiredUpgradeItems.Add("Stone", 50);
        item36.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item36.RequiredUpgradeItems.Add("Ruby", 3);
        Item item37 = new Item("souls", "DragonGreatSword");
        item37.Name.English("Dragon GreatSword");
        item37.Description.English("This sword, one of the rare dragon weapons, came from the tail of the stone dragon of Ash Lake, descendant of the ancient dragons");
        item37.Crafting.Add("BlacksmithAltar", 1);
        item37.RequiredItems.Add("Stone", 120);
        item37.RequiredItems.Add("TrophyDragonQueen", 2);
        item37.RequiredItems.Add("Silver", 40);
        item37.RequiredItems.Add("TwinklingTitanite", 20);
        item37.RequiredUpgradeItems.Add("Stone", 60);
        item37.RequiredUpgradeItems.Add("TrophyDragonQueen", 2);
        item37.RequiredUpgradeItems.Add("Silver", 20);
        item37.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        Item item38 = new Item("souls", "DragonKingGreatAxe");
        item38.Name.English("Dragon King GreatAxe");
        item38.Description.English("This axe, one of the rare dragon weapons, is formed by the tail of the Gaping Dragon, a distant, deformed descendant of the everlasting dragons.");
        item38.Crafting.Add("BlacksmithAltar", 1);
        item38.RequiredItems.Add("Stone", 120);
        item38.RequiredItems.Add("TwinklingTitanite", 5);
        item38.RequiredItems.Add("Silver", 40);
        item38.RequiredItems.Add("DragonEgg", 4);
        item38.RequiredUpgradeItems.Add("Stone", 60);
        item38.RequiredUpgradeItems.Add("Silver", 20);
        item38.RequiredUpgradeItems.Add("DragonEgg", 1);
        item38.RequiredUpgradeItems.Add("TwinklingTitanite", 1);
        Item item39 = new Item("souls", "DragonSlayerGreatBow");
        item39.Name.English("DragonSlayer GreatBow");
        item39.Description.English("Bow of the Dragonslayers, led by Hawkeye Gough, one of Gwyn's Four Knights. This bow's unusual size requires that it be anchored to the ground when fired. Only uses specialized great arrows.");
        item39.Crafting.Add("BlacksmithAltar", 1);
        item39.RequiredItems.Add("Silver", 50);
        item39.RequiredItems.Add("TwinklingTitanite", 15);
        item39.RequiredItems.Add("Chain", 10);
        item39.RequiredItems.Add("RoundLog", 20);
        item39.RequiredUpgradeItems.Add("Silver", 10);
        item39.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        item39.RequiredUpgradeItems.Add("Chain", 2);
        item39.RequiredUpgradeItems.Add("RoundLog", 5);
        Item item40 = new Item("souls", "DragonSlayerSpear");
        item40.Name.English("DragonSlayer Spear");
        item40.Description.English("Cross spear born from the soul of Ornstein, a Dragonslayer guarding Anor Londo cathedral. Inflicts lightning damage; effective against dragons. Two-handed thrust relies on cross and buries deep within a dragon's hide, and sends human foes flying.");
        item40.Crafting.Add("BlacksmithAltar", 1);
        item40.RequiredItems.Add("Iron", 40);
        item40.RequiredItems.Add("Thunderstone", 20);
        item40.RequiredItems.Add("Silver", 20);
        item40.RequiredItems.Add("TwinklingTitanite", 15);
        item40.RequiredUpgradeItems.Add("Iron", 20);
        item40.RequiredUpgradeItems.Add("Thunderstone", 3);
        item40.RequiredUpgradeItems.Add("Silver", 5);
        item40.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        Item item41 = new Item("souls", "DrakeSword");
        item41.Name.English("Drake Sword");
        item41.Description.English("This sword, one of the rare dragon weapons, is formed by a drake's tail. Drakes are seen as undeveloped imitators of the dragons, but they are likely their distant kin.\r\nThe sword is imbued with a mystical power, to be released when held with both hands.");
        item41.Crafting.Add("BlacksmithAltar", 1);
        item41.RequiredItems.Add("TwinklingTitanite", 20);
        item41.RequiredItems.Add("Stone", 20);
        item41.RequiredItems.Add("Wood", 40);
        item41.RequiredItems.Add("Flint", 1);
        item41.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        item41.RequiredUpgradeItems.Add("Wood", 10);
        item41.RequiredUpgradeItems.Add("Flint", 20);
        Item item42 = new Item("souls", "dragontooth");
        item42.Name.English("Dragon Tooth");
        item42.Description.English("Created from an everlasting dragon tooth. Legendary great hammer of Havel the Rock. The dragon tooth will never break as it is harder than stone, and it grants its wielder resistance to magic and flame");
        item42.Crafting.Add("BlacksmithAltar", 1);
        item42.RequiredItems.Add("TwinklingTitanite", 15);
        item42.RequiredItems.Add("Stone", 80);
        item42.RequiredItems.Add("YmirRemains", 10);
        item42.RequiredItems.Add("BoneFragments", 25);
        item42.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        item42.RequiredUpgradeItems.Add("Stone", 20);
        item42.RequiredUpgradeItems.Add("YmirRemains", 2);
        item42.RequiredUpgradeItems.Add("BoneFragments", 5);
        Item item43 = new Item("souls", "FurySword");
        item43.Name.English("Quelags Fury Sword");
        item43.Description.English("A curved sword born from the soul of Quelaag, daughter of the Witch of Izalith, who was transformed into a chaos demon. Like Quelaag's body, the sword features shells, spikes, humanity and a coating of chaos fire.");
        item43.Crafting.Add("BlacksmithAltar", 1);
        item43.RequiredItems.Add("Bronze", 30);
        item43.RequiredItems.Add("SurtlingCore", 20);
        item43.RequiredItems.Add("Chitin", 40);
        item43.RequiredItems.Add("TwinklingTitanite", 15);
        item43.RequiredUpgradeItems.Add("Bronze", 10);
        item43.RequiredUpgradeItems.Add("SurtlingCore", 5);
        item43.RequiredUpgradeItems.Add("Chitin", 20);
        item43.RequiredUpgradeItems.Add("TwinklingTitanite", 4);
        Item item44 = new Item("souls", "GargoyleAxe");
        item44.Name.English("Gargoyle Tail Axe");
        item44.Description.English("Sliced tail of the gargoyle guarding the Bell of Awakening in the Undead Church or patrolling in Anor Londo. Can be used as a bronze battle axe. Bends dramatically during large attacks, owing to its nature as a tail.");
        item44.Crafting.Add("BlacksmithAltar", 1);
        item44.RequiredItems.Add("Stone", 40);
        item44.RequiredItems.Add("Flint", 12);
        item44.RequiredItems.Add("GreydwarfEye", 15);
        item44.RequiredItems.Add("TwinklingTitanite", 10);
        item44.RequiredUpgradeItems.Add("Stone", 8);
        item44.RequiredUpgradeItems.Add("Flint", 2);
        item44.RequiredUpgradeItems.Add("GreydwarfEye", 2);
        item44.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        Item item45 = new Item("souls", "Glordsword");
        item45.Name.English("GraveLord Sword");
        item45.Description.English("Sword wielded only by servants of Gravelord Nito, the first of the dead. Crafted from the bones of the fallen. The miasma of death exudes from the sword, a veritable toxin to any living being.");
        item45.Crafting.Add("BlacksmithAltar", 1);
        item45.RequiredItems.Add("WitheredBone", 30);
        item45.RequiredItems.Add("TwinklingTitanite", 20);
        item45.RequiredItems.Add("Ooze", 20);
        item45.RequiredItems.Add("Guck", 20);
        item45.RequiredUpgradeItems.Add("WitheredBone", 3);
        item45.RequiredUpgradeItems.Add("TwinklingTitanite", 3);
        item45.RequiredUpgradeItems.Add("Ooze", 5);
        item45.RequiredUpgradeItems.Add("Guck", 5);
        Item item46 = new Item("souls", "GoldTracer");
        item46.Name.English("Gold Tracer");
        item46.Description.English("Curved sword used by the Lord's Blade Ciaran, one of Gwyn's Four Knights. Ciaran brandishes her sword in a mesmerizing dance, etching the darkness with dire streaks of gold.");
        item46.Crafting.Add("BlacksmithAltar", 1);
        item46.RequiredItems.Add("Bronze", 40);
        item46.RequiredItems.Add("Coins", 99);
        item46.RequiredItems.Add("Ruby", 40);
        item46.RequiredItems.Add("TwinklingTitanite", 20);
        item46.RequiredUpgradeItems.Add("Bronze", 10);
        item46.RequiredUpgradeItems.Add("Coins", 10);
        item46.RequiredUpgradeItems.Add("Ruby", 5);
        item46.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        Item item47 = new Item("souls", "GoldSilverTracers");
        item47.Name.English("Gold & Silver Tracers");
        item47.Description.English("Dual Weapons used by the Lord's Blade Ciaran, one of Gwyn's Four Knights.");
        item47.Crafting.Add("BlacksmithAltar", 1);
        item47.RequiredItems.Add("GoldTracer", 1);
        item47.RequiredItems.Add("DarkSilverTracer", 1);
        item47.RequiredItems.Add("Bronze", 40);
        item47.RequiredItems.Add("Silver", 20);
        item47.RequiredUpgradeItems.Add("Bronze", 10);
        item47.RequiredUpgradeItems.Add("Silver", 10);
        item47.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        Item item48 = new Item("souls", "GreatLordGreatSword");
        item48.Name.English("Great Lord GreatSword");
        item48.Description.English("Greatsword born from the soul of Gwyn, Lord of Cinder. As bearer of the ultimate soul, Gwyn wielded the bolts of the sun, but before linking the fire, divided that power amongst his children, and set off with only this greatsword as his companion.");
        item48.Crafting.Add("BlacksmithAltar", 1);
        item48.RequiredItems.Add("Flametal", 50);
        item48.RequiredItems.Add("SurtlingCore", 20);
        item48.RequiredItems.Add("TwinklingTitanite", 15);
        item48.RequiredItems.Add("BlackCore", 10);
        item48.RequiredUpgradeItems.Add("Flametal", 20);
        item48.RequiredUpgradeItems.Add("SurtlingCore", 5);
        item48.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        item48.RequiredUpgradeItems.Add("BlackCore", 5);
        Item item49 = new Item("souls", "HavelGreatShield");
        item49.Name.English("Havels GreatShield");
        item49.Description.English("Greatshield of the legendary Havel the Rock. Cut straight from a great slab of stone. This greatshield is imbued with the magic of Havel, proves a strong defense, and is incredibly heavy. A true divine heirloom on par with the Dragon tooth");
        item49.Crafting.Add("BlacksmithAltar", 1);
        item49.RequiredItems.Add("Stone", 50);
        item49.RequiredItems.Add("Iron", 20);
        item49.RequiredItems.Add("Wood", 15);
        item49.RequiredItems.Add("TwinklingTitanite", 10);
        item49.RequiredUpgradeItems.Add("Stone", 20);
        item49.RequiredUpgradeItems.Add("Iron", 5);
        item49.RequiredUpgradeItems.Add("Wood", 5);
        item49.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        Item item50 = new Item("souls", "GrassCrestShield");
        item50.Name.English("Grass-Crest Shield");
        item50.Description.English("Old medium metal shield of unknown origin. The grass crest is lightly imbued with magic, which slightly speeds stamina recovery.");
        item50.Crafting.Add("BlacksmithAltar", 1);
        item50.RequiredItems.Add("Dandelion", 25);
        item50.RequiredItems.Add("FineWood", 20);
        item50.RequiredItems.Add("Resin", 15);
        item50.RequiredItems.Add("TwinklingTitanite", 5);
        item50.RequiredUpgradeItems.Add("Dandelion", 2);
        item50.RequiredUpgradeItems.Add("FineWood", 5);
        item50.RequiredUpgradeItems.Add("Resin", 5);
        item50.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        Item item51 = new Item("souls", "ManusCatalyst");
        item51.Name.English("Manus Catalyst");
        item51.Description.English("A sorcery catalyst born from the soul of Manus, Father of the Abyss. A rough, old wooden catalyst large enough to be used as a strike weapon. Similar to the Tin Crystallization Catalyst, it boosts the strength of sorceries, but limits the number of castings");
        item51.Crafting.Add("BlacksmithAltar", 1);
        item51.RequiredItems.Add("FineWood", 40);
        item51.RequiredItems.Add("TrophyWraith", 5);
        item51.RequiredItems.Add("SurtlingCore", 10);
        item51.RequiredItems.Add("TwinklingTitanite", 20);
        item51.RequiredUpgradeItems.Add("FineWood", 10);
        item51.RequiredUpgradeItems.Add("TrophyWraith", 1);
        item51.RequiredUpgradeItems.Add("SurtlingCore", 2);
        item51.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        Item item52 = new Item("souls", "MaskOfFather");
        item52.Name.English("Mask of the Father");
        item52.Description.English("One of the three masks of the Pinwheel, the necromancer who stole the power of the Gravelord, and reigns over the Catacombs. This mask, belonging to the valiant father, slightly raises equipment load");
        item52.Crafting.Add("BlacksmithAltar", 1);
        item52.RequiredItems.Add("FineWood", 40);
        item52.RequiredItems.Add("GreydwarfEye", 20);
        item52.RequiredItems.Add("TwinklingTitanite", 10);
        item52.RequiredItems.Add("Bronze", 20);
        item52.RequiredUpgradeItems.Add("FineWood", 10);
        item52.RequiredUpgradeItems.Add("GreydwarfEye", 2);
        item52.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item52.RequiredUpgradeItems.Add("Bronze", 5);
        Item item53 = new Item("souls", "MLgreatsword");
        item53.Name.English("Moonlight Greatsword");
        item53.Description.English("This sword, one of the rare dragon weapons, came from the tail of Seath the Scaleless, the pale white dragon who betrayed his own. Seath is the grandfather of sorcery, and this sword is imbued with his magic, which shall be unleashed as a wave of moonlight.");
        item53.Crafting.Add("BlacksmithAltar", 1);
        item53.RequiredItems.Add("TwinklingTitanite", 25);
        item53.RequiredItems.Add("DragonTear", 10);
        item53.RequiredItems.Add("Eitr", 10);
        item53.RequiredItems.Add("Crystal", 20);
        item53.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        item53.RequiredUpgradeItems.Add("DragonTear", 1);
        item53.RequiredUpgradeItems.Add("Eitr", 5);
        item53.RequiredUpgradeItems.Add("Crystal", 10);
        Item item54 = new Item("souls", "Murakumo");
        item54.Name.English("Murakumo");
        item54.Description.English("Giant curved sword forged using special methods in an Eastern Land. This unparalleled weapon cuts like a Katana but is heavier than a Nata machete. Requires extreme strength, dexterity, and stamina to wield");
        item54.Crafting.Add("BlacksmithAltar", 1);
        item54.RequiredItems.Add("TwinklingTitanite", 10);
        item54.RequiredItems.Add("Tin", 10);
        item54.RequiredItems.Add("Wood", 20);
        item54.RequiredItems.Add("Resin", 10);
        item54.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item54.RequiredUpgradeItems.Add("Tin", 1);
        item54.RequiredUpgradeItems.Add("Wood", 5);
        item54.RequiredUpgradeItems.Add("Resin", 2);
        Item item55 = new Item("souls", "MLHorn");
        item55.Name.English("Moonlight Butterfly Horn");
        item55.Description.English("Weapon born from the mystical creature of the Darkroot Garden, the Moonlight Butterfly. The horns of the butterfly, a being created by Seath, are imbued with a pure magic power.");
        item55.Crafting.Add("BlacksmithAltar", 1);
        item55.RequiredItems.Add("AncientSeed", 30);
        item55.RequiredItems.Add("TwinklingTitanite", 10);
        item55.RequiredItems.Add("GreydwarfEye", 20);
        item55.RequiredItems.Add("FineWood", 10);
        item55.RequiredUpgradeItems.Add("AncientSeed", 10);
        item55.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item55.RequiredUpgradeItems.Add("GreydwarfEye", 10);
        item55.RequiredUpgradeItems.Add("FineWood", 5);
        Item item56 = new Item("souls", "Shotel");
        item56.Name.English("Shotel");
        item56.Description.English("Curved sword with sharply curved blade. Created by Arstor, Earl of Carim. Requires great skill to wield, but evades shield defense to sneak in damage.");
        item56.Crafting.Add("BlacksmithAltar", 1);
        item56.RequiredItems.Add("Tin", 40);
        item56.RequiredItems.Add("RoundLog", 20);
        item56.RequiredItems.Add("TwinklingTitanite", 10);
        item56.RequiredItems.Add("DeerHide", 20);
        item56.RequiredUpgradeItems.Add("Bronze", 10);
        item56.RequiredUpgradeItems.Add("Amber", 5);
        item56.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item56.RequiredUpgradeItems.Add("DeerHide", 5);
        Item item57 = new Item("souls", "SmoughHammer");
        item57.Name.English("Smough's Hammer");
        item57.Description.English("Great Hammer from the soul of executioner Smough, who guards the cathedral in the forsaken city of Anor Londo. Smough loved his work, and ground the bones of his victims into his own feed, ruining his hopes of being ranked with the Four Knights");
        item57.Crafting.Add("BlacksmithAltar", 1);
        item57.RequiredItems.Add("Bronze", 40);
        item57.RequiredItems.Add("RoundLog", 20);
        item57.RequiredItems.Add("TwinklingTitanite", 20);
        item57.RequiredItems.Add("FineWood", 20);
        item57.RequiredUpgradeItems.Add("Bronze", 10);
        item57.RequiredUpgradeItems.Add("Amber", 5);
        item57.RequiredUpgradeItems.Add("TwinklingTitanite", 5);
        item57.RequiredUpgradeItems.Add("FineWood", 5);
        Item item58 = new Item("souls", "StaffWood");
        item58.Name.English("Beatrice's Catalyst");
        item58.Description.English("Catalyst belonging to Beatrice, the rogue witch. Contrasts with Vinheim catalysts. This ancient catalyst shows signs of being used for age-old sorceries. It has passed the hands of many generations to get here.");
        item58.Crafting.Add("BlacksmithAltar", 1);
        item58.RequiredItems.Add("Wood", 40);
        item58.RequiredItems.Add("TwinklingTitanite", 10);
        item58.RequiredItems.Add("GreydwarfEye", 20);
        item58.RequiredItems.Add("HardAntler", 5);
        item58.RequiredUpgradeItems.Add("Wood", 10);
        item58.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item58.RequiredUpgradeItems.Add("GreydwarfEye", 5);
        item58.RequiredUpgradeItems.Add("HardAntler", 5);
        Item item59 = new Item("souls", "sunshield1");
        item59.Name.English("sunlight shield");
        item59.Description.English("Shield of Solaire of Astora, Knight of Sunlight. Decorated with a holy symbol, but Solaire illustrated it himself, and it has no divine powers of its own. As it turns out, Solaire's incredible prowess is a product of his own training, and nothing else.");
        item59.Crafting.Add("BlacksmithAltar", 1);
        item59.RequiredItems.Add("Bronze", 25);
        item59.RequiredItems.Add("Amber", 20);
        item59.RequiredItems.Add("BronzeNails", 20);
        item59.RequiredItems.Add("TwinklingTitanite", 10);
        item59.RequiredUpgradeItems.Add("Bronze", 10);
        item59.RequiredUpgradeItems.Add("Amber", 5);
        item59.RequiredUpgradeItems.Add("BronzeNails", 5);
        item59.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        Item item60 = new Item("souls", "SunlightSword");
        item60.Name.English("Sunlight StraightSword");
        item60.Description.English("This standard longsword, belonging to Solaire of Astora, is of high quality, is well-forged, and has been kept in good repair. Easy to use and dependable, but unlikely to live up to its grandiose name.");
        item60.Crafting.Add("BlacksmithAltar", 1);
        item60.RequiredItems.Add("TwinklingTitanite", 10);
        item60.RequiredItems.Add("FineWood", 10);
        item60.RequiredItems.Add("TrophyGreydwarf", 5);
        item60.RequiredItems.Add("Tin", 10);
        item60.RequiredUpgradeItems.Add("Tin", 10);
        item60.RequiredUpgradeItems.Add("FineWood", 10);
        item60.RequiredUpgradeItems.Add("TrophyGreydwarf", 1);
        item60.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        Item item61 = new Item("souls", "washingpole");
        item61.Name.English("Washing Pole");
        item61.Description.English("Katana forged in an Eastern land. Very unusual specimen with a long blade. Has a different move set than the Uchigatana. The blade is extremely long, but as a result, quite easily broken");
        item61.Crafting.Add("BlacksmithAltar", 1);
        item61.RequiredItems.Add("TwinklingTitanite", 12);
        item61.RequiredItems.Add("Resin", 10);
        item61.RequiredItems.Add("FineWood", 10);
        item61.RequiredUpgradeItems.Add("TwinklingTitanite", 2);
        item61.RequiredUpgradeItems.Add("Resin", 10);
        item61.RequiredUpgradeItems.Add("FineWood", 10);
        GameObject val = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_Andre_bye1");
        GameObject val2 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_Andre_greeting1");
        GameObject val3 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_Andre_talk1");
        GameObject val4 = ItemManager.PrefabManager.RegisterPrefab("souls", "firelinkshrine_sfx");
        GameObject val5 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_taurus_attack1");
        GameObject val6 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_taurus_weaponhit1");
        GameObject val7 = ItemManager.PrefabManager.RegisterPrefab("souls", "FX_Taurus_Dead");
        GameObject val8 = ItemManager.PrefabManager.RegisterPrefab("souls", "FX_Taurus_Hit");
        GameObject val9 = ItemManager.PrefabManager.RegisterPrefab("souls", "SFX_Taurus_Alert1");
        GameObject val10 = ItemManager.PrefabManager.RegisterPrefab("souls", "SFX_Taurus_Idle1");
        GameObject val11 = ItemManager.PrefabManager.RegisterPrefab("souls", "SFX_Hollow_Alert");
        GameObject val12 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_Hollow_Attack");
        GameObject val13 = ItemManager.PrefabManager.RegisterPrefab("souls", "SFX_Hollow_Idle");
        GameObject val14 = ItemManager.PrefabManager.RegisterPrefab("souls", "SFX_Hollow_death");
        GameObject val15 = ItemManager.PrefabManager.RegisterPrefab("souls", "FX_BlackKnight_Death");
        GameObject val16 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_blackknight_hit");
        GameObject val17 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_crystalgolem_hurt");
        GameObject val18 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_fangboar_alert");
        GameObject val19 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_fangboar_attack");
        GameObject val20 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_fangboar_dead");
        GameObject val21 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_fangboar_hit");
        GameObject val22 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_fangboar_idle");
        GameObject val23 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_wyvern_idle");
        GameObject val24 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_wyvern_alert");
        GameObject val25 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_wyvern_attack");
        GameObject val26 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_wyvern_hit");
        GameObject val27 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_wyvern_dead");
        GameObject val28 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_idle");
        GameObject val29 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_alert");
        GameObject val30 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_attack");
        GameObject val31 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_hit");
        GameObject val32 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_damage");
        GameObject val33 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_swing");
        GameObject val34 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_capra_footstep");
        GameObject val35 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_hello");
        GameObject val36 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_thankyou");
        GameObject val37 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_helpme");
        GameObject val38 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_verygood");
        GameObject val39 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_imsorry");
        GameObject val40 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_sif_alert");
        GameObject val41 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_sif_attack");
        GameObject val42 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_sif_hit");
        GameObject val43 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_sif_swing");
        Item item62 = new Item("souls", "TaurusWep");
        item62.Configurable = Configurability.Disabled;
        Item item63 = new Item("souls", "TaurusWep1");
        item63.Configurable = Configurability.Disabled;
        Item item64 = new Item("souls", "TaurusWep2");
        item64.Configurable = Configurability.Disabled;
        Item item65 = new Item("souls", "MushroomUnarmed");
        item65.Configurable = Configurability.Disabled;
        Item item66 = new Item("souls", "BlackKnightHalberd1");
        item66.Configurable = Configurability.Disabled;
        Item item67 = new Item("souls", "BlackKnightHalberd2");
        item67.Configurable = Configurability.Disabled;
        Item item68 = new Item("souls", "BlackKnightHalberd3");
        item68.Configurable = Configurability.Disabled;
        Item item69 = new Item("souls", "BlackKnightGreatAxe1");
        item69.Configurable = Configurability.Disabled;
        Item item70 = new Item("souls", "BlackKnightGreatAxe2");
        item70.Configurable = Configurability.Disabled;
        Item item71 = new Item("souls", "BlackKnightUGS1");
        item71.Configurable = Configurability.Disabled;
        Item item72 = new Item("souls", "BlackKnightUGS2");
        item72.Configurable = Configurability.Disabled;
        Item item73 = new Item("souls", "Capra_Attack1");
        item73.Configurable = Configurability.Disabled;
        Item item74 = new Item("souls", "Capra_Attack2");
        item74.Configurable = Configurability.Disabled;
        Item item75 = new Item("souls", "Capra_Attack3");
        item75.Configurable = Configurability.Disabled;
        Item item76 = new Item("souls", "Capra_AttackCombo");
        item76.Configurable = Configurability.Disabled;
        Item item77 = new Item("souls", "Capra_AttackJump");
        item77.Configurable = Configurability.Disabled;
        Item item78 = new Item("souls", "CrystalGolem_Attack");
        item78.Configurable = Configurability.Disabled;
        Item item79 = new Item("souls", "Golem_Unarmed2");
        item79.Configurable = Configurability.Disabled;
        Item item80 = new Item("souls", "Golem_Unarmed3");
        item80.Configurable = Configurability.Disabled;
        Item item81 = new Item("souls", "Skeleton_Attack_1");
        item81.Configurable = Configurability.Disabled;
        Item item82 = new Item("souls", "Skeleton_Attack_2");
        item82.Configurable = Configurability.Disabled;
        Item item83 = new Item("souls", "Skeleton_Attack_3");
        item83.Configurable = Configurability.Disabled;
        Item item84 = new Item("souls", "Skeleton_Attack_4");
        item84.Configurable = Configurability.Disabled;
        Item item85 = new Item("souls", "Skeleton_Attack_5");
        item85.Configurable = Configurability.Disabled;
        Item item86 = new Item("souls", "Skeleton_Attack_6");
        item86.Configurable = Configurability.Disabled;
        Item item87 = new Item("souls", "Skeleton_Attack_7");
        item87.Configurable = Configurability.Disabled;
        Item item88 = new Item("souls", "Skeleton_Attack_8");
        item88.Configurable = Configurability.Disabled;
        Item item89 = new Item("souls", "hollow_bow1");
        item89.Configurable = Configurability.Disabled;
        Item item90 = new Item("souls", "hollow_sword1");
        item90.Configurable = Configurability.Disabled;
        Item item91 = new Item("souls", "attack_breath");
        item91.Configurable = Configurability.Disabled;
        Item item92 = new Item("souls", "attack_breath_arc");
        item92.Configurable = Configurability.Disabled;
        Item item93 = new Item("souls", "attack_breath_flying");
        item93.Configurable = Configurability.Disabled;
        Item item94 = new Item("souls", "ChaosZweihander");
        item94.Configurable = Configurability.Disabled;
        Item item95 = new Item("souls", "ChaosZweihander1");
        item95.Configurable = Configurability.Disabled;
        Item item96 = new Item("souls", "BlackFlame");
        item96.Configurable = Configurability.Disabled;
        Item item97 = new Item("souls", "FangBoar_Attack");
        item97.Configurable = Configurability.Disabled;
        Item item98 = new Item("souls", "FangBoar_Attack2");
        item98.Configurable = Configurability.Disabled;
        Item item99 = new Item("souls", "FangBoar_Attack3");
        item99.Configurable = Configurability.Disabled;
        Item item100 = new Item("souls", "GrassCrestShield1");
        item100.Configurable = Configurability.Disabled;
        Item item101 = new Item("souls", "Ornstein_JumpSlash");
        item101.Configurable = Configurability.Disabled;
        Item item102 = new Item("souls", "Ornstein_JumpStab");
        item102.Configurable = Configurability.Disabled;
        Item item103 = new Item("souls", "Ornstein_LightningSpear1");
        item103.Configurable = Configurability.Disabled;
        Item item104 = new Item("souls", "Ornstein_LightningSpear2");
        item104.Configurable = Configurability.Disabled;
        Item item105 = new Item("souls", "Ornstein_QuickStab");
        item105.Configurable = Configurability.Disabled;
        Item item106 = new Item("souls", "Ornstein_Rush");
        item106.Configurable = Configurability.Disabled;
        Item item107 = new Item("souls", "Ornstein_Slash");
        item107.Configurable = Configurability.Disabled;
        Item item108 = new Item("souls", "Ornstein_Thrust1");
        item108.Configurable = Configurability.Disabled;
        Item item109 = new Item("souls", "Ornstein_Thrust2");
        item109.Configurable = Configurability.Disabled;
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
        Item item118 = new Item("souls", "Wyvern_Bite");
        item118.Configurable = Configurability.Disabled;
        Item item119 = new Item("souls", "WispAttack");
        item119.Configurable = Configurability.Disabled;
        ItemManager.PrefabManager.RegisterPrefab(ItemManager.PrefabManager.RegisterAssetBundle("souls"), "BlackFlame_AOE");
        ItemManager.PrefabManager.RegisterPrefab(ItemManager.PrefabManager.RegisterAssetBundle("souls"), "LightningSpear_Projectile");
        ItemManager.PrefabManager.RegisterPrefab(ItemManager.PrefabManager.RegisterAssetBundle("souls"), "dragon_lightning_projectile");
        ItemManager.PrefabManager.RegisterPrefab(ItemManager.PrefabManager.RegisterAssetBundle("souls"), "projectile_hello");
        ItemManager.PrefabManager.RegisterPrefab(ItemManager.PrefabManager.RegisterAssetBundle("souls"), "projectile_thankyou");
        ItemManager.PrefabManager.RegisterPrefab(ItemManager.PrefabManager.RegisterAssetBundle("souls"), "projectile_helpme");
        ItemManager.PrefabManager.RegisterPrefab(ItemManager.PrefabManager.RegisterAssetBundle("souls"), "projectile_imsorry");
        ItemManager.PrefabManager.RegisterPrefab(ItemManager.PrefabManager.RegisterAssetBundle("souls"), "projectile_verygood");
        ItemManager.PrefabManager.RegisterPrefab(ItemManager.PrefabManager.RegisterAssetBundle("souls"), "CatCharm_projectile");
        ItemManager.PrefabManager.RegisterPrefab(ItemManager.PrefabManager.RegisterAssetBundle("souls"), "WolfCharm_Projectile");
        Creature creature = new Creature("souls", "Capra")
        {
            Biome = (Biome)32,
            GroupSize = new CreatureManager.Range(1f, 2f),
            CheckSpawnInterval = 1900,
            Maximum = 3
        };
        creature.Localize().English("Capra Demon");
        Creature creature2 = new Creature("souls", "BlackKnight")
        {
            Biome = (Biome)32,
            GroupSize = new CreatureManager.Range(1f, 2f),
            CheckSpawnInterval = 1900,
            Maximum = 3
        };
        creature2.Localize().English("BlackKnight");
        creature2.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(1f, 3f);
        creature2.Drops["TwinklingTitanite"].DropChance = 75f;
        Creature creature3 = new Creature("souls", "FangBoar")
        {
            RequiredGlobalKey = GlobalKey.KilledBonemass,
            Biome = (Biome)8,
            GroupSize = new CreatureManager.Range(1f, 2f),
            CheckSpawnInterval = 1500,
            Maximum = 1
        };
        creature3.Localize().English("Fang Boar");
        creature3.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(2f, 4f);
        creature3.Drops["TwinklingTitanite"].DropChance = 75f;
        Creature creature4 = new Creature("souls", "GiantDad")
        {
            RequiredGlobalKey = GlobalKey.KilledBonemass,
            Biome = (Biome)1,
            GroupSize = new CreatureManager.Range(1f, 2f),
            CheckSpawnInterval = 12000,
            Maximum = 1
        };
        creature4.Localize().English("GiantDad");
        creature4.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(2f, 4f);
        creature4.Drops["TwinklingTitanite"].DropChance = 75f;
        Creature creature5 = new Creature("souls", "GiantSkeleton_Sword")
        {
            RequiredGlobalKey = GlobalKey.KilledBonemass,
            Biome = (Biome)64,
            GroupSize = new CreatureManager.Range(1f, 2f),
            CheckSpawnInterval = 12000,
            Maximum = 1
        };
        creature5.Localize().English("Giant Skeleton");
        creature5.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(2f, 4f);
        creature5.Drops["TwinklingTitanite"].DropChance = 75f;
        Creature creature6 = new Creature("souls", "GiantMushroom")
        {
            RequiredGlobalKey = GlobalKey.KilledBonemass,
            Biome = (Biome)10,
            GroupSize = new CreatureManager.Range(1f, 2f),
            CheckSpawnInterval = 1600,
            Maximum = 2
        };
        creature6.Localize().English("Giant Mushroom");
        creature6.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(1f, 3f);
        creature6.Drops["TwinklingTitanite"].DropChance = 75f;
        creature6.Drops["Mushroom"].Amount = new CreatureManager.Range(1f, 3f);
        creature6.Drops["Mushroom"].DropChance = 75f;
        Creature creature7 = new Creature("souls", "HollowSoldier")
        {
            RequiredGlobalKey = GlobalKey.KilledElder,
            Biome = (Biome)11,
            GroupSize = new CreatureManager.Range(1f, 3f),
            CheckSpawnInterval = 2000,
            Maximum = 3
        };
        creature7.Localize().English("Hollow Soldier");
        creature7.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(1f, 2f);
        creature7.Drops["TwinklingTitanite"].DropChance = 75f;
        Creature creature8 = new Creature("souls", "Sif")
        {
            Biome = (Biome)0,
            Maximum = 0,
            FoodItems = "RawMeat"
        };
        creature8.Localize().English("Sif");
        Creature creature9 = new Creature("souls", "Wyvern")
        {
            Biome = (Biome)64,
            GroupSize = new CreatureManager.Range(1f, 2f),
            CheckSpawnInterval = 2000,
            Maximum = 2
        };
        creature9.Localize().English("Blue Wyvern");
        creature9.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(1f, 2f);
        creature9.Drops["TwinklingTitanite"].DropChance = 75f;
        Creature creature10 = new Creature("souls", "CrystalGolem")
        {
            Biome = (Biome)64,
            GroupSize = new CreatureManager.Range(1f, 1f),
            CheckSpawnInterval = 2000,
            Maximum = 2
        };
        creature10.Localize().English("Blue CrystalGolem");
        creature10.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(1f, 2f);
        creature10.Drops["TwinklingTitanite"].DropChance = 75f;
        Creature creature11 = new Creature("souls", "TaurusDemon")
        {
            RequiredGlobalKey = GlobalKey.KilledElder,
            Biome = (Biome)40,
            GroupSize = new CreatureManager.Range(1f, 2f),
            CheckSpawnInterval = 3500,
            Maximum = 1
        };
        creature11.Localize().English("TaurusDemon");
        creature11.Drops["TwinklingTitanite"].Amount = new CreatureManager.Range(2f, 3f);
        creature11.Drops["TwinklingTitanite"].DropChance = 75f;
        Creature creature12 = new Creature("souls", "SweetShalquoir")
        {
            Biome = (Biome)0,
            GroupSize = new CreatureManager.Range(1f, 2f),
            CheckSpawnInterval = 600,
            RequiredWeather = (Weather.Rain | Weather.Fog),
            FoodItems = "FishRaw",
            Maximum = 0
        };
        creature12.Localize().English("Sweet Shalquoir");
        creature12.Drops["Wood"].Amount = new CreatureManager.Range(1f, 2f);
        creature12.Drops["Wood"].DropChance = 100f;
        GameObject val44 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_cat_hit");
        GameObject val45 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_cat_idle");
        GameObject val46 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_cat_alert");
        GameObject val47 = ItemManager.PrefabManager.RegisterPrefab("souls", "sfx_cat_attack");
        Item item120 = new Item("souls", "Cat_AttackP1");
        item120.Configurable = Configurability.Disabled;
        Item item121 = new Item("souls", "Cat_bap");
        item121.Configurable = Configurability.Disabled;
        Item item122 = new Item("souls", "Cat_bite");
        item122.Configurable = Configurability.Disabled;
        Item item123 = new Item("souls", "Cat_jumpattack");
        item123.Configurable = Configurability.Disabled;
        Assembly executingAssembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll();
        SetupWatcher();
    }

    private void OnDestroy()
    {
        ((BaseUnityPlugin)this).Config.Save();
    }

    private void SetupWatcher()
    {
        FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName);
        fileSystemWatcher.Changed += ReadConfigValues;
        fileSystemWatcher.Created += ReadConfigValues;
        fileSystemWatcher.Renamed += ReadConfigValues;
        fileSystemWatcher.IncludeSubdirectories = true;
        fileSystemWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        fileSystemWatcher.EnableRaisingEvents = true;
    }

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(ConfigFileFullPath))
        {
            return;
        }
        try
        {
            PungusSoulsLogger.LogDebug((object)"ReadConfigValues called");
            ((BaseUnityPlugin)this).Config.Reload();
        }
        catch
        {
            PungusSoulsLogger.LogError((object)("There was an issue loading your " + ConfigFileName));
            PungusSoulsLogger.LogError((object)"Please check your config entries for spelling and format!");
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
