using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace FactionGearCustomizer
{
    public class FactionGearPreviewWindow : Window
    {
        private PawnKindDef kindDef;
        private FactionDef factionDef;
        private Pawn previewPawn;
        private Rot4 rotation = Rot4.South;
        private Vector2 scrollPosition = Vector2.zero;
        private string errorMessage = null;

        // Multi-preview support
        private bool isMultiMode = false;
        private List<PawnKindDef> allKinds;
        private Dictionary<PawnKindDef, Pawn> previewPawns = new Dictionary<PawnKindDef, Pawn>();
        private Dictionary<PawnKindDef, string> previewErrors = new Dictionary<PawnKindDef, string>();

        public override Vector2 InitialSize => isMultiMode ? new Vector2(1100f, 750f) : new Vector2(450f, 650f);

        public FactionGearPreviewWindow(PawnKindDef kindDef, FactionDef factionDef)
        {
            this.kindDef = kindDef;
            this.factionDef = factionDef;
            this.isMultiMode = false;
            CommonInit();
        }

        public FactionGearPreviewWindow(List<PawnKindDef> kinds, FactionDef factionDef)
        {
            this.allKinds = kinds;
            this.factionDef = factionDef;
            this.isMultiMode = true;
            CommonInit();
        }

        private void CommonInit()
        {
            this.doCloseX = true;
            this.forcePause = true;
            this.draggable = true;
            this.resizeable = true;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            if (isMultiMode)
            {
                GenerateAllPreviewPawns();
            }
            else
            {
                GenerateSinglePreviewPawn();
            }
        }

        private void GenerateAllPreviewPawns()
        {
            if (allKinds == null) return;

            // Clear existing
            foreach (var p in previewPawns.Values)
            {
                if (p != null && !p.Destroyed) p.Destroy();
            }
            previewPawns.Clear();
            previewErrors.Clear();

            Faction faction = GetFaction();
            if (faction == null && Current.ProgramState == ProgramState.Playing)
            {
                errorMessage = "Cannot preview: Faction not found in current game.";
                return;
            }

            foreach (var k in allKinds)
            {
                try
                {
                    Pawn p = GeneratePawnInternal(k, faction);
                    if (p != null)
                    {
                        previewPawns[k] = p;
                        PortraitsCache.SetDirty(p);
                    }
                    else
                    {
                        previewErrors[k] = "Failed to generate";
                    }
                }
                catch (Exception ex)
                {
                    previewErrors[k] = ex.Message;
                    Log.Warning($"[FactionGearCustomizer] Error generating preview for {k.defName}: {ex}");
                }
            }
        }

        private void GenerateSinglePreviewPawn()
        {
            errorMessage = null;
            if (previewPawn != null)
            {
                if (!previewPawn.Destroyed) previewPawn.Destroy();
                previewPawn = null;
            }

            try
            {
                Faction faction = GetFaction();
                if (faction == null && Current.ProgramState == ProgramState.Playing)
                {
                    Log.Warning($"[FactionGearCustomizer] Could not find active faction for {factionDef.defName}. Preview might fail.");
                    errorMessage = "Cannot preview: Faction not found in current game.";
                    return;
                }

                previewPawn = GeneratePawnInternal(kindDef, faction);
                
                if (previewPawn == null)
                {
                    Log.Error($"[FactionGearCustomizer] PawnGenerator.GeneratePawn returned null for {kindDef.defName}");
                    errorMessage = "Failed to generate preview pawn.";
                    return;
                }

                PortraitsCache.SetDirty(previewPawn);
            }
            catch (Exception ex)
            {
                Log.Error($"[FactionGearCustomizer] Failed to generate preview pawn: {ex}");
                errorMessage = $"Error: {ex.Message}";
            }
        }

        private Faction GetFaction()
        {
            if (Find.FactionManager != null)
            {
                return Find.FactionManager.FirstFactionOfDef(factionDef);
            }
            return null;
        }

        private Pawn GeneratePawnInternal(PawnKindDef kDef, Faction faction)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(
                kDef, 
                faction, 
                PawnGenerationContext.NonPlayer, 
                -1, 
                true, // forceGenerateNewPawn
                false, // newborn
                false, // allowDead
                false, // allowDowned
                true, // canGeneratePawnRelations
                1f, // colonistRelationChanceFactor
                false, // mustBeCapableOfViolence
                true, // forceAddFreeWarmLayerIfNeeded
                true, // allowGay
                false, // allowFood
                false // allowAddictions
            );
            return PawnGenerator.GeneratePawn(request);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (isMultiMode)
            {
                DoMultiWindowContents(inRect);
            }
            else
            {
                DoSingleWindowContents(inRect);
            }
        }

        private void DoMultiWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 150f, 30f), $"Preview All: {factionDef.LabelCap}");
            
            Rect refreshRect = new Rect(inRect.width - 140f, inRect.y, 140f, 30f);
            if (Widgets.ButtonText(refreshRect, "Reroll All"))
            {
                GenerateAllPreviewPawns();
            }
            Text.Font = GameFont.Small;

            if (errorMessage != null)
            {
                 Widgets.Label(new Rect(inRect.x, inRect.y + 40f, inRect.width, 30f), errorMessage);
                 return;
            }

            Rect outRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f);
            
            // Grid Calculation
            float cardWidth = 220f;
            float cardHeight = 350f;
            float spacing = 10f;
            int columns = Mathf.FloorToInt((outRect.width - 16f) / (cardWidth + spacing));
            if (columns < 1) columns = 1;
            
            int rows = Mathf.CeilToInt((float)allKinds.Count / columns);
            float viewHeight = rows * (cardHeight + spacing);

            Widgets.BeginScrollView(outRect, ref scrollPosition, new Rect(0, 0, outRect.width - 16f, viewHeight));
            
            for (int i = 0; i < allKinds.Count; i++)
            {
                var k = allKinds[i];
                int col = i % columns;
                int row = i / columns;
                
                Rect cardRect = new Rect(col * (cardWidth + spacing), row * (cardHeight + spacing), cardWidth, cardHeight);
                DrawPawnCard(cardRect, k);
            }

            Widgets.EndScrollView();
        }

        private void DrawPawnCard(Rect rect, PawnKindDef k)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(5f);
            
            // Header
            Rect headerRect = new Rect(inner.x, inner.y, inner.width, 24f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(headerRect, k.LabelCap);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // Portrait
            Rect portraitRect = new Rect(inner.x + (inner.width - 150f)/2f, inner.y + 25f, 150f, 220f);
            
            if (previewPawns.TryGetValue(k, out Pawn p) && p != null)
            {
                RenderTexture image = PortraitsCache.Get(p, new Vector2(150f, 220f), rotation, new Vector3(0f, 0f, 0f), 1f);
                if (image != null)
                {
                    GUI.DrawTexture(portraitRect, image);
                }
            }
            else
            {
                string err = previewErrors.ContainsKey(k) ? previewErrors[k] : "No Pawn";
                Widgets.Label(portraitRect, err);
            }

            // Inspect Button
            Rect btnRect = new Rect(inner.x + 10f, inner.yMax - 30f, inner.width - 20f, 24f);
            if (Widgets.ButtonText(btnRect, "Inspect / Edit"))
            {
                // Switch to this pawn in the editor (optional but nice)
                EditorSession.SelectedKindDefName = k.defName;
                EditorSession.GearListScrollPos = Vector2.zero;
                
                // Open single preview
                Find.WindowStack.Add(new FactionGearPreviewWindow(k, factionDef));
            }
        }

        private void DoSingleWindowContents(Rect inRect)
        {
            // Original implementation
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), $"Preview: {kindDef.LabelCap}");
            Text.Font = GameFont.Small;

            if (previewPawn == null || errorMessage != null)
            {
                string errorText = errorMessage ?? "Failed to generate preview pawn. (Faction might not be active)";
                Widgets.Label(new Rect(inRect.x, inRect.y + 40f, inRect.width - 20f, 60f), errorText);
                
                Rect retryRect = new Rect(inRect.x + (inRect.width - 120f) / 2f, inRect.y + 100f, 120f, 30f);
                if (Widgets.ButtonText(retryRect, "Retry"))
                {
                    GenerateSinglePreviewPawn();
                }
                return;
            }

            // Draw Pawn
            Rect pawnRect = new Rect(inRect.x + (inRect.width - 200f) / 2f, inRect.y + 40f, 200f, 300f);
            Widgets.DrawWindowBackground(pawnRect);

            // Render Pawn
            RenderTexture image = PortraitsCache.Get(previewPawn, new Vector2(200f, 300f), rotation, new Vector3(0f, 0f, 0f), 1f);
            if (image != null)
            {
                GUI.DrawTexture(pawnRect, image);
            }
            else
            {
                Widgets.Label(pawnRect, "Portrait unavailable");
            }

            // Rotation Buttons
            Rect rotRect = new Rect(pawnRect.x, pawnRect.yMax + 5f, pawnRect.width, 24f);
            if (Widgets.ButtonText(rotRect.LeftHalf(), "< Rotate"))
            {
                rotation.Rotate(RotationDirection.Counterclockwise);
                PortraitsCache.SetDirty(previewPawn);
            }
            if (Widgets.ButtonText(rotRect.RightHalf(), "Rotate >"))
            {
                rotation.Rotate(RotationDirection.Clockwise);
                PortraitsCache.SetDirty(previewPawn);
            }
            
            // Refresh Button
            Rect refreshRect = new Rect(inRect.x + (inRect.width - 120f) / 2f, rotRect.yMax + 10f, 120f, 30f);
            if (Widgets.ButtonText(refreshRect, "Reroll Pawn"))
            {
                GenerateSinglePreviewPawn();
            }

            // Gear List Summary
            float listY = refreshRect.yMax + 10f;
            Rect listRect = new Rect(inRect.x, listY, inRect.width, inRect.height - listY);
            
            Widgets.BeginScrollView(listRect, ref scrollPosition, new Rect(0, 0, listRect.width - 16f, 500f));
            Listing_Standard list = new Listing_Standard();
            list.Begin(new Rect(0, 0, listRect.width - 16f, 500f));
            
            list.Label("<b>Equipped Gear:</b>");
            if (previewPawn.equipment != null)
            {
                foreach (var eq in previewPawn.equipment.AllEquipmentListForReading)
                {
                    var qualityComp = eq.GetComp<CompQuality>();
                    string qualityStr = qualityComp != null ? qualityComp.Quality.ToString() : "Normal";
                    list.Label($"- {eq.LabelCap} ({qualityStr})");
                }
            }
            
            list.Gap();
            list.Label("<b>Apparel Worn:</b>");
            if (previewPawn.apparel != null)
            {
                foreach (var app in previewPawn.apparel.WornApparel)
                {
                    var qualityComp = app.GetComp<CompQuality>();
                    string qualityStr = qualityComp != null ? qualityComp.Quality.ToString() : "Normal";
                    list.Label($"- {app.LabelCap} ({qualityStr})");
                }
            }

            list.End();
            Widgets.EndScrollView();
        }

        public override void PreClose()
        {
            base.PreClose();
            try
            {
                if (previewPawn != null && !previewPawn.Destroyed)
                {
                    previewPawn.Destroy();
                    previewPawn = null;
                }
                
                if (previewPawns != null)
                {
                    foreach (var p in previewPawns.Values)
                    {
                        if (p != null && !p.Destroyed) p.Destroy();
                    }
                    previewPawns.Clear();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[FactionGearCustomizer] Error destroying preview pawn: {ex.Message}");
            }
        }
    }
}
