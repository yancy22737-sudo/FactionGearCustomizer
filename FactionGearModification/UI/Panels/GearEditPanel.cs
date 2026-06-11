using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearModification.UI;
using FactionGearCustomizer.UI;
using FactionGearCustomizer.UI.Dialogs;
using FactionGearCustomizer.Compat;

namespace FactionGearCustomizer.UI.Panels
{
    public static class GearEditPanel
    {
        private static System.Reflection.FieldInfo defaultFactionTypeField;

        public static readonly BodyPartGroupDef GroupShoulders = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Shoulders");
        public static readonly BodyPartGroupDef GroupArms = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Arms");
        public static readonly BodyPartGroupDef GroupHands = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Hands");
        public static readonly BodyPartGroupDef GroupWaist = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Waist");
        public static readonly BodyPartGroupDef GroupLegs = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Legs");
        public static readonly BodyPartGroupDef GroupFeet = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Feet");
        public static void Draw(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            // Check if we have an active preset - if not, disable all editing
            bool hasActivePreset = NoPresetPanel.HasActivePreset();

            // Mode Toggle & Undo/Redo (Row 1)
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 30f);

            bool isAdvanced = EditorSession.CurrentMode == EditorMode.Advanced;
            string advancedLabel = LanguageManager.Get("Advanced");
            float advancedTextWidth = Text.CalcSize(advancedLabel).x;
            float advancedRectWidth = 24f + 5f + advancedTextWidth;
            Rect advancedModeRect = new Rect(headerRect.x, headerRect.y, advancedRectWidth, 24f);

            // Disable checkbox if no active preset
            if (!hasActivePreset)
            {
                GUI.color = Color.gray;
                Widgets.CheckboxLabeled(advancedModeRect, LanguageManager.Get("Advanced"), ref isAdvanced);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(advancedModeRect, LanguageManager.Get("NoPresetEditingDisabledTooltip"));
            }
            else
            {
                Widgets.CheckboxLabeled(advancedModeRect, LanguageManager.Get("Advanced"), ref isAdvanced);
                TooltipHandler.TipRegion(advancedModeRect, LanguageManager.Get("AdvancedModeTooltip"));
                if (isAdvanced != (EditorSession.CurrentMode == EditorMode.Advanced))
                {
                    EditorSession.CurrentMode = isAdvanced ? EditorMode.Advanced : EditorMode.Simple;
                }
            }

            // Undo/Redo Buttons
            float undoX = headerRect.x + advancedRectWidth + 6f;
            Rect undoRect = new Rect(undoX, headerRect.y, 24f, 24f);

            // Disable undo/redo if no active preset
            if (!hasActivePreset)
            {
                GUI.color = Color.gray;
                if (TexCache.UndoTex != null)
                    Widgets.ButtonImage(undoRect, TexCache.UndoTex);
                else
                    Widgets.ButtonText(undoRect, "<", true, false, false);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(undoRect, LanguageManager.Get("NoPresetEditingDisabledTooltip"));
            }
            else
            {
                // Use icon buttons instead of text buttons
                bool canUndo = TexCache.UndoTex != null;
                if (canUndo)
                {
                    if (Widgets.ButtonImage(undoRect, TexCache.UndoTex))
                    {
                        if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                        {
                            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                            var kData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                            UndoManager.Undo();
                            FactionGearEditor.MarkDirty();
                        }
                    }
                }
                else
                {
                    // Fallback to text button if icon not available
                    if (Widgets.ButtonText(undoRect, "<"))
                    {
                        if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                        {
                            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                            var kData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                            UndoManager.Undo();
                            FactionGearEditor.MarkDirty();
                        }
                    }
                }
                TooltipHandler.TipRegion(undoRect, LanguageManager.Get("UndoTooltip"));
            }

            Rect redoRect = new Rect(undoX + 28f, headerRect.y, 24f, 24f);

            // Disable redo if no active preset
            if (!hasActivePreset)
            {
                GUI.color = Color.gray;
                if (TexCache.RedoTex != null)
                    Widgets.ButtonImage(redoRect, TexCache.RedoTex);
                else
                    Widgets.ButtonText(redoRect, ">", true, false, false);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(redoRect, LanguageManager.Get("NoPresetEditingDisabledTooltip"));
            }
            else
            {
                // Use icon buttons instead of text buttons
                bool canRedo = TexCache.RedoTex != null;
                if (canRedo)
                {
                    if (Widgets.ButtonImage(redoRect, TexCache.RedoTex))
                    {
                        if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                        {
                            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                            var kData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                            UndoManager.Redo();
                            FactionGearEditor.MarkDirty();
                        }
                    }
                }
                else
                {
                    // Fallback to text button if icon not available
                    if (Widgets.ButtonText(redoRect, ">"))
                    {
                        if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                        {
                            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                            var kData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                            UndoManager.Redo();
                            FactionGearEditor.MarkDirty();
                        }
                    }
                }
                TooltipHandler.TipRegion(redoRect, LanguageManager.Get("RedoTooltip"));
            }

            // Handle Keyboard Shortcuts - only if has active preset
            if (hasActivePreset && Event.current.type == EventType.KeyDown && Event.current.control)
            {
                if (Event.current.keyCode == KeyCode.Z)
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        var kData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                        UndoManager.Undo();
                        FactionGearEditor.MarkDirty();
                        Event.current.Use();
                    }
                }
                else if (Event.current.keyCode == KeyCode.Y)
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        var kData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                        UndoManager.Redo();
                        FactionGearEditor.MarkDirty();
                        Event.current.Use();
                    }
                }
            }

            bool hasSelection = !string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName);
            float rightEdge = headerRect.xMax;
            bool inGame = Current.Game != null;

            // Raid button — resolve faction relation for color
            bool isRaidHostile = false;
            if (inGame && !string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
            {
                var fDef = DefDatabase<FactionDef>.GetNamedSilentFail(EditorSession.SelectedFactionDefName);
                if (fDef != null)
                {
                    var fac = Find.FactionManager.FirstFactionOfDef(fDef)
                        ?? Find.FactionManager.AllFactions.FirstOrDefault(f => f.def == fDef && !f.IsPlayer);
                    isRaidHostile = fac != null && fac.HostileTo(Faction.OfPlayer);
                }
            }
            Rect raidButtonRect = new Rect(rightEdge - 150f, headerRect.y, 64f, 24f);
            Color prevRaidColor = GUI.color;
            if (!inGame)
            {
                GUI.color = Color.gray;
            }
            else if (isRaidHostile)
            {
                GUI.color = Color.red;
            }
            else
            {
                GUI.color = new Color(0.3f, 0.7f, 0.4f);
            }
            string raidBtnLabel = inGame ? (isRaidHostile ? LanguageManager.Get("Raid") : LanguageManager.Get("Aid")) : LanguageManager.Get("Raid");
            string raidBtnTooltip = inGame ? (isRaidHostile ? LanguageManager.Get("RaidTooltip") : LanguageManager.Get("AidTooltip")) : LanguageManager.Get("PreviewInGameOnlyTooltip");
            if (Widgets.ButtonText(raidButtonRect, raidBtnLabel, true, false, inGame))
            {
                HandleRaidButtonClick();
            }
            TooltipHandler.TipRegion(raidButtonRect, raidBtnTooltip);
            GUI.color = prevRaidColor;

            Rect previewButtonRect = new Rect(rightEdge - 80f, headerRect.y, 80f, 24f);
            Color prevColor = GUI.color;
            if (!inGame)
            {
                GUI.color = Color.gray;
            }
            if (Widgets.ButtonText(previewButtonRect, LanguageManager.Get("Preview"), true, false, inGame))
            {
                HandlePreviewButtonClick();
            }
            TooltipHandler.TipRegion(previewButtonRect, inGame ? LanguageManager.Get("PreviewAllKindsTooltip") : LanguageManager.Get("PreviewInGameOnlyTooltip"));
            GUI.color = prevColor;

            // Toolbar Row 2
            float row2Y = headerRect.y + 30f;
            float row2Left = innerRect.x;
            float row2Right = innerRect.xMax;

            if (hasSelection)
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                var kindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);

                // Batch Apply (Left Aligned)
                float batchWidth = 100f;
                Rect batchButtonRect = new Rect(row2Left, row2Y, batchWidth, 24f);
                if (!hasActivePreset)
                {
                    GUI.color = Color.gray;
                    Widgets.ButtonText(batchButtonRect, LanguageManager.Get("BatchApply"), true, false, false);
                    GUI.color = Color.white;
                    TooltipHandler.TipRegion(batchButtonRect, LanguageManager.Get("NoPresetEditingDisabledTooltip"));
                }
                else if (Widgets.ButtonText(batchButtonRect, LanguageManager.Get("BatchApply")))
                {
                    var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(EditorSession.SelectedFactionDefName);
                    if (factionDef != null && kindData != null)
                    {
                        var initialFlags = Dialog_BatchApply.GetDefaultFlagsForCategory(EditorSession.SelectedCategory);
                        Find.WindowStack.Add(new Dialog_BatchApply(factionDef, kindData, initialFlags));
                    }
                }
                else
                {
                    TooltipHandler.TipRegion(batchButtonRect, LanguageManager.Get("BatchApplyTooltip"));
                }

                // Apply Others (Left Aligned, next to Batch)
                float applyWidth = 110f;
                Rect aaButtonRect = new Rect(batchButtonRect.xMax + 10f, row2Y, applyWidth, 24f);
                if (!hasActivePreset)
                {
                    GUI.color = Color.gray;
                    Widgets.ButtonText(aaButtonRect, LanguageManager.Get("ApplyOthers"), true, false, false);
                    GUI.color = Color.white;
                    TooltipHandler.TipRegion(aaButtonRect, LanguageManager.Get("NoPresetEditingDisabledTooltip"));
                }
                else if (Widgets.ButtonText(aaButtonRect, LanguageManager.Get("ApplyOthers")))
                {
                    Find.WindowStack.Add(new Dialog_MessageBox(
                        LanguageManager.Get("ApplyToAllKindsConfirm"),
                        LanguageManager.Get("Yes"),
                        delegate
                        {
                            FactionGearEditor.CopyKindDefGear();
                            FactionGearEditor.ApplyToAllKindsInFaction();
                            Messages.Message(LanguageManager.Get("AppliedToAllKinds"), MessageTypeDefOf.PositiveEvent);
                        },
                        "No",
                        null
                    ));
                }
                else
                {
                    TooltipHandler.TipRegion(aaButtonRect, LanguageManager.Get("ApplyToOthersTooltip"));
                }

                // Clear All (Right Aligned)
                float clearWidth = 80f;
                Rect clearButtonRect = new Rect(row2Right - clearWidth, row2Y, clearWidth, 24f);
                if (!hasActivePreset)
                {
                    GUI.color = Color.gray;
                    Widgets.ButtonText(clearButtonRect, LanguageManager.Get("ClearAll"), true, false, false);
                    GUI.color = Color.white;
                    TooltipHandler.TipRegion(clearButtonRect, LanguageManager.Get("NoPresetEditingDisabledTooltip"));
                }
                else if (Widgets.ButtonText(clearButtonRect, LanguageManager.Get("ClearAll")))
                {
                    Dialog_ConfirmWithCheckbox.ShowIfNotDismissed(
                        "ClearAllGearConfirm",
                        LanguageManager.Get("ClearAllGearConfirmMessage"),
                        delegate { ClearAllGear(kindData); },
                        LanguageManager.Get("ClearAll"),
                        LanguageManager.Get("Cancel"),
                        null,
                        true
                    );
                }
                else
                {
                    TooltipHandler.TipRegion(clearButtonRect, LanguageManager.Get("ClearAllGearTooltip"));
                }
            }

            Rect contentRect = new Rect(innerRect.x, innerRect.y + 65f, innerRect.width, innerRect.height - 65f);

            if (EditorSession.CurrentMode == EditorMode.Simple)
            {
                DrawSimpleMode(contentRect);
            }
            else
            {
                DrawAdvancedMode(contentRect);
            }
        }

        private static void DrawSimpleMode(Rect innerRect)
        {
            if (string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) || string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                Widgets.Label(innerRect, LanguageManager.Get("SelectAKindDefFirst"));
                return;
            }

            List<GearItem> gearItemsToDraw = new List<GearItem>();
            KindGearData currentKindData = null;
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                currentKindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                gearItemsToDraw = GetCurrentCategoryGear(currentKindData);
            }

            Widgets.Label(new Rect(innerRect.x, innerRect.y, 120f, 24f), LanguageManager.Get("SelectedGear"));

            Rect tabRowRect = new Rect(innerRect.x, innerRect.y + 24f, innerRect.width, 24f);

            Rect clearCatRect = new Rect(tabRowRect.xMax - 70f, tabRowRect.y, 70f, 24f);
            if (Widgets.ButtonText(clearCatRect, LanguageManager.Get("Clear")))
            {
                if (currentKindData != null)
                {
                    Dialog_ConfirmWithCheckbox.ShowIfNotDismissed(
                        "ClearCategoryGearConfirm",
                        LanguageManager.Get("ClearCategoryGearConfirmMessage"),
                        delegate { ClearCategoryGear(currentKindData, EditorSession.SelectedCategory); },
                        LanguageManager.Get("Clear"),
                        LanguageManager.Get("Cancel"),
                        null,
                        true
                    );
                }
            }
            TooltipHandler.TipRegion(clearCatRect, LanguageManager.Get("ClearCategoryTooltip"));

            Rect tabRect = new Rect(tabRowRect.x, tabRowRect.y, tabRowRect.width - 75f, 24f);
            DrawCategoryTabs(tabRect);

            // Preview height removed as requested by user
            float previewHeight = 0f;

            float statsHeight = 24f;

            float listStartY = tabRect.yMax + 5f;
            Rect listOutRect = new Rect(innerRect.x, listStartY, innerRect.width, innerRect.height - (listStartY - innerRect.y) - statsHeight);

            // Filter Advanced Items for Current Category
            List<SpecRequirementEdit> advancedItems = new List<SpecRequirementEdit>();
            if (currentKindData != null)
            {
                if (EditorSession.SelectedCategory == GearCategory.Weapons && !currentKindData.SpecificWeapons.NullOrEmpty())
                {
                    advancedItems.AddRange(currentKindData.SpecificWeapons.Where(x => x.Thing != null && x.Thing.IsRangedWeapon));
                }
                else if (EditorSession.SelectedCategory == GearCategory.MeleeWeapons && !currentKindData.SpecificWeapons.NullOrEmpty())
                {
                    advancedItems.AddRange(currentKindData.SpecificWeapons.Where(x => x.Thing != null && x.Thing.IsMeleeWeapon));
                }
                else if (EditorSession.SelectedCategory == GearCategory.Others && !currentKindData.SpecificApparel.NullOrEmpty())
                {
                    advancedItems.AddRange(currentKindData.SpecificApparel.Where(x => x.Thing != null && IsBelt(x.Thing)));
                }
                else if (EditorSession.SelectedCategory == GearCategory.Armors && !currentKindData.SpecificApparel.NullOrEmpty())
                {
                    advancedItems.AddRange(currentKindData.SpecificApparel.Where(x => x.Thing != null && IsArmor(x.Thing)));
                }
                else if (EditorSession.SelectedCategory == GearCategory.Apparel && !currentKindData.SpecificApparel.NullOrEmpty())
                {
                    advancedItems.AddRange(currentKindData.SpecificApparel.Where(x => x.Thing != null && IsApparel(x.Thing)));
                }
            }

            float contentHeight = 0f;
            foreach (var item in gearItemsToDraw)
            {
                contentHeight += (item == EditorSession.ExpandedGearItem) ? 60f : 30f;
            }
            if (advancedItems.Any())
            {
                contentHeight += 30f; // Header
                foreach (var advItem in advancedItems)
                {
                    contentHeight += (advItem == EditorSession.ExpandedSpecItem) ? 60f : 30f;
                }
            }

            Rect listViewRect = new Rect(0, 0, listOutRect.width - 16f, Mathf.Max(contentHeight, listOutRect.height));

            Widgets.BeginScrollView(listOutRect, ref EditorSession.GearListScrollPos, listViewRect);
            Listing_Standard gearListing = new Listing_Standard();
            gearListing.Begin(listViewRect);

            if (gearItemsToDraw.Any() && currentKindData != null)
            {
                foreach (var gearItem in gearItemsToDraw.ToList())
                {
                    bool isExpanded = (gearItem == EditorSession.ExpandedGearItem);
                    Rect rowRect = gearListing.GetRect(isExpanded ? 56f : 28f);
                    DrawGearItem(rowRect, gearItem, currentKindData, isExpanded);
                    gearListing.Gap(2f);
                }
            }

            if (advancedItems.Any())
            {
                gearListing.GapLine();
                WidgetsUtils.Label(gearListing, $"<b>{LanguageManager.Get("AdvancedSpecificItems")}</b>");
                foreach (var advItem in advancedItems)
                {
                    bool isAdvExpanded = (advItem == EditorSession.ExpandedSpecItem);
                    Rect rowRect = gearListing.GetRect(isAdvExpanded ? 56f : 28f);
                    DrawAdvancedItemSimple(rowRect, advItem, currentKindData);
                    gearListing.Gap(2f);
                }
            }

            gearListing.End();
            Widgets.EndScrollView();

            if (currentKindData != null)
            {
                float currentY = innerRect.yMax - statsHeight;

                Rect statsRect = new Rect(innerRect.x, currentY, innerRect.width, 24f);
                float avgValue = FactionGearEditor.GetAverageValue(currentKindData);
                float avgWeight = FactionGearEditor.GetAverageWeight(currentKindData);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(statsRect, $"{LanguageManager.Get("AvgValue")}: {avgValue:F0} | {LanguageManager.Get("AvgWeight")}: {avgWeight:F1}");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;

                TooltipHandler.TipRegion(statsRect, LanguageManager.Get("WeightMechanicsTooltip"));
            }
        }

        private static void DrawAdvancedMode(Rect rect)
        {
            if (string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) || string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                Widgets.Label(rect, LanguageManager.Get("SelectAKindDefFirst"));
                return;
            }

            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
            var kindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);

            // Row 1: core tabs
            Rect tabRow1 = new Rect(rect.x, rect.y, rect.width, 24f);
            float row1TabW = rect.width / 5f;
            DrawAdvTab(new Rect(tabRow1.x, tabRow1.y, row1TabW, tabRow1.height), LanguageManager.Get("General"), AdvancedTab.General);
            DrawAdvTab(new Rect(tabRow1.x + row1TabW, tabRow1.y, row1TabW, tabRow1.height), LanguageManager.Get("Apparel"), AdvancedTab.Apparel);
            DrawAdvTab(new Rect(tabRow1.x + row1TabW * 2, tabRow1.y, row1TabW, tabRow1.height), LanguageManager.Get("Weapons"), AdvancedTab.Weapons);
            DrawAdvTab(new Rect(tabRow1.x + row1TabW * 3, tabRow1.y, row1TabW, tabRow1.height), LanguageManager.Get("Hediffs"), AdvancedTab.Hediffs);
            DrawAdvTab(new Rect(tabRow1.x + row1TabW * 4, tabRow1.y, row1TabW, tabRow1.height), LanguageManager.Get("Items"), AdvancedTab.Items);

            // Row 2: character tabs (Skills hidden)
            Rect tabRow2 = new Rect(rect.x, rect.y + 28f, rect.width, 24f);
            int row2Count = ModsConfig.BiotechActive ? 3 : 2;
            float row2TabW = rect.width / row2Count;
            DrawAdvTab(new Rect(tabRow2.x, tabRow2.y, row2TabW, tabRow2.height), LanguageManager.Get("Traits"), AdvancedTab.Traits);
            DrawAdvTab(new Rect(tabRow2.x + row2TabW, tabRow2.y, row2TabW, tabRow2.height), LanguageManager.Get("Appearance"), AdvancedTab.Appearance);
            if (ModsConfig.BiotechActive)
                DrawAdvTab(new Rect(tabRow2.x + row2TabW * 2, tabRow2.y, row2TabW, tabRow2.height), LanguageManager.Get("Genes"), AdvancedTab.Genes);

            Rect contentRect = new Rect(rect.x, rect.y + 56f, rect.width, rect.height - 56f);

            float height = 500f;

            float gearListHeight = 0f;
            if (EditorSession.CurrentAdvancedTab == AdvancedTab.Apparel || EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons)
            {
                if (EditorSession.CurrentAdvancedTab == AdvancedTab.Apparel && EditorSession.SelectedCategory != GearCategory.Apparel && EditorSession.SelectedCategory != GearCategory.Armors)
                {
                    EditorSession.SelectedCategory = GearCategory.Apparel;
                    EditorSession.CachedModSources = null;
                }
                if (EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons && EditorSession.SelectedCategory != GearCategory.Weapons && EditorSession.SelectedCategory != GearCategory.MeleeWeapons)
                {
                    EditorSession.SelectedCategory = GearCategory.Weapons;
                    EditorSession.CachedModSources = null;
                }

                var gearItems = GetCurrentCategoryGear(kindData);
                foreach (var item in gearItems)
                {
                    gearListHeight += (item == EditorSession.ExpandedGearItem) ? 60f : 30f;
                }
                gearListHeight += 40f;
            }

            if (EditorSession.CurrentAdvancedTab == AdvancedTab.Apparel)
            {
                height = gearListHeight + 100f;
                if (!kindData.SpecificApparel.NullOrEmpty())
                {
                    foreach (var item in kindData.SpecificApparel)
                        height += ApparelCardUI.GetCardHeight(item) + 8f;
                    height += 100f;
                }
                else
                    height += 200f;
            }
            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons)
            {
                height = gearListHeight + 150f;
                if (!kindData.SpecificWeapons.NullOrEmpty())
                {
                    foreach (var item in kindData.SpecificWeapons)
                        height += ApparelCardUI.GetCardHeight(item) + 8f;
                    height += 100f;
                }
                else
                    height += 200f;
            }
            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Hediffs)
            {
                height = 80f; // header: label + button row + gaps
                if (!kindData.ForcedHediffs.NullOrEmpty())
                {
                    foreach (var item in kindData.ForcedHediffs)
                        height += HediffCardUI.GetCardHeight(item) + 8f;
                }
                height += 40f; // bottom padding
            }
            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Items)
            {
                height = 80f; // header: label + button row + gaps
                if (!kindData.InventoryItems.NullOrEmpty())
                {
                    foreach (var item in kindData.InventoryItems)
                        height += ItemCardUI.GetCardHeight(item) + 8f;
                }
                height += 40f; // bottom padding
            }
            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Traits)
            {
                height = 100f;
                if (!kindData.ForcedTraits.NullOrEmpty())
                {
                    foreach (var item in kindData.ForcedTraits)
                        height += TraitCardUI.GetCardHeight(item) + 8f;
                }
                height += 40f;
            }
            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Appearance)
            {
                height = 60f; // header + override row
                if (kindData.ForcedAppearance != null)
                {
                    height += 30f; // edit/clear button row
                    int lines = 0;
                    if (kindData.ForcedAppearance.HairDef != null) lines++;
                    if (kindData.ForcedAppearance.BeardDef != null) lines++;
                    if (kindData.ForcedAppearance.BodyTypeDef != null) lines++;
                    if (kindData.ForcedAppearance.HeadTypeDef != null) lines++;
                    if (kindData.ForcedAppearance.skinColor.HasValue) lines++;
                    height += lines * 24f + 40f;
                }
            }
            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Genes)
            {
                height = 100f;
                if (!kindData.ForcedGenes.NullOrEmpty())
                {
                    foreach (var item in kindData.ForcedGenes)
                        height += GeneCardUI.GetCardHeight(item) + 8f;
                }
                height += 40f;
            }
            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.General)
            {
                bool isForceIgnore = kindData.ForceIgnoreRestrictions ?? FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;
                height = 310f;
                if (!kindData.ForceNaked) height += 112f;
                if (!isForceIgnore)
                {
                    if (kindData.ApparelMoney.HasValue) height += 24f;
                    if (kindData.WeaponMoney.HasValue) height += 24f;
                }
            }
            else
                height = 350f;

            Rect viewRect = new Rect(0, 0, contentRect.width - 16f, height);

            Widgets.BeginScrollView(contentRect, ref EditorSession.AdvancedScrollPos, viewRect);
            Listing_Standard ui = new Listing_Standard();
            ui.Begin(viewRect);

            switch (EditorSession.CurrentAdvancedTab)
            {
                case AdvancedTab.General:
                    DrawAdvancedGeneral(ui, kindData);
                    break;
                case AdvancedTab.Apparel:
                    DrawAdvancedApparel(ui, kindData);
                    break;
                case AdvancedTab.Weapons:
                    DrawAdvancedWeapons(ui, kindData);
                    break;
                case AdvancedTab.Hediffs:
                    DrawAdvancedHediffs(ui, kindData);
                    break;
                case AdvancedTab.Items:
                    DrawAdvancedItems(ui, kindData);
                    break;
                case AdvancedTab.Traits:
                    DrawAdvancedTraits(ui, kindData);
                    break;
                case AdvancedTab.Appearance:
                    DrawAdvancedAppearance(ui, kindData);
                    break;
                case AdvancedTab.Genes:
                    DrawAdvancedGenes(ui, kindData);
                    break;
            }

            ui.End();
            Widgets.EndScrollView();
        }

        private static void DrawAdvTab(Rect rect, string label, AdvancedTab tab)
        {
            bool isSelected = EditorSession.CurrentAdvancedTab == tab;
            if (isSelected) Widgets.DrawHighlightSelected(rect);
            else Widgets.DrawHighlightIfMouseover(rect);

            if (Widgets.ButtonInvisible(rect))
            {
                if (EditorSession.CurrentAdvancedTab != tab)
                {
                    EditorSession.CurrentAdvancedTab = tab;
                    EditorSession.AdvancedScrollPos = Vector2.zero;
                }
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawCategoryTabs(Rect rect)
        {
            float tabWidth = rect.width / 5f;
            DrawTab(new Rect(rect.x, rect.y, tabWidth, rect.height), LanguageManager.Get("Ranged"), GearCategory.Weapons);
            DrawTab(new Rect(rect.x + tabWidth, rect.y, tabWidth, rect.height), LanguageManager.Get("Melee"), GearCategory.MeleeWeapons);
            DrawTab(new Rect(rect.x + tabWidth * 2, rect.y, tabWidth, rect.height), LanguageManager.Get("Armors"), GearCategory.Armors);
            DrawTab(new Rect(rect.x + tabWidth * 3, rect.y, tabWidth, rect.height), LanguageManager.Get("Clothes"), GearCategory.Apparel);
            DrawTab(new Rect(rect.x + tabWidth * 4, rect.y, tabWidth, rect.height), LanguageManager.Get("Others"), GearCategory.Others);
        }

        private static void DrawTab(Rect rect, string label, GearCategory category)
        {
            bool isSelected = EditorSession.SelectedCategory == category;

            if (isSelected)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                Widgets.DrawHighlightSelected(rect);
                GUI.color = Color.white;
            }
            else
            {
                Widgets.DrawHighlightIfMouseover(rect);
            }

            if (Widgets.ButtonInvisible(rect))
            {
                if (EditorSession.SelectedCategory != category)
                {
                    EditorSession.SelectedCategory = category;
                    EditorSession.ExpandedGearItem = null;
                    EditorSession.CachedModSources = null;
                    EditorSession.SelectedAmmoSets.Clear();
                    FactionGearEditor.GetUniqueModSources();
                    FactionGearEditor.CalculateBounds();
                }
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            if (isSelected) GUI.color = new Color(1f, 0.9f, 0.5f);
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static List<GearItem> GetCurrentCategoryGear(KindGearData kindData)
        {
            switch (EditorSession.SelectedCategory)
            {
                case GearCategory.Weapons: return kindData.weapons;
                case GearCategory.MeleeWeapons: return kindData.meleeWeapons;
                case GearCategory.Armors: return kindData.armors;
                case GearCategory.Apparel: return kindData.apparel;
                case GearCategory.Others: return kindData.others;
                default: return new List<GearItem>();
            }
        }

        private static void ClearAllGear(KindGearData kindData)
        {
            UndoManager.RecordState(kindData);
            kindData.weapons.Clear();
            kindData.meleeWeapons.Clear();
            kindData.armors.Clear();
            kindData.apparel.Clear();
            kindData.others.Clear();

            kindData.SpecificApparel?.Clear();
            kindData.SpecificWeapons?.Clear();
            kindData.ForcedHediffs?.Clear();
            kindData.InventoryItems?.Clear();

            kindData.isModified = true;
            FactionGearEditor.MarkDirty();
        }

        private static void ClearCategoryGear(KindGearData kindData, GearCategory category)
        {
            UndoManager.RecordState(kindData);
            switch (category)
            {
                case GearCategory.Weapons:
                    kindData.weapons.Clear();
                    if (!kindData.SpecificWeapons.NullOrEmpty())
                    {
                        kindData.SpecificWeapons.RemoveAll(x => x.Thing != null && x.Thing.IsRangedWeapon);
                    }
                    break;
                case GearCategory.MeleeWeapons:
                    kindData.meleeWeapons.Clear();
                    if (!kindData.SpecificWeapons.NullOrEmpty())
                    {
                        kindData.SpecificWeapons.RemoveAll(x => x.Thing != null && x.Thing.IsMeleeWeapon);
                    }
                    break;
                case GearCategory.Armors:
                    kindData.armors.Clear();
                    if (!kindData.SpecificApparel.NullOrEmpty())
                    {
                        kindData.SpecificApparel.RemoveAll(x => x.Thing != null && IsArmor(x.Thing));
                    }
                    break;
                case GearCategory.Apparel:
                    kindData.apparel.Clear();
                    if (!kindData.SpecificApparel.NullOrEmpty())
                    {
                        kindData.SpecificApparel.RemoveAll(x => x.Thing != null && IsApparel(x.Thing));
                    }
                    break;
                case GearCategory.Others:
                    kindData.others.Clear();
                    if (!kindData.SpecificApparel.NullOrEmpty())
                    {
                        kindData.SpecificApparel.RemoveAll(x => x.Thing != null && IsBelt(x.Thing));
                    }
                    break;
            }
            kindData.isModified = true;
            FactionGearEditor.MarkDirty();
        }

        // --- Advanced Mode Helpers ---

        private static void DrawAdvancedGeneral(Listing_Standard ui, KindGearData kindData)
        {
            WidgetsUtils.Label(ui, $"<b>{LanguageManager.Get("GeneralOverrides")}</b>");
            ui.Gap();

            Rect exportTagsRect = ui.GetRect(28f);
            if (Widgets.ButtonText(exportTagsRect, LanguageManager.Get("ExportApparelTags")))
            {
                ExportApparelTagsToClipboard(kindData);
            }
            TooltipHandler.TipRegion(exportTagsRect, LanguageManager.Get("ExportApparelTagsTooltip"));

            ui.Gap();

            bool forceIgnore = FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;

            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindData.kindDefName);
            if (kindDef != null)
            {
                WidgetsUtils.Label(ui, LanguageManager.Get("DefaultApparelBudgetValue", FormatBudgetRange(kindDef.apparelMoney)));
            }

            // Force Ignore option at the top
            bool localForceIgnore = kindData.ForceIgnoreRestrictions ?? forceIgnore;
            ui.CheckboxLabeled(LanguageManager.Get("ForceIgnore"), ref localForceIgnore, LanguageManager.Get("ForceIgnoreTooltip"));
            if (localForceIgnore != (kindData.ForceIgnoreRestrictions ?? forceIgnore))
            {
                UndoManager.RecordState(kindData);
                kindData.ForceIgnoreRestrictions = localForceIgnore != forceIgnore ? localForceIgnore : (bool?)null;
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
            }

            ui.Gap();

            ui.CheckboxLabeled(LanguageManager.Get("ForceNaked"), ref kindData.ForceNaked, LanguageManager.Get("ForceNakedTooltip"));
            if (!kindData.ForceNaked)
            {
                // Force Only Selected moved here from Apparel tab
                bool forceOnly = kindData.ForceOnlySelected;
                ui.CheckboxLabeled(LanguageManager.Get("ForceOnlySelected"), ref forceOnly, LanguageManager.Get("ForceOnlySelectedTooltip"));
                if (forceOnly != kindData.ForceOnlySelected)
                {
                    UndoManager.RecordState(kindData);
                    kindData.ForceOnlySelected = forceOnly;
                    kindData.isModified = true;
                    FactionGearEditor.MarkDirty();
                }

                bool forceOverrideHediffs = kindData.ForceOverrideHediffs;
                ui.CheckboxLabeled(LanguageManager.Get("ForceOverrideHediffs"), ref forceOverrideHediffs, LanguageManager.Get("ForceOverrideHediffsTooltip"));
                if (forceOverrideHediffs != kindData.ForceOverrideHediffs)
                {
                    UndoManager.RecordState(kindData);
                    kindData.ForceOverrideHediffs = forceOverrideHediffs;
                    kindData.isModified = true;
                    FactionGearEditor.MarkDirty();
                }

                bool outfitFirst = kindData.OutfitFirstBudgetStrategy;
                ui.CheckboxLabeled(LanguageManager.Get("OutfitFirstBudgetStrategy"), ref outfitFirst, LanguageManager.Get("OutfitFirstBudgetStrategyTooltip"));
                if (outfitFirst != kindData.OutfitFirstBudgetStrategy)
                {
                    UndoManager.RecordState(kindData);
                    kindData.OutfitFirstBudgetStrategy = outfitFirst;
                    kindData.isModified = true;
                    FactionGearEditor.MarkDirty();
                }

                if (CECompat.IsCEActive)
                {
                    bool ammoProtection = FactionGearCustomizerMod.Settings.ammoProtection;
                    ui.CheckboxLabeled(LanguageManager.Get("AmmoProtection"), ref ammoProtection, LanguageManager.Get("AmmoProtectionTooltip"));
                    if (ammoProtection != FactionGearCustomizerMod.Settings.ammoProtection)
                    {
                        FactionGearCustomizerMod.Settings.ammoProtection = ammoProtection;
                        FactionGearCustomizerMod.Settings.Write();
                    }
                }

                bool autoRaid = FactionGearCustomizerMod.Settings.autoRaidPointsEnabled;
                ui.CheckboxLabeled(LanguageManager.Get("AutoRaidPointsEnabled"), ref autoRaid, LanguageManager.Get("AutoRaidPointsEnabledTooltip"));
                if (autoRaid != FactionGearCustomizerMod.Settings.autoRaidPointsEnabled)
                {
                    FactionGearCustomizerMod.Settings.autoRaidPointsEnabled = autoRaid;
                    FactionGearCustomizerMod.Settings.Write();

                    if (autoRaid)
                    {
                        // 开关开启：立即计算所有派系袭击点数
                        FactionGearEditor.TriggerAutoCalculateAll();
                    }
                    else
                    {
                        // 开关关闭：立即恢复默认袭击点数
                        FactionGearEditor.TriggerClearAllOverrides();
                    }
                }

            }

            // Auto-switch weapon by distance (SimpleSidearms only)
            {
                bool ssActiveForSwitch = SimpleSidearmsCompat.IsActive;
                if (!ssActiveForSwitch) GUI.color = Color.gray;
                bool autoSwitch = FactionGearCustomizerMod.Settings.autoSwitchWeaponByRange;
                ui.CheckboxLabeled(LanguageManager.Get("AutoSwitchWeaponByRange"), ref autoSwitch, ssActiveForSwitch ? LanguageManager.Get("AutoSwitchWeaponByRangeTooltip") : LanguageManager.Get("AutoSwitchWeaponByRangeDisabledTooltip"));
                GUI.color = Color.white;
                if (ssActiveForSwitch && autoSwitch != FactionGearCustomizerMod.Settings.autoSwitchWeaponByRange)
                {
                    FactionGearCustomizerMod.Settings.autoSwitchWeaponByRange = autoSwitch;
                    FactionGearCustomizerMod.Settings.Write();
                }
            }

            // Auto-switch weapon by distance for player-controlled colonists
            {
                bool ssActiveForSwitch = SimpleSidearmsCompat.IsActive;
                if (!ssActiveForSwitch) GUI.color = Color.gray;
                bool autoSwitchColonist = FactionGearCustomizerMod.Settings.autoSwitchWeaponByRangeColonist;
                ui.CheckboxLabeled(LanguageManager.Get("AutoSwitchWeaponByRangeColonist"), ref autoSwitchColonist, ssActiveForSwitch ? LanguageManager.Get("AutoSwitchWeaponByRangeColonistTooltip") : LanguageManager.Get("AutoSwitchWeaponByRangeDisabledTooltip"));
                GUI.color = Color.white;
                if (ssActiveForSwitch && autoSwitchColonist != FactionGearCustomizerMod.Settings.autoSwitchWeaponByRangeColonist)
                {
                    FactionGearCustomizerMod.Settings.autoSwitchWeaponByRangeColonist = autoSwitchColonist;
                    FactionGearCustomizerMod.Settings.Write();
                }
            }

            ui.Gap();

            DrawOverrideEnumWithTooltip(ui, LanguageManager.Get("ItemQuality"), kindData.ItemQuality, val => { UndoManager.RecordState(kindData); kindData.ItemQuality = val; FactionGearEditor.MarkDirty(); }, q => LanguageManager.Get("Quality" + q), LanguageManager.Get("ItemQualityTooltip"));
            bool effectiveForceIgnore = kindData.ForceIgnoreRestrictions ?? forceIgnore;
            DrawOverrideFloatRangeWithTooltip(ui, LanguageManager.Get("ApparelBudget"), ref kindData.ApparelMoney, val => { UndoManager.RecordState(kindData); kindData.ApparelMoney = val; FactionGearEditor.MarkDirty(); }, effectiveForceIgnore, LanguageManager.Get("ApparelBudgetTooltip"));
            DrawOverrideFloatRangeWithTooltip(ui, LanguageManager.Get("WeaponBudget"), ref kindData.WeaponMoney, val => { UndoManager.RecordState(kindData); kindData.WeaponMoney = val; FactionGearEditor.MarkDirty(); }, effectiveForceIgnore, LanguageManager.Get("WeaponBudgetTooltip"));
            DrawTechLevelLimitWithTooltip(ui, kindData, LanguageManager.Get("TechLevelLimitTooltip"));

            Rect colorRect = ui.GetRect(28f);
            Widgets.Label(colorRect.LeftHalf(), LanguageManager.Get("NameColor"));
            TooltipHandler.TipRegion(colorRect, LanguageManager.Get("NameColorTooltip"));
            if (kindData.ApparelColor.HasValue)
            {
                Rect rightHalf = colorRect.RightHalf();
                Rect pickRect = new Rect(rightHalf.x, rightHalf.y, rightHalf.width - 35f, 28f);
                Rect xRect = new Rect(rightHalf.xMax - 30f, rightHalf.y, 30f, 28f);

                if (Widgets.ButtonText(pickRect, LanguageManager.Get("PickColor")))
                {
                    Find.WindowStack.Add(new Window_ColorPicker(kindData.ApparelColor.Value, (c) => { UndoManager.RecordState(kindData); kindData.ApparelColor = c; FactionGearEditor.MarkDirty(); }));
                }
                Widgets.DrawBoxSolid(new Rect(rightHalf.x - 30f, rightHalf.y, 28f, 28f), kindData.ApparelColor.Value);
                if (Widgets.ButtonText(xRect, "X"))
                {
                    UndoManager.RecordState(kindData); kindData.ApparelColor = null; FactionGearEditor.MarkDirty();
                }
            }
            else
            {
                if (Widgets.ButtonText(colorRect.RightHalf(), LanguageManager.Get("SetColor")))
                {
                    UndoManager.RecordState(kindData); kindData.ApparelColor = Color.white; FactionGearEditor.MarkDirty();
                }
            }

        }

        private static void ExportApparelTagsToClipboard(KindGearData kindData)
        {
            List<ThingDef> apparelDefs = GetConfiguredApparelDefs(kindData);
            if (apparelDefs.Count == 0)
            {
                Messages.Message(LanguageManager.Get("NoApparelTagsToExport"), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<string> uniqueTags = new List<string>();
            HashSet<string> seenTags = new HashSet<string>(StringComparer.Ordinal);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("<!-- Selected pawn kind -->");
            builder.AppendLine($"<!-- PawnKindDef: {GetPawnKindDefNameForExport(kindData)} -->");
            builder.AppendLine();
            builder.AppendLine("<!-- Unique apparel tags -->");
            builder.AppendLine("<apparelTags>");

            foreach (ThingDef apparelDef in apparelDefs)
            {
                if (apparelDef?.apparel?.tags == null) continue;

                foreach (string tag in apparelDef.apparel.tags)
                {
                    if (string.IsNullOrWhiteSpace(tag)) continue;
                    if (seenTags.Add(tag))
                    {
                        uniqueTags.Add(tag);
                    }
                }
            }

            foreach (string tag in uniqueTags.OrderBy(t => t))
            {
                builder.AppendLine($"  <li>{tag}</li>");
            }

            builder.AppendLine("</apparelTags>");
            builder.AppendLine();
            builder.AppendLine("<!-- Apparel item to tag map -->");

            foreach (ThingDef apparelDef in apparelDefs.OrderBy(d => d.defName))
            {
                builder.AppendLine($"{apparelDef.defName} | {apparelDef.LabelCap}");

                if (apparelDef.apparel?.tags != null && apparelDef.apparel.tags.Count > 0)
                {
                    builder.AppendLine("  tags: " + string.Join(", ", apparelDef.apparel.tags.OrderBy(t => t).ToArray()));
                }
                else
                {
                    builder.AppendLine("  tags: " + LanguageManager.Get("NoApparelTagsForItem"));
                }

                builder.AppendLine();
            }

            GUIUtility.systemCopyBuffer = builder.ToString().TrimEnd();
            Messages.Message(LanguageManager.Get("ApparelTagsExportedToClipboard", apparelDefs.Count, uniqueTags.Count), MessageTypeDefOf.PositiveEvent, false);
        }

        private static string GetPawnKindDefNameForExport(KindGearData kindData)
        {
            if (kindData == null || string.IsNullOrEmpty(kindData.kindDefName))
            {
                return "unknown";
            }

            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindData.kindDefName);
            return kindDef?.defName ?? kindData.kindDefName;
        }

        private static List<ThingDef> GetConfiguredApparelDefs(KindGearData kindData)
        {
            List<ThingDef> result = new List<ThingDef>();
            HashSet<string> seenDefNames = new HashSet<string>(StringComparer.Ordinal);

            AddGearItemDefs(kindData?.apparel, result, seenDefNames);
            AddGearItemDefs(kindData?.armors, result, seenDefNames);
            AddGearItemDefs(kindData?.others, result, seenDefNames);
            AddThingDefs(kindData?.ApparelRequired, result, seenDefNames);

            if (kindData != null && !kindData.SpecificApparel.NullOrEmpty())
            {
                foreach (SpecRequirementEdit item in kindData.SpecificApparel)
                {
                    AddApparelDef(item?.Thing, result, seenDefNames);
                }
            }

            return result;
        }

        private static void AddGearItemDefs(List<GearItem> gearItems, List<ThingDef> result, HashSet<string> seenDefNames)
        {
            if (gearItems.NullOrEmpty()) return;

            foreach (GearItem gearItem in gearItems)
            {
                AddApparelDef(gearItem?.ThingDef, result, seenDefNames);
            }
        }

        private static void AddThingDefs(List<ThingDef> thingDefs, List<ThingDef> result, HashSet<string> seenDefNames)
        {
            if (thingDefs.NullOrEmpty()) return;

            foreach (ThingDef thingDef in thingDefs)
            {
                AddApparelDef(thingDef, result, seenDefNames);
            }
        }

        private static void AddApparelDef(ThingDef thingDef, List<ThingDef> result, HashSet<string> seenDefNames)
        {
            if (thingDef == null || !thingDef.IsApparel) return;
            if (string.IsNullOrEmpty(thingDef.defName)) return;

            if (seenDefNames.Add(thingDef.defName))
            {
                result.Add(thingDef);
            }
        }

        private static void DrawAdvancedApparel(Listing_Standard ui, KindGearData kindData)
        {
            DrawEmbeddedGearList(ui, kindData, GearCategory.Apparel, GearCategory.Armors);

            // Biocode Chance for Apparel
            float biocodeChance = kindData.BiocodeApparelChance ?? 0f;
            float oldBiocode = biocodeChance;
            WidgetsUtils.Label(ui, $"{LanguageManager.Get("BiocodeApparelChance")}: {biocodeChance:P0}");
            biocodeChance = ui.Slider(biocodeChance, 0f, 1f);
            if (Math.Abs(biocodeChance - oldBiocode) > 0.001f)
            {
                UndoManager.RecordState(kindData);
                kindData.BiocodeApparelChance = biocodeChance;
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
            }

            // Moved ForceNaked and ForceOnlySelected to General tab

            ui.GapLine();
            WidgetsUtils.Label(ui, $"<b>{LanguageManager.Get("SpecificApparelAdvanced")}</b>");
            if (ui.ButtonText(LanguageManager.Get("AddNewApparel")))
            {
                if (kindData.SpecificApparel == null) kindData.SpecificApparel = new List<SpecRequirementEdit>();
                Find.WindowStack.Add(new Dialog_ApparelPicker(kindData.SpecificApparel, kindData));
            }
            if (!kindData.SpecificApparel.NullOrEmpty())
            {
                for (int i = 0; i < kindData.SpecificApparel.Count; i++)
                {
                    var item = kindData.SpecificApparel[i];
                    int index = i;
                    ApparelCardUI.Draw(ui, item, index, () =>
                    {
                        UndoManager.RecordState(kindData);
                        kindData.SpecificApparel.RemoveAt(index);
                        kindData.isModified = true;
                        FactionGearEditor.MarkDirty();
                    }, false, kindData);
                }
            }
        }

        private static void DrawAdvancedWeapons(Listing_Standard ui, KindGearData kindData)
        {
            DrawEmbeddedGearList(ui, kindData, GearCategory.Weapons, GearCategory.MeleeWeapons);
            DrawOverrideEnum(ui, LanguageManager.Get("ForcedWeaponQuality"), kindData.ForcedWeaponQuality, val => { kindData.ForcedWeaponQuality = val; FactionGearEditor.MarkDirty(); }, q => LanguageManager.Get("Quality" + q));

            float biocodeChance = kindData.BiocodeWeaponChance ?? 0f;
            float oldBiocode = biocodeChance;
            WidgetsUtils.Label(ui, $"{LanguageManager.Get("BiocodeChance")}: {biocodeChance:P0}");
            biocodeChance = ui.Slider(biocodeChance, 0f, 1f);
            if (Math.Abs(biocodeChance - oldBiocode) > 0.001f)
            {
                kindData.BiocodeWeaponChance = biocodeChance;
                FactionGearEditor.MarkDirty();
            }
            ui.GapLine();
            WidgetsUtils.Label(ui, $"<b>{LanguageManager.Get("SpecificWeaponsAdvanced")}</b>");
            if (ui.ButtonText(LanguageManager.Get("AddNewWeapon")))
            {
                if (kindData.SpecificWeapons == null) kindData.SpecificWeapons = new List<SpecRequirementEdit>();
                var filter = EditorSession.SelectedCategory == GearCategory.MeleeWeapons
                    ? Dialog_WeaponPicker.WeaponFilterMode.MeleeOnly
                    : Dialog_WeaponPicker.WeaponFilterMode.RangedOnly;
                Find.WindowStack.Add(new Dialog_WeaponPicker(kindData.SpecificWeapons, kindData, filter));
            }
            if (!kindData.SpecificWeapons.NullOrEmpty())
            {
                for (int i = 0; i < kindData.SpecificWeapons.Count; i++)
                {
                    var item = kindData.SpecificWeapons[i];
                    int index = i;
                    ApparelCardUI.Draw(ui, item, index, () =>
                    {
                        var weaponDef = kindData.SpecificWeapons[index]?.Thing;
                        UndoManager.RecordState(kindData);
                        kindData.SpecificWeapons.RemoveAt(index);
                        TryRemoveCEAmmoForWeapon(kindData, weaponDef);
                        kindData.isModified = true;
                        FactionGearEditor.MarkDirty();
                    }, true, kindData);
                }
            }
        }

        private static void DrawAdvancedHediffs(Listing_Standard ui, KindGearData kindData)
        {
            WidgetsUtils.Label(ui, $"<b>{LanguageManager.Get("ForcedHediffs")}</b>");
            Rect buttonRow = ui.GetRect(28f);
            Rect addButtonRect = new Rect(buttonRow.x, buttonRow.y, buttonRow.width - 175f, buttonRow.height);
            Rect quickRect = new Rect(buttonRow.x + buttonRow.width - 165f, buttonRow.y, 85f, buttonRow.height);
            Rect clearButtonRect = new Rect(buttonRow.xMax - 75f, buttonRow.y, 75f, buttonRow.height);

            if (Widgets.ButtonText(addButtonRect, LanguageManager.Get("AddNewHediff")))
            {
                UndoManager.RecordState(kindData);
                if (kindData.ForcedHediffs == null) kindData.ForcedHediffs = new List<ForcedHediff>();
                Find.WindowStack.Add(new Dialog_HediffPicker(kindData.ForcedHediffs));
            }
            if (Widgets.ButtonText(quickRect, LanguageManager.Get("QuickAddPool")))
            {
                ShowQuickHediffPoolMenu(kindData);
            }
            TooltipHandler.TipRegion(quickRect, LanguageManager.Get("QuickAddHediffPoolTooltip"));

            if (Widgets.ButtonText(clearButtonRect, LanguageManager.Get("Clear"), true, false, !kindData.ForcedHediffs.NullOrEmpty()))
            {
                if (kindData.ForcedHediffs != null)
                {
                    UndoManager.RecordState(kindData);
                    kindData.ForcedHediffs.Clear();
                    kindData.isModified = true;
                    FactionGearEditor.MarkDirty();
                }
            }
            TooltipHandler.TipRegion(clearButtonRect, LanguageManager.Get("ClearStatusesTooltip"));

            if (!kindData.ForcedHediffs.NullOrEmpty())
            {
                for (int i = 0; i < kindData.ForcedHediffs.Count; i++)
                {
                    var item = kindData.ForcedHediffs[i];
                    int index = i;
                    HediffCardUI.Draw(ui, item, index, () =>
                    {
                        UndoManager.RecordState(kindData);
                        kindData.ForcedHediffs.RemoveAt(index);
                        kindData.isModified = true;
                        FactionGearEditor.MarkDirty();
                    }, kindData);
                }
            }
        }

        private static void ShowQuickHediffPoolMenu(KindGearData kindData)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            options.Add(new FloatMenuOption(LanguageManager.Get("HediffPool_AnyDrugHigh"), () => AddHediffPool(kindData, HediffPoolType.AnyDrugHigh)));
            options.Add(new FloatMenuOption(LanguageManager.Get("HediffPool_AnyAddiction"), () => AddHediffPool(kindData, HediffPoolType.AnyAddiction)));
            options.Add(new FloatMenuOption(LanguageManager.Get("HediffPool_AnyImplant"), () => AddHediffPool(kindData, HediffPoolType.AnyImplant)));
            // Note: HediffPool_AnyBuff option removed from UI but preserved for save compatibility

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void AddHediffPool(KindGearData kindData, HediffPoolType poolType)
        {
            if (kindData.ForcedHediffs == null) kindData.ForcedHediffs = new List<ForcedHediff>();

            var poolDef = new ForcedHediff()
            {
                HediffDef = null,
                PoolType = poolType,
                chance = 1f,
                maxPartsRange = new IntRange(1, 1),
                severityRange = new FloatRange(0.5f, 1f)
            };

            kindData.ForcedHediffs.Add(poolDef);
            UndoManager.RecordState(kindData);
            FactionGearEditor.MarkDirty();
        }

        private static void AddItemPool(KindGearData kindData, ItemPoolType poolType)
        {
            if (kindData.InventoryItems == null) kindData.InventoryItems = new List<SpecRequirementEdit>();
            kindData.InventoryItems.Add(new SpecRequirementEdit()
            {
                PoolType = poolType,
                SelectionMode = ApparelSelectionMode.AlwaysTake,
                SelectionChance = 1f,
                CountRange = new IntRange(1, 1)
            });
            UndoManager.RecordState(kindData);
            FactionGearEditor.MarkDirty();
        }

        private static void DrawAdvancedItems(Listing_Standard ui, KindGearData kindData)
        {
            WidgetsUtils.Label(ui, $"<b>{LanguageManager.Get("InventoryItems")}</b>");
            Rect buttonRow = ui.GetRect(28f);
            Rect addButtonRect = new Rect(buttonRow.x, buttonRow.y, buttonRow.width - 175f, buttonRow.height);
            Rect quickRect = new Rect(buttonRow.x + buttonRow.width - 165f, buttonRow.y, 85f, buttonRow.height);
            Rect clearButtonRect = new Rect(buttonRow.xMax - 75f, buttonRow.y, 75f, buttonRow.height);

            if (Widgets.ButtonText(addButtonRect, LanguageManager.Get("AddNewItem")))
            {
                UndoManager.RecordState(kindData);
                if (kindData.InventoryItems == null) kindData.InventoryItems = new List<SpecRequirementEdit>();
                Find.WindowStack.Add(new Dialog_InventoryItemPicker(kindData.InventoryItems));
            }
            if (Widgets.ButtonText(quickRect, LanguageManager.Get("QuickAddPool")))
            {
                ShowQuickItemPoolMenu(kindData);
            }
            TooltipHandler.TipRegion(quickRect, LanguageManager.Get("QuickAddPoolTooltip"));

            if (Widgets.ButtonText(clearButtonRect, LanguageManager.Get("Clear"), true, false, !kindData.InventoryItems.NullOrEmpty()))
            {
                if (kindData.InventoryItems != null)
                {
                    UndoManager.RecordState(kindData);
                    kindData.InventoryItems.Clear();
                    kindData.isModified = true;
                    FactionGearEditor.MarkDirty();
                }
            }
            TooltipHandler.TipRegion(clearButtonRect, LanguageManager.Get("ClearInventoryTooltip"));

            if (!kindData.InventoryItems.NullOrEmpty())
            {
                for (int i = 0; i < kindData.InventoryItems.Count; i++)
                {
                    var item = kindData.InventoryItems[i];
                    int index = i;
                    ItemCardUI.Draw(ui, item, index, () =>
                    {
                        UndoManager.RecordState(kindData);
                        kindData.InventoryItems.RemoveAt(index);
                        kindData.isModified = true;
                        FactionGearEditor.MarkDirty();
                    });
                }
            }
        }

        private static void DrawAdvancedSkills(Listing_Standard ui, KindGearData kindData)
        {
            // Override toggle
            Rect overrideRow = ui.GetRect(28f);
            bool ov = kindData.ForceOverrideSkills;
            Widgets.CheckboxLabeled(new Rect(overrideRow.x, overrideRow.y, 160f, 28f), LanguageManager.Get("ForceOverrideSkills"), ref ov);
            if (ov != kindData.ForceOverrideSkills) { kindData.ForceOverrideSkills = ov; FactionGearEditor.MarkDirty(); }
            // Do NOT early-return; allow editing and visibility even when not enabled

            // Global random range
            Rect rangeRow = ui.GetRect(28f);
            Widgets.Label(new Rect(rangeRow.x, rangeRow.y, 120f, 28f), LanguageManager.Get("SkillRandomRange") + ": ±" + kindData.SkillRandomRange);
            int nr = Mathf.RoundToInt(Widgets.HorizontalSlider(new Rect(rangeRow.x + 130f, rangeRow.y, 120f, 28f), kindData.SkillRandomRange, 0f, 10f, false, null, "0", "10"));
            if (nr != kindData.SkillRandomRange) { kindData.SkillRandomRange = nr; FactionGearEditor.MarkDirty(); }
            ui.Gap(4f);

            // Stable vanilla skill order
            string[] orderedSkillNames =
            {
                "Shooting", "Melee", "Construction", "Mining", "Cooking",
                "Plants", "Animals", "Crafting", "Artistic", "Medical", "Social", "Intellectual"
            };

            // Init skills using stable vanilla skill names in a fixed order, with robust Medical fallback
            if (kindData.ForcedSkills == null)
                kindData.ForcedSkills = new List<ForcedSkill>();
            if (kindData.ForcedSkills.Count == 0)
            {
                foreach (var name in orderedSkillNames)
                {
                    SkillDef def = DefDatabase<SkillDef>.GetNamedSilentFail(name);
                    if (def == null && name == "Medical")
                    {
                        def = DefDatabase<SkillDef>.AllDefs.FirstOrDefault(d =>
                            (d.defName ?? "").Equals("Medical", System.StringComparison.OrdinalIgnoreCase) ||
                            (d.defName ?? "").Equals("Medicine", System.StringComparison.OrdinalIgnoreCase) ||
                            (d.defName ?? "").IndexOf("med", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (d.label ?? "").IndexOf("medical", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (d.label ?? "").IndexOf("medicine", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (d.LabelCap.ToString()).IndexOf("医", System.StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                    if (def != null)
                    {
                        kindData.ForcedSkills.Add(new ForcedSkill
                        {
                            SkillDef = def,
                            skillDefName = def.defName,
                            level = 0,
                            minLevel = 0,
                            maxLevel = 20,
                            chance = 1f
                        });
                    }
                }
            }
            // Ensure required vanilla skills exist even in old presets/saves

            foreach (var name in orderedSkillNames)
            {
                bool exists = kindData.ForcedSkills.Any(s =>
                    (s.skillDefName ?? "").Equals(name, System.StringComparison.OrdinalIgnoreCase) ||
                    (name == "Medical" && ((s.skillDefName ?? "").Equals("Medicine", System.StringComparison.OrdinalIgnoreCase) ||
                                            (s.skillDefName ?? "").Equals("Medical", System.StringComparison.OrdinalIgnoreCase))));
                if (!exists)
                {
                    SkillDef def = DefDatabase<SkillDef>.GetNamedSilentFail(name);
                    if (def == null && name == "Medical")
                    {
                        def = DefDatabase<SkillDef>.AllDefs.FirstOrDefault(d =>
                            (d.defName ?? "").Equals("Medical", System.StringComparison.OrdinalIgnoreCase) ||
                            (d.defName ?? "").Equals("Medicine", System.StringComparison.OrdinalIgnoreCase) ||
                            (d.defName ?? "").IndexOf("med", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (d.label ?? "").IndexOf("medical", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (d.label ?? "").IndexOf("medicine", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (d.LabelCap.ToString()).IndexOf("医", System.StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                    if (def != null)
                    {
                        kindData.ForcedSkills.Add(new ForcedSkill { SkillDef = def, skillDefName = def.defName, level = 0, minLevel = 0, maxLevel = 20, chance = 1f });
                    }
                }
            }

            // Stable display order without replacing the original list reference
            var orderMap = new System.Collections.Generic.Dictionary<string, int>
            {
                { "Shooting", 0 }, { "Melee", 1 }, { "Construction", 2 }, { "Mining", 3 }, { "Cooking", 4 },
                { "Plants", 5 }, { "Animals", 6 }, { "Crafting", 7 }, { "Artistic", 8 }, { "Medical", 9 },
                { "Medicine", 9 }, { "Social", 10 }, { "Intellectual", 11 }
            };
            var ordered = kindData.ForcedSkills
                .OrderBy(s => orderMap.TryGetValue(s.skillDefName ?? "", out var idx) ? idx : 999)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                try
                {
                    var item = ordered[i];
                    SkillCardUI.Draw(ui, item, i, () => { Find.WindowStack.Add(new Dialog_SkillEditor(item)); });
                }
                catch (System.Exception ex) { Log.Error("[FGC] Skill row " + i + " error: " + ex); }
            }
        }

        private static void DrawAdvancedTraits(Listing_Standard ui, KindGearData kindData)
        {
            WidgetsUtils.Label(ui, $"<b>{LanguageManager.Get("ForcedTraits")}</b>");
            Rect buttonRow = ui.GetRect(28f);
            Rect addButtonRect = new Rect(buttonRow.x, buttonRow.y, buttonRow.width - 205f, buttonRow.height);
            Rect overrideRect = new Rect(buttonRow.x + buttonRow.width - 200f, buttonRow.y, 120f, buttonRow.height);
            Rect clearButtonRect = new Rect(buttonRow.xMax - 75f, buttonRow.y, 75f, buttonRow.height);

            if (Widgets.ButtonText(addButtonRect, LanguageManager.Get("AddTrait")))
            {
                UndoManager.RecordState(kindData);
                if (kindData.ForcedTraits == null) kindData.ForcedTraits = new List<ForcedTrait>();
                Find.WindowStack.Add(new Dialog_TraitPicker(kindData.ForcedTraits));
            }

            bool overrideVal = kindData.ForceOverrideTraits;
            Widgets.CheckboxLabeled(overrideRect, LanguageManager.Get("ForceOverrideTraits"), ref overrideVal);
            if (overrideVal != kindData.ForceOverrideTraits)
            {
                kindData.ForceOverrideTraits = overrideVal;
                FactionGearEditor.MarkDirty();
            }

            if (Widgets.ButtonText(clearButtonRect, LanguageManager.Get("Clear"), true, false, !kindData.ForcedTraits.NullOrEmpty()))
            {
                if (kindData.ForcedTraits != null)
                {
                    UndoManager.RecordState(kindData);
                    kindData.ForcedTraits.Clear();
                    kindData.isModified = true;
                    FactionGearEditor.MarkDirty();
                }
            }
            TooltipHandler.TipRegion(clearButtonRect, LanguageManager.Get("ClearTraitsTooltip"));

            if (!kindData.ForcedTraits.NullOrEmpty())
            {
                for (int i = 0; i < kindData.ForcedTraits.Count; i++)
                {
                    var item = kindData.ForcedTraits[i];
                    int index = i;
                    TraitCardUI.Draw(ui, item, index, () =>
                    {
                        UndoManager.RecordState(kindData);
                        kindData.ForcedTraits.RemoveAt(index);
                        kindData.isModified = true;
                        FactionGearEditor.MarkDirty();
                    });
                }
            }
        }

        private static void DrawAdvancedAppearance(Listing_Standard ui, KindGearData kindData)
        {
            // Override toggle
            Rect ovRow = ui.GetRect(28f);
            bool ov = kindData.ForceOverrideAppearance;
            Widgets.CheckboxLabeled(new Rect(ovRow.x, ovRow.y, 160f, 28f), LanguageManager.Get("ForceOverrideAppearance"), ref ov);
            if (ov != kindData.ForceOverrideAppearance) { kindData.ForceOverrideAppearance = ov; FactionGearEditor.MarkDirty(); }

            Rect buttonRow = ui.GetRect(28f);
            Rect editBtnRect = new Rect(buttonRow.x, buttonRow.y, buttonRow.width - 80f, buttonRow.height);
            Rect clearBtnRect = new Rect(buttonRow.xMax - 75f, buttonRow.y, 75f, buttonRow.height);

            if (Widgets.ButtonText(editBtnRect, LanguageManager.Get("EditAppearance")))
            {
                UndoManager.RecordState(kindData);
                if (kindData.ForcedAppearance == null)
                    kindData.ForcedAppearance = new ForcedAppearance();
                Find.WindowStack.Add(new Dialog_AppearanceEditor(kindData.ForcedAppearance));
            }

            if (Widgets.ButtonText(clearBtnRect, LanguageManager.Get("Clear"), true, false, kindData.ForcedAppearance != null))
            {
                UndoManager.RecordState(kindData);
                kindData.ForcedAppearance = null;
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
            }

            // Show summary if appearance is configured
            if (kindData.ForcedAppearance != null)
            {
                if (!kindData.ForceOverrideAppearance)
                {
                    GUI.color = Color.gray;
                    Widgets.Label(ui.GetRect(22f), "  (Configured but not enabled)");
                    GUI.color = Color.white;
                }
                ui.Gap(6f);
                var app = kindData.ForcedAppearance;
                if (app.HairDef != null)
                    Widgets.Label(ui.GetRect(22f), "  Hair: " + app.HairDef.LabelCap);
                if (app.BeardDef != null)
                    Widgets.Label(ui.GetRect(22f), "  Beard: " + app.BeardDef.LabelCap);
                if (app.BodyTypeDef != null)
                    Widgets.Label(ui.GetRect(22f), "  Body: " + app.BodyTypeDef.LabelCap);
                if (app.HeadTypeDef != null)
                    Widgets.Label(ui.GetRect(22f), "  Head: " + app.HeadTypeDef.LabelCap);
                if (app.skinColor.HasValue)
                {
                    Rect colorRow = ui.GetRect(22f);
                    Widgets.Label(new Rect(colorRow.x, colorRow.y, 80f, 22f), "  Skin Color:");
                    Widgets.DrawBoxSolid(new Rect(colorRow.x + 80f, colorRow.y + 2f, 30f, 18f), app.skinColor.Value);
                }
                Widgets.Label(ui.GetRect(22f), "  Chance: " + (app.chance * 100f).ToString("F0") + "%");
            }
        }

        private static void DrawAdvancedGenes(Listing_Standard ui, KindGearData kindData)
        {
            WidgetsUtils.Label(ui, $"<b>{LanguageManager.Get("ForcedGenes")}</b>");
            Rect buttonRow = ui.GetRect(28f);
            Rect addButtonRect = new Rect(buttonRow.x, buttonRow.y, buttonRow.width - 160f, buttonRow.height);
            Rect overrideRect = new Rect(buttonRow.x + buttonRow.width - 155f, buttonRow.y, 80f, buttonRow.height);
            Rect clearButtonRect = new Rect(buttonRow.xMax - 75f, buttonRow.y, 75f, buttonRow.height);

            if (Widgets.ButtonText(addButtonRect, LanguageManager.Get("AddGene")))
            {
                UndoManager.RecordState(kindData);
                if (kindData.ForcedGenes == null) kindData.ForcedGenes = new List<ForcedGene>();
                Find.WindowStack.Add(new Dialog_GenePicker(kindData.ForcedGenes));
            }

            bool overrideVal = kindData.ForceOverrideGenes;
            Widgets.CheckboxLabeled(overrideRect, LanguageManager.Get("ForceOverrideGenes"), ref overrideVal);
            if (overrideVal != kindData.ForceOverrideGenes)
            {
                kindData.ForceOverrideGenes = overrideVal;
                FactionGearEditor.MarkDirty();
            }

            if (Widgets.ButtonText(clearButtonRect, LanguageManager.Get("Clear"), true, false, !kindData.ForcedGenes.NullOrEmpty()))
            {
                if (kindData.ForcedGenes != null)
                {
                    UndoManager.RecordState(kindData);
                    kindData.ForcedGenes.Clear();
                    kindData.isModified = true;
                    FactionGearEditor.MarkDirty();
                }
            }
            TooltipHandler.TipRegion(clearButtonRect, LanguageManager.Get("ClearGenesTooltip"));

            if (!kindData.ForcedGenes.NullOrEmpty())
            {
                for (int i = 0; i < kindData.ForcedGenes.Count; i++)
                {
                    var item = kindData.ForcedGenes[i];
                    int index = i;
                    GeneCardUI.Draw(ui, item, index, () =>
                    {
                        UndoManager.RecordState(kindData);
                        kindData.ForcedGenes.RemoveAt(index);
                        kindData.isModified = true;
                        FactionGearEditor.MarkDirty();
                    });
                }
            }
        }

        private static void ShowQuickItemPoolMenu(KindGearData kindData)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            options.Add(new FloatMenuOption(LanguageManager.Get("Pool_AnyFood"), () => AddItemPool(kindData, ItemPoolType.AnyFood)));
            options.Add(new FloatMenuOption(LanguageManager.Get("Pool_AnyMeal"), () => AddItemPool(kindData, ItemPoolType.AnyMeal)));
            options.Add(new FloatMenuOption(LanguageManager.Get("Pool_AnyRawFood"), () => AddItemPool(kindData, ItemPoolType.AnyRawFood)));
            options.Add(new FloatMenuOption(LanguageManager.Get("Pool_AnyMeat"), () => AddItemPool(kindData, ItemPoolType.AnyMeat)));
            options.Add(new FloatMenuOption(LanguageManager.Get("Pool_AnyVegetable"), () => AddItemPool(kindData, ItemPoolType.AnyVegetable)));
            options.Add(new FloatMenuOption(LanguageManager.Get("Pool_AnyMedicine"), () => AddItemPool(kindData, ItemPoolType.AnyMedicine)));
            options.Add(new FloatMenuOption(LanguageManager.Get("Pool_AnySocialDrug"), () => AddItemPool(kindData, ItemPoolType.AnySocialDrug)));
            options.Add(new FloatMenuOption(LanguageManager.Get("Pool_AnyHardDrug"), () => AddItemPool(kindData, ItemPoolType.AnyHardDrug)));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void DrawOverrideEnum<T>(Listing_Standard ui, string label, T? currentValue, Action<T?> setValue, Func<T, string> labelFormatter = null) where T : struct
        {
            Rect rect = ui.GetRect(28f);
            Widgets.Label(rect.LeftHalf(), label + ":");

            string GetLabel(T val) => labelFormatter != null ? labelFormatter(val) : val.ToString();

            string btnLabel = currentValue.HasValue ? GetLabel(currentValue.Value) : LanguageManager.Get("Default");
            if (Widgets.ButtonText(rect.RightHalf(), btnLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption(LanguageManager.Get("Default"), () => setValue(null)));
                foreach (T val in Enum.GetValues(typeof(T)))
                {
                    options.Add(new FloatMenuOption(GetLabel(val), () => setValue(val)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void DrawOverrideEnumWithTooltip<T>(Listing_Standard ui, string label, T? currentValue, Action<T?> setValue, Func<T, string> labelFormatter, string tooltip) where T : struct
        {
            Rect rect = ui.GetRect(28f);
            Widgets.Label(rect.LeftHalf(), label + ":");
            TooltipHandler.TipRegion(rect, tooltip);

            string GetLabel(T val) => labelFormatter != null ? labelFormatter(val) : val.ToString();

            string btnLabel = currentValue.HasValue ? GetLabel(currentValue.Value) : LanguageManager.Get("Default");
            if (Widgets.ButtonText(rect.RightHalf(), btnLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption(LanguageManager.Get("Default"), () => setValue(null)));
                foreach (T val in Enum.GetValues(typeof(T)))
                {
                    options.Add(new FloatMenuOption(GetLabel(val), () => setValue(val)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void DrawTechLevelLimit(Listing_Standard ui, KindGearData kindData)
        {
            Rect rect = ui.GetRect(28f);
            Widgets.Label(rect.LeftHalf(), LanguageManager.Get("TechLevelLimit") + ":");

            // 使用反射获取 defaultFactionType 字段
            if (defaultFactionTypeField == null)
            {
                defaultFactionTypeField = typeof(PawnKindDef).GetField("defaultFactionType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }

            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindData.kindDefName);
            FactionDef factionDef = null;
            if (defaultFactionTypeField != null && kindDef != null)
            {
                factionDef = defaultFactionTypeField.GetValue(kindDef) as FactionDef;
            }
            TechLevel defaultTechLevel = factionDef?.techLevel ?? TechLevel.Undefined;

            string btnLabel;
            if (kindData.TechLevelLimit.HasValue)
            {
                btnLabel = ("TechLevel_" + kindData.TechLevelLimit.Value).Translate().ToString();
            }
            else if (defaultTechLevel != TechLevel.Undefined)
            {
                btnLabel = LanguageManager.Get("Default") + " (" + ("TechLevel_" + defaultTechLevel).Translate() + ")";
            }
            else
            {
                btnLabel = LanguageManager.Get("Default");
            }

            if (Widgets.ButtonText(rect.RightHalf(), btnLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                string defaultLabel = LanguageManager.Get("Default");
                if (defaultTechLevel != TechLevel.Undefined)
                {
                    defaultLabel += " (" + ("TechLevel_" + defaultTechLevel).Translate() + ")";
                }
                options.Add(new FloatMenuOption(defaultLabel, () =>
                {
                    UndoManager.RecordState(kindData);
                    kindData.TechLevelLimit = null;
                    FactionGearEditor.MarkDirty();
                }));
                foreach (TechLevel level in Enum.GetValues(typeof(TechLevel)))
                {
                    if (level == TechLevel.Undefined) continue;
                    TechLevel captured = level;
                    string label = ("TechLevel_" + captured).Translate();
                    options.Add(new FloatMenuOption(label, () =>
                    {
                        UndoManager.RecordState(kindData);
                        kindData.TechLevelLimit = captured;
                        FactionGearEditor.MarkDirty();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void DrawTechLevelLimitWithTooltip(Listing_Standard ui, KindGearData kindData, string tooltip)
        {
            Rect rect = ui.GetRect(28f);
            Widgets.Label(rect.LeftHalf(), LanguageManager.Get("TechLevelLimit") + ":");
            TooltipHandler.TipRegion(rect, tooltip);

            // 使用反射获取 defaultFactionType 字段
            if (defaultFactionTypeField == null)
            {
                defaultFactionTypeField = typeof(PawnKindDef).GetField("defaultFactionType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            }

            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindData.kindDefName);
            FactionDef factionDef = null;
            if (defaultFactionTypeField != null && kindDef != null)
            {
                factionDef = defaultFactionTypeField.GetValue(kindDef) as FactionDef;
            }
            TechLevel defaultTechLevel = factionDef?.techLevel ?? TechLevel.Undefined;

            string btnLabel;
            if (kindData.TechLevelLimit.HasValue)
            {
                btnLabel = ("TechLevel_" + kindData.TechLevelLimit.Value).Translate().ToString();
            }
            else if (defaultTechLevel != TechLevel.Undefined)
            {
                btnLabel = LanguageManager.Get("Default") + " (" + ("TechLevel_" + defaultTechLevel).Translate() + ")";
            }
            else
            {
                btnLabel = LanguageManager.Get("Default");
            }

            if (Widgets.ButtonText(rect.RightHalf(), btnLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                string defaultLabel = LanguageManager.Get("Default");
                if (defaultTechLevel != TechLevel.Undefined)
                {
                    defaultLabel += " (" + ("TechLevel_" + defaultTechLevel).Translate() + ")";
                }
                options.Add(new FloatMenuOption(defaultLabel, () =>
                {
                    UndoManager.RecordState(kindData);
                    kindData.TechLevelLimit = null;
                    FactionGearEditor.MarkDirty();
                }));
                foreach (TechLevel level in Enum.GetValues(typeof(TechLevel)))
                {
                    if (level == TechLevel.Undefined) continue;
                    TechLevel captured = level;
                    string label = ("TechLevel_" + captured).Translate();
                    options.Add(new FloatMenuOption(label, () =>
                    {
                        UndoManager.RecordState(kindData);
                        kindData.TechLevelLimit = captured;
                        FactionGearEditor.MarkDirty();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void DrawOverrideFloatRange(Listing_Standard ui, string label, ref FloatRange? range, Action<FloatRange?> setRange, bool forceIgnore = false)
        {
            Rect rect = ui.GetRect(28f);
            Widgets.Label(rect.LeftHalf(), label + ":");

            if (forceIgnore)
            {
                GUI.color = Color.gray;
                Widgets.Label(rect.RightHalf(), LanguageManager.Get("MaxForceIgnore"));
                GUI.color = Color.white;
                return;
            }

            if (range.HasValue)
            {
                if (Widgets.ButtonText(rect.RightHalf(), $"{range.Value.min}-{range.Value.max}"))
                {
                    setRange(null);
                    return;
                }
                FloatRange val = range.Value;
                FloatRange oldVal = val;
                WidgetsUtils.FloatRange(ui.GetRect(24f), label.GetHashCode(), ref val, 0f, 10000f);
                if (val != oldVal)
                {
                    setRange(val);
                    range = val;
                }
            }
            else
            {
                if (Widgets.ButtonText(rect.RightHalf(), LanguageManager.Get("Default")))
                {
                    var newRange = new FloatRange(0, 1000);
                    setRange(newRange);
                    range = newRange;
                }
            }
        }

        private static void DrawOverrideFloatRangeWithTooltip(Listing_Standard ui, string label, ref FloatRange? range, Action<FloatRange?> setRange, bool forceIgnore, string tooltip)
        {
            Rect rect = ui.GetRect(28f);
            Widgets.Label(rect.LeftHalf(), label + ":");
            TooltipHandler.TipRegion(rect, tooltip);

            if (forceIgnore)
            {
                GUI.color = Color.gray;
                Widgets.Label(rect.RightHalf(), LanguageManager.Get("MaxForceIgnore"));
                GUI.color = Color.white;
                return;
            }

            if (range.HasValue)
            {
                if (Widgets.ButtonText(rect.RightHalf(), $"{range.Value.min}-{range.Value.max}"))
                {
                    setRange(null);
                    return;
                }
                FloatRange val = range.Value;
                FloatRange oldVal = val;
                WidgetsUtils.FloatRange(ui.GetRect(24f), label.GetHashCode(), ref val, 0f, 10000f);
                if (val != oldVal)
                {
                    setRange(val);
                    range = val;
                }
            }
            else
            {
                if (Widgets.ButtonText(rect.RightHalf(), LanguageManager.Get("Default")))
                {
                    var newRange = new FloatRange(0, 1000);
                    setRange(newRange);
                    range = newRange;
                }
            }
        }

        private static string FormatBudgetRange(FloatRange range)
        {
            return $"{range.min:0.#}-{range.max:0.#}";
        }



        private static void DrawEmbeddedGearList(Listing_Standard ui, KindGearData kindData, params GearCategory[] allowedCategories)
        {
            Rect fullRect = ui.GetRect(24f);
            Rect tabRect = new Rect(fullRect.x, fullRect.y, fullRect.width - 75f, fullRect.height);
            Rect clearCatRect = new Rect(fullRect.xMax - 70f, fullRect.y, 70f, fullRect.height);
            float tabWidth = tabRect.width / allowedCategories.Length;
            for (int i = 0; i < allowedCategories.Length; i++)
            {
                var cat = allowedCategories[i];
                string label = cat.ToString();
                if (cat == GearCategory.Weapons) label = LanguageManager.Get("Ranged");
                else if (cat == GearCategory.MeleeWeapons) label = LanguageManager.Get("Melee");
                else if (cat == GearCategory.Armors) label = LanguageManager.Get("Armors");
                else if (cat == GearCategory.Apparel) label = LanguageManager.Get("Clothes");

                Rect catRect = new Rect(tabRect.x + tabWidth * i, tabRect.y, tabWidth, tabRect.height);
                bool isSelected = EditorSession.SelectedCategory == cat;
                if (isSelected) { Widgets.DrawHighlightSelected(catRect); }
                else { Widgets.DrawHighlightIfMouseover(catRect); }
                if (Widgets.ButtonInvisible(catRect))
                {
                    if (EditorSession.SelectedCategory != cat)
                    {
                        EditorSession.SelectedCategory = cat;
                        EditorSession.ExpandedGearItem = null;
                        EditorSession.SelectedAmmoSets.Clear();
                        EditorSession.CachedModSources = null;
                        FactionGearEditor.GetUniqueModSources();
                        FactionGearEditor.CalculateBounds();
                    }
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(catRect, label);
                Text.Anchor = TextAnchor.UpperLeft;
            }
            if (Widgets.ButtonText(clearCatRect, LanguageManager.Get("Clear")))
            {
                Dialog_ConfirmWithCheckbox.ShowIfNotDismissed(
                    "ClearCategoryGearConfirm",
                    LanguageManager.Get("ClearCategoryGearConfirmMessage"),
                    delegate { ClearCategoryGear(kindData, EditorSession.SelectedCategory); },
                    LanguageManager.Get("Clear"),
                    LanguageManager.Get("Cancel"),
                    null,
                    true
                );
            }
            ui.Gap(5f);
            var gearItems = GetCurrentCategoryGear(kindData);
            if (gearItems.Any())
            {
                foreach (var gearItem in gearItems.ToList())
                {
                    bool isExpanded = (gearItem == EditorSession.ExpandedGearItem);
                    Rect rowRect = ui.GetRect(isExpanded ? 56f : 28f);
                    DrawGearItem(rowRect, gearItem, kindData, isExpanded);
                    ui.Gap(2f);
                }
            }

            List<SpecRequirementEdit> advancedItems = new List<SpecRequirementEdit>();
            if (EditorSession.SelectedCategory == GearCategory.Weapons && !kindData.SpecificWeapons.NullOrEmpty())
            {
                advancedItems.AddRange(kindData.SpecificWeapons.Where(x => x.Thing != null && x.Thing.IsRangedWeapon));
            }
            else if (EditorSession.SelectedCategory == GearCategory.MeleeWeapons && !kindData.SpecificWeapons.NullOrEmpty())
            {
                advancedItems.AddRange(kindData.SpecificWeapons.Where(x => x.Thing != null && x.Thing.IsMeleeWeapon));
            }
            else if (EditorSession.SelectedCategory == GearCategory.Others && !kindData.SpecificApparel.NullOrEmpty())
            {
                advancedItems.AddRange(kindData.SpecificApparel.Where(x => x.Thing != null && IsBelt(x.Thing)));
            }
            else if (EditorSession.SelectedCategory == GearCategory.Armors && !kindData.SpecificApparel.NullOrEmpty())
            {
                advancedItems.AddRange(kindData.SpecificApparel.Where(x => x.Thing != null && IsArmor(x.Thing)));
            }
            else if (EditorSession.SelectedCategory == GearCategory.Apparel && !kindData.SpecificApparel.NullOrEmpty())
            {
                advancedItems.AddRange(kindData.SpecificApparel.Where(x => x.Thing != null && IsApparel(x.Thing)));
            }

            if (advancedItems.Any())
            {
                if (gearItems.Any()) ui.GapLine();
                WidgetsUtils.Label(ui, $"<b>{LanguageManager.Get("AdvancedSpecificItems")}</b>");
                foreach (var advItem in advancedItems)
                {
                    bool isAdvExpanded = (advItem == EditorSession.ExpandedSpecItem);
                    Rect rowRect = ui.GetRect(isAdvExpanded ? 56f : 28f);
                    DrawAdvancedItemSimple(rowRect, advItem, kindData);
                    ui.Gap(2f);
                }
            }
            else if (!gearItems.Any())
            {
                WidgetsUtils.Label(ui, LanguageManager.Get("NoItemsInThisCategory"));
            }
            ui.GapLine();
        }

        private static void DrawGearItem(Rect rect, GearItem gearItem, KindGearData kindData, bool isExpanded)
        {
            var thingDef = gearItem.ThingDef;
            if (thingDef == null) return;
            Widgets.DrawHighlightIfMouseover(rect);
            if (gearItem.weight != 1f)
            {
                GUI.color = new Color(1f, 0.8f, 0.4f, 0.3f);
                Widgets.DrawHighlight(rect);
                GUI.color = Color.white;
            }
            Rect row1 = new Rect(rect.x, rect.y, rect.width, 28f);
            Rect iconRect = new Rect(row1.x + 4f, row1.y + (row1.height - 30f) / 2f, 30f, 30f);
            float infoButtonOffset = EditorSession.IsInGame ? 28f : 0f;
            Rect infoButtonRect = new Rect(iconRect.xMax + 4f, row1.y + (row1.height - 24f) / 2f, 24f, 24f);
            Rect removeRect = new Rect(row1.xMax - 26f, row1.y + (row1.height - 24f) / 2f, 24f, 24f);
            Rect nameRect = new Rect(infoButtonRect.xMax + 4f, row1.y, removeRect.x - infoButtonRect.xMax - 8f - infoButtonOffset, 28f);

            if (EditorSession.IsInGame)
            {
                Widgets.InfoCardButton(infoButtonRect.x, infoButtonRect.y, thingDef);
            }
            Rect interactRect = new Rect(row1.x, row1.y, row1.width - 30f, 28f);
            if (Widgets.ButtonInvisible(interactRect))
            {
                EditorSession.ExpandedGearItem = (EditorSession.ExpandedGearItem == gearItem) ? null : gearItem;
            }
            Texture2D icon = FactionGearEditor.GetIconWithLazyLoading(thingDef);
            if (icon != null) WidgetsUtils.DrawTextureFitted(iconRect, icon, 1f);
            else if (thingDef.graphic?.MatSingle?.mainTexture != null) WidgetsUtils.DrawTextureFitted(iconRect, thingDef.graphic.MatSingle.mainTexture, 1f);

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            float nameWidth = nameRect.width;
            if (!isExpanded && gearItem.weight != 1f) nameWidth -= 45f;
            Widgets.Label(new Rect(nameRect.x, nameRect.y, nameWidth, nameRect.height), thingDef.LabelCap);
            Text.WordWrap = true;
            if (!isExpanded && gearItem.weight != 1f)
            {
                Text.Anchor = TextAnchor.MiddleRight;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(nameRect.xMax - 45f, row1.y, 45f, 28f), $"{LanguageManager.Get("Weight")}:{gearItem.weight:F1}");
                GUI.color = Color.white;
            }
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonImage(removeRect, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
            {
                UndoManager.RecordState(kindData);
                var removedDef = gearItem.ThingDef;
                GetCurrentCategoryGear(kindData).Remove(gearItem);
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
                if (removedDef != null && removedDef.IsRangedWeapon)
                    TryRemoveCEAmmoForWeapon(kindData, removedDef);
                if (EditorSession.ExpandedGearItem == gearItem) EditorSession.ExpandedGearItem = null;
            }
            if (isExpanded)
            {
                Rect row2 = new Rect(rect.x, rect.y + 28f, rect.width, 28f);
                Rect sliderRect = new Rect(row2.x + 10f, row2.y + 4f, row2.width - 60f, 20f);
                Rect weightRect = new Rect(row2.xMax - 40f, row2.y, 35f, 28f);
                if (Event.current.type == EventType.MouseDown && sliderRect.Contains(Event.current.mousePosition)) UndoManager.RecordState(kindData);
                float newWeight = Widgets.HorizontalSlider(sliderRect, gearItem.weight, 0.1f, 10f, true);
                if (newWeight != gearItem.weight)
                {
                    gearItem.weight = newWeight;
                    kindData.isModified = true;
                    FactionGearEditor.MarkDirty();
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(weightRect, gearItem.weight.ToString("F1"));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            TooltipHandler.TipRegion(rect, new TipSignal(() => FactionGearEditor.GetDetailedItemTooltip(thingDef, kindData), thingDef.GetHashCode()));
        }

        private static void DrawLayerPreview(Rect rect, KindGearData kindData)
        {
            if (kindData == null) return;
            List<ThingDef> apparels = new List<ThingDef>();
            foreach (var item in kindData.apparel) if (item.ThingDef != null) apparels.Add(item.ThingDef);
            foreach (var item in kindData.armors) if (item.ThingDef != null) apparels.Add(item.ThingDef);
            foreach (var item in kindData.others) if (item.ThingDef != null) apparels.Add(item.ThingDef);

            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(4f);
            float lineHeight = 24f; // Increased height for better visibility
            float y = inner.y;

            // Header
            Rect headerRect = new Rect(inner.x, y, inner.width, lineHeight);
            DrawCompactLayerHeader(headerRect);
            y += lineHeight;

            // Draw divider
            Widgets.DrawLineHorizontal(inner.x, y, inner.width);
            y += 2f;

            int rowIndex = 0;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Head"), BodyPartGroupDefOf.FullHead, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Torso"), BodyPartGroupDefOf.Torso, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Shoulders"), GroupShoulders, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Arms"), GroupArms, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Hands"), GroupHands, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Waist"), GroupWaist, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Legs"), GroupLegs, apparels, rowIndex++); y += lineHeight;
            DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), LanguageManager.Get("Feet"), GroupFeet, apparels, rowIndex++); y += lineHeight;
        }

        private static void DrawCompactLayerHeader(Rect rect)
        {
            Widgets.DrawHighlight(rect);

            float labelWidth = 70f; // Width for the "Part" label column
            float x = rect.x + labelWidth;
            float width = (rect.width - labelWidth) / 5f;

            // Draw column headers
            DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), LanguageManager.Get("Skin")); x += width;
            DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), LanguageManager.Get("Mid")); x += width;
            DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), LanguageManager.Get("Shell")); x += width;
            DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), LanguageManager.Get("Belt")); x += width;
            DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), LanguageManager.Get("Over")); x += width;
        }

        private static void DrawLayerHeaderLabel(Rect rect, string label)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.gray;
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static void DrawCompactLayerRow(Rect rect, string label, BodyPartGroupDef group, List<ThingDef> apparels, int rowIndex)
        {
            if (group == null) return;

            // Use standard RimWorld alternating row background
            if (rowIndex % 2 == 1)
            {
                Widgets.DrawAltRect(rect);
            }

            // Draw row label (Body Part)
            float labelWidth = 70f;
            Rect labelRect = new Rect(rect.x + 4f, rect.y, labelWidth - 4f, rect.height);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            // Draw vertical line after label
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineVertical(rect.x + labelWidth, rect.y, rect.height);
            GUI.color = Color.white;

            float x = rect.x + labelWidth;
            float width = (rect.width - labelWidth) / 5f;

            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.OnSkin, apparels); x += width;
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.Middle, apparels); x += width;
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.Shell, apparels); x += width;
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.Belt, apparels); x += width;
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.Overhead, apparels); x += width;
        }

        private static void DrawCompactLayerCell(Rect rect, BodyPartGroupDef group, ApparelLayerDef layer, List<ThingDef> apparels)
        {
            if (layer == null) return;

            // Draw subtle grid borders
            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            Widgets.DrawBox(rect);
            GUI.color = Color.white;

            var coveringApparel = apparels.FirstOrDefault(a => a.apparel != null && a.apparel.bodyPartGroups.Contains(group) && a.apparel.layers.Contains(layer));

            if (coveringApparel != null)
            {
                Rect inner = rect.ContractedBy(2f);

                // Draw a subtle background for occupied slots
                GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                Widgets.DrawBoxSolid(inner, Color.white);
                GUI.color = Color.white;

                Texture2D icon = coveringApparel.uiIcon;
                if (icon != null)
                {
                    // Use DrawTextureFitted for better aspect ratio
                    Widgets.DrawTextureFitted(inner, icon, 1f);
                }

                // Enhanced Tooltip
                TooltipHandler.TipRegion(rect, new TipSignal(() =>
                {
                    string text = coveringApparel.LabelCap;
                    float armorSharp = coveringApparel.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp);
                    float armorBlunt = coveringApparel.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt);
                    if (armorSharp > 0 || armorBlunt > 0)
                    {
                        text += $"\nSharp: {armorSharp:P0}\nBlunt: {armorBlunt:P0}";
                    }
                    return text;
                }, coveringApparel.GetHashCode()));

                // Interaction: Click to show info card
                if (Widgets.ButtonInvisible(rect))
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(coveringApparel));
                }

                Widgets.DrawHighlightIfMouseover(rect);
            }
        }

        private static bool IsBelt(ThingDef t) => t.apparel?.layers?.Contains(ApparelLayerDefOf.Belt) ?? false;

        private static bool IsArmor(ThingDef t)
        {
            if (t.apparel == null) return false;
            if (IsBelt(t)) return false;
            return t.apparel.layers.Contains(ApparelLayerDefOf.Shell) || t.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) > 0.4f;
        }

        private static bool IsApparel(ThingDef t)
        {
            if (t.apparel == null) return false;
            if (IsBelt(t)) return false;
            if (IsArmor(t)) return false;
            return true;
        }

        private static void DrawAdvancedItemSimple(Rect rect, SpecRequirementEdit item, KindGearData kindData)
        {
            if (item.Thing == null) return;

            bool isExpanded = (item == EditorSession.ExpandedSpecItem);
            Widgets.DrawHighlightIfMouseover(rect);
            if (item.weight != 1f)
            {
                GUI.color = new Color(1f, 0.8f, 0.4f, 0.3f);
                Widgets.DrawHighlight(rect);
                GUI.color = Color.white;
            }

            Rect row1 = new Rect(rect.x, rect.y, rect.width, 28f);
            Rect iconRect = new Rect(row1.x + 4f, row1.y + (row1.height - 24f) / 2f, 24f, 24f);
            float infoButtonOffset = EditorSession.IsInGame ? 28f : 0f;
            Rect infoButtonRect = new Rect(iconRect.xMax + 4f, row1.y + (row1.height - 24f) / 2f, 24f, 24f);
            Rect removeRect = new Rect(row1.xMax - 26f, row1.y + (row1.height - 24f) / 2f, 24f, 24f);
            Rect nameRect = new Rect(infoButtonRect.xMax + 4f, row1.y, removeRect.x - infoButtonRect.xMax - 8f - infoButtonOffset, 28f);

            if (EditorSession.IsInGame)
            {
                Widgets.InfoCardButton(infoButtonRect.x, infoButtonRect.y, item.Thing);
            }

            Texture2D icon = FactionGearEditor.GetIconWithLazyLoading(item.Thing);
            if (icon != null) WidgetsUtils.DrawTextureFitted(iconRect, icon, 1f);

            Rect interactRect = new Rect(row1.x, row1.y, row1.width - 30f, 28f);
            if (Widgets.ButtonInvisible(interactRect))
            {
                EditorSession.ExpandedSpecItem = (EditorSession.ExpandedSpecItem == item) ? null : item;
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            float nameWidth = nameRect.width;
            if (!isExpanded && item.weight != 1f) nameWidth -= 45f;
            Widgets.Label(new Rect(nameRect.x, nameRect.y, nameWidth, nameRect.height), item.Thing.LabelCap);
            Text.WordWrap = true;
            if (!isExpanded && item.weight != 1f)
            {
                Text.Anchor = TextAnchor.MiddleRight;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(nameRect.xMax - 45f, row1.y, 45f, 28f), $"{LanguageManager.Get("Weight")}:{item.weight:F1}");
                GUI.color = Color.white;
            }
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonImage(removeRect, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
            {
                UndoManager.RecordState(kindData);
                if (kindData.SpecificApparel != null && kindData.SpecificApparel.Contains(item))
                    kindData.SpecificApparel.Remove(item);
                else if (kindData.SpecificWeapons != null && kindData.SpecificWeapons.Contains(item))
                {
                    kindData.SpecificWeapons.Remove(item);
                    TryRemoveCEAmmoForWeapon(kindData, item.Thing);
                }

                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
                if (EditorSession.ExpandedSpecItem == item) EditorSession.ExpandedSpecItem = null;
            }

            if (isExpanded)
            {
                Rect row2 = new Rect(rect.x, rect.y + 28f, rect.width, 28f);
                Rect sliderRect = new Rect(row2.x + 10f, row2.y + 4f, row2.width - 60f, 20f);
                Rect weightRect = new Rect(row2.xMax - 40f, row2.y, 35f, 28f);
                if (Event.current.type == EventType.MouseDown && sliderRect.Contains(Event.current.mousePosition)) UndoManager.RecordState(kindData);
                float newWeight = Widgets.HorizontalSlider(sliderRect, item.weight, 0.1f, 10f, true);
                if (newWeight != item.weight)
                {
                    item.weight = newWeight;
                    kindData.isModified = true;
                    FactionGearEditor.MarkDirty();
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(weightRect, item.weight.ToString("F1"));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            TooltipHandler.TipRegion(rect, LanguageManager.Get("AdvancedItemTooltip"));
        }

        private static void TryRemoveCEAmmoForWeapon(KindGearData kindData, ThingDef weaponDef)
        {
            if (kindData == null) return;
            if (!CECompat.IsCEActive) return;
            if (weaponDef == null || !weaponDef.IsRangedWeapon) return;
            if (kindData.InventoryItems.NullOrEmpty()) return;

            var ammoDef = CECompat.GetDefaultAmmoFor(weaponDef);
            if (ammoDef == null) return;

            kindData.InventoryItems.RemoveAll(x => x?.Thing == ammoDef);
        }

        private static void HandlePreviewButtonClick()
        {
            if (string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
                return;

            var fDef = DefDatabase<FactionDef>.GetNamedSilentFail(EditorSession.SelectedFactionDefName);
            if (fDef == null)
                return;

            if (Find.FactionManager.FirstFactionOfDef(fDef) == null)
            {
                Messages.Message(LanguageManager.Get("CannotPreviewFactionNotPresent"), MessageTypeDefOf.RejectInput, false);
                return;
            }

            var kinds = FactionGearEditor.GetFactionKinds(fDef);

            if (FactionGearEditor.IsDirty && !FactionGearCustomizerMod.Settings.autoSaveBeforePreview)
            {
                var dialog = Dialog_ConfirmWithCheckbox.CreateWithAutoSaveOption(
                    LanguageManager.Get("ConfirmSaveBeforePreview"),
                    LanguageManager.Get("AutoSaveBeforePreview"),
                    onSaveAndConfirm: () =>
                    {
                        FactionGearEditor.SaveChanges();
                        Find.WindowStack.Add(new FactionGearPreviewWindow(kinds, fDef));
                    },
                    onConfirmOnly: () =>
                    {
                        Find.WindowStack.Add(new FactionGearPreviewWindow(kinds, fDef));
                    },
                    onCheckboxChanged: (isChecked) =>
                    {
                        FactionGearCustomizerMod.Settings.autoSaveBeforePreview = isChecked;
                        FactionGearCustomizerMod.Settings.Write();
                    }
                );
                Find.WindowStack.Add(dialog);
            }
            else
            {
                if (FactionGearEditor.IsDirty && FactionGearCustomizerMod.Settings.autoSaveBeforePreview)
                {
                    FactionGearEditor.SaveChanges();
                }
                Find.WindowStack.Add(new FactionGearPreviewWindow(kinds, fDef));
            }
        }

        private static void HandleRaidButtonClick()
        {
            if (Current.Game == null) return;

            // Resolve selected faction — spawn if needed
            Faction faction = null;
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
            {
                var fDef = DefDatabase<FactionDef>.GetNamedSilentFail(EditorSession.SelectedFactionDefName);
                if (fDef != null)
                {
                    faction = Find.FactionManager.FirstFactionOfDef(fDef);
                    if (faction == null)
                    {
                        faction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def == fDef && !f.IsPlayer);
                    }
                    if (faction == null)
                    {
                        faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(fDef));
                        faction.Name = NameGenerator.GenerateName(fDef.factionNameMaker,
                            from f in Find.FactionManager.AllFactionsVisible select f.Name, false, null);
                        Find.FactionManager.Add(faction);
                        foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
                        {
                            if (other != faction)
                                faction.TryMakeInitialRelationsWith(other);
                        }
                    }
                }
            }

            // Close faction editor windows
            foreach (var window in Find.WindowStack.Windows.ToList())
            {
                if (window is FactionGearSettingsWindow || window is FactionGearMainTabWindow)
                {
                    window.Close();
                }
            }

            Find.WindowStack.Add(new Dialog_RaidConfig(faction));
        }
    }
}
