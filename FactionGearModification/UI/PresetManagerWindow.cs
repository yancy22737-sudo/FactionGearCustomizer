using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public class PresetManagerWindow : Window
    {
        // [修复] 环世界原生窗口必须通过重写 InitialSize 来设定弹窗大小
        public override Vector2 InitialSize => new Vector2(850f, 650f);

        private Vector2 presetListScrollPos = Vector2.zero;
        private Vector2 detailsScrollPos = Vector2.zero;
        private string newPresetName = "";
        private string newPresetDescription = "";
        private FactionGearPreset selectedPreset = null;
        private Vector2 factionPreviewScrollPos = Vector2.zero;
        private Vector2 modListScrollPos = Vector2.zero;
        private string presetSearchText = "";

        public PresetManagerWindow() : base()
        {
            // 删除错误的 windowRect 设定
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.forcePause = true; // 建议加上：打开预设管理器时暂停游戏底层时间
        }

        public override void DoWindowContents(Rect inRect)
        {
            // 布局
            Rect listRect = new Rect(inRect.x, inRect.y, 300, inRect.height);
            Rect detailsRect = new Rect(inRect.x + 310, inRect.y, inRect.width - 310, inRect.height);

            // 绘制预设列表
            DrawPresetList(listRect);
            
            // 绘制预设详情
            DrawPresetDetails(detailsRect);
        }

        private void DrawPresetList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);
            
            // 1. 顶部标题和搜索框
            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 24f), "Saved Presets");
            presetSearchText = DrawTextFieldWithPlaceholder(new Rect(innerRect.x, innerRect.y + 24f, innerRect.width, 24f), presetSearchText, "Search presets...");
            
            // 2. 列表区域动态高度
            float bottomAreaHeight = 95f;
            Rect listOutRect = new Rect(innerRect.x, innerRect.y + 55f, innerRect.width, innerRect.height - 55f - bottomAreaHeight);
            
            // 3. 执行搜索过滤
            List<FactionGearPreset> presets = FactionGearCustomizerMod.Settings.presets;
            if (!string.IsNullOrEmpty(presetSearchText))
            {
                presets = presets.Where(p => p.name.ToLower().Contains(presetSearchText.ToLower())).ToList();
            }
            
            Rect listViewRect = new Rect(0, 0, listOutRect.width - 16f, presets.Count * 30f);
            
            Widgets.BeginScrollView(listOutRect, ref presetListScrollPos, listViewRect);
            float y = 0;
            foreach (var preset in presets)
            {
                Rect rowRect = new Rect(0, y, listViewRect.width, 24f);
                if (selectedPreset == preset)
                    Widgets.DrawHighlightSelected(rowRect);
                else if (Mouse.IsOver(rowRect))
                    Widgets.DrawHighlight(rowRect);
                
                string displayName = preset.name;
                bool isActive = preset.name == FactionGearCustomizerMod.Settings.currentPresetName;
                if (isActive)
                {
                    displayName += " (Active)";
                    GUI.color = Color.green;
                }
                
                Widgets.Label(rowRect, displayName);
                GUI.color = Color.white;
                
                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedPreset = preset;
                }
                y += 30f;
            }
            Widgets.EndScrollView();
            
            // 【新增】将新建预设移到左侧底部
            float bottomY = listOutRect.yMax + 5f;
            Widgets.DrawLineHorizontal(innerRect.x, bottomY, innerRect.width);
            
            // Import Button
            Rect importRect = new Rect(innerRect.x, bottomY + 5f, innerRect.width, 24f);
            if (Widgets.ButtonText(importRect, "Import from Clipboard"))
            {
                ImportPreset();
            }
            TooltipHandler.TipRegion(importRect, "Import a preset from base64 string in your clipboard.");
            
            Widgets.DrawLineHorizontal(innerRect.x, bottomY + 34f, innerRect.width);

            // Create New Section
            Widgets.Label(new Rect(innerRect.x, bottomY + 40f, innerRect.width, 20f), "Create New:");
            newPresetName = DrawTextFieldWithPlaceholder(new Rect(innerRect.x, bottomY + 62f, innerRect.width - 65f, 24f), newPresetName, "Name...");
            
            if (Widgets.ButtonText(new Rect(innerRect.xMax - 60f, bottomY + 62f, 60f, 24f), "Add"))
            {
                CreateNewPreset();
            }
        }

        private void DrawPresetDetails(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            if (selectedPreset == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "Please select a preset from the left list.");
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            Rect innerRect = rect.ContractedBy(10f);
            
            // 底部按钮区域高度
            float bottomHeight = 35f;
            // 内容区域高度
            float contentHeight = innerRect.height - bottomHeight - 5f;
            
            Rect contentRect = new Rect(innerRect.x, innerRect.y, innerRect.width, contentHeight);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(contentRect);

            listing.Label("Preset Details:");
            
            // Name
            Rect nameRect = listing.GetRect(24f);
            string newName = DrawTextFieldWithPlaceholder(nameRect, selectedPreset.name, "Preset Name...");
            if (newName != selectedPreset.name)
            {
                selectedPreset.name = newName;
            }
            listing.Gap(4f);
            
            // Description
            Rect descRect = listing.GetRect(50f);
            string newDesc = DrawTextFieldWithPlaceholder(descRect, selectedPreset.description, "Description...", true);
            if (newDesc != selectedPreset.description)
            {
                selectedPreset.description = newDesc;
            }
            
            listing.Gap(10f);
            
            // Save/Update Section
            listing.Label("<b>Management:</b>");
            
            // Two columns for update buttons
            Rect updateRow = listing.GetRect(30f);
            if (Widgets.ButtonText(updateRow.LeftHalf().ContractedBy(2f), "Save Name/Desc"))
            {
                 SavePreset();
                 Messages.Message("Preset metadata saved.", MessageTypeDefOf.TaskCompletion, false);
            }
            
            if (Widgets.ButtonText(updateRow.RightHalf().ContractedBy(2f), "Update from Game")) 
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    $"Overwrite preset '{selectedPreset.name}' with current game settings?",
                    "Yes", delegate { SaveFromCurrentSettings(); Messages.Message("Preset updated from game settings.", MessageTypeDefOf.PositiveEvent); },
                    "No", null
                ));
            }
            TooltipHandler.TipRegion(updateRow.RightHalf(), "Overwrite this preset's gear data with your current in-game configuration.");

            listing.GapLine();

            // Mod List
            listing.Label("Required Mods:");
            DrawModList(listing.GetRect(100f)); 
            listing.Gap(10f);

            listing.Label("Faction Preview:");
            // 使用剩余空间，但要保留底部按钮空间
            float remainingHeight = contentRect.height - listing.CurHeight;
            if (remainingHeight > 50f)
            {
                DrawFactionPreview(listing.GetRect(remainingHeight));
            }
            
            listing.End();

            // Bottom Actions
            Rect bottomRect = new Rect(innerRect.x, innerRect.yMax - 30f, innerRect.width, 30f);
            float btnWidth = (bottomRect.width - 10f) / 3f;
            
            if (Widgets.ButtonText(new Rect(bottomRect.x, bottomRect.y, btnWidth, 30f), "Load Preset")) ApplyPreset();
            TooltipHandler.TipRegion(new Rect(bottomRect.x, bottomRect.y, btnWidth, 30f), "Apply this preset to your game.");
            
            if (Widgets.ButtonText(new Rect(bottomRect.x + btnWidth + 5f, bottomRect.y, btnWidth, 30f), "Export")) ExportPreset();
            TooltipHandler.TipRegion(new Rect(bottomRect.x + btnWidth + 5f, bottomRect.y, btnWidth, 30f), "Copy preset to clipboard for sharing.");
            
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (Widgets.ButtonText(new Rect(bottomRect.x + (btnWidth + 5f) * 2, bottomRect.y, btnWidth, 30f), "Delete")) DeletePreset();
            GUI.color = Color.white;
        }

        private void CreateNewPreset()
        {
            // 如果玩家没填名字，自动生成一个带时间戳的默认名
            string finalName = string.IsNullOrEmpty(newPresetName) ? "New Preset " + DateTime.Now.ToString("MM-dd HH:mm") : newPresetName;
            
            var existingPreset = FactionGearCustomizerMod.Settings.presets.FirstOrDefault(p => p.name == finalName);
            if (existingPreset != null)
            {
                // 如果名称已存在，弹出覆盖确认对话框
                Find.WindowStack.Add(new Dialog_MessageBox(
                    $"A preset named '{finalName}' already exists. Do you want to overwrite it?",
                    "Overwrite", delegate
                    {
                        // 覆盖现有预设
                        existingPreset.SaveFromCurrentSettings(FactionGearCustomizerMod.Settings.factionGearData);
                        existingPreset.description = newPresetDescription;
                        FactionGearCustomizerMod.Settings.UpdatePreset(existingPreset);
                        selectedPreset = existingPreset;
                        newPresetName = "";
                        newPresetDescription = "";
                        Messages.Message($"Preset '{finalName}' overwritten with current settings!", MessageTypeDefOf.PositiveEvent);
                    },
                    "Cancel", null, null, true));
            }
            else
            {
                // 创建新预设
                var newPreset = new FactionGearPreset();
                newPreset.name = finalName;
                newPreset.description = newPresetDescription;
                
                // 【关键修复】新建时直接抓取当前游戏内的数据，不再生成空壳
                newPreset.SaveFromCurrentSettings(FactionGearCustomizerMod.Settings.factionGearData);
                
                FactionGearCustomizerMod.Settings.AddPreset(newPreset);
                // [New] Set as current preset
                FactionGearCustomizerMod.Settings.currentPresetName = newPreset.name;
                
                selectedPreset = newPreset;
                newPresetName = "";
                newPresetDescription = "";
                Messages.Message("New preset created and saved from current settings!", MessageTypeDefOf.PositiveEvent);
            }
        }

        private void SavePreset()
        {
            if (selectedPreset != null)
            {
                selectedPreset.CalculateRequiredMods();
                FactionGearCustomizerMod.Settings.UpdatePreset(selectedPreset);
            }
        }

        private void ApplyPreset()
        {
            if (selectedPreset == null) return;

            // [新增] 检查模组依赖
            List<string> missingMods = new List<string>();
            foreach (var modName in selectedPreset.requiredMods)
            {
                if (!LoadedModManager.RunningMods.Any(m => m.Name == modName))
                {
                    missingMods.Add(modName);
                }
            }

            if (missingMods.Any())
            {
                string missingList = string.Join("\n- ", missingMods);
                Find.WindowStack.Add(new Dialog_MessageBox(
                    $"Warning: The following mods required by this preset are missing or not active:\n\n- {missingList}\n\nApplying this preset may result in missing items or errors. Continue?",
                    "Yes (Risk it)", 
                    delegate { ShowApplyConfirmation(); },
                    "No (Cancel)", 
                    null, 
                    null, 
                    true
                ));
            }
            else
            {
                ShowApplyConfirmation();
            }
        }

        private void ShowApplyConfirmation()
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                "Do you want to CLEAR current custom gear before applying this preset?\n\nYes: Overwrite everything (Recommended)\nNo: Merge preset with current tweaks",
                "Yes (Overwrite)", delegate
                {
                    // 彻底清空当前设置
                    FactionGearCustomizerMod.Settings.ResetToDefault();
                    ExecuteApplyPreset();
                },
                "No (Merge)", delegate
                {
                    ExecuteApplyPreset();
                },
                null, true));
        }

        private void ExecuteApplyPreset()
        {
            if (selectedPreset == null) return;
            
            // 这里放你原来 ApplyPreset 里面的深拷贝代码
            foreach (var factionData in selectedPreset.factionGearData)
            {
                var existingFactionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(factionData.factionDefName);
                foreach (var kindData in factionData.kindGearData)
                {
                    // [修复] 必须使用深拷贝 (Deep Copy) 隔离内存！否则修改当前装备会连带摧毁预设数据
                    KindGearData clonedData = new KindGearData(kindData.kindDefName)
                    {
                        isModified = kindData.isModified,
                        weapons = kindData.weapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                        meleeWeapons = kindData.meleeWeapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                        armors = kindData.armors.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                        apparel = kindData.apparel.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                        others = kindData.others.Select(g => new GearItem(g.thingDefName, g.weight)).ToList()
                    };
                    existingFactionData.AddOrUpdateKindData(clonedData);
                }
            }
            
            // [New] Set as current preset
            FactionGearCustomizerMod.Settings.currentPresetName = selectedPreset.name;
            
            FactionGearCustomizerMod.Settings.Write();
            // [优化] 添加原版右上角浮动提示，让玩家知道点下去了
            Messages.Message("Preset applied successfully to current game!", MessageTypeDefOf.PositiveEvent);
        }

        private void DeletePreset()
        {
            if (selectedPreset != null)
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    $"Are you sure you want to permanently delete preset '{selectedPreset.name}'?",
                    "Delete", delegate
                    {
                        // [New] If deleting current preset, clear currentPresetName
                        if (FactionGearCustomizerMod.Settings.currentPresetName == selectedPreset.name)
                        {
                            FactionGearCustomizerMod.Settings.currentPresetName = null;
                        }

                        FactionGearCustomizerMod.Settings.RemovePreset(selectedPreset);
                        selectedPreset = null;
                        Messages.Message("Preset deleted.", MessageTypeDefOf.NeutralEvent);
                    },
                    "Cancel", null, null, true));
            }
        }

        private void SaveFromCurrentSettings()
        {
            if (selectedPreset != null)
            {
                selectedPreset.SaveFromCurrentSettings(FactionGearCustomizerMod.Settings.factionGearData);
                FactionGearCustomizerMod.Settings.UpdatePreset(selectedPreset);
            }
        }

        private void ExportPreset()
        {
            if (selectedPreset != null)
            {
                try
                {
                    // 使用新的 PresetIOManager 导出预设
                    string base64Content = PresetIOManager.ExportToBase64(selectedPreset);
                    if (!string.IsNullOrEmpty(base64Content))
                    {
                        // 复制到剪贴板
                        GUIUtility.systemCopyBuffer = base64Content;
                        Log.Message("[FactionGearCustomizer] Preset exported to clipboard!");
                        Messages.Message($"Preset '{selectedPreset.name}' exported to clipboard!", MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Log.Message("[FactionGearCustomizer] Export failed!");
                    }
                }
                catch (System.Exception e)
                {
                    Log.Message("[FactionGearCustomizer] Export failed: " + e.Message);
                }
            }
        }

        private void ImportPreset()
        {
            try
            {
                // 从剪贴板读取
                string base64Content = GUIUtility.systemCopyBuffer;
                if (string.IsNullOrEmpty(base64Content))
                {
                    Log.Message("[FactionGearCustomizer] Clipboard is empty!");
                    Messages.Message("Clipboard is empty!", MessageTypeDefOf.RejectInput, false);
                    return;
                }
                
                // 使用新的 PresetIOManager 导入预设
                FactionGearPreset newPreset = PresetIOManager.ImportFromBase64(base64Content);
                if (newPreset != null)
                {
                    // 检查重名并处理
                    string originalName = newPreset.name;
                    string finalName = originalName;
                    int count = 1;
                    while (FactionGearCustomizerMod.Settings.presets.Any(p => p.name == finalName))
                    {
                        finalName = $"{originalName} (Imported {count})";
                        count++;
                    }
                    newPreset.name = finalName;

                    // 添加到预设列表
                    FactionGearCustomizerMod.Settings.AddPreset(newPreset);
                    selectedPreset = newPreset;
                    Log.Message("[FactionGearCustomizer] Preset imported successfully!");
                    Messages.Message($"Preset '{newPreset.name}' imported from clipboard!", MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Log.Message("[FactionGearCustomizer] Import failed: Invalid preset data!");
                    Messages.Message("Import failed: Invalid preset data in clipboard!", MessageTypeDefOf.RejectInput, false);
                }
            }
            catch (System.Exception e)
            {
                Log.Message("[FactionGearCustomizer] Import failed: " + e.Message);
                Messages.Message("Import failed: " + e.Message, MessageTypeDefOf.RejectInput, false);
            }
        }

        private void DrawFactionPreview(Rect rect)
        {
            if (selectedPreset != null && selectedPreset.factionGearData.Any())
            {
                Widgets.DrawBox(rect);
                Rect innerRect = rect.ContractedBy(5f);
                
                Widgets.BeginScrollView(innerRect, ref factionPreviewScrollPos, new Rect(0, 0, innerRect.width - 16f, selectedPreset.factionGearData.Count * 40f));
                float y = 0;
                
                foreach (var factionData in selectedPreset.factionGearData)
                {
                    var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(factionData.factionDefName);
                    string factionName = factionDef != null ? factionDef.LabelCap.ToString() : factionData.factionDefName;
                    
                    Widgets.Label(new Rect(0, y, innerRect.width, 24f), factionName);
                    y += 25f;
                    
                    foreach (var kindData in factionData.kindGearData)
                    {
                        var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindData.kindDefName);
                        string kindName = kindDef != null ? kindDef.LabelCap.ToString() : kindData.kindDefName;
                        
                        Widgets.Label(new Rect(20, y, innerRect.width - 20f, 24f), "- " + kindName);
                        y += 20f;
                    }
                    y += 10f;
                }
                
                Widgets.EndScrollView();
            }
            else
            {
                Widgets.Label(rect, "No faction data in preset.");
            }
        }

        private void DrawModList(Rect rect)
        {
            if (selectedPreset != null && selectedPreset.requiredMods.Any())
            {
                Widgets.DrawBox(rect);
                Rect innerRect = rect.ContractedBy(5f);
                
                Widgets.BeginScrollView(innerRect, ref modListScrollPos, new Rect(0, 0, innerRect.width - 16f, selectedPreset.requiredMods.Count * 24f));
                float y = 0;
                
                foreach (var mod in selectedPreset.requiredMods)
                {
                    bool isActive = LoadedModManager.RunningMods.Any(m => m.Name == mod);
                    GUI.color = isActive ? Color.green : Color.red;
                    
                    Rect labelRect = new Rect(0, y, innerRect.width, 24f);
                    Widgets.Label(labelRect, mod + (isActive ? "" : " (Missing)"));
                    
                    if (!isActive)
                    {
                        TooltipHandler.TipRegion(labelRect, "This mod is not active in your current game.");
                    }
                    
                    y += 24f;
                }
                GUI.color = Color.white;
                
                Widgets.EndScrollView();
            }
            else
            {
                Widgets.Label(rect, "No required mods.");
            }
        }

        private string DrawTextFieldWithPlaceholder(Rect rect, string text, string placeholder, bool isMultiLine = false)
        {
            string result;
            if (isMultiLine)
            {
                result = Widgets.TextArea(rect, text);
            }
            else
            {
                result = Widgets.TextField(rect, text);
            }

            if (string.IsNullOrEmpty(result))
            {
                var anchor = Text.Anchor;
                var color = GUI.color;
                Text.Anchor = isMultiLine ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                Widgets.Label(new Rect(rect.x + 5f, rect.y, rect.width - 5f, rect.height), placeholder);
                GUI.color = color;
                Text.Anchor = anchor;
            }
            return result;
        }
    }
}