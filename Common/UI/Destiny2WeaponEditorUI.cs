using System;
using System.Collections.Generic;
using System.Linq;
using Destiny2.Common.Perks;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Destiny2.Common.UI
{
    public sealed class Destiny2WeaponEditorUI : UIState
    {
        private const float PanelWidth = 600f;
        private float currentPanelHeight = 600f;
        private const float LeftPadding = 16f;
        private const float RightPadding = 16f;
        private const float Column1Width = 120f;
        private const float SliderWidth = 240f;
        private const float InputWidth = 60f;
        private const float RowHeight = 36f;
        private const float ResetButtonWidth = 140f;
        private const float ResetButtonHeight = 32f;

        private UIPanel mainPanel;
        private readonly Dictionary<string, Destiny2UISlider> statSliders = new Dictionary<string, Destiny2UISlider>();
        private readonly Dictionary<string, Destiny2UITextInput> statInputs = new Dictionary<string, Destiny2UITextInput>();
        private readonly List<Destiny2UIDropdown> perkDropdowns = new List<Destiny2UIDropdown>();
        private Destiny2UIDropdown frameDropdown;
        private Destiny2UIDropdown elementDropdown;

        private static readonly string[] PerkSlotNames = new[] { "Barrel", "Magazine", "Major Perk 1", "Major Perk 2" };
        private static readonly PerkSlotType[] PerkSlotTypes = new[] { PerkSlotType.Barrel, PerkSlotType.Magazine, PerkSlotType.Major, PerkSlotType.Major };
        private static readonly Destiny2WeaponElement[] WeaponElements = (Destiny2WeaponElement[])Enum.GetValues(typeof(Destiny2WeaponElement));

        private UIText itemText;
        private UIText statsSourceText;
        private Item selectedItem;
        private bool _updatingFromLogic;

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            // Draw open dropdown on top
            Destiny2UIDropdown.OpenDropdown?.DrawOptions(spriteBatch);
        }

        public override void OnInitialize()
        {
            mainPanel = new UIPanel
            {
                Width = { Pixels = PanelWidth },
                Height = { Pixels = currentPanelHeight },
                HAlign = 0.5f,
                VAlign = 0.5f,
                BackgroundColor = Destiny2UIStyle.PanelBack,
                BorderColor = Destiny2UIStyle.PanelBorder
            };
            Append(mainPanel);

            UIText title = new UIText("WEAPON MODIFICATION PROTOCOL", 0.8f, true) { TextColor = Destiny2UIStyle.Gold };
            title.Left.Set(LeftPadding, 0f);
            title.Top.Set(12f, 0f);
            mainPanel.Append(title);

            itemText = new UIText("NO WEAPON DETECTED") { TextColor = Color.Gray };
            itemText.Left.Set(LeftPadding, 0f);
            itemText.Top.Set(42f, 0f);
            mainPanel.Append(itemText);

            statsSourceText = new UIText("SOURCE: UNKNOWN", 0.8f) { TextColor = Color.DarkGray };
            statsSourceText.Left.Set(LeftPadding, 0f);
            statsSourceText.Top.Set(62f, 0f);
            mainPanel.Append(statsSourceText);

            float rowTop = 90f;
            AddStatRow(mainPanel, "Range", "Range", rowTop, 0, 100, (v) => ApplyStat("Range", v));
            rowTop += RowHeight;
            AddStatRow(mainPanel, "Stability", "Stability", rowTop, 0, 100, (v) => ApplyStat("Stability", v));
            rowTop += RowHeight;
            AddStatRow(mainPanel, "Reload", "Reload", rowTop, 0, 100, (v) => ApplyStat("Reload", v));
            rowTop += RowHeight;
            AddStatRow(mainPanel, "RPM", "RPM", rowTop, 1, 1200, (v) => ApplyStat("RPM", (int)v));
            rowTop += RowHeight;
            AddStatRow(mainPanel, "Magazine", "Magazine", rowTop, 0, 200, (v) => ApplyStat("Magazine", (int)v));
            rowTop += RowHeight + 10f;

            AddFrameDropdown(mainPanel, rowTop);
            rowTop += RowHeight;
            AddElementDropdown(mainPanel, rowTop);
            rowTop += RowHeight;

            for (int i = 0; i < PerkSlotNames.Length; i++)
            {
                AddPerkDropdown(mainPanel, rowTop, i);
                rowTop += RowHeight;
            }

            rowTop += 20f; // Bottom spacing
            currentPanelHeight = rowTop + ResetButtonHeight + 20f;
            mainPanel.Height.Set(currentPanelHeight, 0f);

            var resetStats = CreateButton("RESET PARAMETERS", () =>
            {
                if (TryGetWeapon(out var w, out _)) w.ClearCustomStats();
            });
            resetStats.Left.Set(LeftPadding, 0f);
            resetStats.Top.Set(currentPanelHeight - 50f, 0f);
            resetStats.Width.Set(ResetButtonWidth, 0f);
            resetStats.Height.Set(ResetButtonHeight, 0f);
            mainPanel.Append(resetStats);

            var useHeld = CreateButton("SYNC HELD", () =>
            {
                Item held = Main.LocalPlayer.HeldItem;
                if (held?.ModItem is Destiny2WeaponItem) selectedItem = held;
            });
            useHeld.Left.Set(PanelWidth - RightPadding - ResetButtonWidth, 0f);
            useHeld.Top.Set(12f, 0f);
            useHeld.Width.Set(ResetButtonWidth, 0f);
            useHeld.Height.Set(ResetButtonHeight, 0f);
            mainPanel.Append(useHeld);
        }

        private void AddStatRow(UIPanel panel, string label, string key, float top, float min, float max, Action<float> onApply)
        {
            UIText title = new UIText(label) { TextColor = Destiny2UIStyle.TextBase };
            title.Left.Set(LeftPadding, 0f);
            title.Top.Set(top + 4, 0f);
            panel.Append(title);

            Destiny2UISlider slider = new Destiny2UISlider();
            slider.Left.Set(Column1Width, 0f);
            slider.Top.Set(top + 6, 0f);
            slider.Width.Set(SliderWidth, 0f);
            slider.OnValueChanged = (p) =>
            {
                if (_updatingFromLogic) return;
                float val = min + (max - min) * p;
                onApply(val);
                if (statInputs.TryGetValue(key, out var input)) input.Text = val.ToString("0");
            };
            panel.Append(slider);
            statSliders[key] = slider;

            Destiny2UITextInput input = new Destiny2UITextInput();
            input.Left.Set(Column1Width + SliderWidth + 10f, 0f);
            input.Top.Set(top + 2, 0f);
            input.Width.Set(InputWidth, 0f);
            input.OnTextChange = (s) =>
            {
                if (_updatingFromLogic) return;
                if (float.TryParse(s, out float val))
                {
                    val = MathHelper.Clamp(val, min, max);
                    onApply(val);
                    slider.Percentage = (val - min) / (max - min);
                }
            };
            panel.Append(input);
            statInputs[key] = input;
        }

        private void AddFrameDropdown(UIPanel panel, float top)
        {
            UIText title = new UIText("Archetype") { TextColor = Destiny2UIStyle.TextBase };
            title.Left.Set(LeftPadding, 0f);
            title.Top.Set(top + 4, 0f);
            panel.Append(title);

            frameDropdown = new Destiny2UIDropdown("Select Frame...");
            frameDropdown.Left.Set(Column1Width, 0f);
            frameDropdown.Top.Set(top, 0f);
            frameDropdown.Width.Set(SliderWidth + InputWidth + 10f, 0f);

            var frames = Destiny2PerkSystem.FramePerks;
            frameDropdown.Options = frames.Select(f => f.DisplayName).ToList();
            frameDropdown.OnSelected = (idx) =>
            {
                if (TryGetWeapon(out var w, out _)) w.ReplaceFramePerk(frames[idx].Key);
            };
            panel.Append(frameDropdown);
        }

        private void AddElementDropdown(UIPanel panel, float top)
        {
            UIText title = new UIText("Element") { TextColor = Destiny2UIStyle.TextBase };
            title.Left.Set(LeftPadding, 0f);
            title.Top.Set(top + 4, 0f);
            panel.Append(title);

            elementDropdown = new Destiny2UIDropdown("Select Element...");
            elementDropdown.Left.Set(Column1Width, 0f);
            elementDropdown.Top.Set(top, 0f);
            elementDropdown.Width.Set(SliderWidth + InputWidth + 10f, 0f);
            elementDropdown.Options = WeaponElements.Select(e => e.ToString()).ToList();
            elementDropdown.OnSelected = (idx) =>
            {
                if (TryGetWeapon(out var w, out _)) w.SetWeaponElement(WeaponElements[idx]);
            };
            panel.Append(elementDropdown);
        }

        private void AddPerkDropdown(UIPanel panel, float top, int slot)
        {
            UIText title = new UIText(PerkSlotNames[slot]) { TextColor = Destiny2UIStyle.TextBase };
            title.Left.Set(LeftPadding, 0f);
            title.Top.Set(top + 4, 0f);
            panel.Append(title);

            var dropdown = new Destiny2UIDropdown($"Select {PerkSlotNames[slot]}...");
            dropdown.Left.Set(Column1Width, 0f);
            dropdown.Top.Set(top, 0f);
            dropdown.Width.Set(SliderWidth + InputWidth + 10f, 0f);

            var perks = GetPerksForSlot(slot);
            dropdown.Options = perks.Select(p => p.DisplayName).ToList();
            dropdown.OnSelected = (idx) =>
            {
                if (TryGetWeapon(out var w, out _)) w.ReplacePerkAtSlot(slot, perks[idx].Key);
            };
            panel.Append(dropdown);
            perkDropdowns.Add(dropdown);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Block hotbar scrolling if a dropdown is open or hovered
            bool dropdownOpen = Destiny2UIDropdown.OpenDropdown != null;
            bool panelHovered = mainPanel.ContainsPoint(Main.MouseScreen);

            if (dropdownOpen || panelHovered)
            {
                Main.LocalPlayer.mouseInterface = true;
                // Zero out the scroll wheel delta to prevent hotbar switching
                // In some versions of tModLoader, this is enough to block it
                // We can also try setting delayUseItem
                Main.LocalPlayer.delayUseItem = true;
            }


            if (!TryGetWeapon(out var weapon, out var item))
            {
                itemText.SetText("NO WEAPON DETECTED");
                statsSourceText.SetText("SOURCE: UNKNOWN");
                return;
            }

            itemText.SetText($"PROTOCOL: {item.Name.ToUpper()}");
            statsSourceText.SetText(weapon.HasCustomStats ? "SOURCE: MODIFIED DATA" : "SOURCE: FACTORY DEFAULTS");

            _updatingFromLogic = true;
            var stats = weapon.GetEditableStats();
            SyncStat("Range", stats.Range, 0, 100);
            SyncStat("Stability", stats.Stability, 0, 100);
            SyncStat("Reload", stats.ReloadSpeed, 0, 100);
            SyncStat("RPM", stats.RoundsPerMinute, 1, 1200);
            SyncStat("Magazine", stats.Magazine, 0, 200);

            if (!string.IsNullOrWhiteSpace(weapon.FramePerkKey) && Destiny2PerkSystem.TryGet(weapon.FramePerkKey, out var frame))
                frameDropdown.SetSelected(frame.DisplayName);

            elementDropdown.SetSelected(weapon.WeaponElement.ToString());

            // Refresh options if they were initially empty (due to early initialization)
            if (frameDropdown.Options.Count == 0)
                frameDropdown.Options = Destiny2PerkSystem.FramePerks.Select(f => f.DisplayName).ToList();
            if (elementDropdown.Options.Count == 0)
                elementDropdown.Options = WeaponElements.Select(e => e.ToString()).ToList();

            for (int i = 0; i < perkDropdowns.Count; i++)
            {
                if (perkDropdowns[i].Options.Count == 0)
                {
                    var perks = GetPerksForSlot(i);
                    perkDropdowns[i].Options = perks.Select(p => p.DisplayName).ToList();
                }

                if (weapon.PerkKeys.Count > i && Destiny2PerkSystem.TryGet(weapon.PerkKeys[i], out var perk))
                    perkDropdowns[i].SetSelected(perk.DisplayName);
                else
                    perkDropdowns[i].SetSelected("None");
            }
            _updatingFromLogic = false;
        }

        private void SyncStat(string key, float val, float min, float max)
        {
            if (statSliders.TryGetValue(key, out var s)) s.Percentage = (val - min) / (max - min);
            if (statInputs.TryGetValue(key, out var i)) i.Text = val.ToString("0");
        }

        private void ApplyStat(string key, float val)
        {
            if (!TryGetWeapon(out var weapon, out _)) return;
            var stats = weapon.GetEditableStats();
            switch (key)
            {
                case "Range": stats.Range = val; break;
                case "Stability": stats.Stability = val; break;
                case "Reload": stats.ReloadSpeed = val; break;
                case "RPM": stats.RoundsPerMinute = (int)val; break;
                case "Magazine": stats.Magazine = (int)val; break;
            }
            weapon.ApplyCustomStats(stats);
        }

        private bool TryGetWeapon(out Destiny2WeaponItem weapon, out Item item)
        {
            Item cand = selectedItem ?? Main.LocalPlayer.HeldItem;
            if (cand?.ModItem is Destiny2WeaponItem w) { weapon = w; item = cand; return true; }
            weapon = null; item = null; return false;
        }

        private static UITextPanel<string> CreateButton(string text, Action onClick)
        {
            var b = new UITextPanel<string>(text) { BackgroundColor = Color.Black * 0.4f };
            b.OnMouseOver += (_, _) => b.BackgroundColor = Destiny2UIStyle.ButtonHover;
            b.OnMouseOut += (_, _) => b.BackgroundColor = Color.Black * 0.4f;
            b.OnLeftClick += (_, _) => onClick?.Invoke();
            return b;
        }
        private static List<Destiny2Perk> GetPerksForSlot(int slotIndex)
        {
            var type = slotIndex < PerkSlotTypes.Length ? PerkSlotTypes[slotIndex] : PerkSlotType.Major;
            return Destiny2PerkSystem.Perks.Where(p => p.SlotType == type).ToList();
        }
    }
}
