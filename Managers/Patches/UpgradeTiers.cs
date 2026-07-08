using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PungusSouls
{
    public static class UpgradeMaps
    {
        public static void Register()
        {
            UpgradeMapRegistry.Register(
                new UpgradeMap
                {
                    Name = "Standard",

                    Tiers =
                    {
                        new UpgradeTier
                        {
                            StartLevel = 1,
                            EndLevel = 4,

                            StationLevel = 1,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteShard",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "MushroomYellow",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 5,
                            EndLevel = 8,

                            StationLevel = 2,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "LargeTitaniteShard",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "SerpentScale",
                                    5)
                            }
                        },

                        new UpgradeTier
                        {
                            StartLevel = 9,
                            EndLevel = 12,

                            StationLevel = 3,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteChunk",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "WolfClaw",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 13,
                            EndLevel = 16,

                            StationLevel = 4,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteSlab",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "LinenThread",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 17,
                            EndLevel = 20,

                            StationLevel = 5,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TwinklingTitanite",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "BlackCore",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 21,
                            EndLevel = 24,

                            StationLevel = 6,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "DemonTitanite",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "FlametalNew",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 25,
                            EndLevel = 28,

                            StationLevel = 7,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteScale",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "FlametalNew",
                                    5)
                            }
                        }
                    }
                });
            UpgradeMapRegistry.Register(
                new UpgradeMap
            {
                Name = "Tier2",

                Tiers =
                    {
                        new UpgradeTier
                        {
                            StartLevel = 1,
                            EndLevel = 4,

                            StationLevel = 2,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "LargeTitaniteShard",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "SerpentScale",
                                    5)
                            }
                        },

                        new UpgradeTier
                        {
                            StartLevel = 5,
                            EndLevel = 8,

                            StationLevel = 3,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteChunk",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "WolfClaw",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 9,
                            EndLevel = 12,

                            StationLevel = 4,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteSlab",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "LinenThread",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 13,
                            EndLevel = 16,

                            StationLevel = 5,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TwinklingTitanite",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "BlackCore",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 17,
                            EndLevel = 20,

                            StationLevel = 6,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "DemonTitanite",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "FlametalNew",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 21,
                            EndLevel = 24,

                            StationLevel = 7,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteScale",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "FlametalNew",
                                    5)
                            }
                        }
                    }
            });
            UpgradeMapRegistry.Register(
                new UpgradeMap
                {
                    Name = "Tier3",

                    Tiers =
                    {
                        new UpgradeTier
                        {
                            StartLevel = 1,
                            EndLevel = 4,

                            StationLevel = 3,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteChunk",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "WolfClaw",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 5,
                            EndLevel = 8,

                            StationLevel = 4,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteSlab",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "LinenThread",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 9,
                            EndLevel = 12,

                            StationLevel = 5,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TwinklingTitanite",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "BlackCore",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 13,
                            EndLevel = 16,

                            StationLevel = 6,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "DemonTitanite",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "FlametalNew",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 17,
                            EndLevel = 20,

                            StationLevel = 7,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteScale",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "FlametalNew",
                                    5)
                            }
                        }                 
                    }
                });
            UpgradeMapRegistry.Register(
                new UpgradeMap
                {
                    Name = "Tier4",

                    Tiers =
                    {

                        new UpgradeTier
                        {
                            StartLevel = 1,
                            EndLevel = 4,

                            StationLevel = 4,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteSlab",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "LinenThread",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 5,
                            EndLevel = 8,

                            StationLevel = 5,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TwinklingTitanite",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "BlackCore",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 9,
                            EndLevel = 12,

                            StationLevel = 6,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "DemonTitanite",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "FlametalNew",
                                    5)
                            }
                        },
                         new UpgradeTier
                        {
                            StartLevel = 13,
                            EndLevel = 16,

                            StationLevel = 7,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteScale",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "FlametalNew",
                                    5)
                            }
                        }
                    }
                });
            UpgradeMapRegistry.Register(
                new UpgradeMap
                {
                    Name = "Tier5",

                    Tiers =
                    {

                        new UpgradeTier
                        {
                            StartLevel = 1,
                            EndLevel = 4,

                            StationLevel = 5,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TwinklingTitanite",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "BlackCore",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 5,
                            EndLevel = 8,

                            StationLevel = 6,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "DemonTitanite",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "FlametalNew",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 9,
                            EndLevel = 12,

                            StationLevel = 7,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteScale",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "FlametalNew",
                                    5)
                            }
                        }           
                    }
                });
            UpgradeMapRegistry.Register(
                new UpgradeMap
                {
                    Name = "Tier6",

                    Tiers =
                    {
                        new UpgradeTier
                        {
                            StartLevel = 1,
                            EndLevel = 4,

                            StationLevel = 6,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "DemonTitanite",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "FlametalNew",
                                    5)
                            }
                        },
                        new UpgradeTier
                        {
                            StartLevel = 5,
                            EndLevel = 8,

                            StationLevel = 7,

                            Multiplier = 2f,

                            Requirements = new[]
                            {
                                new WeaponUpgradeRequirementLevel(
                                    "TitaniteScale",
                                    5),

                                new WeaponUpgradeRequirementLevel(
                                    "FlametalNew",
                                    5)
                            }
                        }                 
                    }
                });

        }
    }
}