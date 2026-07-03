using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mistforge.EditorTools
{
    /// One-click project bootstrap: creates every ScriptableObject from the
    /// §8 data tables, the GameDatabase in Resources, and the four scenes
    /// (Boot / Town_Rimwatch / Field_Hushfields / Battle), then registers them
    /// in Build Settings. Idempotent — safe to re-run after tweaking values.
    public static class MistforgeGenerator
    {
        [MenuItem("Mistforge/Generate All (Data + Scenes)")]
        public static void GenerateAll()
        {
            ImportTMPEssentialsIfMissing();
            GenerateData();
            GenerateScenes();
            Debug.Log("Mistforge: generation complete. Open Assets/Scenes/Boot.unity and press Play.");
        }

        [MenuItem("Mistforge/Generate Data Assets")]
        public static void GenerateData()
        {
            EnsureFolders(
                "Assets/Resources",
                "Assets/ScriptableObjects/Arts",
                "Assets/ScriptableObjects/Characters",
                "Assets/ScriptableObjects/Enemies",
                "Assets/ScriptableObjects/Materials",
                "Assets/ScriptableObjects/Items",
                "Assets/ScriptableObjects/Rarities",
                "Assets/ScriptableObjects/Affixes",
                "Assets/ScriptableObjects/Sets",
                "Assets/ScriptableObjects/Recipes");

            // ---- Arts (§8.2) ----
            var kadeArts = new[]
            {
                Art("kade_twin_fang", "Twin Fang", "LRL", 1.8f, 12),
                Art("kade_skyfall_combo", "Skyfall Combo", "UDU", 1.8f, 12),
                Art("kade_ground_breaker", "Ground Breaker", "DDU", 2.05f, 16),
                Art("kade_iron_cascade", "Iron Cascade", "LLD", 2.05f, 16),
                Art("kade_cyclone_fist", "Cyclone Fist", "UDLR", 2.6f, 24),
                Art("kade_meteor_crash", "Meteor Crash", "DUDUD", 3.4f, 34),
            };
            var wrenArts = new[]
            {
                Art("wren_twin_gale", "Twin Gale", "RLR", 1.8f, 12),
                Art("wren_falling_star", "Falling Star", "UUD", 1.8f, 12),
                Art("wren_spiral_lance", "Spiral Lance", "DRU", 2.05f, 16),
                Art("wren_wind_piercer", "Wind Piercer", "LUL", 2.05f, 16),
                Art("wren_tempest_reach", "Tempest Reach", "RDRU", 2.6f, 24),
                Art("wren_heavens_spiral", "Heaven's Spiral", "LRLRU", 3.4f, 34),
            };

            // ---- Characters (§8.1) ----
            var kade = Asset<CharacterDef>("Assets/ScriptableObjects/Characters/Kade.asset", c =>
            {
                c.id = "kade"; c.displayName = "Kade"; c.role = "Brawler (Gauntlet)";
                c.baseHP = 52; c.baseAP = 60; c.baseATK = 9; c.baseDEF = 4; c.baseCritPct = 5;
                c.weaponType = WeaponType.Gauntlet;
                c.attackLeft = "Jab"; c.attackRight = "Cross"; c.attackUp = "Uppercut"; c.attackDown = "Slam";
                c.arts = kadeArts;
                c.placeholderColor = new Color(0.80f, 0.38f, 0.28f);
            });
            var wren = Asset<CharacterDef>("Assets/ScriptableObjects/Characters/Wren.asset", c =>
            {
                c.id = "wren"; c.displayName = "Wren"; c.role = "Skirmisher (Spear)";
                c.baseHP = 42; c.baseAP = 64; c.baseATK = 8; c.baseDEF = 3; c.baseCritPct = 8;
                c.weaponType = WeaponType.Spear;
                c.attackLeft = "Thrust"; c.attackRight = "Sweep"; c.attackUp = "Vault Strike"; c.attackDown = "Pin";
                c.arts = wrenArts;
                c.placeholderColor = new Color(0.30f, 0.62f, 0.58f);
            });

            // ---- Materials (§8.4) ----
            var dust = Material("dust", "Mist Dust", 0, new Color(0.75f, 0.82f, 0.9f));
            var fang = Material("fang", "Beast Fang", 0, new Color(0.9f, 0.85f, 0.7f));
            var spore = Material("spore", "Spore Sac", 0, new Color(0.7f, 0.6f, 0.75f));
            var ore = Material("ore", "Ore Chunk", 1, new Color(0.55f, 0.58f, 0.65f));
            var core = Material("core", "Gleaming Core", 2, new Color(1f, 0.8f, 0.3f));

            // ---- Enemies (§8.3) ----
            var wisp = Asset<EnemyDef>("Assets/ScriptableObjects/Enemies/MistWisp.asset", e =>
            {
                e.id = "wisp"; e.displayName = "Mist Wisp";
                e.maxHP = 16; e.atkMin = 3; e.atkMax = 5; e.goldMin = 3; e.goldMax = 6;
                e.rarityBoost = 0; e.spawnWeight = 30;
                e.loot = new[] { Loot(dust, 2), Loot(fang, 1) };
                e.placeholderColor = new Color(0.72f, 0.82f, 0.95f);
                e.framePixels = new Vector2Int(32, 32);
            });
            var hound = Asset<EnemyDef>("Assets/ScriptableObjects/Enemies/BrambleHound.asset", e =>
            {
                e.id = "hound"; e.displayName = "Bramble Hound";
                e.maxHP = 24; e.atkMin = 4; e.atkMax = 7; e.goldMin = 4; e.goldMax = 8;
                e.rarityBoost = 0; e.spawnWeight = 30;
                e.loot = new[] { Loot(fang, 2), Loot(spore, 1) };
                e.placeholderColor = new Color(0.52f, 0.36f, 0.24f);
                e.framePixels = new Vector2Int(32, 32);
            });
            var fungus = Asset<EnemyDef>("Assets/ScriptableObjects/Enemies/AshcapFungus.asset", e =>
            {
                e.id = "fungus"; e.displayName = "Ashcap Fungus";
                e.maxHP = 20; e.atkMin = 3; e.atkMax = 6; e.goldMin = 3; e.goldMax = 7;
                e.rarityBoost = 0; e.spawnWeight = 30;
                e.loot = new[] { Loot(spore, 1), Loot(dust, 1) };
                e.placeholderColor = new Color(0.62f, 0.52f, 0.56f);
                e.framePixels = new Vector2Int(32, 32);
            });
            var golem = Asset<EnemyDef>("Assets/ScriptableObjects/Enemies/StoneCub.asset", e =>
            {
                e.id = "golem"; e.displayName = "Stone Cub";
                e.maxHP = 38; e.atkMin = 6; e.atkMax = 9; e.goldMin = 8; e.goldMax = 14;
                e.rarityBoost = 1; e.spawnWeight = 10;
                e.loot = new[] { Loot(ore, 2), Loot(core, 1, 0.4f) };
                e.placeholderColor = new Color(0.5f, 0.54f, 0.6f);
                e.framePixels = new Vector2Int(40, 40);
            });

            // ---- Item bases (§8.5) ----
            var gauntlet = ItemBase("gauntlet", "Gauntlet", EquipSlot.Weapon, WeaponType.Gauntlet, StatType.ATK, 7, 11);
            var spear = ItemBase("spear", "War Spear", EquipSlot.Weapon, WeaponType.Spear, StatType.ATK, 6, 10);
            var hide = ItemBase("hide", "Hide Vest", EquipSlot.Armor, WeaponType.Gauntlet, StatType.DEF, 3, 5);
            var mail = ItemBase("mail", "Ring Mail", EquipSlot.Armor, WeaponType.Gauntlet, StatType.DEF, 5, 8);
            var charm = ItemBase("charm", "Mist Charm", EquipSlot.Accessory, WeaponType.Gauntlet, StatType.MaxAP, 4, 8);
            var band = ItemBase("band", "Ember Band", EquipSlot.Accessory, WeaponType.Gauntlet, StatType.CritPct, 2, 5);

            // ---- Rarities (§8.6). weightPerBoost implements the POC's
            // rollRarity(qualityBoost) shift toward Rare/Set. ----
            var common = Rarity("common", "Common", 52, 0, 1.0f, false, -22, new Color(0.85f, 0.85f, 0.85f));
            var magic = Rarity("magic", "Magic", 29, 1, 1.15f, false, 0, new Color(0.5f, 0.65f, 1f));
            var rare = Rarity("rare", "Rare", 15, 2, 1.35f, false, 14, new Color(0.95f, 0.82f, 0.3f));
            var set = Rarity("set", "Set", 4, 1, 1.25f, true, 8, new Color(1f, 0.5f, 0.2f));

            // ---- Affixes (§8.7) ----
            var affixes = new[]
            {
                Affix("atk", "Attack", "Honed", StatType.ATK, 2, 7),
                Affix("crit", "Crit", "Vicious", StatType.CritPct, 3, 9),
                Affix("apgain", "AP Gain", "Resonant", StatType.APGainPct, 5, 14),
                Affix("artdmg", "Art Power", "Ember-kissed", StatType.ArtDmgPct, 6, 18),
                Affix("maxap", "Max AP", "Deepwell", StatType.MaxAP, 3, 9),
                Affix("def", "Defense", "Warded", StatType.DEF, 2, 6),
            };

            // ---- Sets (§8.8) ----
            var emberwake = Asset<SetDef>("Assets/ScriptableObjects/Sets/Emberwake.asset", s =>
            {
                s.id = "emberwake"; s.displayName = "Emberwake";
                s.color = new Color(1f, 0.5f, 0.2f);
                s.bonuses = new[]
                {
                    new SetDef.SetBonus { pieces = 2, description = "+15% Art Power", stat = StatType.ArtDmgPct, value = 15 },
                    new SetDef.SetBonus { pieces = 3, description = "+20% AP Gain, attacks may ignite foes", stat = StatType.APGainPct, value = 20, grantsIgnite = true },
                };
            });

            // ---- Recipes (§8.9) ----
            var recipes = new[]
            {
                Recipe("Gauntlet", gauntlet, 8, Cost(fang, 2), Cost(dust, 1)),
                Recipe("WarSpear", spear, 8, Cost(fang, 2), Cost(dust, 1)),
                Recipe("HideVest", hide, 6, Cost(spore, 2), Cost(dust, 1)),
                Recipe("RingMail", mail, 14, Cost(ore, 2), Cost(spore, 1)),
                Recipe("MistCharm", charm, 6, Cost(dust, 2), Cost(spore, 1)),
                Recipe("EmberBand", band, 10, Cost(ore, 1), Cost(fang, 1)),
            };

            // ---- Database ----
            Asset<GameDatabase>("Assets/Resources/GameDatabase.asset", db =>
            {
                db.characters = new[] { kade, wren };
                db.enemies = new[] { wisp, hound, fungus, golem };
                db.materials = new[] { dust, fang, spore, ore, core };
                db.itemBases = new[] { gauntlet, spear, hide, mail, charm, band };
                db.rarities = new[] { common, magic, rare, set };
                db.affixes = affixes;
                db.sets = new[] { emberwake };
                db.recipes = recipes;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Mistforge: data assets generated.");
        }

        [MenuItem("Mistforge/Generate Scenes")]
        public static void GenerateScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolders("Assets/Scenes");

            CreateScene("Boot", () =>
            {
                new GameObject("Boot").AddComponent<BootLoader>();
            });
            CreateScene("Town_Rimwatch", () =>
            {
                var builder = new GameObject("Overworld").AddComponent<OverworldBuilder>();
                builder.map = MapKind.Town;
            });
            CreateScene("Field_Hushfields", () =>
            {
                var builder = new GameObject("Overworld").AddComponent<OverworldBuilder>();
                builder.map = MapKind.Field;
            });
            CreateScene("Battle", () =>
            {
                new GameObject("Battle").AddComponent<BattleManager>();
            });

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Boot.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Town_Rimwatch.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Field_Hushfields.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Battle.unity", true),
            };

            EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");
            Debug.Log("Mistforge: scenes generated and added to Build Settings.");
        }

        // ------------------------------------------------------------- helpers

        static void ImportTMPEssentialsIfMissing()
        {
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) return;
            const string package = "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage";
            if (File.Exists(Path.GetFullPath(package)))
            {
                AssetDatabase.ImportPackage(package, false);
                Debug.Log("Mistforge: imported TMP Essential Resources.");
            }
            else
            {
                Debug.LogWarning("Mistforge: TMP essentials not found — run Window > TextMeshPro > Import TMP Essential Resources.");
            }
        }

        static void CreateScene(string name, Action populate)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            populate();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/" + name + ".unity");
        }

        static void EnsureFolders(params string[] paths)
        {
            foreach (var path in paths)
            {
                var parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }

        static T Asset<T>(string path, Action<T> init) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            init(asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static ArtDef Art(string id, string name, string sequence, float mult, int apCost)
        {
            return Asset<ArtDef>("Assets/ScriptableObjects/Arts/" + id + ".asset", a =>
            {
                a.id = id;
                a.displayName = name;
                a.damageMultiplier = mult;
                a.apCost = apCost;
                a.sequence = new ComboDirection[sequence.Length];
                for (int i = 0; i < sequence.Length; i++)
                {
                    switch (sequence[i])
                    {
                        case 'L': a.sequence[i] = ComboDirection.Left; break;
                        case 'R': a.sequence[i] = ComboDirection.Right; break;
                        case 'U': a.sequence[i] = ComboDirection.Up; break;
                        default: a.sequence[i] = ComboDirection.Down; break;
                    }
                }
            });
        }

        static MaterialDef Material(string id, string name, int tier, Color color)
        {
            return Asset<MaterialDef>("Assets/ScriptableObjects/Materials/" + name.Replace(" ", "") + ".asset", m =>
            {
                m.id = id; m.displayName = name; m.qualityTier = tier; m.placeholderColor = color;
            });
        }

        static EnemyDef.LootEntry Loot(MaterialDef material, int quantity, float chance = 0.65f)
        {
            return new EnemyDef.LootEntry { material = material, quantity = quantity, dropChance = chance };
        }

        static ItemBaseDef ItemBase(string id, string name, EquipSlot slot, WeaponType weapon,
            StatType stat, int min, int max)
        {
            return Asset<ItemBaseDef>("Assets/ScriptableObjects/Items/" + name.Replace(" ", "") + ".asset", b =>
            {
                b.id = id; b.displayName = name; b.slot = slot; b.weaponType = weapon;
                b.primaryStat = stat; b.statMin = min; b.statMax = max;
            });
        }

        static RarityDef Rarity(string id, string name, int weight, int affixCount,
            float mult, bool isSet, int weightPerBoost, Color color)
        {
            return Asset<RarityDef>("Assets/ScriptableObjects/Rarities/" + name + ".asset", r =>
            {
                r.id = id; r.displayName = name; r.weight = weight; r.affixCount = affixCount;
                r.statMultiplier = mult; r.isSet = isSet; r.weightPerBoost = weightPerBoost;
                r.minWeight = 4; r.color = color;
            });
        }

        static AffixDef Affix(string id, string label, string prefix, StatType stat, int min, int max)
        {
            return Asset<AffixDef>("Assets/ScriptableObjects/Affixes/" + id + ".asset", a =>
            {
                a.id = id; a.label = label; a.prefix = prefix; a.stat = stat; a.min = min; a.max = max;
            });
        }

        static RecipeDef.MaterialCost Cost(MaterialDef material, int quantity)
        {
            return new RecipeDef.MaterialCost { material = material, quantity = quantity };
        }

        static RecipeDef Recipe(string assetName, ItemBaseDef result, int gold, params RecipeDef.MaterialCost[] costs)
        {
            return Asset<RecipeDef>("Assets/ScriptableObjects/Recipes/" + assetName + ".asset", r =>
            {
                r.result = result; r.goldCost = gold; r.costs = costs;
            });
        }
    }
}
