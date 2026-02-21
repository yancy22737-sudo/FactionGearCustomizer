using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace FactionGearCustomizer
{
    public static class EditorSession
    {
        // Selection State
        public static string SelectedFactionDefName = "";
        public static string SelectedKindDefName = "";
        public static GearCategory SelectedCategory = GearCategory.Weapons;
        public static HashSet<string> SelectedModSources = new HashSet<string>();
        public static TechLevel? SelectedTechLevel = null;
        public static GearItem ExpandedGearItem = null;

        // UI State (Scroll Positions)
        public static Vector2 FactionListScrollPos = Vector2.zero;
        public static Vector2 KindListScrollPos = Vector2.zero;
        public static Vector2 GearListScrollPos = Vector2.zero;
        public static Vector2 LibraryScrollPos = Vector2.zero;
        public static Vector2 AdvancedScrollPos = Vector2.zero;

        // Filter State
        public static string SearchText = "";
        public static FloatRange RangeFilter = new FloatRange(0f, 100f);
        public static FloatRange DamageFilter = new FloatRange(0f, 100f);
        public static FloatRange MarketValueFilter = new FloatRange(0f, 10000f);
        
        // Filter Bounds (Calculated)
        public static float MinRange = 0f;
        public static float MaxRange = 100f;
        public static float MinDamage = 0f;
        public static float MaxDamage = 100f;
        public static float MinMarketValue = 0f;
        public static float MaxMarketValue = 10000f;
        public static bool NeedCalculateBounds = true;

        // Sort State
        public static string SortField = "Name";
        public static bool SortAscending = true;

        // Mode State
        public static EditorMode CurrentMode = EditorMode.Simple;
        public static AdvancedTab CurrentAdvancedTab = AdvancedTab.General;
        
        // Settings State
        public static bool UseInGameNames = false;
        
        // Clipboard
        public static KindGearData CopiedKindGearData = null;

        // Caching
        public static List<string> CachedModSources = null;
        
        // Performance Caching
        public static List<ThingDef> CachedFilteredItems = null;
        public static List<ThingDef> CachedAllWeapons = null;
        
        // Cache Invalidation Check Variables
        public static string LastSearchText = "";
        public static GearCategory LastCategory = GearCategory.Weapons;
        public static string LastSortField = "Name";
        public static bool LastSortAscending = true;
        public static HashSet<string> LastSelectedModSources = new HashSet<string>();
        public static TechLevel? LastSelectedTechLevel = null;
        public static FloatRange LastRangeFilter = new FloatRange(0f, 100f);
        public static FloatRange LastDamageFilter = new FloatRange(0f, 100f);
        public static FloatRange LastMarketValueFilter = new FloatRange(0f, 10000f);

        // Reset Methods
        public static void ResetFilters()
        {
            SearchText = "";
            SelectedModSources.Clear();
            SelectedTechLevel = null;
            
            // Reset ranges to full bounds
            RangeFilter = new FloatRange(MinRange, MaxRange);
            DamageFilter = new FloatRange(MinDamage, MaxDamage);
            MarketValueFilter = new FloatRange(MinMarketValue, MaxMarketValue);
            
            CachedFilteredItems = null;
        }

        public static void ResetScrollPositions()
        {
            FactionListScrollPos = Vector2.zero;
            KindListScrollPos = Vector2.zero;
            GearListScrollPos = Vector2.zero;
            LibraryScrollPos = Vector2.zero;
            AdvancedScrollPos = Vector2.zero;
        }
    }
}
