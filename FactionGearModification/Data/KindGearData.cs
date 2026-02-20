using System.Collections.Generic;
using System.Linq;
using RimWorld;
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
        }

        public KindGearData DeepCopy()
        {
            return new KindGearData(kindDefName)
            {
                isModified = this.isModified,
                weapons = this.weapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                meleeWeapons = this.meleeWeapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                armors = this.armors.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                apparel = this.apparel.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                others = this.others.Select(g => new GearItem(g.thingDefName, g.weight)).ToList()
            };
        }

        public void CopyFrom(KindGearData source)
        {
            this.isModified = source.isModified;
            this.weapons = source.weapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            this.meleeWeapons = source.meleeWeapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            this.armors = source.armors.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            this.apparel = source.apparel.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            this.others = source.others.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
        }
    }
}