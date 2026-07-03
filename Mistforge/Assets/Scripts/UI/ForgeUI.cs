using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mistforge
{
    /// The Forge as an in-town overlay Canvas (§6.4 recommendation — no scene
    /// load). Materials panel, recipe cards, equipment paperdoll, and the live
    /// set-bonus readout (§7). Entire panel is rebuilt on each state change;
    /// cheap at vertical-slice scale.
    public class ForgeUI : MonoBehaviour
    {
        public static bool AnyOpen { get; private set; }

        GameManager gm;
        Canvas canvas;
        RectTransform content;
        string selectedCharId = "kade";
        bool infuseWithCore;
        string lastCraftMessage = "";

        public void Open()
        {
            gm = GameManager.Ensure();
            if (canvas == null) Build();
            canvas.gameObject.SetActive(true);
            AnyOpen = true;
            Refresh();
        }

        public void Close()
        {
            if (canvas != null) canvas.gameObject.SetActive(false);
            AnyOpen = false;
        }

        void OnDestroy() { AnyOpen = false; }

        void Update()
        {
            if (AnyOpen && InputHelper.CancelPressed()) Close();
        }

        void Build()
        {
            canvas = UIFactory.CreateCanvas("ForgeCanvas", 200);
            var backdrop = UIFactory.Panel(canvas.transform, "Backdrop", new Color(0.06f, 0.07f, 0.1f, 0.97f));
            UIFactory.Stretch(backdrop);
            content = backdrop;
            canvas.gameObject.SetActive(false);
        }

        void Refresh()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            BuildHeader();
            BuildMaterialsColumn();
            BuildRecipeColumn();
            BuildCharacterColumn();
        }

        // ---------------------------------------------------------------- sections

        void BuildHeader()
        {
            var title = UIFactory.Text(content, "The Forge", 30, TextAlignmentOptions.Left,
                new Color(0.95f, 0.7f, 0.3f));
            UIFactory.Place(title.rectTransform, new Vector2(0.02f, 0.92f), new Vector2(0.4f, 1f),
                Vector2.zero, Vector2.zero);

            var toast = UIFactory.Text(content, lastCraftMessage, 18, TextAlignmentOptions.Center,
                new Color(0.8f, 0.9f, 0.6f));
            UIFactory.Place(toast.rectTransform, new Vector2(0.3f, 0.92f), new Vector2(0.85f, 1f),
                Vector2.zero, Vector2.zero);

            var close = UIFactory.Button(content, "X  Close", Close, new Color(0.4f, 0.2f, 0.2f), 17f);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.9f, 0.93f), new Vector2(0.99f, 0.99f),
                Vector2.zero, Vector2.zero);
        }

        void BuildMaterialsColumn()
        {
            var panel = UIFactory.Panel(content, "Materials", new Color(0.1f, 0.11f, 0.15f, 0.9f));
            UIFactory.Place(panel, new Vector2(0.01f, 0.02f), new Vector2(0.23f, 0.9f),
                Vector2.zero, Vector2.zero);

            var lines = new List<string> { "<b>Gold: " + gm.gold + "</b>", "" };
            foreach (var mat in gm.db.materials)
                lines.Add(mat.displayName + "  x" + gm.MaterialCount(mat.id) +
                    (mat.qualityTier > 0 ? "  <size=75%>(T" + mat.qualityTier + ")</size>" : ""));

            var text = UIFactory.Text(panel, string.Join("\n", lines), 18, TextAlignmentOptions.TopLeft);
            UIFactory.Place(text.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(12f, 10f), new Vector2(-8f, -10f));

            // Optional Gleaming Core infusion: extra rarity-quality boost (§8.6).
            bool hasCore = gm.MaterialCount("core") > 0;
            var infuse = UIFactory.Button(panel,
                (infuseWithCore ? "[x] " : "[ ] ") + "Infuse Gleaming Core\n<size=70%>+2 quality on next craft</size>",
                () => { if (hasCore) { infuseWithCore = !infuseWithCore; Refresh(); } },
                infuseWithCore ? new Color(0.5f, 0.35f, 0.15f) : new Color(0.2f, 0.22f, 0.28f), 14f);
            UIFactory.Place((RectTransform)infuse.transform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.16f),
                Vector2.zero, Vector2.zero);
            infuse.interactable = hasCore;
        }

        void BuildRecipeColumn()
        {
            var panel = UIFactory.Panel(content, "Recipes", new Color(0.1f, 0.11f, 0.15f, 0.9f));
            UIFactory.Place(panel, new Vector2(0.24f, 0.02f), new Vector2(0.55f, 0.9f),
                Vector2.zero, Vector2.zero);

            var header = UIFactory.Text(panel, "Recipes", 20, TextAlignmentOptions.Center,
                new Color(0.85f, 0.85f, 0.9f));
            UIFactory.Place(header.rectTransform, new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);

            var recipes = gm.db.recipes;
            for (int i = 0; i < recipes.Length; i++)
            {
                var recipe = recipes[i];
                float top = 0.93f - i * 0.15f;
                var card = UIFactory.Panel(panel, "Card", new Color(0.15f, 0.16f, 0.21f));
                UIFactory.Place(card, new Vector2(0.03f, top - 0.14f), new Vector2(0.97f, top),
                    Vector2.zero, Vector2.zero);

                var costParts = new List<string>();
                foreach (var c in recipe.costs)
                    costParts.Add(c.material.displayName + " x" + c.quantity);
                costParts.Add(recipe.goldCost + "g");

                bool affordable = CanAfford(recipe);
                var label = UIFactory.Text(card,
                    "<b>" + recipe.result.displayName + "</b>  <size=75%>(" + SlotLabel(recipe.result) + ")</size>\n" +
                    "<size=78%>" + string.Join(", ", costParts) + "</size>",
                    16, TextAlignmentOptions.Left,
                    affordable ? Color.white : new Color(0.6f, 0.6f, 0.65f));
                UIFactory.Place(label.rectTransform, new Vector2(0.03f, 0f), new Vector2(0.72f, 1f),
                    Vector2.zero, Vector2.zero);

                var forge = UIFactory.Button(card, "Forge", () => Craft(recipe),
                    affordable ? new Color(0.6f, 0.4f, 0.15f) : new Color(0.25f, 0.25f, 0.3f), 16f);
                UIFactory.Place((RectTransform)forge.transform, new Vector2(0.75f, 0.15f), new Vector2(0.97f, 0.85f),
                    Vector2.zero, Vector2.zero);
                forge.interactable = affordable;
            }
        }

        void BuildCharacterColumn()
        {
            var panel = UIFactory.Panel(content, "Characters", new Color(0.1f, 0.11f, 0.15f, 0.9f));
            UIFactory.Place(panel, new Vector2(0.56f, 0.02f), new Vector2(0.99f, 0.9f),
                Vector2.zero, Vector2.zero);

            // Character tabs.
            for (int i = 0; i < gm.party.Count; i++)
            {
                var member = gm.party[i];
                var character = gm.db.Character(member.charId);
                bool selected = member.charId == selectedCharId;
                string id = member.charId;
                var tab = UIFactory.Button(panel, character.displayName,
                    () => { selectedCharId = id; Refresh(); },
                    selected ? new Color(0.5f, 0.35f, 0.15f) : new Color(0.2f, 0.22f, 0.28f), 17f);
                UIFactory.Place((RectTransform)tab.transform,
                    new Vector2(0.03f + i * 0.25f, 0.92f), new Vector2(0.26f + i * 0.25f, 0.99f),
                    Vector2.zero, Vector2.zero);
            }

            var selectedMember = gm.Member(selectedCharId);
            var stats = gm.StatsFor(selectedMember);
            var statLine = UIFactory.Text(panel,
                "HP " + stats.maxHP + "  AP " + stats.maxAP + "  ATK " + stats.atk + "  DEF " + stats.def +
                "  Crit " + stats.critPct + "%\nAP Gain +" + stats.apGainPct + "%   Art Power +" + stats.artDmgPct + "%",
                15, TextAlignmentOptions.TopLeft);
            UIFactory.Place(statLine.rectTransform, new Vector2(0.03f, 0.8f), new Vector2(0.97f, 0.91f),
                Vector2.zero, Vector2.zero);

            // Paperdoll: 3 slots, click to unequip (§7).
            string[] slotNames = { "Weapon", "Armor", "Accessory" };
            for (int slot = 0; slot < 3; slot++)
            {
                var item = selectedMember.equipped[slot];
                int slotIndex = slot;
                var button = UIFactory.Button(panel,
                    "<size=70%>" + slotNames[slot] + "</size>\n" + (item != null ? item.displayName : "—"),
                    () => Unequip(selectedMember, slotIndex),
                    item != null ? Dim(RarityColorOf(item), 0.55f) : new Color(0.16f, 0.17f, 0.22f), 14f);
                UIFactory.Place((RectTransform)button.transform,
                    new Vector2(0.03f + slot * 0.325f, 0.66f), new Vector2(0.34f + slot * 0.325f, 0.79f),
                    Vector2.zero, Vector2.zero);
            }

            // Live set-bonus readout — the payoff panel (§7).
            var bonuses = StatsCalculator.ActiveSetBonuses(gm.db, selectedMember.equipped);
            var bonusText = UIFactory.Text(panel,
                bonuses.Count == 0 ? "<i>No set pieces equipped</i>" : string.Join("\n", bonuses),
                14, TextAlignmentOptions.TopLeft, new Color(1f, 0.72f, 0.35f));
            UIFactory.Place(bonusText.rectTransform, new Vector2(0.03f, 0.53f), new Vector2(0.97f, 0.65f),
                Vector2.zero, Vector2.zero);

            // Inventory: click an item to equip it on the selected character.
            var invHeader = UIFactory.Text(panel, "Inventory (click to equip)", 15,
                TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.85f));
            UIFactory.Place(invHeader.rectTransform, new Vector2(0.03f, 0.46f), new Vector2(0.97f, 0.52f),
                Vector2.zero, Vector2.zero);

            const int maxShown = 7;
            for (int i = 0; i < gm.inventory.Count && i < maxShown; i++)
            {
                var item = gm.inventory[i];
                float top = 0.45f - i * 0.062f;
                var row = UIFactory.Button(panel,
                    item.displayName + "  <size=72%>" + item.Describe(gm.db) + "</size>",
                    () => Equip(selectedMember, item), Dim(RarityColorOf(item), 0.4f), 13f);
                UIFactory.Place((RectTransform)row.transform, new Vector2(0.03f, top - 0.058f), new Vector2(0.97f, top),
                    Vector2.zero, Vector2.zero);
            }
            if (gm.inventory.Count > maxShown)
            {
                var more = UIFactory.Text(panel, "(+" + (gm.inventory.Count - maxShown) + " more)", 13,
                    TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 0.65f));
                UIFactory.Place(more.rectTransform, new Vector2(0.03f, 0.0f), new Vector2(0.97f, 0.04f),
                    Vector2.zero, Vector2.zero);
            }
        }

        // ---------------------------------------------------------------- actions

        bool CanAfford(RecipeDef recipe)
        {
            if (gm.gold < recipe.goldCost) return false;
            foreach (var c in recipe.costs)
                if (gm.MaterialCount(c.material.id) < c.quantity) return false;
            return true;
        }

        void Craft(RecipeDef recipe)
        {
            if (!CanAfford(recipe)) return;
            gm.gold -= recipe.goldCost;
            foreach (var c in recipe.costs)
                gm.AddMaterial(c.material.id, -c.quantity);

            int boost = recipe.BaseQualityBoost();
            if (infuseWithCore && gm.MaterialCount("core") > 0)
            {
                gm.AddMaterial("core", -1);
                boost += 2;
                infuseWithCore = false;
            }

            var item = ItemGenerator.Generate(gm.db, recipe.result, boost);
            gm.inventory.Add(item);
            var rarity = gm.db.Rarity(item.rarityId);
            lastCraftMessage = "Forged: " + item.displayName +
                (rarity != null ? " (" + rarity.displayName + ")" : "");
            Refresh();
        }

        void Equip(GameManager.PartyMemberState member, ItemInstance item)
        {
            // Weapons are locked to the matching weapon type (§8.5).
            var character = gm.db.Character(member.charId);
            if (item.slot == EquipSlot.Weapon && item.weaponType != character.weaponType)
            {
                lastCraftMessage = character.displayName + " can't wield a " + item.displayName + ".";
                Refresh();
                return;
            }

            int slot = (int)item.slot;
            gm.inventory.Remove(item);
            if (member.equipped[slot] != null)
                gm.inventory.Add(member.equipped[slot]);
            member.equipped[slot] = item;

            // Re-clamp HP against the (possibly changed) max.
            member.currentHP = Mathf.Min(member.currentHP, gm.StatsFor(member).maxHP);
            lastCraftMessage = character.displayName + " equips " + item.displayName + ".";
            Refresh();
        }

        void Unequip(GameManager.PartyMemberState member, int slot)
        {
            if (member.equipped[slot] == null) return;
            gm.inventory.Add(member.equipped[slot]);
            member.equipped[slot] = null;
            member.currentHP = Mathf.Min(member.currentHP, gm.StatsFor(member).maxHP);
            Refresh();
        }

        static Color Dim(Color c, float f)
        {
            return new Color(c.r * f, c.g * f, c.b * f, 1f);
        }

        Color RarityColorOf(ItemInstance item)
        {
            var rarity = gm.db.Rarity(item.rarityId);
            return rarity != null ? rarity.color : Color.white;
        }

        static string SlotLabel(ItemBaseDef def)
        {
            if (def.slot == EquipSlot.Weapon)
                return def.weaponType == WeaponType.Gauntlet ? "Weapon — Kade" : "Weapon — Wren";
            return def.slot.ToString();
        }
    }
}
