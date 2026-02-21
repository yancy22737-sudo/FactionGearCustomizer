using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public static class GearApplier
    {
        public static void ApplyCustomGear(Pawn pawn, Faction faction)
        {
            if (pawn == null || faction == null) return;

            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(faction.def.defName);
            var kindDefName = pawn.kindDef?.defName;

            if (!string.IsNullOrEmpty(kindDefName))
            {
                var kindData = factionData.GetKindData(kindDefName);
                if (kindData != null)
                {
                    ApplyWeapons(pawn, kindData);
                    ApplyApparel(pawn, kindData);
                    ApplyHediffs(pawn, kindData);
                    Log.Message($"[FactionGearCustomizer] Successfully applied custom gear to pawn {pawn.Name.ToStringFull} ({pawn.kindDef.defName}) of faction {faction.Name}");
                }
            }
        }

        private static void ApplyHediffs(Pawn pawn, KindGearData kindData)
        {
            if (kindData.ForcedHediffs.NullOrEmpty()) return;

            foreach (var forcedHediff in kindData.ForcedHediffs)
            {
                if (forcedHediff.HediffDef == null) continue;
                if (!Rand.Chance(forcedHediff.chance)) continue;

                int count = forcedHediff.maxParts > 0 ? forcedHediff.maxParts : forcedHediff.maxPartsRange.RandomInRange;
                if (count <= 0) count = 1;

                // Logic to find parts
                List<BodyPartRecord> partsToHit = new List<BodyPartRecord>();
                if (!forcedHediff.parts.NullOrEmpty())
                {
                     foreach (var partDef in forcedHediff.parts)
                     {
                         partsToHit.AddRange(pawn.RaceProps.body.GetPartsWithDef(partDef));
                     }
                }
                
                if (partsToHit.Count == 0 && !forcedHediff.parts.NullOrEmpty()) continue; // Specific parts requested but not found

                for (int i = 0; i < count; i++)
                {
                    BodyPartRecord part = partsToHit.NullOrEmpty() ? null : partsToHit.RandomElement();
                    if (pawn.health.hediffSet.HasHediff(forcedHediff.HediffDef, part)) continue;
                    
                    pawn.health.AddHediff(forcedHediff.HediffDef, part);
                }
            }
        }

        private static void ApplyWeapons(Pawn pawn, KindGearData kindData)
        {
            // Global Force Ignore check
            bool forceIgnore = FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;

            // Clear existing weapons if configured or if we are forcing new ones
            if (pawn.equipment != null)
            {
                // Check if we should clear everything
                bool clearAll = kindData.ForceNaked || kindData.ForceOnlySelected;
                
                var equipmentToDestroy = pawn.equipment.AllEquipmentListForReading
                    .Where(eq => clearAll || eq.def.destroyOnDrop)
                    .ToList();
                
                foreach (var equipment in equipmentToDestroy)
                {
                    if (pawn.Spawned && pawn.Position.IsValid)
                    {
                        if (!pawn.equipment.TryDropEquipment(equipment, out _, pawn.Position, true))
                        {
                            equipment.Destroy();
                        }
                    }
                    else
                    {
                        equipment.Destroy();
                    }
                }
            }

            // Advanced Mode: SpecificWeapons
            if (!kindData.SpecificWeapons.NullOrEmpty())
            {
                foreach (var item in GetWhatToGive(kindData.SpecificWeapons, pawn))
                {
                    if (item.Thing == null) continue;
                    var created = GenerateItem(pawn, item, kindData);
                    if (created is ThingWithComps weapon && pawn.equipment != null)
                    {
                        if (weapon.def.equipmentType == EquipmentType.Primary && pawn.equipment.Primary != null)
                        {
                             pawn.equipment.Remove(pawn.equipment.Primary);
                        }
                        pawn.equipment.AddEquipment(weapon);
                    }
                    else if (created != null)
                    {
                        created.Destroy();
                    }
                }
            }
            // Simple Mode: Old Lists (Fallback)
            else if (kindData.weapons.Any() || kindData.meleeWeapons.Any())
            {
                // ... Existing simple logic ...
                if (kindData.weapons.Any())
                {
                    var weaponItem = GetRandomGearItem(kindData.weapons);
                    if (weaponItem?.ThingDef != null)
                    {
                        // Handle conflicting apparel
                        if (forceIgnore && pawn.apparel != null)
                        {
                            var conflictingApparel = pawn.apparel.WornApparel
                                .Where(a => {
                                    var comp = a.GetComp<CompShield>();
                                    return comp != null && weaponItem.ThingDef.IsRangedWeapon;
                                })
                                .ToList();
                            foreach (var apparel in conflictingApparel)
                            {
                                pawn.apparel.Remove(apparel);
                                apparel.Destroy();
                            }
                        }

                        var weapon = GenerateSimpleItem(pawn, weaponItem.ThingDef, kindData, true);
                        if (weapon is ThingWithComps wc)
                        {
                            if (wc.def.equipmentType == EquipmentType.Primary && pawn.equipment.Primary != null)
                            {
                                ThingWithComps primary = pawn.equipment.Primary;
                                if (pawn.Spawned && pawn.equipment.TryDropEquipment(primary, out var dropped, pawn.Position, false))
                                {
                                    dropped.Destroy();
                                }
                                else
                                {
                                    pawn.equipment.Remove(primary);
                                    primary.Destroy();
                                }
                            }
                            pawn.equipment.AddEquipment(wc);
                        }
                    }
                }

                if (kindData.meleeWeapons.Any())
                {
                    var meleeItem = GetRandomGearItem(kindData.meleeWeapons);
                    if (meleeItem?.ThingDef != null)
                    {
                         var weapon = GenerateSimpleItem(pawn, meleeItem.ThingDef, kindData, true);
                        if (weapon is ThingWithComps wc)
                        {
                            if (wc.def.equipmentType == EquipmentType.Primary && pawn.equipment.Primary != null)
                            {
                                ThingWithComps primary = pawn.equipment.Primary;
                                if (pawn.Spawned && pawn.equipment.TryDropEquipment(primary, out var dropped, pawn.Position, false))
                                {
                                    dropped.Destroy();
                                }
                                else
                                {
                                    pawn.equipment.Remove(primary);
                                    primary.Destroy();
                                }
                            }
                            pawn.equipment.AddEquipment(wc);
                        }
                    }
                }
            }
        }

        private static void ApplyApparel(Pawn pawn, KindGearData kindData)
        {
            if (kindData.ForceNaked)
            {
                pawn.apparel?.DestroyAll();
                return;
            }

            // Check if we have ANY data to apply. If not, don't strip (unless forced).
            bool hasAdvancedData = !kindData.SpecificApparel.NullOrEmpty() || !kindData.ApparelRequired.NullOrEmpty();
            bool hasSimpleData = !kindData.armors.NullOrEmpty() || !kindData.apparel.NullOrEmpty() || !kindData.others.NullOrEmpty();
            
            if (!hasAdvancedData && !hasSimpleData && !kindData.ForceOnlySelected)
            {
                return;
            }

            // Strip logic
            if (kindData.ForceOnlySelected)
            {
                 // Strip everything that isn't required (complex check, simplified here to strip all then re-add)
                 // Actually, TotalControl logic is: "Destroy what is worn that is NOT in the allowed list".
                 // For now, let's stick to: Strip all, then add what we want.
                 pawn.apparel?.DestroyAll();
            }
            else
            {
                // Only destroy "destroyOnDrop" items (vanilla behavior for pawns)
                var apparelToDestroy = pawn.apparel.WornApparel
                    .Where(app => app.def.destroyOnDrop)
                    .ToList();
                foreach (var apparel in apparelToDestroy)
                {
                    pawn.apparel.Remove(apparel);
                    apparel.Destroy();
                }
            }

            // Advanced Mode: SpecificApparel
            if (hasAdvancedData)
            {
                // Budget Logic (Advanced Mode)
                bool forceIgnore = FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;
                float budget = kindData.ApparelMoney?.RandomInRange ?? pawn.kindDef.apparelMoney.RandomInRange;
                float currentSpent = 0f;
                if (pawn.apparel != null)
                {
                    currentSpent = pawn.apparel.WornApparel.Sum(a => a.MarketValue);
                }

                // Simple Required List
                if (!kindData.ApparelRequired.NullOrEmpty())
                {
                    foreach (var def in kindData.ApparelRequired)
                    {
                        var item = new SpecRequirementEdit() { Thing = def, SelectionMode = ApparelSelectionMode.AlwaysTake };
                        var created = GenerateItem(pawn, item, kindData);
                        if (created is Apparel app && ApparelUtility.HasPartsToWear(pawn, app.def))
                        {
                            // Check Budget
                            if (!forceIgnore && (currentSpent + app.MarketValue > budget))
                            {
                                created.Destroy();
                                continue;
                            }

                            try
                            {
                                pawn.apparel.Wear(app, true);
                                currentSpent += app.MarketValue;
                            }
                            catch (Exception ex)
                            {
                                Log.Warning($"[FactionGearCustomizer] Failed to wear {app} on {pawn}: {ex.Message}");
                                app.Destroy();
                            }
                        }
                    }
                }

                // Complex Specific List
                if (!kindData.SpecificApparel.NullOrEmpty())
                {
                    foreach (var item in GetWhatToGive(kindData.SpecificApparel, pawn))
                    {
                         if (item.Thing == null) continue;
                         var created = GenerateItem(pawn, item, kindData);
                         if (created is Apparel app && ApparelUtility.HasPartsToWear(pawn, app.def))
                         {
                             // Check Budget
                             if (!forceIgnore && (currentSpent + app.MarketValue > budget))
                             {
                                 created.Destroy();
                                 continue;
                             }

                             try
                             {
                                 pawn.apparel.Wear(app, true);
                                 currentSpent += app.MarketValue;
                             }
                             catch (Exception ex)
                             {
                                 Log.Warning($"[FactionGearCustomizer] Failed to wear {app} on {pawn}: {ex.Message}");
                                 app.Destroy();
                             }
                         }
                    }
                }
            }
            // Simple Mode: Old Lists (Fallback)
            if (hasSimpleData)
            {
                // Fix: Always try to equip multiple items from the list.
                // This allows users to specify multiple items (e.g. helmet + armor) in the same list
                // and have them all applied if they don't conflict.
                // Previously this was only enabled for ForceIgnore, but it should be the default behavior
                // as users expect "what they list is what they get" (limited by slots/conflicts).
                bool tryEquipMultiple = true;
                bool forceIgnore = FactionGearCustomizerMod.Settings.forceIgnoreRestrictions;

                // Budget Logic
                float budget = kindData.ApparelMoney?.RandomInRange ?? pawn.kindDef.apparelMoney.RandomInRange;
                float currentSpent = 0f;
                if (pawn.apparel != null)
                {
                    currentSpent = pawn.apparel.WornApparel.Sum(a => a.MarketValue);
                }

                // Log budget info for debugging
                // Log.Message($"[FactionGearCustomizer] Budget Check for {pawn}: Budget={budget}, CurrentSpent={currentSpent}, ForceIgnore={forceIgnore}");

                void EquipApparelList(List<GearItem> gearList)
                {
                    if (gearList.NullOrEmpty()) return;

                    if (tryEquipMultiple)
                    {
                        var workingList = gearList.ToList();
                        // Max attempts to prevent any infinite loops, though list size is natural limit
                        int maxAttempts = workingList.Count * 2;
                        int attempts = 0;

                        while (workingList.Any() && attempts < maxAttempts)
                        {
                            attempts++;
                            var item = GetRandomGearItem(workingList);
                            if (item == null) break;
                            
                            // Remove from working list so we don't pick it again
                            workingList.Remove(item);

                            if (item.ThingDef != null)
                            {
                                var created = GenerateSimpleItem(pawn, item.ThingDef, kindData, false);
                                if (created is Apparel app)
                                {
                                    // Check Budget (unless ignored)
                                    if (!forceIgnore && (currentSpent + app.MarketValue > budget))
                                    {
                                        created.Destroy();
                                        continue;
                                    }

                                    if (ApparelUtility.HasPartsToWear(pawn, app.def))
                                    {
                                        try
                                        {
                                            pawn.apparel.Wear(app, true);
                                            currentSpent += app.MarketValue;
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Warning($"[FactionGearCustomizer] Failed to wear {app} on {pawn}: {ex.Message}");
                                            app.Destroy();
                                        }
                                    }
                                    else
                                    {
                                        created.Destroy();
                                    }
                                }
                                else if (created != null)
                                {
                                    created.Destroy();
                                }
                            }
                        }
                    }
                    else
                    {
                        var item = GetRandomGearItem(gearList);
                        if (item?.ThingDef != null)
                        {
                            var created = GenerateSimpleItem(pawn, item.ThingDef, kindData, false);
                            if (created is Apparel app) 
                            {
                                // Check Budget (unless ignored)
                                if (!forceIgnore && (currentSpent + app.MarketValue > budget))
                                {
                                    created.Destroy();
                                    return;
                                }

                                if (ApparelUtility.HasPartsToWear(pawn, app.def))
                                {
                                    try
                                    {
                                        pawn.apparel.Wear(app, true);
                                        currentSpent += app.MarketValue;
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Warning($"[FactionGearCustomizer] Failed to wear {app} on {pawn}: {ex.Message}");
                                        app.Destroy();
                                    }
                                }
                                else
                                {
                                    created.Destroy();
                                }
                            }
                            else if (created != null)
                            {
                                created.Destroy();
                            }
                        }
                    }
                }

                EquipApparelList(kindData.armors);
                EquipApparelList(kindData.apparel);
                EquipApparelList(kindData.others);
            }
        }

        // Helper for Simple Mode items
        private static Thing GenerateSimpleItem(Pawn pawn, ThingDef def, KindGearData kindData, bool isWeapon)
        {
            if (def == null)
            {
                Log.Error("[FactionGearCustomizer] GenerateSimpleItem called with null def.");
                return null;
            }
            if (kindData == null)
            {
                Log.Error("[FactionGearCustomizer] GenerateSimpleItem called with null kindData.");
                return null;
            }

            ThingDef stuff = def.MadeFromStuff ? GenStuff.RandomStuffFor(def) : null;
            if (def.MadeFromStuff && stuff == null)
            {
                Log.Warning($"[FactionGearCustomizer] Could not find stuff for {def.defName}. Skipping generation.");
                return null;
            }
            var thing = ThingMaker.MakeThing(def, stuff);

            // Quality
            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                QualityCategory q = QualityCategory.Normal;
                if (isWeapon && kindData.ForcedWeaponQuality.HasValue) q = kindData.ForcedWeaponQuality.Value;
                else if (kindData.ItemQuality.HasValue) q = kindData.ItemQuality.Value;
                
                compQuality.SetQuality(q, ArtGenerationContext.Outsider);
            }
            
            // Color
            if (kindData.ApparelColor.HasValue && thing.TryGetComp<CompColorable>() != null)
            {
                thing.SetColor(kindData.ApparelColor.Value);
            }
            
            // Biocode
            if (isWeapon && kindData.BiocodeWeaponChance.HasValue)
            {
                var code = thing.TryGetComp<CompBiocodable>();
                if (code != null && code.Biocodable && Rand.Chance(kindData.BiocodeWeaponChance.Value))
                {
                    code.CodeFor(pawn);
                }
            }

            return thing;
        }

        // Helper for Advanced Mode items
        private static Thing GenerateItem(Pawn pawn, SpecRequirementEdit spec, KindGearData kindData)
        {
            ThingDef stuff = spec.Material;
            if (stuff == null && spec.Thing.MadeFromStuff)
            {
                 stuff = GenStuff.RandomStuffFor(spec.Thing);
            }

            Thing thing = ThingMaker.MakeThing(spec.Thing, stuff);
            if (thing == null) return null;

            if (spec.Style != null)
            {
                thing.SetStyleDef(spec.Style);
            }

            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                QualityCategory q = spec.Quality ?? kindData.ItemQuality ?? QualityCategory.Normal;
                if (thing.def.IsWeapon && kindData.ForcedWeaponQuality.HasValue) q = kindData.ForcedWeaponQuality.Value;
                
                compQuality.SetQuality(q, ArtGenerationContext.Outsider);
            }

            Color color = spec.Color != default ? spec.Color : (kindData.ApparelColor ?? default);
            if (color != default)
            {
                thing.SetColor(color, false);
            }

            CompBiocodable code = thing.TryGetComp<CompBiocodable>();
            if (code != null && code.Biocodable)
            {
                if (code.Biocoded) code.UnCode();
                
                bool shouldBiocode = spec.Biocode;
                if (!shouldBiocode && thing.def.IsWeapon && kindData.BiocodeWeaponChance.HasValue)
                {
                    shouldBiocode = Rand.Chance(kindData.BiocodeWeaponChance.Value);
                }

                if (shouldBiocode)
                {
                    code.CodeFor(pawn);
                }
            }

            return thing;
        }

        private static IEnumerable<SpecRequirementEdit> GetWhatToGive(List<SpecRequirementEdit> allSpecs, Pawn pawn)
        {
            var always = allSpecs.Where(x => x.SelectionMode == ApparelSelectionMode.AlwaysTake).ToList();
            var chance = allSpecs.Where(x => x.SelectionMode == ApparelSelectionMode.RandomChance).ToList();
            var pool1 = allSpecs.Where(x => x.SelectionMode == ApparelSelectionMode.FromPool1).ToList();
            var pool2 = allSpecs.Where(x => x.SelectionMode == ApparelSelectionMode.FromPool2).ToList();
            var pool3 = allSpecs.Where(x => x.SelectionMode == ApparelSelectionMode.FromPool3).ToList();
            var pool4 = allSpecs.Where(x => x.SelectionMode == ApparelSelectionMode.FromPool4).ToList();

            foreach (var item in always) yield return item;

            foreach (var item in chance)
                if (Rand.Chance(item.SelectionChance))
                    yield return item;

            var selected = PickFromPool(pool1, pawn);
            if (selected != null) yield return selected;

            selected = PickFromPool(pool2, pawn);
            if (selected != null) yield return selected;

            selected = PickFromPool(pool3, pawn);
            if (selected != null) yield return selected;

            selected = PickFromPool(pool4, pawn);
            if (selected != null) yield return selected;
        }

        private static SpecRequirementEdit PickFromPool(List<SpecRequirementEdit> pool, Pawn pawn)
        {
            if (pool.NullOrEmpty()) return null;
            
            var valid = pool.Where(a => a.Thing != null && (a.Thing.IsApparel ? a.Thing.apparel.PawnCanWear(pawn) : true));
            
            var validList = valid.ToList();
            if (validList.NullOrEmpty()) return null;
            
            float totalWeight = validList.Sum(i => i.weight * i.SelectionChance);
            if (totalWeight <= 0f) return validList.RandomElement();
            
            float randomValue = Rand.Value * totalWeight;
            float currentWeight = 0f;
            foreach (var item in validList)
            {
                currentWeight += item.weight * item.SelectionChance;
                if (randomValue <= currentWeight) return item;
            }
            return validList.First();
        }

        private static GearItem GetRandomGearItem(List<GearItem> items)
        {
            if (!items.Any()) return null;
            var totalWeight = items.Sum(item => item.weight);
            if (totalWeight <= 0f) return items.RandomElement();
            var randomValue = Rand.Value * totalWeight;
            var currentWeight = 0f;
            foreach (var item in items)
            {
                currentWeight += item.weight;
                if (randomValue <= currentWeight) return item;
            }
            return items.First();
        }
    }
}
