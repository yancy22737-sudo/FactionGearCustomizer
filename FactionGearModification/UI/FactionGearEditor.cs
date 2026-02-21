using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FactionGearCustomizer.UI.Panels;

namespace FactionGearCustomizer
{
    public static class FactionGearEditor
    {
        // ================== Caching & State ==================
        
        // Icon Cache
        private static Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();
        private static int maxCacheSize = 500;
        
        // Data Caching (Delegated to FactionGearManager mostly, but some local caching for UI performance)
        private static DateTime weaponsCacheTime = DateTime.MinValue;
        private static DateTime meleeCacheTime = DateTime.MinValue;
        private static DateTime armorsCacheTime = DateTime.MinValue;
        private static DateTime apparelCacheTime = DateTime.MinValue;
        private static DateTime othersCacheTime = DateTime.MinValue;
        private static readonly TimeSpan cacheExpiry = TimeSpan.FromMinutes(5);

        // Dirty State
        public static bool IsDirty = false;
        private static FactionGearCustomizerSettings backupSettings = null;

        // ================== Public Methods ==================

        public static List<PawnKindDef> GetFactionKinds(FactionDef factionDef)
        {
            if (factionDef == null) return new List<PawnKindDef>();

            var list = new List<PawnKindDef>();
            if (factionDef.pawnGroupMakers != null)
            {
                var seenKinds = new HashSet<string>();
                foreach (var pgm in factionDef.pawnGroupMakers)
                {
                    if (pgm.options != null)
                    {
                        foreach (var opt in pgm.options)
                        {
                            if (opt.kind != null && !seenKinds.Contains(opt.kind.defName))
                            {
                                list.Add(opt.kind);
                                seenKinds.Add(opt.kind.defName);
                            }
                        }
                    }
                }
            }
            list.Sort((a, b) => (a.label ?? a.defName).CompareTo(b.label ?? b.defName));
            return list;
        }

        public static void MarkDirty()
        {
            if (!IsDirty)
            {
                backupSettings = FactionGearCustomizerMod.Settings.DeepCopy();
                IsDirty = true;
            }

            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                var kindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                if (kindData != null) kindData.isModified = true;
            }
            
            FactionListPanel.MarkDirty(); // Notify panel to refresh list if needed
        }

        public static void OnFactionSelected()
        {
            EditorSession.KindListScrollPos = Vector2.zero;
            EditorSession.GearListScrollPos = Vector2.zero;
            // Additional logic when a faction is selected can be added here
        }

        public static void DiscardChanges()
        {
            if (backupSettings != null)
            {
                FactionGearCustomizerMod.Settings.RestoreFrom(backupSettings);
                backupSettings = null;
            }
            IsDirty = false;
            
            // Clear caches
            EditorSession.CachedFilteredItems = null;
            EditorSession.CachedModSources = null;
            KindListPanel.ClearCache();
            
            EditorSession.NeedCalculateBounds = true;
            CalculateBounds();
            Log.Message("[FactionGearCustomizer] Settings changes discarded.");
        }

        public static void SaveChanges()
        {
            if (!string.IsNullOrEmpty(FactionGearCustomizerMod.Settings.currentPresetName))
            {
                var preset = FactionGearCustomizerMod.Settings.presets.FirstOrDefault(p => p.name == FactionGearCustomizerMod.Settings.currentPresetName);
                if (preset != null)
                {
                    preset.SaveFromCurrentSettings(FactionGearCustomizerMod.Settings.factionGearData);
                    Messages.Message($"Preset '{preset.name}' updated.", MessageTypeDefOf.TaskCompletion, false);
                }
            }

            FactionGearCustomizerMod.Settings.Write();
            IsDirty = false;
            backupSettings = null; 
            Log.Message("[FactionGearCustomizer] Settings saved successfully.");
        }

        public static void DrawEditor(Rect inRect)
        {
            // Initialize
            if (EditorSession.NeedCalculateBounds)
            {
                CalculateBounds();
                EditorSession.NeedCalculateBounds = false;
            }

            Text.Font = GameFont.Small;

            // Default Selection Logic
            if (string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                var allFactions = DefDatabase<FactionDef>.AllDefs.Where(f => f.humanlikeFaction && !f.hidden).OrderBy(f => f.LabelCap.ToString()).ToList();
                if (allFactions.Any())
                {
                    EditorSession.SelectedFactionDefName = allFactions.First().defName;
                    var factionDef = allFactions.First();
                    if (factionDef.pawnGroupMakers != null)
                    {
                        foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                        {
                            if (pawnGroupMaker.options != null && pawnGroupMaker.options.Any())
                            {
                                var firstOption = pawnGroupMaker.options.First();
                                if (firstOption.kind != null)
                                {
                                    EditorSession.SelectedKindDefName = firstOption.kind.defName;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            DrawTopBar(inRect);

            // Layout
            float topRowHeight = 40f;
            Rect mainRect = new Rect(inRect.x, inRect.y + topRowHeight, inRect.width, inRect.height - topRowHeight);
            float totalWidth = mainRect.width - 20f;
            
            Rect leftPanel = new Rect(mainRect.x, mainRect.y, totalWidth * 0.22f, mainRect.height);
            Rect middlePanel = new Rect(leftPanel.xMax + 10f, mainRect.y, totalWidth * 0.40f, mainRect.height);
            Rect rightPanel = new Rect(middlePanel.xMax + 10f, mainRect.y, totalWidth * 0.38f, mainRect.height);

            // Left Panel: Split into Faction List (Top) and Kind List (Bottom)
            // Original code used a fixed height for Faction List or ratio?
            // "float factionListHeight = innerRect.height * 0.6f;" in DrawLeftPanel
            // So we split leftPanel manually.
            Widgets.DrawMenuSection(leftPanel);
            Rect leftInner = leftPanel.ContractedBy(5f);
            float factionHeight = leftInner.height * 0.6f;
            Rect factionRect = new Rect(leftInner.x, leftInner.y, leftInner.width, factionHeight);
            
            // Faction List
            // Since FactionListPanel.Draw expects the container rect and draws a MenuSection,
            // we should pass a rect that INCLUDES the border if we want it to look like before?
            // Actually FactionListPanel.Draw calls Widgets.DrawMenuSection(rect).
            // So we should pass a rect for the Faction List Panel.
            // But wait, the original code had ONE MenuSection for the whole Left Panel, and then drew contents inside.
            // My FactionListPanel.Draw draws a MenuSection.
            // If I want to split it, I should probably change FactionListPanel to NOT draw MenuSection, or draw two MenuSections.
            // Drawing two MenuSections (one for Faction, one for Kind) might look better or different.
            // Let's stick to the component design: Panel draws its own section.
            
            // Adjust rects for separate panels
            Rect leftTopPanelRect = new Rect(leftPanel.x, leftPanel.y, leftPanel.width, leftPanel.height * 0.6f);
            Rect leftBottomPanelRect = new Rect(leftPanel.x, leftTopPanelRect.yMax + 4f, leftPanel.width, leftPanel.height * 0.4f - 4f);
            
            FactionListPanel.Draw(leftTopPanelRect);
            KindListPanel.Draw(leftBottomPanelRect);
            
            GearEditPanel.Draw(middlePanel);
            ItemLibraryPanel.Draw(rightPanel);
        }

        private static void DrawTopBar(Rect inRect)
        {
            // Pre-calculate version info for layout
            string versionLabel = "ver: " + ModVersion.Current;
            Vector2 verSize = Text.CalcSize(versionLabel);
            float gap = 24f; // Gap of roughly one character width

            // GitHub Link
            string githubLink = "GitHub";
            Vector2 githubSize = Text.CalcSize(githubLink);
            Rect githubRect = new Rect(inRect.xMax - (githubSize.x + 10f + gap + verSize.x + 10f), inRect.y, githubSize.x + 10f, 24f);
            GUI.color = Color.cyan;
            if (Widgets.ButtonText(githubRect, githubLink, true, false, true))
            {
                Application.OpenURL("https://github.com/yancy22737-sudo/FactionGearCustomizer");
            }
            GUI.color = Color.white;

            // Version Label
            Rect verRect = new Rect(githubRect.xMax + gap, inRect.y, verSize.x + 10f, 24f);
            
            string changelog = ModVersion.GetChangelog();
            if (Mouse.IsOver(verRect))
            {
                Widgets.DrawHighlight(verRect);
                TooltipHandler.TipRegion(verRect, changelog);
            }
            Widgets.Label(verRect, versionLabel);

            WidgetRow buttonRow = new WidgetRow(inRect.x, inRect.y, UIDirection.RightThenDown, inRect.width, 4f);

            GUI.color = IsDirty ? Color.green : Color.white;
            string saveTooltip = "Save changes to current game";
            if (!string.IsNullOrEmpty(FactionGearCustomizerMod.Settings.currentPresetName))
            {
                saveTooltip += $"\nAnd update active preset: '{FactionGearCustomizerMod.Settings.currentPresetName}'";
            }

            if (buttonRow.ButtonText("Apply & Save", saveTooltip))
            {
                SaveChanges();
                Messages.Message("Settings saved successfully!", MessageTypeDefOf.PositiveEvent);
                if (FactionGearCustomizerMod.Settings.presets.Count == 0)
                {
                    Find.WindowStack.Add(new PresetManagerWindow());
                    Messages.Message("Tip: Please create a preset to safely back up your hard work!", MessageTypeDefOf.NeutralEvent);
                }
            }
            GUI.color = Color.cyan;
            if (buttonRow.ButtonText("Presets", "Manage gear presets"))
            {
                Find.WindowStack.Add(new PresetManagerWindow());
            }
            GUI.color = Color.white;

            buttonRow.Gap(10f);
            string currentPreset = FactionGearCustomizerMod.Settings.currentPresetName;
            if (!string.IsNullOrEmpty(currentPreset))
            {
                GUI.color = new Color(0.6f, 1f, 0.6f);
                string fullLabel = $"[Active: {currentPreset}]";
                // Truncate to avoid UI overflow if name is too long
                Rect labelRect = buttonRow.Label(fullLabel.Truncate(250f));
                if (Mouse.IsOver(labelRect))
                {
                    TooltipHandler.TipRegion(labelRect, fullLabel);
                }
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.gray;
                buttonRow.Label("[No Preset]");
                GUI.color = Color.white;
            }

            bool forceIgnore = FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;
            if (buttonRow.ButtonText($"Force Ignore: {(forceIgnore ? "ON" : "OFF")}", "Force remove conflicting apparel/weapons and ignore budget limits. Pawns will attempt to equip all possible items from the configured list which allows you to ignore constraints and always create god-tier units."))
            {
                FactionGearCustomizerMod.Settings.forceIgnoreRestrictions = !FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;
                MarkDirty();
            }

            if (buttonRow.ButtonText("Reset Options ▼"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Reset Filters", EditorSession.ResetFilters),
                    new FloatMenuOption("Reset Current Kind", ResetCurrentKind),
                    new FloatMenuOption("Load Default Faction", () => {
                        if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
                        {
                            FactionGearManager.LoadDefaultPresets(EditorSession.SelectedFactionDefName);
                            MarkDirty();
                        }
                    }),
                    new FloatMenuOption("Reset Current Faction", ResetCurrentFaction),
                    new FloatMenuOption("Reset EVERYTHING", () => {
                        FactionGearCustomizerMod.Settings.ResetToDefault();
                        MarkDirty();
                    }, MenuOptionPriority.High, null, null, 0f, null, null, true, 0)
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        // ================== Shared Logic ==================

        public static List<ThingDef> GetCachedAllWeapons()
        {
            if (EditorSession.CachedAllWeapons == null || DateTime.Now - weaponsCacheTime > cacheExpiry)
            {
                EditorSession.CachedAllWeapons = FactionGearManager.GetAllWeapons();
                weaponsCacheTime = DateTime.Now;
            }
            return EditorSession.CachedAllWeapons;
        }
        // ... (Other GetCached methods delegated to FactionGearManager directly or cached in Session)
        // Actually EditorSession doesn't have these caches, I should probably keep them in EditorSession if I want to remove static fields from here.
        // Or just use FactionGearManager directly since it handles caching too?
        // FactionGearManager has "cachedAllWeapons" etc.
        // So I can just call FactionGearManager.GetAllWeapons() and it will handle it.
        // The local cache in FactionGearEditor was maybe redundant?
        // Let's check FactionGearManager. It has "EnsureCacheInitialized".
        // So yes, I can just call FactionGearManager methods.

        public static List<ThingDef> GetCachedAllMeleeWeapons() => FactionGearManager.GetAllMeleeWeapons();
        public static List<ThingDef> GetCachedAllArmors() => FactionGearManager.GetAllArmors();
        public static List<ThingDef> GetCachedAllApparel() => FactionGearManager.GetAllApparel();
        public static List<ThingDef> GetCachedAllOthers() => FactionGearManager.GetAllOthers();

        public static void CalculateBounds()
        {
            if (EditorSession.CachedModSources == null && EditorSession.SelectedModSources.Count > 0)
            {
                GetUniqueModSources();
            }

            List<ThingDef> allItems = new List<ThingDef>();
            switch (EditorSession.SelectedCategory)
            {
                case GearCategory.Weapons: allItems = GetCachedAllWeapons(); break;
                case GearCategory.MeleeWeapons: allItems = GetCachedAllMeleeWeapons(); break;
                case GearCategory.Armors: allItems = GetCachedAllArmors(); break;
                case GearCategory.Apparel: allItems = GetCachedAllApparel(); break;
                case GearCategory.Others: allItems = GetCachedAllOthers(); break;
            }

            if (EditorSession.SelectedModSources.Count > 0)
            {
                allItems = allItems.Where(t => EditorSession.SelectedModSources.Contains(FactionGearManager.GetModSource(t))).ToList();
            }

            if (EditorSession.SelectedTechLevel.HasValue)
            {
                allItems = allItems.Where(t => t.techLevel == EditorSession.SelectedTechLevel.Value).ToList();
            }

            if (allItems.Any())
            {
                EditorSession.MinMarketValue = 0f;
                float maxVal = allItems.Max(t => t.BaseMarketValue);
                EditorSession.MaxMarketValue = Mathf.Ceil(maxVal / 100f) * 100f;
                if (EditorSession.MaxMarketValue < 100f) EditorSession.MaxMarketValue = 100f;

                if (EditorSession.MarketValueFilter.max > EditorSession.MaxMarketValue) EditorSession.MarketValueFilter.max = EditorSession.MaxMarketValue;
                if (EditorSession.MarketValueFilter.min > EditorSession.MarketValueFilter.max)
                {
                    EditorSession.MarketValueFilter.min = EditorSession.MinMarketValue;
                    EditorSession.MarketValueFilter.max = EditorSession.MaxMarketValue;
                }
            }
            else
            {
                EditorSession.MinMarketValue = 0f;
                EditorSession.MaxMarketValue = 10000f;
                EditorSession.MarketValueFilter = new FloatRange(0f, 10000f);
            }

            if (EditorSession.SelectedCategory == GearCategory.Weapons || EditorSession.SelectedCategory == GearCategory.MeleeWeapons)
            {
                if (allItems.Any())
                {
                    EditorSession.MinRange = 0f;
                    float maxR = allItems.Max(t => FactionGearManager.GetWeaponRange(t));
                    EditorSession.MaxRange = Mathf.Ceil(maxR / 5f) * 5f;
                    if (EditorSession.MaxRange < 40f) EditorSession.MaxRange = 40f;

                    EditorSession.MinDamage = 0f;
                    float maxD = allItems.Max(t => FactionGearManager.GetWeaponDamage(t));
                    EditorSession.MaxDamage = Mathf.Ceil(maxD / 5f) * 5f;
                    if (EditorSession.MaxDamage < 50f) EditorSession.MaxDamage = 50f;

                    if (EditorSession.RangeFilter.max > EditorSession.MaxRange) EditorSession.RangeFilter.max = EditorSession.MaxRange;
                    if (EditorSession.RangeFilter.min > EditorSession.RangeFilter.max)
                    {
                        EditorSession.RangeFilter.min = EditorSession.MinRange;
                        EditorSession.RangeFilter.max = EditorSession.MaxRange;
                    }

                    if (EditorSession.DamageFilter.max > EditorSession.MaxDamage) EditorSession.DamageFilter.max = EditorSession.MaxDamage;
                    if (EditorSession.DamageFilter.min > EditorSession.DamageFilter.max)
                    {
                        EditorSession.DamageFilter.min = EditorSession.MinDamage;
                        EditorSession.DamageFilter.max = EditorSession.MaxDamage;
                    }
                }
                else
                {
                    EditorSession.MinRange = 0f; EditorSession.MaxRange = 100f;
                    EditorSession.MinDamage = 0f; EditorSession.MaxDamage = 100f;
                }
            }
        }

        public static List<string> GetUniqueModSources()
        {
            var rawSources = new HashSet<string>();
            List<ThingDef> items = null;

            switch (EditorSession.SelectedCategory)
            {
                case GearCategory.Weapons: items = GetCachedAllWeapons(); break;
                case GearCategory.MeleeWeapons: items = GetCachedAllMeleeWeapons(); break;
                case GearCategory.Armors: items = GetCachedAllArmors(); break;
                case GearCategory.Apparel: items = GetCachedAllApparel(); break;
                case GearCategory.Others: items = GetCachedAllOthers(); break;
            }

            if (items != null)
            {
                foreach (var item in items)
                {
                    rawSources.Add(FactionGearManager.GetModSource(item));
                }
            }

            EditorSession.CachedModSources = rawSources.OrderBy(s => s).ToList();
            return EditorSession.CachedModSources;
        }

        public static Texture2D GetIconWithLazyLoading(ThingDef thingDef)
        {
            if (thingDef == null) return null;
            if (iconCache.TryGetValue(thingDef.defName, out Texture2D tex)) return tex;

            if (iconCache.Count >= maxCacheSize)
            {
                iconCache.Clear();
            }

            Texture2D resolvedIcon = null;
            if (thingDef.uiIcon != null) resolvedIcon = thingDef.uiIcon;
            else if (thingDef.graphic != null) resolvedIcon = thingDef.graphic.MatSingle?.mainTexture as Texture2D;
            
            if (resolvedIcon != null)
            {
                iconCache[thingDef.defName] = resolvedIcon;
            }
            return resolvedIcon;
        }

        public static string GetDetailedItemTooltip(ThingDef item, KindGearData kindData)
        {
            if (item == null) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>{item.LabelCap}</b>");
            sb.AppendLine($"<color=gray>{item.description}</color>");
            sb.AppendLine();
            sb.AppendLine($"Base Market Value: {item.BaseMarketValue:F0}");
            sb.AppendLine($"Mass: {item.BaseMass:F2} kg");
            
            if (item.IsWeapon)
            {
                float damage = FactionGearManager.GetWeaponDamage(item);
                float range = FactionGearManager.GetWeaponRange(item);
                float dps = FactionGearManager.CalculateWeaponDPS(item);
                sb.AppendLine($"Damage: {damage}");
                sb.AppendLine($"Range: {range}");
                sb.AppendLine($"DPS: {dps:F1}");
            }
            else if (item.IsApparel)
            {
                float sharp = FactionGearManager.GetArmorRatingSharp(item);
                float blunt = FactionGearManager.GetArmorRatingBlunt(item);
                sb.AppendLine($"Armor (Sharp): {sharp:P0}");
                sb.AppendLine($"Armor (Blunt): {blunt:P0}");
            }
            return sb.ToString();
        }

        public static List<GearItem> GetCurrentCategoryGear(KindGearData kindData, GearCategory category)
        {
            switch (category)
            {
                case GearCategory.Weapons: return kindData.weapons;
                case GearCategory.MeleeWeapons: return kindData.meleeWeapons;
                case GearCategory.Armors: return kindData.armors;
                case GearCategory.Apparel: return kindData.apparel;
                case GearCategory.Others: return kindData.others;
                default: return new List<GearItem>();
            }
        }
        
        public static float GetAverageValue(KindGearData kindData)
        {
            var gear = GetCurrentCategoryGear(kindData, EditorSession.SelectedCategory);
            if (!gear.Any()) return 0f;
            float total = gear.Sum(g => (g.ThingDef?.BaseMarketValue ?? 0) * g.weight);
            return total / gear.Count;
        }
        
        public static float GetAverageWeight(KindGearData kindData)
        {
            var gear = GetCurrentCategoryGear(kindData, EditorSession.SelectedCategory);
            if (!gear.Any()) return 0f;
            return gear.Average(g => g.weight);
        }

        public static void CopyKindDefGear()
        {
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                var kindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                EditorSession.CopiedKindGearData = kindData.DeepCopy();
                Messages.Message("Copied KindDef gear!", MessageTypeDefOf.TaskCompletion, false);
            }
        }

        public static void ApplyToAllKindsInFaction()
        {
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && EditorSession.CopiedKindGearData != null)
            {
                var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(EditorSession.SelectedFactionDefName);
                if (factionDef != null && factionDef.pawnGroupMakers != null)
                {
                    var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                    foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                    {
                        if (pawnGroupMaker.options != null)
                        {
                            foreach (var option in pawnGroupMaker.options)
                            {
                                if (option.kind != null)
                                {
                                    var targetKindData = factionData.GetOrCreateKindData(option.kind.defName);
                                    if (targetKindData != null)
                                    {
                                        targetKindData.CopyFrom(EditorSession.CopiedKindGearData);
                                        targetKindData.isModified = true;
                                    }
                                }
                            }
                        }
                    }
                    MarkDirty();
                }
            }
        }

        public static void InitializeWorkingSettings(bool force = false)
        {
            if (force)
            {
                // Reset state to ensure clean slate if forced
                IsDirty = false;
                backupSettings = null;
            }
        }

        public static void Cleanup()
        {
            // Clear caches to free memory
            iconCache.Clear();
            EditorSession.CachedAllWeapons = null;
            EditorSession.CachedFilteredItems = null;
            EditorSession.CachedModSources = null;
        }

        private static void ResetCurrentFaction()
        {
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                factionData.ResetToDefault();
                MarkDirty();
                Log.Message($"[FactionGearCustomizer] Reset faction settings to default: {EditorSession.SelectedFactionDefName}");
            }
        }

        private static void ResetCurrentKind()
        {
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                var kindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                kindData.ResetToDefault();
                FactionGearManager.LoadKindDefGear(DefDatabase<PawnKindDef>.GetNamedSilentFail(EditorSession.SelectedKindDefName), kindData);
                MarkDirty();
            }
        }
    }
}
