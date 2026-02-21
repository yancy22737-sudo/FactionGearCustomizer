using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer.UI.Panels
{
    public static class KindListPanel
    {
        private static Dictionary<string, List<PawnKindDef>> cachedFactionKinds = new Dictionary<string, List<PawnKindDef>>();

        public static void ClearCache()
        {
            cachedFactionKinds.Clear();
        }

        public static void Draw(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            // Title "Kind Defs"
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(innerRect.x, innerRect.y, 150f, 30f), "Kind Defs");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // List area calculation
            float listStartY = innerRect.y + 35f;
            float kindListHeight = innerRect.height - 35f;

            // Draw current preset label at the bottom
            string currentPreset = FactionGearCustomizerMod.Settings.currentPresetName;
            if (!string.IsNullOrEmpty(currentPreset))
            {
                float labelHeight = 24f;
                kindListHeight -= labelHeight;
                
                Rect presetRect = new Rect(innerRect.x, innerRect.y + kindListHeight + 35f, innerRect.width, labelHeight);
                // Adjust Y to be at the bottom of the allocated space
                presetRect.y = innerRect.yMax - labelHeight;

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = Color.cyan;
                Widgets.Label(presetRect, $"Preset: {currentPreset}");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                
                TooltipHandler.TipRegion(presetRect, "Currently active preset. Clicking 'Save' will overwrite this preset.");
            }
            
            Rect kindListOutRect = new Rect(innerRect.x, listStartY, innerRect.width, kindListHeight);

            // Get Kinds to draw
            List<PawnKindDef> kindDefsToDraw = GetKindsToDraw();

            Rect kindListViewRect = new Rect(0, 0, kindListOutRect.width - 16f, kindDefsToDraw.Count * 32f);
            Widgets.BeginScrollView(kindListOutRect, ref EditorSession.KindListScrollPos, kindListViewRect);
            
            float kindY = 0;
            foreach (var kindDef in kindDefsToDraw)
            {
                DrawKindRow(kindDef, kindY, kindListViewRect.width);
                kindY += 32f;
            }
            
            Widgets.EndScrollView();
        }

        private static List<PawnKindDef> GetKindsToDraw()
        {
            if (string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
            {
                return new List<PawnKindDef>();
            }

            if (cachedFactionKinds.TryGetValue(EditorSession.SelectedFactionDefName, out var cachedList))
            {
                return cachedList;
            }

            var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(EditorSession.SelectedFactionDefName);
            var list = FactionGearEditor.GetFactionKinds(factionDef);
            
            cachedFactionKinds[EditorSession.SelectedFactionDefName] = list;
            return list;
        }

        private static void DrawKindRow(PawnKindDef kindDef, float y, float width)
        {
            Rect rowRect = new Rect(0, y, width, 32f);
            
            // Highlight
            if (EditorSession.SelectedKindDefName == kindDef.defName)
            {
                Widgets.DrawHighlightSelected(rowRect);
            }
            else if (Mouse.IsOver(rowRect))
            {
                Widgets.DrawHighlight(rowRect);
            }

            // Selection Click
            if (Widgets.ButtonInvisible(rowRect))
            {
                if (EditorSession.SelectedKindDefName != kindDef.defName)
                {
                    EditorSession.SelectedKindDefName = kindDef.defName;
                    EditorSession.GearListScrollPos = Vector2.zero; // Reset gear list scroll
                }
            }

            // Check Modification Status
            bool isModified = false;
            if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.factionGearData.FirstOrDefault(f => f.factionDefName == EditorSession.SelectedFactionDefName);
                if (factionData != null)
                {
                    var kindData = factionData.kindGearData.FirstOrDefault(k => k.kindDefName == kindDef.defName);
                    if (kindData != null)
                    {
                        isModified = kindData.isModified;
                    }
                }
            }

            // Draw Label
            Rect labelRect = new Rect(rowRect.x + 6f, rowRect.y, rowRect.width - 60f, rowRect.height); // Reserve space for buttons
            string labelText = kindDef.label != null ? kindDef.LabelCap.ToString() : kindDef.defName;
            
            if (isModified)
            {
                GUI.color = Color.yellow;
                Widgets.Label(new Rect(labelRect.x + 20f, labelRect.y + 6f, labelRect.width - 20f, 20f), labelText);
                
                // Modification Icon
                Texture2D modTex = TexCache.ApplyTex ?? Widgets.CheckboxOnTex; // Use fallback
                Widgets.DrawTextureFitted(new Rect(labelRect.x, labelRect.y + 8f, 16f, 16f), modTex, 1f);
                
                GUI.color = Color.white;
            }
            else
            {
                Widgets.Label(new Rect(labelRect.x, labelRect.y + 6f, labelRect.width, 20f), labelText);
            }

            // Copy/Paste Buttons
            float btnSize = 20f;
            float btnSpacing = 4f;
            float btnX = rowRect.width - (btnSize * 2 + btnSpacing + 4f); // Align to right
            float btnY = rowRect.y + (rowRect.height - btnSize) / 2f;

            // Copy
            Texture2D kindCopyTex = TexCache.CopyTex ?? Widgets.CheckboxOnTex;
            Rect copyBtnRect = new Rect(btnX, btnY, btnSize, btnSize);
            if (Widgets.ButtonImage(copyBtnRect, kindCopyTex))
            {
                if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(kindDef.defName))
                {
                    var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                    var kindData = factionData.GetOrCreateKindData(kindDef.defName);
                    EditorSession.CopiedKindGearData = kindData.DeepCopy();
                    Messages.Message("Copied gear from " + labelText, MessageTypeDefOf.TaskCompletion, false);
                }
            }
            TooltipHandler.TipRegion(copyBtnRect, "Copy this KindDef's gear");

            // Paste
            btnX += btnSize + btnSpacing;
            Texture2D kindPasteTex = TexCache.PasteTex ?? Widgets.CheckboxOnTex;
            Rect pasteBtnRect = new Rect(btnX, btnY, btnSize, btnSize);
            
            if (EditorSession.CopiedKindGearData != null)
            {
                if (Widgets.ButtonImage(pasteBtnRect, kindPasteTex))
                {
                    if (!string.IsNullOrEmpty(EditorSession.SelectedFactionDefName) && !string.IsNullOrEmpty(kindDef.defName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(EditorSession.SelectedFactionDefName);
                        var targetKindData = factionData.GetOrCreateKindData(kindDef.defName);
                        if (targetKindData != null)
                        {
                            UndoManager.RecordState(targetKindData);
                            targetKindData.CopyFrom(EditorSession.CopiedKindGearData);
                            targetKindData.isModified = true;
                            FactionGearEditor.MarkDirty(); // Notify editor
                            Messages.Message("Pasted gear to " + labelText, MessageTypeDefOf.TaskCompletion, false);
                        }
                    }
                }
                TooltipHandler.TipRegion(pasteBtnRect, "Paste gear to this KindDef");
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.3f);
                Widgets.ButtonImage(pasteBtnRect, kindPasteTex);
                GUI.color = Color.white;
            }
        }
    }
}
