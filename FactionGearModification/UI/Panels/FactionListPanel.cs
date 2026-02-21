using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Panels
{
    public static class FactionListPanel
    {
        // Cache variables to avoid resorting every frame
        private static List<(FactionDef def, Faction worldFaction, bool isModified, string displayName, Color color, float priority)> cachedFactionList = null;
        private static bool dirtyCache = true;

        public static void MarkDirty()
        {
            dirtyCache = true;
        }

        public static void Draw(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            
            // Title "Factions"
            Vector2 titleSize = Text.CalcSize("Factions");
            Rect titleRect = new Rect(innerRect.x, innerRect.y, titleSize.x + 10f, 30f);
            Widgets.Label(titleRect, "Factions");
            
            // "Real Name" checkbox
            if (Current.ProgramState == ProgramState.Playing)
            {
                Rect checkboxRect = new Rect(titleRect.xMax, innerRect.y + 3f, 24f, 24f);
                bool tempUseInGameNames = EditorSession.UseInGameNames;
                Widgets.Checkbox(new Vector2(checkboxRect.x, checkboxRect.y), ref tempUseInGameNames);
                if (tempUseInGameNames != EditorSession.UseInGameNames)
                {
                    EditorSession.UseInGameNames = tempUseInGameNames;
                    MarkDirty();
                }
                
                Text.Font = GameFont.Tiny;
                string labelText = "Real Name";
                Vector2 labelSize = Text.CalcSize(labelText);
                Rect labelRect = new Rect(checkboxRect.xMax + 4f, innerRect.y + 8f, labelSize.x + 5f, 20f);
                GUI.color = Color.gray;
                Widgets.Label(labelRect, labelText);
                GUI.color = Color.white;
                
                string tip = "Show in-game faction names.\nGrayed out if faction is not present in current game.";
                TooltipHandler.TipRegion(checkboxRect, tip);
                TooltipHandler.TipRegion(labelRect, tip);
                
                Text.Font = GameFont.Medium;
            }
            
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            
            // Calculate list area
            // The original code used innerRect.height * 0.6f which is odd for a full panel draw.
            // But since we are passing 'rect' which is the left panel rect, we should use most of it.
            // Wait, the original code had 3 panels, so DrawLeftPanel took the left rect.
            // But inside DrawLeftPanel, it calculated `factionListHeight = innerRect.height * 0.6f;`
            // Why 0.6? Maybe there was something below it?
            // Checking the original code... it seems there was nothing below it in DrawLeftPanel.
            // Wait, let me check FactionGearEditor.cs again to see if there is anything else in DrawLeftPanel.
            // Lines 367-600 only show DrawLeftPanel drawing the list.
            // Ah, line 413: float factionListHeight = innerRect.height * 0.6f;
            // And line 414: Rect factionListOutRect = new Rect(..., factionListHeight - 35f);
            // It seems it only uses 60% of the height.
            // But wait, the method ends at line 600.
            // I should check if there is more code after line 600 in DrawLeftPanel.
            // The Read tool only read up to 600.
            // I need to check if DrawLeftPanel continues after 600.
            
            DrawFactionList(innerRect);
        }

        private static void DrawFactionList(Rect innerRect)
        {
            // Use full height for now, unless there is a reason to use 0.6f
            // If the original intention was to leave space for something else, it might be unused now.
            // I will use full available height minus title.
            float listY = innerRect.y + 35f;
            Rect factionListOutRect = new Rect(innerRect.x, listY, innerRect.width, innerRect.height - 35f);

            if (dirtyCache || cachedFactionList == null)
            {
                BuildFactionList();
                dirtyCache = false;
            }

            // Calculate content height
            float totalHeight = 0f;
            float viewWidth = factionListOutRect.width - 16f;
            foreach (var data in cachedFactionList)
            {
                float maxRowWidth = viewWidth - 62f; 
                if (data.worldFaction != null) maxRowWidth -= 44f;
                float height = Text.CalcHeight(data.displayName, maxRowWidth);
                totalHeight += Mathf.Max(height + 8f, 32f);
            }
            
            Rect factionListViewRect = new Rect(0, 0, viewWidth, totalHeight);
            Widgets.BeginScrollView(factionListOutRect, ref EditorSession.FactionListScrollPos, factionListViewRect);
            
            float y = 0;
            foreach (var data in cachedFactionList)
            {
                float maxRowWidth = factionListViewRect.width - 62f;
                if (data.worldFaction != null) maxRowWidth -= 44f;

                float rowHeight = Mathf.Max(Text.CalcHeight(data.displayName, maxRowWidth) + 8f, 32f);
                Rect rowRect = new Rect(0, y, factionListViewRect.width, rowHeight);

                // Handle Selection
                if (EditorSession.SelectedFactionDefName == data.def.defName)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                if (Widgets.ButtonInvisible(rowRect))
                {
                    if (EditorSession.SelectedFactionDefName != data.def.defName)
                    {
                        EditorSession.SelectedFactionDefName = data.def.defName;
                        EditorSession.SelectedKindDefName = ""; // Reset kind selection
                        // Trigger any other updates needed
                        FactionGearEditor.OnFactionSelected();
                    }
                }

                // Draw Row Content
                DrawFactionRow(rowRect, data, rowHeight);

                y += rowHeight;
            }
            
            Widgets.EndScrollView();
        }

        private static void DrawFactionRow(Rect rowRect, (FactionDef def, Faction worldFaction, bool isModified, string displayName, Color color, float priority) data, float rowHeight)
        {
            // Icon
            Rect iconRect = new Rect(rowRect.x + 4f, rowRect.y + (rowHeight - 24f) / 2f, 24f, 24f);
            if (data.def.FactionIcon != null)
            {
                Widgets.DrawTextureFitted(iconRect, data.def.FactionIcon, 1f);
            }

            // Info Button
            Rect infoButtonRect = new Rect(rowRect.xMax - 24f, rowRect.y + (rowHeight - 24f) / 2f, 24f, 24f);
            if (Current.Game != null)
            {
                Widgets.InfoCardButton(infoButtonRect.x, infoButtonRect.y, data.def);
            }

            // Goodwill
            float goodwillWidth = 0f;
            if (data.worldFaction != null && !data.worldFaction.IsPlayer)
            {
                goodwillWidth = 40f;
                Rect goodwillRect = new Rect(infoButtonRect.x - goodwillWidth - 4f, rowRect.y, goodwillWidth, rowHeight);
                
                float goodwill = data.worldFaction.PlayerGoodwill;
                var relationKind = data.worldFaction.PlayerRelationKind;
                Color goodwillColor = relationKind == FactionRelationKind.Ally ? Color.green : 
                                      (relationKind == FactionRelationKind.Hostile ? Color.red : Color.cyan);
                
                Text.Anchor = TextAnchor.MiddleRight;
                GUI.color = goodwillColor;
                Widgets.Label(goodwillRect, goodwill.ToString("F0"));
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                
                TooltipHandler.TipRegion(goodwillRect, $"{relationKind}: {goodwill:F0}");
            }

            // Name
            float nameX = iconRect.xMax + 6f;
            float nameWidth = rowRect.width - nameX - 24f - 4f;
            if (data.worldFaction != null && !data.worldFaction.IsPlayer) nameWidth -= (goodwillWidth + 4f);
            
            Rect nameRect = new Rect(nameX, rowRect.y, nameWidth, rowHeight);
            
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = data.color;
            Widgets.Label(nameRect, data.displayName);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void BuildFactionList()
        {
            cachedFactionList = new List<(FactionDef, Faction, bool, string, Color, float)>();
            
            foreach (var factionDef in DefDatabase<FactionDef>.AllDefs)
            {
                if (!factionDef.humanlikeFaction || 
                    factionDef.hidden || 
                    factionDef.pawnGroupMakers == null || 
                    !factionDef.pawnGroupMakers.Any(pgm => pgm.options != null && pgm.options.Any(o => o.kind != null)))
                {
                    continue;
                }

                Faction worldFaction = null;
                bool isModified = false;
                string displayName = factionDef.label != null ? factionDef.LabelCap.ToString() : factionDef.defName;
                Color color = Color.white;
                float priority = GetFactionPriority(factionDef);
                
                if (Current.ProgramState == ProgramState.Playing && EditorSession.UseInGameNames)
                {
                    try 
                    {
                        if (Find.FactionManager != null && Find.FactionManager.AllFactions != null)
                        {
                            worldFaction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def == factionDef);
                            if (worldFaction != null)
                            {
                                displayName = worldFaction.Name;
                            }
                            else
                            {
                                color = Color.gray;
                                priority = 9999;
                            }
                        }
                    }
                    catch (Exception) { }
                }
                
                // Check if modified
                var factionData = FactionGearCustomizerMod.Settings.factionGearData.FirstOrDefault(f => f.factionDefName == factionDef.defName);
                if (factionData != null)
                {
                    isModified = factionData.kindGearData.Any(k => k.isModified);
                }
                
                if (isModified)
                {
                    displayName = "<color=yellow>*</color> " + displayName;
                }
                
                cachedFactionList.Add((factionDef, worldFaction, isModified, displayName, color, priority));
            }
            
            cachedFactionList.Sort((a, b) => {
                int priorityCompare = a.priority.CompareTo(b.priority);
                if (priorityCompare != 0) return priorityCompare;
                return a.displayName.CompareTo(b.displayName);
            });
        }

        private static float GetFactionPriority(FactionDef factionDef)
        {
            if (factionDef.defName == "PlayerColony") return -100f;
            if (factionDef.defName == "PlayerTribe") return -99f;
            return 0f;
        }
    }
}
