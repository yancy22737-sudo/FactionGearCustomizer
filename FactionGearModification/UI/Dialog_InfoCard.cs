using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public class Dialog_InfoCard : Window
    {
        private readonly Def def;
        private Vector2 scrollPosition = Vector2.zero;
        
        public override Vector2 InitialSize => new Vector2(600f, 600f);
        
        public Dialog_InfoCard(Def def)
        {
            this.def = def;
            forcePause = false;
            absorbInputAroundWindow = true;
            closeOnAccept = true;
            closeOnCancel = true;
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            string title = def.LabelCap;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), title);
            Text.Font = GameFont.Small;
            
            // Create a larger rectangle for scrolling content
            Rect scrollRect = new Rect(0f, 35f, inRect.width - 16f, 800f); // Increased height for scrolling
            
            // Create the view rect for content
            string description = def.description ?? "No description available.";
            List<string> lines = new List<string> { description };
            
            // Add additional info based on def type
            if (def is ThingDef thingDef)
            {
                lines.Add("\nThing Details:");
                lines.Add($"Tech Level: {thingDef.techLevel}");
                lines.Add($"Base Market Value: {thingDef.BaseMarketValue:F2}");
                lines.Add($"Base Mass: {thingDef.BaseMass:F2}");
                
                if (thingDef.IsWeapon)
                {
                    lines.Add($"\nWeapon Stats:");
                    lines.Add($"Melee Damage: {GetMeleeDamage(thingDef)}");
                    
                    if (thingDef.Verbs != null && thingDef.Verbs.Count > 0)
                    {
                        var verb = thingDef.Verbs[0];
                        lines.Add($"Range: {verb.range:F1}");
                        if (verb.defaultProjectile != null)
                        {
                            lines.Add($"Damage: {verb.defaultProjectile.projectile.GetDamageAmount(null):F1}");
                        }
                    }
                }
                
                if (thingDef.IsApparel && thingDef.apparel != null)
                {
                    lines.Add($"\nApparel Stats:");
                    lines.Add($"Layers: {thingDef.apparel.layers?.Count ?? 0}");
                    lines.Add($"Insulation Cold: {thingDef.GetStatValueAbstract(StatDefOf.Insulation_Cold):F1}");
                    lines.Add($"Insulation Heat: {thingDef.GetStatValueAbstract(StatDefOf.Insulation_Heat):F1}");
                    lines.Add($"Armor Blunt: {thingDef.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt):F2}");
                    lines.Add($"Armor Sharp: {thingDef.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp):F2}");
                    lines.Add($"Armor Heat: {thingDef.GetStatValueAbstract(StatDefOf.ArmorRating_Heat):F2}");
                }
                
                lines.Add($"\nMod Source: {thingDef.modContentPack?.Name ?? "Unknown"}");
            }
            else if (def is FactionDef factionDef)
            {
                lines.Add($"\nFaction Details:");
                lines.Add($"Hidden: {factionDef.hidden}");
                lines.Add($"Natural Enemy: {factionDef.naturalEnemy}");
                
                if (Current.ProgramState == ProgramState.Playing && Find.FactionManager != null)
                {
                    var worldFaction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def == factionDef);
                    if (worldFaction != null)
                    {
                        lines.Add($"Current Goodwill: {worldFaction.PlayerGoodwill}");
                    }
                }
            }
            
            string fullText = string.Join("\n", lines);
            
            // Calculate content height dynamically
            float contentHeight = Text.CalcHeight(fullText, scrollRect.width);
            Rect viewRect = new Rect(0f, 0f, scrollRect.width, contentHeight + 20f);
            
            // Begin scrolling
            Widgets.BeginScrollView(inRect.AtZero(), ref scrollPosition, viewRect);
            
            // Draw the content
            Rect contentRect = new Rect(5f, 40f, viewRect.width - 10f, viewRect.height);
            Widgets.Label(contentRect, fullText);
            
            // End scrolling
            Widgets.EndScrollView();
        }
        
        private float GetMeleeDamage(ThingDef thingDef)
        {
            if (thingDef.tools != null)
            {
                float maxDamage = 0f;
                foreach (var tool in thingDef.tools)
                {
                    if (tool.power > maxDamage)
                    {
                        maxDamage = tool.power;
                    }
                }
                return maxDamage;
            }
            return 0f;
        }
    }
}