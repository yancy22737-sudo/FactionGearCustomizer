using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public class KindGearData : IExposable
    {
        public string kindDefName;
        public List<GearItem> weapons = new List<GearItem>();
        public List<GearItem> meleeWeapons = new List<GearItem>();
        public List<GearItem> armors = new List<GearItem>();
        public List<GearItem> apparel = new List<GearItem>();
        public List<GearItem> others = new List<GearItem>();
        public bool isModified = false;

        // New fields ported from TotalControl
        public bool ForceNaked = false;
        public bool ForceOnlySelected = false;
        public QualityCategory? ItemQuality = null;
        public QualityCategory? ForcedWeaponQuality = null;
        public float? BiocodeWeaponChance = null;
        public float? TechHediffChance = null;
        public int? TechHediffsMaxAmount = null;
        public FloatRange? ApparelMoney = null;
        public FloatRange? TechMoney = null;
        public FloatRange? WeaponMoney = null;
        public Color? ApparelColor = null;

        public List<string> TechHediffTags = null;
        public List<string> TechHediffDisallowedTags = null;
        public List<string> WeaponTags = null;
        public List<string> ApparelTags = null;
        public List<string> ApparelDisallowedTags = null;

        public List<ThingDef> ApparelRequired = null;
        public List<ThingDef> TechRequired = null;

        public List<SpecRequirementEdit> SpecificApparel = null;
        public List<SpecRequirementEdit> SpecificWeapons = null;
        public List<ForcedHediff> ForcedHediffs = null;

        public KindGearData() { }

        public KindGearData(string kindDefName)
        {
            this.kindDefName = kindDefName;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref kindDefName, "kindDefName");
            Scribe_Collections.Look(ref weapons, "weapons", LookMode.Deep);
            Scribe_Collections.Look(ref meleeWeapons, "meleeWeapons", LookMode.Deep);
            Scribe_Collections.Look(ref armors, "armors", LookMode.Deep);
            Scribe_Collections.Look(ref apparel, "apparel", LookMode.Deep);
            Scribe_Collections.Look(ref others, "others", LookMode.Deep);
            Scribe_Values.Look(ref isModified, "isModified", false);

            Scribe_Values.Look(ref ForceNaked, "forceNaked");
            Scribe_Values.Look(ref ForceOnlySelected, "forceOnlySelected");
            Scribe_Values.Look(ref ItemQuality, "itemQuality");
            Scribe_Values.Look(ref ForcedWeaponQuality, "forcedWeaponQuality");
            Scribe_Values.Look(ref BiocodeWeaponChance, "biocodeWeaponChance");
            Scribe_Values.Look(ref TechHediffChance, "techHediffChance");
            Scribe_Values.Look(ref TechHediffsMaxAmount, "techHediffsMaxAmount");
            Scribe_Values.Look(ref ApparelMoney, "apparelMoney");
            Scribe_Values.Look(ref TechMoney, "techMoney");
            Scribe_Values.Look(ref WeaponMoney, "weaponMoney");
            Scribe_Values.Look(ref ApparelColor, "apparelColor");

            Scribe_Collections.Look(ref TechHediffTags, "techHediffTags");
            Scribe_Collections.Look(ref TechHediffDisallowedTags, "techHediffDisallowedTags");
            Scribe_Collections.Look(ref WeaponTags, "weaponTags");
            Scribe_Collections.Look(ref ApparelTags, "apparelTags");
            Scribe_Collections.Look(ref ApparelDisallowedTags, "apparelDisallowedTags");
            
            Scribe_Collections.Look(ref ApparelRequired, "apparelRequired", LookMode.Def);
            Scribe_Collections.Look(ref TechRequired, "techRequired", LookMode.Def);

            Scribe_Collections.Look(ref SpecificApparel, "specificApparel", LookMode.Deep);
            Scribe_Collections.Look(ref SpecificWeapons, "specificWeapons", LookMode.Deep);
            Scribe_Collections.Look(ref ForcedHediffs, "forcedHediffs", LookMode.Deep);

            if (weapons == null) weapons = new List<GearItem>();
            if (meleeWeapons == null) meleeWeapons = new List<GearItem>();
            if (armors == null) armors = new List<GearItem>();
            if (apparel == null) apparel = new List<GearItem>();
            if (others == null) others = new List<GearItem>();
        }

        public void ResetToDefault()
        {
            weapons.Clear();
            meleeWeapons.Clear();
            armors.Clear();
            apparel.Clear();
            others.Clear();
            isModified = false;
            
            ForceNaked = false;
            ForceOnlySelected = false;
            ItemQuality = null;
            ForcedWeaponQuality = null;
            BiocodeWeaponChance = null;
            TechHediffChance = null;
            TechHediffsMaxAmount = null;
            ApparelMoney = null;
            TechMoney = null;
            WeaponMoney = null;
            ApparelColor = null;
            
            TechHediffTags = null;
            TechHediffDisallowedTags = null;
            WeaponTags = null;
            ApparelTags = null;
            ApparelDisallowedTags = null;
            
            ApparelRequired = null;
            TechRequired = null;
            SpecificApparel = null;
            SpecificWeapons = null;
            ForcedHediffs = null;
        }

        public KindGearData DeepCopy()
        {
            var copy = new KindGearData(kindDefName)
            {
                isModified = this.isModified,
                weapons = this.weapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                meleeWeapons = this.meleeWeapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                armors = this.armors.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                apparel = this.apparel.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                others = this.others.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                
                ForceNaked = this.ForceNaked,
                ForceOnlySelected = this.ForceOnlySelected,
                ItemQuality = this.ItemQuality,
                ForcedWeaponQuality = this.ForcedWeaponQuality,
                BiocodeWeaponChance = this.BiocodeWeaponChance,
                TechHediffChance = this.TechHediffChance,
                TechHediffsMaxAmount = this.TechHediffsMaxAmount,
                ApparelMoney = this.ApparelMoney,
                TechMoney = this.TechMoney,
                WeaponMoney = this.WeaponMoney,
                ApparelColor = this.ApparelColor
            };

            if (this.TechHediffTags != null) copy.TechHediffTags = new List<string>(this.TechHediffTags);
            if (this.TechHediffDisallowedTags != null) copy.TechHediffDisallowedTags = new List<string>(this.TechHediffDisallowedTags);
            if (this.WeaponTags != null) copy.WeaponTags = new List<string>(this.WeaponTags);
            if (this.ApparelTags != null) copy.ApparelTags = new List<string>(this.ApparelTags);
            if (this.ApparelDisallowedTags != null) copy.ApparelDisallowedTags = new List<string>(this.ApparelDisallowedTags);

            if (this.ApparelRequired != null) copy.ApparelRequired = new List<ThingDef>(this.ApparelRequired);
            if (this.TechRequired != null) copy.TechRequired = new List<ThingDef>(this.TechRequired);

            // Deep copy complex objects if needed, currently shallow copy for lists of objects (Exposable)
            // Scribe handles deep save, but for runtime copy we need to be careful.
            // SpecRequirementEdit is a class, so we need to deep copy it if we want independence.
            // But SpecRequirementEdit doesn't have a DeepCopy method yet. Let's assume shallow copy of list is okay for now or implement DeepCopy later.
            // Actually, for "DeepCopy" used in Copy/Paste, we definitely need new instances.
            // I'll implement manual deep copy for these lists.
            
            if (this.SpecificApparel != null) 
            {
                copy.SpecificApparel = new List<SpecRequirementEdit>();
                foreach(var item in this.SpecificApparel)
                {
                    var newItem = new SpecRequirementEdit();
                    // Copy fields
                    newItem.Thing = item.Thing;
                    newItem.Material = item.Material;
                    newItem.Style = item.Style;
                    newItem.Quality = item.Quality;
                    newItem.Biocode = item.Biocode;
                    newItem.Color = item.Color;
                    newItem.SelectionMode = item.SelectionMode;
                    newItem.SelectionChance = item.SelectionChance;
                    copy.SpecificApparel.Add(newItem);
                }
            }

            if (this.SpecificWeapons != null)
            {
                copy.SpecificWeapons = new List<SpecRequirementEdit>();
                foreach (var item in this.SpecificWeapons)
                {
                    var newItem = new SpecRequirementEdit();
                    newItem.Thing = item.Thing;
                    newItem.Material = item.Material;
                    newItem.Style = item.Style;
                    newItem.Quality = item.Quality;
                    newItem.Biocode = item.Biocode;
                    newItem.Color = item.Color;
                    newItem.SelectionMode = item.SelectionMode;
                    newItem.SelectionChance = item.SelectionChance;
                    copy.SpecificWeapons.Add(newItem);
                }
            }

            if (this.ForcedHediffs != null)
            {
                copy.ForcedHediffs = new List<ForcedHediff>();
                foreach (var item in this.ForcedHediffs)
                {
                    var newItem = new ForcedHediff();
                    newItem.HediffDef = item.HediffDef;
                    newItem.maxParts = item.maxParts;
                    newItem.maxPartsRange = item.maxPartsRange;
                    newItem.chance = item.chance;
                    if (item.parts != null) newItem.parts = new List<BodyPartDef>(item.parts);
                    copy.ForcedHediffs.Add(newItem);
                }
            }

            return copy;
        }

        public void CopyFrom(KindGearData source)
        {
            this.isModified = source.isModified;
            this.weapons = source.weapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            this.meleeWeapons = source.meleeWeapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            this.armors = source.armors.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            this.apparel = source.apparel.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            this.others = source.others.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();

            this.ForceNaked = source.ForceNaked;
            this.ForceOnlySelected = source.ForceOnlySelected;
            this.ItemQuality = source.ItemQuality;
            this.ForcedWeaponQuality = source.ForcedWeaponQuality;
            this.BiocodeWeaponChance = source.BiocodeWeaponChance;
            this.TechHediffChance = source.TechHediffChance;
            this.TechHediffsMaxAmount = source.TechHediffsMaxAmount;
            this.ApparelMoney = source.ApparelMoney;
            this.TechMoney = source.TechMoney;
            this.WeaponMoney = source.WeaponMoney;
            this.ApparelColor = source.ApparelColor;

            this.TechHediffTags = source.TechHediffTags == null ? null : new List<string>(source.TechHediffTags);
            this.TechHediffDisallowedTags = source.TechHediffDisallowedTags == null ? null : new List<string>(source.TechHediffDisallowedTags);
            this.WeaponTags = source.WeaponTags == null ? null : new List<string>(source.WeaponTags);
            this.ApparelTags = source.ApparelTags == null ? null : new List<string>(source.ApparelTags);
            this.ApparelDisallowedTags = source.ApparelDisallowedTags == null ? null : new List<string>(source.ApparelDisallowedTags);

            this.ApparelRequired = source.ApparelRequired == null ? null : new List<ThingDef>(source.ApparelRequired);
            this.TechRequired = source.TechRequired == null ? null : new List<ThingDef>(source.TechRequired);

            if (source.SpecificApparel != null)
            {
                this.SpecificApparel = new List<SpecRequirementEdit>();
                foreach (var item in source.SpecificApparel)
                {
                    var newItem = new SpecRequirementEdit();
                    newItem.Thing = item.Thing;
                    newItem.Material = item.Material;
                    newItem.Style = item.Style;
                    newItem.Quality = item.Quality;
                    newItem.Biocode = item.Biocode;
                    newItem.Color = item.Color;
                    newItem.SelectionMode = item.SelectionMode;
                    newItem.SelectionChance = item.SelectionChance;
                    this.SpecificApparel.Add(newItem);
                }
            }
            else this.SpecificApparel = null;

            if (source.SpecificWeapons != null)
            {
                this.SpecificWeapons = new List<SpecRequirementEdit>();
                foreach (var item in source.SpecificWeapons)
                {
                    var newItem = new SpecRequirementEdit();
                    newItem.Thing = item.Thing;
                    newItem.Material = item.Material;
                    newItem.Style = item.Style;
                    newItem.Quality = item.Quality;
                    newItem.Biocode = item.Biocode;
                    newItem.Color = item.Color;
                    newItem.SelectionMode = item.SelectionMode;
                    newItem.SelectionChance = item.SelectionChance;
                    this.SpecificWeapons.Add(newItem);
                }
            }
            else this.SpecificWeapons = null;

            if (source.ForcedHediffs != null)
            {
                this.ForcedHediffs = new List<ForcedHediff>();
                foreach (var item in source.ForcedHediffs)
                {
                    var newItem = new ForcedHediff();
                    newItem.HediffDef = item.HediffDef;
                    newItem.maxParts = item.maxParts;
                    newItem.maxPartsRange = item.maxPartsRange;
                    newItem.chance = item.chance;
                    if (item.parts != null) newItem.parts = new List<BodyPartDef>(item.parts);
                    this.ForcedHediffs.Add(newItem);
                }
            }
            else this.ForcedHediffs = null;
        }
    }
}