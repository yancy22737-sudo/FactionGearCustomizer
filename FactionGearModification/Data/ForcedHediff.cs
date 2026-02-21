using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public class ForcedHediff : IExposable
    {
        public HediffDef HediffDef;
        public int maxParts = 0;
        public IntRange maxPartsRange = default(IntRange);
        public float chance = 1f;
        public List<BodyPartDef> parts;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref HediffDef, "hediffDef");
            Scribe_Values.Look(ref maxParts, "maxParts");
            Scribe_Values.Look(ref maxPartsRange, "maxPartsRange");
            Scribe_Values.Look(ref chance, "chance");
            Scribe_Collections.Look(ref parts, "parts", LookMode.Def);
        }
    }
}
