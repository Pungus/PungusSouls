using CreatureManager;
using LocalizationManager;
using UnityEngine;

namespace PungusSouls.Main
{
    internal class PungusSouls
    {
        public void Awake()
        {
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

            #endregion
        }
    }
}