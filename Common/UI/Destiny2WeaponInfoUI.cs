using System;
using System.Collections.Generic;
using Destiny2.Common.Perks;
using Destiny2.Common.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace Destiny2.Common.UI
{
	public sealed class Destiny2WeaponInfoUI : UIState
	{
		private const float PanelWidth = 860f;
		private const float PanelHeight = 480f;
		private const float LeftPadding = 18f;
		private const float RightPadding = 18f;
		private const float TopPadding = 12f;
		private const float BottomPadding = 14f;
		private const float PerkColumnTop = 72f;
		private const float PerkPanelWidth = 240f;
		private const float PerkPanelPadding = 8f;
		private const float ImageWidth = 320f;
		private const float ImageHeight = 210f;
		private const float StatsPanelWidth = 260f;
		private const float StatsRowHeight = 20f;

		private static readonly string[] PerkRowLabels =
		{
			"Frame",
			"Barrel",
			"Magazine",
			"Major Perk",
			"Major Perk",
			"Mod Slot"
		};

		private static readonly string[] StatRowLabels =
		{
			"Range",
			"Stability",
			"Reload",
			"RPM",
			"Magazine"
		};

		private UIText itemNameText;
		private DamageStatRow damageRow;
		private readonly List<UIText> statTexts = new List<UIText>();
		private readonly List<PerkEntryElement> perkEntries = new List<PerkEntryElement>();
		private WeaponPreviewElement previewElement;

		public override void OnInitialize()
		{
			UIPanel panel = new UIPanel
			{
				Width = { Pixels = PanelWidth },
				Height = { Pixels = PanelHeight },
				HAlign = 0.5f,
				VAlign = 0.5f,
				BackgroundColor = new Color(30, 30, 30) * 0.95f,
				BorderColor = new Color(120, 106, 72)
			};
			Append(panel);

			itemNameText = new UIText("Weapon: --", 1.15f);
			itemNameText.Left.Set(LeftPadding, 0f);
			itemNameText.Top.Set(TopPadding, 0f);
			panel.Append(itemNameText);

			SeparatorElement headerLine = new SeparatorElement(Color.White * 0.15f);
			headerLine.Left.Set(LeftPadding, 0f);
			headerLine.Top.Set(TopPadding + 26f, 0f);
			headerLine.Width.Set(PanelWidth - LeftPadding - RightPadding, 0f);
			headerLine.Height.Set(2f, 0f);
			panel.Append(headerLine);

			float perkRowHeight = PerkEntryElement.GetPreferredHeight(PerkPanelWidth - (PerkPanelPadding * 2f));
			float perkPanelHeight = (perkRowHeight * PerkRowLabels.Length) + (PerkPanelPadding * 2f);
			UIPanel perkPanel = new UIPanel
			{
				Width = { Pixels = PerkPanelWidth },
				Height = { Pixels = perkPanelHeight },
				Left = { Pixels = LeftPadding },
				Top = { Pixels = PerkColumnTop },
				BackgroundColor = new Color(22, 22, 22) * 0.9f,
				BorderColor = new Color(90, 80, 55)
			};
			panel.Append(perkPanel);

			float perkTop = PerkPanelPadding;
			foreach (string label in PerkRowLabels)
			{
				PerkEntryElement entry = new PerkEntryElement(label, PerkPanelWidth - (PerkPanelPadding * 2f), perkRowHeight);
				entry.Left.Set(PerkPanelPadding, 0f);
				entry.Top.Set(perkTop, 0f);
				perkPanel.Append(entry);
				perkEntries.Add(entry);
				perkTop += perkRowHeight;
			}

			previewElement = new WeaponPreviewElement
			{
				Width = { Pixels = ImageWidth },
				Height = { Pixels = ImageHeight }
			};
			previewElement.Left.Set((PanelWidth - ImageWidth) * 0.5f, 0f);
			previewElement.Top.Set(PerkColumnTop, 0f);
			panel.Append(previewElement);

			float statsPanelHeight = (StatRowLabels.Length * StatsRowHeight) + 50f;
			float statsPanelTop = PanelHeight - BottomPadding - statsPanelHeight;
			UIPanel statsPanel = new UIPanel
			{
				Width = { Pixels = StatsPanelWidth },
				Height = { Pixels = statsPanelHeight },
				Left = { Pixels = PanelWidth - RightPadding - StatsPanelWidth },
				Top = { Pixels = statsPanelTop },
				BackgroundColor = new Color(22, 22, 22) * 0.9f,
				BorderColor = new Color(90, 80, 55)
			};
			panel.Append(statsPanel);

			UIText statsHeaderText = new UIText("Stats", 0.9f);
			statsHeaderText.Left.Set(10f, 0f);
			statsHeaderText.Top.Set(6f, 0f);
			statsPanel.Append(statsHeaderText);

			damageRow = new DamageStatRow
			{
				Width = { Pixels = StatsPanelWidth - 20f },
				Height = { Pixels = StatsRowHeight }
			};
			damageRow.Left.Set(10f, 0f);
			damageRow.Top.Set(28f, 0f);
			statsPanel.Append(damageRow);

			float statTop = 28f + StatsRowHeight;
			foreach (string label in StatRowLabels)
			{
				UIText statText = new UIText($"{label}: --");
				statText.Left.Set(12f, 0f);
				statText.Top.Set(statTop, 0f);
				statsPanel.Append(statText);
				statTexts.Add(statText);
				statTop += StatsRowHeight;
			}
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			Item item = Main.LocalPlayer?.HeldItem;
			if (item?.ModItem is Destiny2WeaponItem weapon)
			{
				itemNameText.SetText(item.Name);
				previewElement.SetItem(item);

				Destiny2WeaponStats stats = weapon.BaseStats;
				string elementName = weapon.WeaponElement.ToString();
				damageRow.SetValues($"{item.damage} {elementName} Damage", weapon.WeaponElement);
				statTexts[0].SetText($"Range: {stats.Range:0}");
				statTexts[1].SetText($"Stability: {stats.Stability:0}");
				statTexts[2].SetText($"Reload: {stats.ReloadSpeed:0}");
				statTexts[3].SetText($"RPM: {stats.RoundsPerMinute}");
				statTexts[4].SetText($"Magazine: {stats.Magazine}");

				SetPerkEntry(0, weapon.FramePerkKey);
				SetPerkEntry(1, weapon.PerkKeys, 0);
				SetPerkEntry(2, weapon.PerkKeys, 1);
				SetPerkEntry(3, weapon.PerkKeys, 2);
				SetPerkEntry(4, weapon.PerkKeys, 3);
				perkEntries[5].SetEmpty();
				return;
			}

			itemNameText.SetText("Weapon: --");
			previewElement.ClearItem();
			damageRow.SetValues("-- Damage", Destiny2WeaponElement.Kinetic);
			for (int i = 0; i < statTexts.Count; i++)
				statTexts[i].SetText($"{StatRowLabels[i]}: --");
			for (int i = 0; i < perkEntries.Count; i++)
				perkEntries[i].SetEmpty();
		}

		private void SetPerkEntry(int entryIndex, string perkKey)
		{
			if (entryIndex < 0 || entryIndex >= perkEntries.Count)
				return;

			if (!string.IsNullOrWhiteSpace(perkKey) && Destiny2PerkSystem.TryGet(perkKey, out Destiny2Perk perk))
				perkEntries[entryIndex].SetPerk(perk);
			else
				perkEntries[entryIndex].SetEmpty();
		}

		private void SetPerkEntry(int entryIndex, IReadOnlyList<string> perkKeys, int slotIndex)
		{
			string perkKey = perkKeys.Count > slotIndex ? perkKeys[slotIndex] : null;
			SetPerkEntry(entryIndex, perkKey);
		}

		private static Color GetElementColor(Destiny2WeaponElement element)
		{
			return element switch
			{
				Destiny2WeaponElement.Void => new Color(196, 0, 240),
				Destiny2WeaponElement.Strand => new Color(55, 218, 100),
				Destiny2WeaponElement.Stasis => new Color(51, 91, 196),
				Destiny2WeaponElement.Solar => new Color(236, 85, 0),
				Destiny2WeaponElement.Arc => new Color(7, 208, 255),
				Destiny2WeaponElement.Kinetic => new Color(255, 248, 163),
				_ => new Color(255, 248, 163)
			};
		}

		private sealed class PerkEntryElement : UIElement
		{
			private const float IconSize = 22f;
			private const float IconPadding = 6f;
			private const float NameScale = 0.78f;
			private const float DescScale = 0.6f;
			private const float TextGap = 6f;
			private const int MaxDescLines = 2;
			private readonly string label;
			private readonly float rowWidth;
			private readonly float textLeft;
			private readonly float descTop;
			private readonly UIText nameText;
			private readonly UIText descText;
			private Texture2D icon;

			public PerkEntryElement(string label, float rowWidth, float rowHeight)
			{
				this.label = label;
				this.rowWidth = rowWidth;
				textLeft = IconPadding + IconSize + TextGap;

				Width.Set(rowWidth, 0f);
				Height.Set(rowHeight, 0f);

				DynamicSpriteFont font = FontAssets.MouseText.Value;
				float nameHeight = font.LineSpacing * NameScale;
				float nameTop = IconPadding;
				descTop = Math.Max(nameTop + nameHeight + 2f, IconPadding + IconSize + 2f);

				nameText = new UIText($"{label}: --", NameScale);
				nameText.Left.Set(textLeft, 0f);
				nameText.Top.Set(nameTop, 0f);
				Append(nameText);

				descText = new UIText(string.Empty, DescScale);
				descText.Left.Set(textLeft, 0f);
				descText.Top.Set(descTop, 0f);
				Append(descText);
			}

			public static float GetPreferredHeight(float rowWidth)
			{
				DynamicSpriteFont font = FontAssets.MouseText.Value;
				float nameHeight = font.LineSpacing * NameScale;
				float nameTop = IconPadding;
				float descTop = Math.Max(nameTop + nameHeight + 2f, IconPadding + IconSize + 2f);
				float descHeight = font.LineSpacing * DescScale * MaxDescLines;
				float textHeight = descTop + descHeight + IconPadding;
				float iconHeight = IconPadding * 2f + IconSize;
				return Math.Max(textHeight, iconHeight);
			}

			public void SetPerk(Destiny2Perk perk)
			{
				if (perk == null)
				{
					SetEmpty();
					return;
				}

				nameText.SetText($"{label}: {perk.DisplayName}");
				string description = perk.Description ?? string.Empty;
				float descWidth = Math.Max(0f, rowWidth - textLeft - IconPadding);
				descText.SetText(WrapText(description, descWidth, MaxDescLines, DescScale));
				if (!string.IsNullOrWhiteSpace(perk.IconTexture))
					icon = ModContent.Request<Texture2D>(perk.IconTexture).Value;
				else
					icon = null;
			}

			public void SetEmpty()
			{
				nameText.SetText($"{label}: --");
				descText.SetText(string.Empty);
				icon = null;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				Rectangle bounds = GetDimensions().ToRectangle();
				Rectangle iconRect = new Rectangle(bounds.X + (int)IconPadding, bounds.Y + (int)IconPadding, (int)IconSize, (int)IconSize);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, iconRect, Color.Black * 0.35f);
				if (icon != null)
					spriteBatch.Draw(icon, iconRect, Color.White);
			}
		}

		private sealed class DamageStatRow : UIElement
		{
			private const float IconSize = 18f;
			private const float IconPadding = 6f;
			private readonly UIText text;
			private Texture2D icon;

			public DamageStatRow()
			{
				text = new UIText("-- Damage");
				text.Left.Set(IconSize + (IconPadding * 2f), 0f);
				text.Top.Set(0f, 0f);
				Append(text);
			}

			public void SetValues(string value, Destiny2WeaponElement element)
			{
				text.SetText(value);
				text.TextColor = GetElementColor(element);
				string iconTexture = element.GetIconTexture();
				icon = ModContent.Request<Texture2D>(iconTexture).Value;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				Rectangle bounds = GetDimensions().ToRectangle();
				Rectangle iconRect = new Rectangle(bounds.X, bounds.Y, (int)IconSize, (int)IconSize);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, iconRect, Color.Black * 0.35f);
				if (icon != null)
					spriteBatch.Draw(icon, iconRect, Color.White);
			}
		}

		private sealed class WeaponPreviewElement : UIElement
		{
			private const float Padding = 10f;
			private Item item;

			public WeaponPreviewElement()
			{
				item = new Item();
				item.TurnToAir();
			}

			public void SetItem(Item source)
			{
				if (source == null || source.IsAir)
				{
					item.TurnToAir();
					return;
				}

				item = source.Clone();
			}

			public void ClearItem()
			{
				item.TurnToAir();
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);

				Rectangle bounds = GetDimensions().ToRectangle();
				Rectangle shadow = bounds;
				shadow.Offset(3, 3);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, shadow, Color.Black * 0.35f);

				spriteBatch.Draw(TextureAssets.MagicPixel.Value, bounds, new Color(16, 16, 16) * 0.9f);
				Rectangle top = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height / 2);
				Rectangle bottom = new Rectangle(bounds.X, bounds.Y + bounds.Height / 2, bounds.Width, bounds.Height - (bounds.Height / 2));
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, top, new Color(32, 32, 32) * 0.35f);
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, bottom, new Color(6, 6, 6) * 0.35f);
				Destiny2WeaponInfoUI.DrawBorder(spriteBatch, bounds, new Color(120, 106, 72) * 0.6f);

				if (item == null || item.IsAir)
					return;

				Main.instance.LoadItem(item.type);
				Texture2D texture = TextureAssets.Item[item.type].Value;
				Rectangle source = texture.Bounds;

				float maxWidth = bounds.Width - (Padding * 2f);
				float maxHeight = bounds.Height - (Padding * 2f);
				float scale = Math.Min(maxWidth / source.Width, maxHeight / source.Height);
				scale = MathHelper.Clamp(scale, 0.1f, 4f) * 0.92f;

				Vector2 center = bounds.Center.ToVector2();
				Vector2 origin = source.Size() * 0.5f;
				spriteBatch.Draw(texture, center, source, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
			}
		}

		private sealed class SeparatorElement : UIElement
		{
			private readonly Color color;

			public SeparatorElement(Color color)
			{
				this.color = color;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch)
			{
				base.DrawSelf(spriteBatch);
				Rectangle bounds = GetDimensions().ToRectangle();
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, bounds, color);
			}
		}

		private static void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color color)
		{
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), color);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), color);
		}

		private static string WrapText(string text, float maxWidth, int maxLines, float scale)
		{
			if (string.IsNullOrWhiteSpace(text))
				return string.Empty;

			DynamicSpriteFont font = FontAssets.MouseText.Value;
			string[] paragraphs = text.Replace("\r", string.Empty).Split('\n');
			List<string> lines = new List<string>();

			foreach (string paragraph in paragraphs)
			{
				if (string.IsNullOrWhiteSpace(paragraph))
				{
					lines.Add(string.Empty);
					continue;
				}

				string[] words = paragraph.Split(' ');
				string line = string.Empty;
				foreach (string word in words)
				{
					string testLine = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
					float width = font.MeasureString(testLine).X * scale;
					if (width <= maxWidth)
					{
						line = testLine;
						continue;
					}

					if (!string.IsNullOrEmpty(line))
						lines.Add(line);
					line = word;
				}

				if (!string.IsNullOrEmpty(line))
					lines.Add(line);
			}

			if (maxLines > 0 && lines.Count > maxLines)
			{
				lines = lines.GetRange(0, maxLines);
				if (!lines[^1].EndsWith("...", StringComparison.Ordinal))
					lines[^1] = lines[^1].TrimEnd() + "...";
			}

			return string.Join("\n", lines);
		}
	}
}
