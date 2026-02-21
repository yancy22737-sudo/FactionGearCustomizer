using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Panels
{
    public static class ItemLibraryPanel
    {
        private struct FilteredItemsCacheKey
        {
            public string SearchText { get; set; }
            public GearCategory Category { get; set; }
            public string SortField { get; set; }
            public bool SortAscending { get; set; }
            public HashSet<string> ModSources { get; set; }
            public TechLevel? TechLevel { get; set; }
            public FloatRange Range { get; set; }
            public FloatRange Damage { get; set; }
            public FloatRange MarketValue { get; set; }
        }
        
        private static Dictionary<FilteredItemsCacheKey, List<ThingDef>> filteredItemsCache = new Dictionary<FilteredItemsCacheKey, List<ThingDef>>();

        public static void Draw(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            // Title and Add All
            Rect titleRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 24f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(titleRect, "Item Library");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            
            Rect addAllButtonRect = new Rect(innerRect.xMax - 80f, innerRect.y, 75f, 22f);
            
            bool canAddAll = true;
            if (EditorSession.CurrentMode == EditorMode.Advanced && 
                EditorSession.CurrentAdvancedTab != AdvancedTab.Apparel && 
                EditorSession.CurrentAdvancedTab != AdvancedTab.Weapons)
            {
                canAddAll = false;
            }

            if (canAddAll)
            {
                if (Widgets.ButtonText(addAllButtonRect, "Add All"))
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        var kindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
                        var itemsToAdd = GetFilteredAndSortedItems();
                        
                        int addedCount = 0;
                        
                        if (EditorSession.CurrentMode == EditorMode.Advanced)
                        {
                            if (EditorSession.CurrentAdvancedTab == AdvancedTab.Apparel)
                            {
                                if (kindData.SpecificApparel == null) kindData.SpecificApparel = new List<SpecRequirementEdit>();
                                foreach (var thingDef in itemsToAdd.Where(t => t.IsApparel))
                                {
                                    if (!kindData.SpecificApparel.Any(x => x.Thing == thingDef))
                                    {
                                        kindData.SpecificApparel.Add(new SpecRequirementEdit() { Thing = thingDef });
                                        addedCount++;
                                    }
                                }
                            }
                            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons)
                            {
                                if (kindData.SpecificWeapons == null) kindData.SpecificWeapons = new List<SpecRequirementEdit>();
                                foreach (var thingDef in itemsToAdd.Where(t => t.IsWeapon))
                                {
                                    if (!kindData.SpecificWeapons.Any(x => x.Thing == thingDef))
                                    {
                                        kindData.SpecificWeapons.Add(new SpecRequirementEdit() { Thing = thingDef });
                                        addedCount++;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var currentCategory = FactionGearEditor.GetCurrentCategoryGear(kindData, EditorSession.SelectedCategory);
                            foreach (var thingDef in itemsToAdd)
                            {
                                if (!currentCategory.Any(g => g.thingDefName == thingDef.defName))
                                {
                                    currentCategory.Add(new GearItem(thingDef.defName));
                                    addedCount++;
                                }
                            }
                        }
                        
                        if (addedCount > 0)
                        {
                            kindData.isModified = true;
                            FactionGearEditor.MarkDirty();
                            Messages.Message($"Added {addedCount} items to gear list", MessageTypeDefOf.TaskCompletion, false);
                        }
                    }
                }
            }
            else
            {
                 GUI.color = new Color(1f, 1f, 1f, 0.3f);
                 Widgets.ButtonText(addAllButtonRect, "Add All");
                 GUI.color = Color.white;
                 TooltipHandler.TipRegion(addAllButtonRect, "Cannot add items in current Advanced Mode tab.");
            }

            Rect filterRect = new Rect(innerRect.x, innerRect.y + 26f, innerRect.width, 120f);
            DrawFilters(filterRect);

            float listStartY = filterRect.yMax + 4f;
            float listHeight = innerRect.yMax - listStartY;
            Rect listOutRect = new Rect(innerRect.x, listStartY, innerRect.width, listHeight);

            var itemsToDraw = GetFilteredAndSortedItems();
            float itemHeight = 50f;
            float gapHeight = 2f;
            float totalHeight = itemsToDraw.Count * (itemHeight + gapHeight);
            totalHeight = Mathf.Max(totalHeight, listOutRect.height);
            Rect listViewRect = new Rect(0, 0, listOutRect.width - 16f, totalHeight);

            Widgets.BeginScrollView(listOutRect, ref EditorSession.LibraryScrollPos, listViewRect);

            if (itemsToDraw.Any())
            {
                float currentY = 0f;
                float viewTop = EditorSession.LibraryScrollPos.y;
                float viewBottom = EditorSession.LibraryScrollPos.y + listOutRect.height;

                foreach (var thingDef in itemsToDraw)
                {
                    if (currentY + itemHeight >= viewTop - itemHeight && currentY <= viewBottom + itemHeight)
                    {
                        Rect rowRect = new Rect(0, currentY, listViewRect.width, itemHeight);
                        DrawItemButton(rowRect, thingDef);
                    }
                    currentY += itemHeight + gapHeight;
                }
            }
            Widgets.EndScrollView();
        }

        private static void DrawFilters(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            // Search
            Rect searchRect = listing.GetRect(24f);
            Rect searchLabelRect = searchRect.LeftPartPixels(60f);
            Rect searchInputRect = searchRect.RightPartPixels(searchRect.width - 60f);
            
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(searchLabelRect, "Search:");
            Text.Anchor = TextAnchor.UpperLeft;
            
            string newSearchText = Widgets.TextField(searchInputRect, EditorSession.SearchText);
            if (string.IsNullOrEmpty(newSearchText))
            {
                var anchor = Text.Anchor;
                var color = GUI.color;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                Widgets.Label(new Rect(searchInputRect.x + 5f, searchInputRect.y, searchInputRect.width - 5f, searchInputRect.height), "Search...");
                GUI.color = color;
                Text.Anchor = anchor;
            }

            if (newSearchText != EditorSession.SearchText)
            {
                EditorSession.SearchText = newSearchText;
            }
            
            if (!string.IsNullOrEmpty(EditorSession.SearchText))
            {
                Rect clearButtonRect = new Rect(searchInputRect.xMax - 18f, searchInputRect.y + 2f, 16f, 16f);
                if (Widgets.ButtonImage(clearButtonRect, Widgets.CheckboxOffTex))
                {
                    EditorSession.SearchText = "";
                }
                TooltipHandler.TipRegion(clearButtonRect, "Clear search");
            }

            listing.Gap(4f);

            // Filters
            Rect filterRowRect = listing.GetRect(24f);
            Rect modSourceRect = new Rect(filterRowRect.x, filterRowRect.y, filterRowRect.width * 0.55f - 2f, filterRowRect.height);
            Rect techLevelRect = new Rect(filterRowRect.x + filterRowRect.width * 0.55f + 2f, filterRowRect.y, filterRowRect.width * 0.45f - 2f, filterRowRect.height);

            // Mod Source
            if (EditorSession.CachedModSources == null) EditorSession.CachedModSources = FactionGearEditor.GetUniqueModSources();
            
            string modButtonText;
            if (EditorSession.SelectedModSources.Count == 0)
            {
                modButtonText = "Mod: All";
            }
            else if (EditorSession.SelectedModSources.Count == 1)
            {
                string s = EditorSession.SelectedModSources.First();
                modButtonText = "Mod: " + (s.Length > 10 ? s.Substring(0, 8) + "..." : s);
            }
            else
            {
                modButtonText = $"Mod: {EditorSession.SelectedModSources.Count} Selected";
            }

            if (Widgets.ButtonText(modSourceRect, modButtonText))
            {
                OpenModFilterMenu();
            }
            TooltipHandler.TipRegion(modSourceRect, "Filter by Mod (Multi-select)");

            // Tech Level
            string techButtonText = !EditorSession.SelectedTechLevel.HasValue ? "Tech: All" : EditorSession.SelectedTechLevel.Value.ToString();
            if (Widgets.ButtonText(techLevelRect, techButtonText))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("All", () => { EditorSession.SelectedTechLevel = null; FactionGearEditor.CalculateBounds(); }));
                foreach (TechLevel level in Enum.GetValues(typeof(TechLevel)))
                {
                    options.Add(new FloatMenuOption(level.ToString(), () => { EditorSession.SelectedTechLevel = level; FactionGearEditor.CalculateBounds(); }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            listing.Gap(4f);

            // Sort
            Rect sortRowRect = listing.GetRect(24f);
            Rect sortFieldRect = new Rect(sortRowRect.x, sortRowRect.y, sortRowRect.width - 26f, sortRowRect.height);
            Rect sortOrderRect = new Rect(sortRowRect.xMax - 24f, sortRowRect.y, 24f, sortRowRect.height);

            List<string> dynamicSortOptions = new List<string> { "Name", "MarketValue", "TechLevel", "ModSource" };
            if (EditorSession.SelectedCategory == GearCategory.Weapons) dynamicSortOptions.AddRange(new[] { "Range", "Accuracy", "Damage", "DPS" });
            else if (EditorSession.SelectedCategory == GearCategory.MeleeWeapons) dynamicSortOptions.AddRange(new[] { "Damage", "DPS" });
            else if (EditorSession.SelectedCategory == GearCategory.Armors || EditorSession.SelectedCategory == GearCategory.Apparel) dynamicSortOptions.AddRange(new[] { "Armor_Sharp", "Armor_Blunt" });

            if (!dynamicSortOptions.Contains(EditorSession.SortField)) EditorSession.SortField = dynamicSortOptions[0];

            if (Widgets.ButtonText(sortFieldRect, $"Sort by: {EditorSession.SortField}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (string option in dynamicSortOptions)
                {
                    options.Add(new FloatMenuOption(option, () => EditorSession.SortField = option));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            if (Widgets.ButtonText(sortOrderRect, EditorSession.SortAscending ? "▲" : "▼"))
            {
                EditorSession.SortAscending = !EditorSession.SortAscending;
            }
            TooltipHandler.TipRegion(sortOrderRect, EditorSession.SortAscending ? "Ascending" : "Descending");

            listing.Gap(4f);

            // Range Filters
            DrawRangeFilter(listing, ref EditorSession.MarketValueFilter, EditorSession.MinMarketValue, EditorSession.MaxMarketValue, "Value", ToStringStyle.Money);

            if (EditorSession.SelectedCategory == GearCategory.Weapons || EditorSession.SelectedCategory == GearCategory.MeleeWeapons)
            {
                DrawRangeFilter(listing, ref EditorSession.RangeFilter, 0f, EditorSession.MaxRange, "Range", ToStringStyle.FloatOne);
                DrawRangeFilter(listing, ref EditorSession.DamageFilter, 0f, EditorSession.MaxDamage, "Damage", ToStringStyle.FloatOne);
            }

            listing.End();
        }
        
        private static void OpenModFilterMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            
            // "All" option
            options.Add(new FloatMenuOption("All (Clear Selection)", () => { 
                EditorSession.SelectedModSources.Clear(); 
                FactionGearEditor.CalculateBounds(); 
            }));

            // Individual mod options
            foreach (var src in EditorSession.CachedModSources)
            {
                bool isSelected = EditorSession.SelectedModSources.Contains(src);
                string label = isSelected ? $"[X] {src}" : $"[  ] {src}";
                
                options.Add(new FloatMenuOption(label, () => {
                    if (isSelected)
                        EditorSession.SelectedModSources.Remove(src);
                    else
                        EditorSession.SelectedModSources.Add(src);
                        
                    FactionGearEditor.CalculateBounds();
                    
                    // Re-open menu to allow continuous selection
                    OpenModFilterMenu();
                }));
            }
            
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void DrawRangeFilter(Listing_Standard listing, ref FloatRange range, float min, float max, string label, ToStringStyle style)
        {
            Rect rect = listing.GetRect(28f);
            Rect labelRect = new Rect(rect.x, rect.y, 45f, rect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            
            Text.Font = GameFont.Tiny;
            string minText = range.min.ToString(style == ToStringStyle.Money ? "F0" : "F1");
            string maxText = range.max.ToString(style == ToStringStyle.Money ? "F0" : "F1");
            if (style == ToStringStyle.Money) { minText = "$" + minText; maxText = "$" + maxText; }
            
            float minWidth = Text.CalcSize(minText).x + 2f;
            float maxWidth = Text.CalcSize(maxText).x + 2f;
            
            GUI.color = Color.gray;
            Widgets.Label(new Rect(labelRect.xMax, rect.y, minWidth, rect.height), minText);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(rect.xMax - maxWidth, rect.y, maxWidth, rect.height), maxText);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Rect sliderRect = new Rect(labelRect.xMax + minWidth + 2f, rect.y, rect.width - 45f - minWidth - maxWidth - 4f, rect.height);
            Widgets.FloatRange(sliderRect, label.GetHashCode(), ref range, min, max, null, style);
        }

        private static List<ThingDef> GetFilteredAndSortedItems()
        {
            if (EditorSession.CachedModSources == null && EditorSession.SelectedModSources.Count > 0)
            {
                EditorSession.CachedModSources = FactionGearEditor.GetUniqueModSources();
            }

            var cacheKey = new FilteredItemsCacheKey
            {
                SearchText = EditorSession.SearchText,
                Category = EditorSession.SelectedCategory,
                SortField = EditorSession.SortField,
                SortAscending = EditorSession.SortAscending,
                ModSources = new HashSet<string>(EditorSession.SelectedModSources),
                TechLevel = EditorSession.SelectedTechLevel,
                Range = EditorSession.RangeFilter,
                Damage = EditorSession.DamageFilter,
                MarketValue = EditorSession.MarketValueFilter
            };
            
            foreach (var cacheEntry in filteredItemsCache)
            {
                var key = cacheEntry.Key;
                if (key.SearchText == cacheKey.SearchText &&
                    key.Category == cacheKey.Category &&
                    key.SortField == cacheKey.SortField &&
                    key.SortAscending == cacheKey.SortAscending &&
                    key.ModSources.SetEquals(cacheKey.ModSources) &&
                    key.TechLevel == cacheKey.TechLevel &&
                    Mathf.Approximately(key.Range.min, cacheKey.Range.min) &&
                    Mathf.Approximately(key.Range.max, cacheKey.Range.max) &&
                    Mathf.Approximately(key.Damage.min, cacheKey.Damage.min) &&
                    Mathf.Approximately(key.Damage.max, cacheKey.Damage.max) &&
                    Mathf.Approximately(key.MarketValue.min, cacheKey.MarketValue.min) &&
                    Mathf.Approximately(key.MarketValue.max, cacheKey.MarketValue.max))
                {
                    return cacheEntry.Value;
                }
            }

            List<ThingDef> items = new List<ThingDef>();
            switch (EditorSession.SelectedCategory)
            {
                case GearCategory.Weapons: items = FactionGearEditor.GetCachedAllWeapons(); break;
                case GearCategory.MeleeWeapons: items = FactionGearEditor.GetCachedAllMeleeWeapons(); break;
                case GearCategory.Armors: items = FactionGearEditor.GetCachedAllArmors(); break;
                case GearCategory.Apparel: items = FactionGearEditor.GetCachedAllApparel(); break;
                case GearCategory.Others: items = FactionGearEditor.GetCachedAllOthers(); break;
            }

            if (!string.IsNullOrEmpty(EditorSession.SearchText))
            {
                string lowerSearchText = EditorSession.SearchText.ToLower();
                items = items.Where(t => t.label != null && t.label.ToLower().Contains(lowerSearchText)).ToList();
            }

            if (EditorSession.SelectedModSources.Count > 0)
            {
                items = items.Where(t => EditorSession.SelectedModSources.Contains(FactionGearManager.GetModSource(t))).ToList();
            }

            if (EditorSession.SelectedTechLevel.HasValue)
            {
                items = items.Where(t => t.techLevel == EditorSession.SelectedTechLevel.Value).ToList();
            }

            items = items.Where(t =>
            {
                float range = FactionGearManager.GetWeaponRange(t);
                float damage = FactionGearManager.GetWeaponDamage(t);
                float marketValue = t.BaseMarketValue;
                return range >= EditorSession.RangeFilter.min && range <= EditorSession.RangeFilter.max &&
                       damage >= EditorSession.DamageFilter.min && damage <= EditorSession.DamageFilter.max &&
                       marketValue >= EditorSession.MarketValueFilter.min && marketValue <= EditorSession.MarketValueFilter.max;
            }).ToList();

            switch (EditorSession.SortField)
            {
                case "Name":
                    items = EditorSession.SortAscending ? items.OrderBy(t => t.label ?? "").ToList() : items.OrderByDescending(t => t.label ?? "").ToList();
                    break;
                case "MarketValue":
                    items = EditorSession.SortAscending ? items.OrderBy(t => t.BaseMarketValue).ToList() : items.OrderByDescending(t => t.BaseMarketValue).ToList();
                    break;
                case "Range":
                    var rangePairs = items.Select(t => new { Thing = t, Range = FactionGearManager.GetWeaponRange(t) }).ToList();
                    rangePairs = EditorSession.SortAscending ? rangePairs.OrderBy(x => x.Range).ToList() : rangePairs.OrderByDescending(x => x.Range).ToList();
                    items = rangePairs.Select(x => x.Thing).ToList();
                    break;
                case "Accuracy":
                    var accuracyPairs = items.Select(t => new { Thing = t, Accuracy = GetWeaponAccuracy(t) }).ToList();
                    accuracyPairs = EditorSession.SortAscending ? accuracyPairs.OrderBy(x => x.Accuracy).ToList() : accuracyPairs.OrderByDescending(x => x.Accuracy).ToList();
                    items = accuracyPairs.Select(x => x.Thing).ToList();
                    break;
                case "Damage":
                    var damagePairs = items.Select(t => new { Thing = t, Damage = FactionGearManager.GetWeaponDamage(t) }).ToList();
                    damagePairs = EditorSession.SortAscending ? damagePairs.OrderBy(x => x.Damage).ToList() : damagePairs.OrderByDescending(x => x.Damage).ToList();
                    items = damagePairs.Select(x => x.Thing).ToList();
                    break;
                case "DPS":
                    var dpsPairs = items.Select(t => new { Thing = t, DPS = FactionGearManager.CalculateWeaponDPS(t) }).ToList();
                    dpsPairs = EditorSession.SortAscending ? dpsPairs.OrderBy(x => x.DPS).ToList() : dpsPairs.OrderByDescending(x => x.DPS).ToList();
                    items = dpsPairs.Select(x => x.Thing).ToList();
                    break;
                case "TechLevel":
                    items = EditorSession.SortAscending ? items.OrderBy(t => t.techLevel).ToList() : items.OrderByDescending(t => t.techLevel).ToList();
                    break;
                case "ModSource":
                    items = EditorSession.SortAscending ? items.OrderBy(t => FactionGearManager.GetModSource(t)).ToList() : items.OrderByDescending(t => FactionGearManager.GetModSource(t)).ToList();
                    break;
                case "Armor_Sharp":
                    var sharpArmorPairs = items.Select(t => new { Thing = t, Armor = t.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) }).ToList();
                    sharpArmorPairs = EditorSession.SortAscending ? sharpArmorPairs.OrderBy(x => x.Armor).ToList() : sharpArmorPairs.OrderByDescending(x => x.Armor).ToList();
                    items = sharpArmorPairs.Select(x => x.Thing).ToList();
                    break;
                case "Armor_Blunt":
                    var bluntArmorPairs = items.Select(t => new { Thing = t, Armor = t.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt) }).ToList();
                    bluntArmorPairs = EditorSession.SortAscending ? bluntArmorPairs.OrderBy(x => x.Armor).ToList() : bluntArmorPairs.OrderByDescending(x => x.Armor).ToList();
                    items = bluntArmorPairs.Select(x => x.Thing).ToList();
                    break;
            }

            if (filteredItemsCache.Count > 20)
            {
                var oldestKey = filteredItemsCache.Keys.First();
                filteredItemsCache.Remove(oldestKey);
            }
            
            filteredItemsCache[cacheKey] = items;
            return items;
        }

        private static float GetWeaponAccuracy(ThingDef weaponDef)
        {
            if (weaponDef == null) return 0f;
            try { return weaponDef.GetStatValueAbstract(StatDefOf.AccuracyMedium); }
            catch { return 0f; }
        }

        private static void DrawItemButton(Rect rect, ThingDef thingDef)
        {
            KindGearData kindData = null;
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(EditorSession.SelectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                kindData = factionData.GetOrCreateKindData(EditorSession.SelectedKindDefName);
            }

            Rect iconRect = new Rect(rect.x, rect.y + (rect.height - 48f) / 2f, 48f, 48f);
            Rect infoButtonRect = new Rect(iconRect.xMax + 8f, rect.y + (rect.height - 24f) / 2f, 24f, 24f);
            Rect labelRect = new Rect(infoButtonRect.xMax + 8f, rect.y, rect.width - 148f, rect.height);

            Texture2D icon = FactionGearEditor.GetIconWithLazyLoading(thingDef);
            if (icon != null) Widgets.DrawTextureFitted(iconRect, icon, 1f);

            if (Current.Game != null) Widgets.InfoCardButton(infoButtonRect.x, infoButtonRect.y, thingDef);

            Text.WordWrap = false;
            string itemName = thingDef.LabelCap;
            float nameWidth = Text.CalcSize(itemName).x;
            if (nameWidth > labelRect.width) itemName = GenText.Truncate(itemName, labelRect.width);

            bool showModName = thingDef.modContentPack != null && !thingDef.modContentPack.IsCoreMod;
            if (showModName)
            {
                Rect nameRect = new Rect(labelRect.x, rect.y + 4f, labelRect.width, 24f);
                Rect modRect = new Rect(labelRect.x, rect.y + 26f, labelRect.width, 20f);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(nameRect, itemName);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(modRect, $"出自：{thingDef.modContentPack.Name}");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
            else
            {
                Rect nameRect = new Rect(labelRect.x, rect.y + (rect.height - 24f) / 2f, labelRect.width, 24f);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(nameRect, itemName);
            }
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;

            bool isAdded = false;
            if (kindData != null)
            {
                if (EditorSession.CurrentMode == EditorMode.Advanced)
                {
                    if (EditorSession.CurrentAdvancedTab == AdvancedTab.Apparel && thingDef.IsApparel)
                        isAdded = kindData.SpecificApparel != null && kindData.SpecificApparel.Any(x => x.Thing == thingDef);
                    else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons && thingDef.IsWeapon)
                        isAdded = kindData.SpecificWeapons != null && kindData.SpecificWeapons.Any(x => x.Thing == thingDef);
                }
                else
                {
                    var currentCategory = FactionGearEditor.GetCurrentCategoryGear(kindData, EditorSession.SelectedCategory);
                    isAdded = currentCategory.Any(g => g.thingDefName == thingDef.defName);
                }
            }

            Rect addButtonRect = new Rect(rect.xMax - 28f, rect.y + (rect.height - 24f) / 2f, 24f, 24f);
            Rect priceRect = new Rect(addButtonRect.x - 70f, rect.y + (rect.height - 24f) / 2f, 65f, 24f);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(priceRect, $"${thingDef.BaseMarketValue:F0}");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            if (isAdded)
            {
                if (Widgets.ButtonImage(addButtonRect, Widgets.CheckboxOnTex))
                {
                    if (kindData != null)
                    {
                        UndoManager.RecordState(kindData);
                        if (EditorSession.CurrentMode == EditorMode.Advanced)
                        {
                            if (EditorSession.CurrentAdvancedTab == AdvancedTab.Apparel && thingDef.IsApparel && kindData.SpecificApparel != null)
                            {
                                kindData.SpecificApparel.RemoveAll(x => x.Thing == thingDef);
                                kindData.isModified = true;
                                FactionGearEditor.MarkDirty();
                            }
                            else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons && thingDef.IsWeapon && kindData.SpecificWeapons != null)
                            {
                                kindData.SpecificWeapons.RemoveAll(x => x.Thing == thingDef);
                                kindData.isModified = true;
                                FactionGearEditor.MarkDirty();
                            }
                        }
                        else
                        {
                            var currentCategory = FactionGearEditor.GetCurrentCategoryGear(kindData, EditorSession.SelectedCategory);
                            var existingItem = currentCategory.FirstOrDefault(g => g.thingDefName == thingDef.defName);
                            if (existingItem != null)
                            {
                                currentCategory.Remove(existingItem);
                                kindData.isModified = true;
                                FactionGearEditor.MarkDirty();
                                if (EditorSession.ExpandedGearItem == existingItem) EditorSession.ExpandedGearItem = null;
                            }
                        }
                    }
                }
            }
            else
            {
                bool canAdd = true;
                if (EditorSession.CurrentMode == EditorMode.Advanced)
                {
                     if (EditorSession.CurrentAdvancedTab == AdvancedTab.Apparel && !thingDef.IsApparel) canAdd = false;
                     else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons && !thingDef.IsWeapon) canAdd = false;
                     else if (EditorSession.CurrentAdvancedTab == AdvancedTab.General || EditorSession.CurrentAdvancedTab == AdvancedTab.Hediffs) canAdd = false;
                }

                if (canAdd)
                {
                    if (Widgets.ButtonImage(addButtonRect, TexButton.Plus))
                    {
                        if (kindData != null)
                        {
                            UndoManager.RecordState(kindData);
                            if (EditorSession.CurrentMode == EditorMode.Advanced)
                            {
                                if (EditorSession.CurrentAdvancedTab == AdvancedTab.Apparel && thingDef.IsApparel)
                                {
                                    if (kindData.SpecificApparel == null) kindData.SpecificApparel = new List<SpecRequirementEdit>();
                                    kindData.SpecificApparel.Add(new SpecRequirementEdit() { Thing = thingDef });
                                    kindData.isModified = true;
                                    FactionGearEditor.MarkDirty();
                                }
                                else if (EditorSession.CurrentAdvancedTab == AdvancedTab.Weapons && thingDef.IsWeapon)
                                {
                                    if (kindData.SpecificWeapons == null) kindData.SpecificWeapons = new List<SpecRequirementEdit>();
                                    kindData.SpecificWeapons.Add(new SpecRequirementEdit() { Thing = thingDef });
                                    kindData.isModified = true;
                                    FactionGearEditor.MarkDirty();
                                }
                            }
                            else
                            {
                                var currentCategory = FactionGearEditor.GetCurrentCategoryGear(kindData, EditorSession.SelectedCategory);
                                currentCategory.Add(new GearItem(thingDef.defName));
                                kindData.isModified = true;
                                FactionGearEditor.MarkDirty();
                            }
                        }
                    }
                }
                else
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.3f);
                    Widgets.ButtonImage(addButtonRect, TexButton.Plus);
                    GUI.color = Color.white;
                }
            }
            TooltipHandler.TipRegion(rect, new TipSignal(() => FactionGearEditor.GetDetailedItemTooltip(thingDef, kindData), thingDef.GetHashCode()));
        }
    }
}
