using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Panels
{
    public static class GearEditPanel
    {
        public static readonly BodyPartGroupDef GroupShoulders = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Shoulders");
        public static readonly BodyPartGroupDef GroupArms = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Arms");
        public static readonly BodyPartGroupDef GroupHands = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Hands");
        public static readonly BodyPartGroupDef GroupLegs = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Legs");
        public static readonly BodyPartGroupDef GroupFeet = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("Feet");
        public static void Draw(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            // Mode Toggle
            Rect headerRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 30f);
            
            bool isAdvanced = EditorSession.CurrentMode == EditorMode.Advanced;
            Widgets.CheckboxLabeled(new Rect(headerRect.x, headerRect.y, 130f, 24f), "Advanced", ref isAdvanced);
            if (isAdvanced != (EditorSession.CurrentMode == EditorMode.Advanced))
            {
                EditorSession.CurrentMode = isAdvanced ? EditorMode.Advanced : EditorMode.Simple;
            }

            // Undo/Redo Buttons
            float undoX = headerRect.x + 140f;
            Rect undoRect = new Rect(undoX, headerRect.y, 24f, 24f);
            
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
                        UndoManager.Undo(kData);
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
                        UndoManager.Undo(kData);
                        FactionGearEditor.MarkDirty();
                    }
                }
            }
            TooltipHandler.TipRegion(undoRect, "Undo (Ctrl+Z)");

            Rect redoRect = new Rect(undoX + 28f, headerRect.y, 24f, 24f);
            
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
                        UndoManager.Redo(kData);
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
                        UndoManager.Redo(kData);
                        FactionGearEditor.MarkDirty();
                    }
                }
            }
            TooltipHandler.TipRegion(redoRect, "Redo (Ctrl+Y)");
            
            // Handle Keyboard Shortcuts
            if (Event.current.type == EventType.KeyDown && Event.current.control)
            {
                if (Event.current.keyCode == KeyCode.Z)
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        var kData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                        UndoManager.Undo(kData);
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
                        UndoManager.Redo(kData);
                        FactionGearEditor.MarkDirty();
                        Event.current.Use();
                    }
                }
            }

            // Preview Button
            if (Current.ProgramState == ProgramState.Playing)
            {
                Rect previewButtonRect = new Rect(headerRect.xMax - 80f, headerRect.y, 80f, 24f);
                if (Widgets.ButtonText(previewButtonRect, "Preview"))
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                    {
                        var fDef = DefDatabase<FactionDef>.GetNamedSilentFail(EditorSession.SelectedFactionDefName);
                        var kDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(EditorSession.SelectedKindDefName);
                        if (fDef != null && kDef != null)
                        {
                            if (Find.FactionManager.FirstFactionOfDef(fDef) != null)
                            {
                                Find.WindowStack.Add(new FactionGearPreviewWindow(kDef, fDef));
                            }
                            else
                            {
                                Messages.Message("Cannot preview: Faction not present in current game.", MessageTypeDefOf.RejectInput, false);
                            }
                        }
                    }
                }
                TooltipHandler.TipRegion(previewButtonRect, "Preview generated pawn with current settings (In-game only)");

                Rect previewAllRect = new Rect(previewButtonRect.x - 85f, headerRect.y, 80f, 24f);
                if (Widgets.ButtonText(previewAllRect, "Preview All"))
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
                    {
                        var fDef = DefDatabase<FactionDef>.GetNamedSilentFail(EditorSession.SelectedFactionDefName);
                        if (fDef != null)
                        {
                            if (Find.FactionManager.FirstFactionOfDef(fDef) != null)
                            {
                                var kinds = FactionGearEditor.GetFactionKinds(fDef);
                                Find.WindowStack.Add(new FactionGearPreviewWindow(kinds, fDef));
                            }
                            else
                            {
                                Messages.Message("Cannot preview: Faction not present in current game.", MessageTypeDefOf.RejectInput, false);
                            }
                        }
                    }
                }
                TooltipHandler.TipRegion(previewAllRect, "Preview ALL kinds in this faction (In-game only)");
            }

            Rect contentRect = new Rect(innerRect.x, innerRect.y + 35f, innerRect.width, innerRect.height - 35f);

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
            List<GearItem> gearItemsToDraw = new List<GearItem>();
            KindGearData currentKindData = null;
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                currentKindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                gearItemsToDraw = GetCurrentCategoryGear(currentKindData);
            }

            Widgets.Label(new Rect(innerRect.x, innerRect.y, 120f, 24f), "Selected Gear");

            Rect clearButtonRect = new Rect(innerRect.xMax - 70f, innerRect.y, 70f, 20f);
            if (Widgets.ButtonText(clearButtonRect, "Clear All"))
            {
                if (currentKindData != null) { ClearAllGear(currentKindData); }
            }

            Rect aaButtonRect = new Rect(clearButtonRect.x - 115f, innerRect.y, 110f, 20f);
            if (Widgets.ButtonText(aaButtonRect, "Apply Others"))
            {
                if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                {
                    Find.WindowStack.Add(new Dialog_MessageBox(
                        "Are you sure you want to overwrite ALL Kind Defs in this faction with the current gear loadout?",
                        "Yes",
                        delegate
                        {
                            FactionGearEditor.CopyKindDefGear(); 
                            FactionGearEditor.ApplyToAllKindsInFaction(); 
                            Messages.Message("Applied current gear to ALL Kind Defs in this faction!", MessageTypeDefOf.PositiveEvent);
                        },
                        "No",
                        null
                    ));
                }
            }
            TooltipHandler.TipRegion(aaButtonRect, "Apply current gear to ALL other Kind Defs in this faction");

            Rect tabRowRect = new Rect(innerRect.x, innerRect.y + 24f, innerRect.width, 24f);
            
            Rect clearCatRect = new Rect(tabRowRect.xMax - 70f, tabRowRect.y, 70f, 24f);
            if (Widgets.ButtonText(clearCatRect, "Clear"))
            {
                if (currentKindData != null)
                {
                    ClearCategoryGear(currentKindData, EditorSession.SelectedCategory);
                }
            }
            TooltipHandler.TipRegion(clearCatRect, "Clear all items in the current category.");

            Rect tabRect = new Rect(tabRowRect.x, tabRowRect.y, tabRowRect.width - 75f, 24f);
            DrawCategoryTabs(tabRect);

            float previewHeight = 0f;
            if (EditorSession.SelectedCategory == GearCategory.Apparel || EditorSession.SelectedCategory == GearCategory.Armors)
            {
                previewHeight = 150f; 
            }
            float statsHeight = 24f + previewHeight;
            
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
            foreach (var item in gearItemsToDraw) {
                contentHeight += (item == EditorSession.ExpandedGearItem) ? 60f : 30f;
            }
            if (advancedItems.Any())
            {
                contentHeight += 30f; // Header
                contentHeight += advancedItems.Count * 30f;
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
                gearListing.Label("<b>Advanced/Specific Items</b>");
                foreach (var advItem in advancedItems)
                {
                    Rect rowRect = gearListing.GetRect(28f);
                    DrawAdvancedItemSimple(rowRect, advItem, currentKindData);
                    gearListing.Gap(2f);
                }
            }

            gearListing.End();
            Widgets.EndScrollView();

            if (currentKindData != null)
            {
                float currentY = innerRect.yMax - statsHeight;
                
                if (previewHeight > 0)
                {
                    Rect previewRect = new Rect(innerRect.x, currentY, innerRect.width, previewHeight);
                    DrawLayerPreview(previewRect, currentKindData);
                    currentY += previewHeight;
                }
                
                Rect statsRect = new Rect(innerRect.x, currentY, innerRect.width, 24f);
                float avgValue = FactionGearEditor.GetAverageValue(currentKindData);
                float avgWeight = FactionGearEditor.GetAverageWeight(currentKindData);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(statsRect, $"Avg Value: {avgValue:F0} | Avg Weight: {avgWeight:F1}");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                
                TooltipHandler.TipRegion(statsRect, "Weight Mechanics: In RimWorld, gear weight acts as a selection weight, not a guarantee.");
            }
        }

        private static void DrawAdvancedMode(Rect rect)
        {
            if (string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) || string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                Widgets.Label(rect, "Select a KindDef first.");
                return;
            }

            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
            var kindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);

            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 24f);
            Widgets.Label(new Rect(headerRect.x, headerRect.y, 120f, 24f), "Advanced Settings");

            Rect clearButtonRect = new Rect(headerRect.xMax - 70f, headerRect.y, 70f, 20f);
            if (Widgets.ButtonText(clearButtonRect, "Clear All"))
            {
                ClearAllGear(kindData);
            }

            Rect aaButtonRect = new Rect(clearButtonRect.x - 115f, headerRect.y, 110f, 20f);
            if (Widgets.ButtonText(aaButtonRect, "Apply Others"))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    "Are you sure you want to overwrite ALL Kind Defs in this faction with the current gear loadout?",
                    "Yes",
                    delegate
                    {
                        FactionGearEditor.CopyKindDefGear();
                        FactionGearEditor.ApplyToAllKindsInFaction();
                        Messages.Message("Applied current gear to ALL Kind Defs in this faction!", MessageTypeDefOf.PositiveEvent);
                    },
                    "No",
                    null
                ));
            }
            TooltipHandler.TipRegion(aaButtonRect, "Apply current gear to ALL other Kind Defs in this faction");

            Rect tabRect = new Rect(rect.x, rect.y + 28f, rect.width, 24f);
            float tabWidth = rect.width / 4f;
            
            DrawAdvTab(new Rect(tabRect.x, tabRect.y, tabWidth, tabRect.height), "General", AdvancedTab.General);
            DrawAdvTab(new Rect(tabRect.x + tabWidth, tabRect.y, tabWidth, tabRect.height), "Apparel", AdvancedTab.Apparel);
            DrawAdvTab(new Rect(tabRect.x + tabWidth * 2, tabRect.y, tabWidth, tabRect.height), "Weapons", AdvancedTab.Weapons);
            DrawAdvTab(new Rect(tabRect.x + tabWidth * 3, tabRect.y, tabWidth, tabRect.height), "Hediffs", AdvancedTab.Hediffs);

            Rect contentRect = new Rect(rect.x, rect.y + 58f, rect.width, rect.height - 58f);
            
            float height = 500f; 

            float gearListHeight = 0f;
            if (EditorSession.CurrentAdvancedTab == AdvancedTab.Apparel || EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons)
            {
                if (EditorSession.CurrentAdvancedTab == AdvancedTab.Apparel && EditorSession.SelectedCategory != GearCategory.Apparel && EditorSession.SelectedCategory != GearCategory.Armors)
                    EditorSession.SelectedCategory = GearCategory.Apparel;
                if (EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons && EditorSession.SelectedCategory != GearCategory.Weapons && EditorSession.SelectedCategory != GearCategory.MeleeWeapons)
                    EditorSession.SelectedCategory = GearCategory.Weapons;

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
                     height += kindData.SpecificApparel.Count * 170f + 100f;
                 else
                     height += 200f;
            }
            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons)
            {
                 height = gearListHeight + 150f;
                 if (!kindData.SpecificWeapons.NullOrEmpty())
                     height += kindData.SpecificWeapons.Count * 170f + 100f;
                 else
                     height += 200f;
            }
            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Hediffs && !kindData.ForcedHediffs.NullOrEmpty())
                 height += kindData.ForcedHediffs.Count * 120f + 100f;
            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Hediffs)
                 height = 200f;
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
            DrawTab(new Rect(rect.x, rect.y, tabWidth, rect.height), "Ranged", GearCategory.Weapons);
            DrawTab(new Rect(rect.x + tabWidth, rect.y, tabWidth, rect.height), "Melee", GearCategory.MeleeWeapons);
            DrawTab(new Rect(rect.x + tabWidth * 2, rect.y, tabWidth, rect.height), "Armors", GearCategory.Armors);
            DrawTab(new Rect(rect.x + tabWidth * 3, rect.y, tabWidth, rect.height), "Clothes", GearCategory.Apparel);
            DrawTab(new Rect(rect.x + tabWidth * 4, rect.y, tabWidth, rect.height), "Others", GearCategory.Others);
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
            
            kindData.isModified = true;
            FactionGearEditor.MarkDirty();
        }

        private static void ClearCategoryGear(KindGearData kindData, GearCategory category)
        {
            UndoManager.RecordState(kindData);
            switch (category)
            {
                case GearCategory.Weapons: kindData.weapons.Clear(); break;
                case GearCategory.MeleeWeapons: kindData.meleeWeapons.Clear(); break;
                case GearCategory.Armors: kindData.armors.Clear(); break;
                case GearCategory.Apparel: kindData.apparel.Clear(); break;
                case GearCategory.Others: kindData.others.Clear(); break;
            }
            kindData.isModified = true;
            FactionGearEditor.MarkDirty();
        }

        // --- Advanced Mode Helpers ---
        
        private static void DrawAdvancedGeneral(Listing_Standard ui, KindGearData kindData)
        {
            ui.Label("<b>General Overrides</b>");
            ui.Gap();
            DrawOverrideEnum(ui, "Item Quality", kindData.ItemQuality, val => { UndoManager.RecordState(kindData); kindData.ItemQuality = val; FactionGearEditor.MarkDirty(); });
            DrawOverrideFloatRange(ui, "Apparel Money", ref kindData.ApparelMoney, val => { UndoManager.RecordState(kindData); kindData.ApparelMoney = val; FactionGearEditor.MarkDirty(); });
            DrawOverrideFloatRange(ui, "Weapon Money", ref kindData.WeaponMoney, val => { UndoManager.RecordState(kindData); kindData.WeaponMoney = val; FactionGearEditor.MarkDirty(); });
            DrawOverrideFloatRange(ui, "Tech Money", ref kindData.TechMoney, val => { UndoManager.RecordState(kindData); kindData.TechMoney = val; FactionGearEditor.MarkDirty(); });

            Rect colorRect = ui.GetRect(28f);
            Widgets.Label(colorRect.LeftHalf(), "Apparel Color:");
            if (kindData.ApparelColor.HasValue)
            {
                Rect rightHalf = colorRect.RightHalf();
                Rect pickRect = new Rect(rightHalf.x, rightHalf.y, rightHalf.width - 35f, 28f);
                Rect xRect = new Rect(rightHalf.xMax - 30f, rightHalf.y, 30f, 28f);

                if (Widgets.ButtonText(pickRect, "Pick Color"))
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
                if (Widgets.ButtonText(colorRect.RightHalf(), "Set Color"))
                {
                    UndoManager.RecordState(kindData); kindData.ApparelColor = Color.white; FactionGearEditor.MarkDirty();
                }
            }
        }

        private static void DrawAdvancedApparel(Listing_Standard ui, KindGearData kindData)
        {
            DrawEmbeddedGearList(ui, kindData, GearCategory.Apparel, GearCategory.Armors);
            ui.CheckboxLabeled("Force Naked", ref kindData.ForceNaked);
            if (kindData.ForceNaked) return;
            ui.CheckboxLabeled("Force Only Selected (Strip others)", ref kindData.ForceOnlySelected);
            ui.GapLine();
            ui.Label("<b>Specific Apparel (Advanced)</b>");
            if (ui.ButtonText("Add New Apparel"))
            {
                if (kindData.SpecificApparel == null) kindData.SpecificApparel = new List<SpecRequirementEdit>();
                kindData.SpecificApparel.Add(new SpecRequirementEdit() { Thing = ThingDefOf.Apparel_Parka });
                FactionGearEditor.MarkDirty();
            }
            if (!kindData.SpecificApparel.NullOrEmpty())
            {
                for (int i = 0; i < kindData.SpecificApparel.Count; i++)
                {
                    DrawSpecEdit(ui, kindData.SpecificApparel[i], i, kindData.SpecificApparel);
                }
            }
        }

        private static void DrawAdvancedWeapons(Listing_Standard ui, KindGearData kindData)
        {
            DrawEmbeddedGearList(ui, kindData, GearCategory.Weapons, GearCategory.MeleeWeapons);
            DrawOverrideEnum(ui, "Forced Weapon Quality", kindData.ForcedWeaponQuality, val => { kindData.ForcedWeaponQuality = val; FactionGearEditor.MarkDirty(); });
            
            float biocodeChance = kindData.BiocodeWeaponChance ?? 0f;
            float oldBiocode = biocodeChance;
            ui.Label($"Biocode Chance: {biocodeChance:P0}");
            biocodeChance = ui.Slider(biocodeChance, 0f, 1f);
            if (Math.Abs(biocodeChance - oldBiocode) > 0.001f)
            {
                kindData.BiocodeWeaponChance = biocodeChance;
                FactionGearEditor.MarkDirty();
            }

            ui.GapLine();
            ui.Label("<b>Specific Weapons (Advanced)</b>");
            if (ui.ButtonText("Add New Weapon"))
            {
                if (kindData.SpecificWeapons == null) kindData.SpecificWeapons = new List<SpecRequirementEdit>();
                kindData.SpecificWeapons.Add(new SpecRequirementEdit() { Thing = ThingDef.Named("Gun_AssaultRifle") });
                FactionGearEditor.MarkDirty();
            }
            if (!kindData.SpecificWeapons.NullOrEmpty())
            {
                for (int i = 0; i < kindData.SpecificWeapons.Count; i++)
                {
                    DrawSpecEdit(ui, kindData.SpecificWeapons[i], i, kindData.SpecificWeapons);
                }
            }
        }

        private static void DrawAdvancedHediffs(Listing_Standard ui, KindGearData kindData)
        {
            ui.Label("<b>Forced Hediffs</b>");
            if (ui.ButtonText("Add New Hediff"))
            {
                if (kindData.ForcedHediffs == null) kindData.ForcedHediffs = new List<ForcedHediff>();
                kindData.ForcedHediffs.Add(new ForcedHediff() { HediffDef = HediffDefOf.Scaria });
                FactionGearEditor.MarkDirty();
            }
            if (!kindData.ForcedHediffs.NullOrEmpty())
            {
                for (int i = 0; i < kindData.ForcedHediffs.Count; i++)
                {
                    var item = kindData.ForcedHediffs[i];
                    Rect area = ui.GetRect(110f);
                    Widgets.DrawBoxSolidWithOutline(area, new Color(0.1f, 0.1f, 0.1f, 0.5f), Color.gray);
                    area = area.ContractedBy(5f);
                    Listing_Standard sub = new Listing_Standard();
                    sub.Begin(area);
                    Rect header = sub.GetRect(24f);
                    if (Widgets.ButtonText(header.LeftPart(0.7f), item.HediffDef?.LabelCap ?? "Select Hediff"))
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach (var def in DefDatabase<HediffDef>.AllDefs.Where(h => h.isBad || h.makesSickThought || h.countsAsAddedPartOrImplant).OrderBy(d => d.label))
                        {
                            options.Add(new FloatMenuOption(def.LabelCap, () => { item.HediffDef = def; FactionGearEditor.MarkDirty(); }));
                        }
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                    if (Widgets.ButtonText(header.RightPart(0.2f), "Remove"))
                    {
                        kindData.ForcedHediffs.RemoveAt(i);
                        FactionGearEditor.MarkDirty();
                        sub.End();
                        continue;
                    }
                    Rect rangeRect = sub.GetRect(24f);
                    Widgets.Label(rangeRect.LeftHalf(), "Parts Count Range:");
                    Widgets.IntRange(rangeRect.RightHalf(), 1234 + i, ref item.maxPartsRange, 1, 10);
                    Rect chanceRect = sub.GetRect(24f);
                    item.chance = Widgets.HorizontalSlider(chanceRect, item.chance, 0f, 1f, true, $"Chance: {item.chance:P0}");
                    sub.End();
                    ui.Gap(5f);
                }
            }
        }

        private static void DrawOverrideEnum<T>(Listing_Standard ui, string label, T? currentValue, Action<T?> setValue) where T : struct
        {
            Rect rect = ui.GetRect(28f);
            Widgets.Label(rect.LeftHalf(), label + ":");
            string btnLabel = currentValue.HasValue ? currentValue.Value.ToString() : "Default";
            if (Widgets.ButtonText(rect.RightHalf(), btnLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("Default", () => setValue(null)));
                foreach (T val in Enum.GetValues(typeof(T)))
                {
                    options.Add(new FloatMenuOption(val.ToString(), () => setValue(val)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static void DrawOverrideFloatRange(Listing_Standard ui, string label, ref FloatRange? range, Action<FloatRange?> setRange)
        {
            Rect rect = ui.GetRect(28f);
            Widgets.Label(rect.LeftHalf(), label + ":");
            if (range.HasValue)
            {
                if (Widgets.ButtonText(rect.RightHalf(), $"{range.Value.min}-{range.Value.max}"))
                {
                    setRange(null);
                    return;
                }
                FloatRange val = range.Value;
                Widgets.FloatRange(ui.GetRect(24f), label.GetHashCode(), ref val, 0f, 5000f);
                range = val;
            }
            else
            {
                if (Widgets.ButtonText(rect.RightHalf(), "Default"))
                {
                    setRange(new FloatRange(0, 1000));
                }
            }
        }
        
        private static void DrawSpecEdit(Listing_Standard ui, SpecRequirementEdit item, int index, List<SpecRequirementEdit> list)
        {
            Rect area = ui.GetRect(160f);
            Widgets.DrawBoxSolidWithOutline(area, new Color(0.15f, 0.15f, 0.15f, 0.5f), Color.gray);
            area = area.ContractedBy(5f);
            Rect iconRect = new Rect(area.x, area.y, 48f, 48f);
            if (item.Thing != null) Widgets.DrawTextureFitted(iconRect, item.Thing.uiIcon, 1f);
            Rect labelRect = new Rect(area.x + 55f, area.y, area.width - 120f, 24f);
            if (Widgets.ButtonText(labelRect, item.Thing?.LabelCap ?? "Select Item"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                IEnumerable<ThingDef> candidates = DefDatabase<ThingDef>.AllDefs.Where(t => t.IsApparel || t.IsWeapon).OrderBy(t => t.label);
                foreach (var def in candidates)
                {
                    options.Add(new FloatMenuOption(def.LabelCap, () => { item.Thing = def; FactionGearEditor.MarkDirty(); }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            Rect removeRect = new Rect(area.xMax - 60f, area.y, 60f, 24f);
            if (Widgets.ButtonText(removeRect, "Remove"))
            {
                list.RemoveAt(index);
                FactionGearEditor.MarkDirty();
                return;
            }
            
            // ... (Rest of DrawSpecEdit logic - skipping details for brevity but assuming functionality is similar to original)
            // Ideally I should copy the rest of DrawSpecEdit too.
            // But for now let's assume this is enough or I should check the original again.
            // I'll add the rest.
            
            Rect matRect = new Rect(area.x + 55f, area.y + 26f, area.width - 60f, 24f);
            string matLabel = item.Material == null ? "Material: Random/Default" : $"Material: {item.Material.LabelCap}";
            if (Widgets.ButtonText(matRect, matLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("Random/Default", () => { item.Material = null; FactionGearEditor.MarkDirty(); }));
                if (item.Thing != null && item.Thing.MadeFromStuff)
                {
                    foreach (var stuff in GenStuff.AllowedStuffsFor(item.Thing))
                    {
                        options.Add(new FloatMenuOption(stuff.LabelCap, () => { item.Material = stuff; FactionGearEditor.MarkDirty(); }));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            Rect qualRect = new Rect(area.x, area.y + 55f, area.width / 2f, 24f);
            string qualLabel = item.Quality.HasValue ? $"Quality: {item.Quality}" : "Quality: Default";
            if (Widgets.ButtonText(qualRect, qualLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("Default", () => { item.Quality = null; FactionGearEditor.MarkDirty(); }));
                foreach (QualityCategory q in Enum.GetValues(typeof(QualityCategory)))
                {
                    options.Add(new FloatMenuOption(q.ToString(), () => { item.Quality = q; FactionGearEditor.MarkDirty(); }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            Rect bioRect = new Rect(area.x + area.width / 2f, area.y + 55f, area.width / 2f, 24f);
            Widgets.CheckboxLabeled(bioRect, "Biocode", ref item.Biocode);

            Rect modeRect = new Rect(area.x, area.y + 85f, area.width, 24f);
            string modeLabel = $"Mode: {item.SelectionMode}";
            if (Widgets.ButtonText(modeRect, modeLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (ApparelSelectionMode m in Enum.GetValues(typeof(ApparelSelectionMode)))
                {
                    options.Add(new FloatMenuOption(m.ToString(), () => { item.SelectionMode = m; FactionGearEditor.MarkDirty(); }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            if (item.SelectionMode != ApparelSelectionMode.AlwaysTake)
            {
                Rect chanceRect = new Rect(area.x, area.y + 115f, area.width, 24f);
                item.SelectionChance = Widgets.HorizontalSlider(chanceRect, item.SelectionChance, 0f, 1f, true, $"Chance/Weight: {item.SelectionChance:P0}");
            }

            ui.Gap(10f);
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
                if (cat == GearCategory.Weapons) label = "Ranged";
                else if (cat == GearCategory.MeleeWeapons) label = "Melee";
                else if (cat == GearCategory.Armors) label = "Armors";
                else if (cat == GearCategory.Apparel) label = "Clothes";
                
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
                        FactionGearEditor.CalculateBounds();
                    }
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(catRect, label);
                Text.Anchor = TextAnchor.UpperLeft;
            }
            if (Widgets.ButtonText(clearCatRect, "Clear"))
            {
                ClearCategoryGear(kindData, EditorSession.SelectedCategory);
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
            else
            {
                ui.Label("No items in this category.");
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
            Rect infoButtonRect = new Rect(iconRect.xMax + 4f, row1.y + (row1.height - 24f) / 2f, 24f, 24f);
            Rect removeRect = new Rect(row1.xMax - 26f, row1.y + (row1.height - 24f) / 2f, 24f, 24f);
            Rect nameRect = new Rect(infoButtonRect.xMax + 4f, row1.y, removeRect.x - infoButtonRect.xMax - 8f, 28f);
            
            if (Current.Game != null) Widgets.InfoCardButton(infoButtonRect.x, infoButtonRect.y, thingDef);
            Rect interactRect = new Rect(row1.x, row1.y, row1.width - 30f, 28f);
            if (Widgets.ButtonInvisible(interactRect))
            {
                EditorSession.ExpandedGearItem = (EditorSession.ExpandedGearItem == gearItem) ? null : gearItem;
            }
            Texture2D icon = FactionGearEditor.GetIconWithLazyLoading(thingDef);
            if (icon != null) Widgets.DrawTextureFitted(iconRect, icon, 1f);
            else if (thingDef.graphic?.MatSingle?.mainTexture != null) Widgets.DrawTextureFitted(iconRect, thingDef.graphic.MatSingle.mainTexture, 1f);
            
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
                Widgets.Label(new Rect(nameRect.xMax - 45f, row1.y, 45f, 28f), $"W:{gearItem.weight:F1}");
                GUI.color = Color.white;
            }
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonImage(removeRect, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
            {
                UndoManager.RecordState(kindData);
                GetCurrentCategoryGear(kindData).Remove(gearItem);
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
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
             // ... (Implementation of DrawLayerPreview, using same logic as original)
             // For brevity, assuming it's copied or I can delegate to FactionGearEditor if I keep it static there?
             // No, I should move it here or to a helper.
             // Since it's UI drawing, it belongs here.
             
             if (kindData == null) return;
             List<ThingDef> apparels = new List<ThingDef>();
             foreach (var item in kindData.apparel) if (item.ThingDef != null) apparels.Add(item.ThingDef);
             foreach (var item in kindData.armors) if (item.ThingDef != null) apparels.Add(item.ThingDef);

             Widgets.DrawMenuSection(rect);
             Rect inner = rect.ContractedBy(4f);
             float lineHeight = 20f;
             float y = inner.y;
             
             Rect headerRect = new Rect(inner.x, y, inner.width, lineHeight);
             DrawCompactLayerHeader(headerRect);
             y += lineHeight;

             DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), "Head", BodyPartGroupDefOf.FullHead, apparels); y += lineHeight;
             DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), "Torso", BodyPartGroupDefOf.Torso, apparels); y += lineHeight;
             DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), "Shoulders", GroupShoulders, apparels); y += lineHeight;
             DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), "Arms", GroupArms, apparels); y += lineHeight;
             DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), "Hands", GroupHands, apparels); y += lineHeight;
             DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), "Legs", GroupLegs, apparels); y += lineHeight;
             DrawCompactLayerRow(new Rect(inner.x, y, inner.width, lineHeight), "Feet", GroupFeet, apparels); y += lineHeight;
        }

        private static void DrawCompactLayerHeader(Rect rect)
        {
             float x = rect.x + 60f;
             float width = (rect.width - 60f) / 5f;
             DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), "Skin"); x += width;
             DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), "Mid"); x += width;
             DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), "Shell"); x += width;
             DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), "Belt"); x += width;
             DrawLayerHeaderLabel(new Rect(x, rect.y, width, rect.height), "Over"); x += width;
        }
        
        private static void DrawLayerHeaderLabel(Rect rect, string label)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static void DrawCompactLayerRow(Rect rect, string label, BodyPartGroupDef group, List<ThingDef> apparels)
        {
            if (group == null) return;
            Rect labelRect = new Rect(rect.x, rect.y, 60f, rect.height);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            float x = rect.x + 60f;
            float width = (rect.width - 60f) / 5f; 
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.OnSkin, apparels); x += width;
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.Middle, apparels); x += width;
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.Shell, apparels); x += width;
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.Belt, apparels); x += width;
            DrawCompactLayerCell(new Rect(x, rect.y, width, rect.height), group, ApparelLayerDefOf.Overhead, apparels); x += width;
        }

        private static void DrawCompactLayerCell(Rect rect, BodyPartGroupDef group, ApparelLayerDef layer, List<ThingDef> apparels)
        {
             if (layer == null) return;
             var coveringApparel = apparels.FirstOrDefault(a => a.apparel != null && a.apparel.bodyPartGroups.Contains(group) && a.apparel.layers.Contains(layer));
             Rect iconRect = new Rect(rect.x + (rect.width - 16f)/2f, rect.y + (rect.height - 16f)/2f, 16f, 16f);
             GUI.color = new Color(1f, 1f, 1f, 0.2f);
             Widgets.DrawBox(iconRect, 1);
             GUI.color = Color.white;
             if (coveringApparel != null)
             {
                 GUI.DrawTexture(iconRect, Widgets.CheckboxOnTex);
                 TooltipHandler.TipRegion(rect, coveringApparel.LabelCap);
             }
             else
             {
                 TooltipHandler.TipRegion(rect, "Empty");
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
            Widgets.DrawHighlightIfMouseover(rect);

            Rect iconRect = new Rect(rect.x + 4f, rect.y + (rect.height - 24f) / 2f, 24f, 24f);
            Rect infoButtonRect = new Rect(iconRect.xMax + 4f, rect.y + (rect.height - 24f) / 2f, 24f, 24f);
            Rect removeRect = new Rect(rect.xMax - 26f, rect.y + (rect.height - 24f) / 2f, 24f, 24f);
            Rect nameRect = new Rect(infoButtonRect.xMax + 4f, rect.y, removeRect.x - infoButtonRect.xMax - 8f, rect.height);

            if (Current.Game != null) Widgets.InfoCardButton(infoButtonRect.x, infoButtonRect.y, item.Thing);
            
            Texture2D icon = FactionGearEditor.GetIconWithLazyLoading(item.Thing);
            if (icon != null) Widgets.DrawTextureFitted(iconRect, icon, 1f);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(nameRect, item.Thing.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonImage(removeRect, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
            {
                UndoManager.RecordState(kindData);
                if (kindData.SpecificApparel != null && kindData.SpecificApparel.Contains(item))
                    kindData.SpecificApparel.Remove(item);
                else if (kindData.SpecificWeapons != null && kindData.SpecificWeapons.Contains(item))
                    kindData.SpecificWeapons.Remove(item);
                
                kindData.isModified = true;
                FactionGearEditor.MarkDirty();
            }
            TooltipHandler.TipRegion(rect, "This is an item added via Advanced Mode (Specific Requirement).");
        }
    }
}
