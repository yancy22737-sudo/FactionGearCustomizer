using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public static class FactionGearEditor
    {
        private static string selectedFactionDefName = "";
        private static string selectedKindDefName = "";
        private static GearCategory selectedCategory = GearCategory.Weapons;


        private static Vector2 factionListScrollPos = Vector2.zero;
        private static Vector2 kindListScrollPos = Vector2.zero;
        private static Vector2 gearListScrollPos = Vector2.zero;
        private static Vector2 libraryScrollPos = Vector2.zero;
        private static string searchText = "";

        // 用于记录当前展开权重的物品
        private static GearItem expandedGearItem = null;

        private static string selectedModSource = "All";
        private static TechLevel? selectedTechLevel = null;
        private static List<string> cachedModSources = null;

        // [重构] 使用环世界官方的区间滑块
        // 筛选范围
        private static FloatRange rangeFilter = new FloatRange(0f, 100f);
        private static FloatRange damageFilter = new FloatRange(0f, 100f);
        private static FloatRange marketValueFilter = new FloatRange(0f, 10000f);
        
        // 边界值
        private static float minRange = 0f;
        private static float maxRange = 100f;
        private static float minDamage = 0f;
        private static float maxDamage = 100f;
        private static float minMarketValue = 0f;
        private static float maxMarketValue = 10000f;
        
        // 边界值计算标志
        private static bool needCalculateBounds = true;

        // 排序相关字段
        private static string sortField = "Name";
        private static bool sortAscending = true;

        private static KindGearData copiedKindGearData = null;
        
        // 缓存变量，用于优化性能
        private static List<ThingDef> cachedFilteredItems = null;
        private static string lastSearchText = "";
        private static GearCategory lastCategory = GearCategory.Weapons;
        private static string lastSortField = "Name";
        private static bool lastSortAscending = true;
        private static string lastSelectedModSource = "All";
        private static TechLevel? lastSelectedTechLevel = null;
        private static FloatRange lastRangeFilter = new FloatRange(0f, 100f);
        private static FloatRange lastDamageFilter = new FloatRange(0f, 100f);
        private static FloatRange lastMarketValueFilter = new FloatRange(0f, 10000f);
        
        // 图标缓存，用于懒加载
        private static Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();
        private static int maxCacheSize = 500;
        
        // 额外的缓存以提高性能
        private static List<ThingDef> cachedAllWeapons = null;
        private static List<ThingDef> cachedAllMeleeWeapons = null;
        private static List<ThingDef> cachedAllArmors = null;
        private static List<ThingDef> cachedAllApparel = null;
        private static List<ThingDef> cachedAllOthers = null;
        
        // 派系和兵种列表缓存 - 避免每帧重新排序
        private static List<FactionDef> cachedSortedFactions = null;
        private static Dictionary<string, List<PawnKindDef>> cachedFactionKinds = new Dictionary<string, List<PawnKindDef>>();
        
        // 每个缓存的独立过期时间
        private static DateTime weaponsCacheTime = DateTime.MinValue;
        private static DateTime meleeCacheTime = DateTime.MinValue;
        private static DateTime armorsCacheTime = DateTime.MinValue;
        private static DateTime apparelCacheTime = DateTime.MinValue;
        private static DateTime othersCacheTime = DateTime.MinValue;
        private static readonly TimeSpan cacheExpiry = TimeSpan.FromMinutes(5);
        
        // 权重滑块防抖相关
        private static bool isDraggingWeight = false;
        private static float pendingWeightSaveTime = 0f;
        private const float WeightSaveDelay = 0.5f;
        
        // 暂存设置


        public static void DrawEditor(Rect inRect)
        {
            // 规范化字体为环世界标准字体
            Text.Font = GameFont.Small;
            

            // 默认先读取第一个kind
            if (string.IsNullOrEmpty(selectedFactionDefName) && string.IsNullOrEmpty(selectedKindDefName))
            {
                var allFactions = DefDatabase<FactionDef>.AllDefs.OrderBy(f => f.LabelCap.ToString()).ToList();
                if (allFactions.Any())
                {
                    selectedFactionDefName = allFactions.First().defName;
                    
                    // 选择该派系的第一个兵种
                    var factionDef = allFactions.First();
                    if (factionDef.pawnGroupMakers != null)
                    {
                        foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                        {
                            if (pawnGroupMaker.options != null && pawnGroupMaker.options.Any())
                            {
                                var firstOption = pawnGroupMaker.options.First();
                                if (firstOption.kind != null)
                                {
                                    selectedKindDefName = firstOption.kind.defName;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // 1. 顶部按钮栏：预留两行高度(70f)，改为 RightThenDown 向下折行，防止按钮飞出屏幕
            Rect topRect = new Rect(inRect.x, inRect.y, inRect.width, 70f);
            WidgetRow buttonRow = new WidgetRow(topRect.x, topRect.y, UIDirection.RightThenDown, topRect.width, 4f);

            // 核心操作加颜色
            GUI.color = Color.green;
            if (buttonRow.ButtonText("Apply & Save", "Save changes to current game"))
            {
                FactionGearCustomizerMod.Settings.Write();
                Messages.Message("Settings saved successfully!", MessageTypeDefOf.PositiveEvent);

                if (FactionGearCustomizerMod.Settings.presets.Count == 0)
                {
                    Find.WindowStack.Add(new PresetManagerWindow());
                    Messages.Message("Tip: Please create a preset to safely back up your hard work!", MessageTypeDefOf.NeutralEvent);
                }
            }
            GUI.color = Color.cyan;
            if (buttonRow.ButtonText("Presets", "Manage gear presets"))
            {
                Find.WindowStack.Add(new PresetManagerWindow());
            }
            GUI.color = Color.white;

            // 将四个 Reset 按钮合并为一个浮动菜单
            if (buttonRow.ButtonText("Reset Options ▼"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Reset Filters", ResetFilters),
                    new FloatMenuOption("Reset Current Kind", ResetCurrentKind),
                    new FloatMenuOption("Load Default Faction", () => {
                        if (!string.IsNullOrEmpty(selectedFactionDefName))
                        {
                            FactionGearManager.LoadDefaultPresets(selectedFactionDefName);
                            FactionGearCustomizerMod.Settings.Write();
                        }
                    }),
                    new FloatMenuOption("Reset Current Faction", () => {
                        if (!string.IsNullOrEmpty(selectedFactionDefName))
                        {
                            ResetCurrentFaction();
                        }
                    }),
                    new FloatMenuOption("Reset EVERYTHING", () => {
                        FactionGearCustomizerMod.Settings.ResetToDefault();
                    }, MenuOptionPriority.High, null, null, 0f, null, null, true, 0)
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // 隐藏Auto Fill按钮
            // if (buttonRow.ButtonText("Auto Fill"))
            // {
            //     AutoFillGear();
            // }

            // 2. 划分三大主面板：为上面两行的按钮腾出 70f 的空间，防止面板重叠
            Rect mainRect = new Rect(inRect.x, inRect.y + 70f, inRect.width, inRect.height - 70f);
            float totalWidth = mainRect.width - 20f;
            
            // 非对称的黄金比例分配
            Rect leftPanel = new Rect(mainRect.x, mainRect.y, totalWidth * 0.22f, mainRect.height); // 左侧缩减到 22%
            Rect middlePanel = new Rect(leftPanel.xMax + 10f, mainRect.y, totalWidth * 0.40f, mainRect.height); // 中间扩大到 40%
            Rect rightPanel = new Rect(middlePanel.xMax + 10f, mainRect.y, totalWidth * 0.38f, mainRect.height); // 右侧占 38%

            DrawLeftPanel(leftPanel);
            DrawMiddlePanel(middlePanel);
            DrawRightPanel(rightPanel);

            if (isDraggingWeight && Time.time >= pendingWeightSaveTime)
            {
                isDraggingWeight = false;
                FactionGearCustomizerMod.Settings.Write();
            }
        }

        private static void DrawLeftPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 30f), "Factions");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            
            float factionListHeight = innerRect.height * 0.6f;
            Rect factionListOutRect = new Rect(innerRect.x, innerRect.y + 35f, innerRect.width, factionListHeight - 35f);

            // 【修复 4】按照常见频率优先级排序
            var allFactions = DefDatabase<FactionDef>.AllDefs
                .OrderBy(f => GetFactionPriority(f))
                .ThenBy(f => f.label != null ? f.LabelCap.ToString() : f.defName)
                .ToList();

            Rect factionListViewRect = new Rect(0, 0, factionListOutRect.width - 16f, allFactions.Count * 32f);
            Widgets.BeginScrollView(factionListOutRect, ref factionListScrollPos, factionListViewRect);
            float y = 0;
            foreach (var factionDef in allFactions)
            {
                Rect rowRect = new Rect(0, y, factionListViewRect.width, 32f);

                if (selectedFactionDefName == factionDef.defName)
                    Widgets.DrawHighlightSelected(rowRect);
                else if (Mouse.IsOver(rowRect))
                    Widgets.DrawHighlight(rowRect);

                // 【修复】抛弃反射，并增加 try-catch 防止某些Mod的坏图导致报错
                Texture2D factionIcon = null;
                try { factionIcon = factionDef.FactionIcon; } catch { }
                
                if (factionIcon != null)
                {
                    Widgets.DrawTextureFitted(new Rect(rowRect.x + 6f, rowRect.y + 4f, 24f, 24f), factionIcon, 1f);
                }

                // 简化显示，只显示种类和名称
                Rect infoRect = new Rect(rowRect.x + 36f, rowRect.y, rowRect.width - 50f, rowRect.height);
                string factionLabel = factionDef.label != null ? factionDef.LabelCap.ToString() : factionDef.defName;
                
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(infoRect, factionLabel);
                Text.Anchor = TextAnchor.UpperLeft;

                // 计算文字实际宽度，让 info 按钮紧跟其后
                float nameWidth = Text.CalcSize(factionLabel).x;
                Rect infoButtonRect = new Rect(rowRect.x + 36f + nameWidth + 10f, rowRect.y + 8f, 16f, 16f);
                if (Widgets.ButtonInvisible(infoButtonRect))
                {
                    if (Current.ProgramState == ProgramState.Playing)
                    {
                        Find.WindowStack.Add(new Dialog_InfoCard(factionDef));
                    }
                    else
                    {
                        Messages.Message("Cannot open info card outside of gameplay", MessageTypeDefOf.RejectInput, false);
                    }
                }
                Widgets.Label(infoButtonRect, "i");

                // 点击选择派系
                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedFactionDefName = factionDef.defName;
                    selectedKindDefName = "";

                    if (factionDef.pawnGroupMakers != null)
                    {
                        foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                        {
                            if (pawnGroupMaker.options != null && pawnGroupMaker.options.Any())
                            {
                                var firstOption = pawnGroupMaker.options.First();
                                if (firstOption.kind != null)
                                {
                                    selectedKindDefName = firstOption.kind.defName;
                                    break;
                                }
                            }
                        }
                    }
                    kindListScrollPos = Vector2.zero;
                    gearListScrollPos = Vector2.zero;
                }
                y += 32f;
            }
            Widgets.EndScrollView();

            // ================= 兵种列表绘制 =================
            float kindListY = innerRect.y + factionListHeight + 10f;
            float kindListHeight = innerRect.height - factionListHeight - 10f;
            
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(innerRect.x, kindListY, 150f, 30f), "Kind Defs");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // 【修复 2】删去了此处标题栏右侧的复制、粘贴、应用三个图标

            Rect kindListOutRect = new Rect(innerRect.x, kindListY + 35f, innerRect.width, kindListHeight - 35f);

            List<PawnKindDef> kindDefsToDraw;
            if (!string.IsNullOrEmpty(selectedFactionDefName))
            {
                kindDefsToDraw = GetCachedFactionKinds(selectedFactionDefName);
            }
            else
            {
                kindDefsToDraw = DefDatabase<PawnKindDef>.AllDefs
                    .OrderBy(k => k.label != null ? k.LabelCap.ToString() : k.defName)
                    .ToList();
            }

            Rect kindListViewRect = new Rect(0, 0, kindListOutRect.width - 16f, kindDefsToDraw.Count * 32f);
            Widgets.BeginScrollView(kindListOutRect, ref kindListScrollPos, kindListViewRect);
            float kindY = 0;
            
            foreach (var kindDef in kindDefsToDraw)
            {
                Rect rowRect = new Rect(0, kindY, kindListViewRect.width, 32f);
                if (selectedKindDefName == kindDef.defName)
                    Widgets.DrawHighlightSelected(rowRect);
                else if (Mouse.IsOver(rowRect))
                    Widgets.DrawHighlight(rowRect);

                bool isModified = false;
                if (!string.IsNullOrEmpty(selectedFactionDefName))
                {
                    var factionData = FactionGearCustomizerMod.Settings.factionGearData.FirstOrDefault(f => f.factionDefName == selectedFactionDefName);
                    if (factionData != null)
                    {
                        var kindData = factionData.kindGearData.FirstOrDefault(k => k.kindDefName == kindDef.defName);
                        if (kindData != null)
                        {
                            isModified = kindData.isModified;
                        }
                    }
                }

                Rect labelRect = new Rect(rowRect.x + 6f, rowRect.y, rowRect.width - 20f, rowRect.height);
                
                string labelText = kindDef.label != null ? kindDef.LabelCap.ToString() : kindDef.defName;
                
                // 绘制兵种名称
                if (isModified)
                {
                    GUI.color = Color.yellow;
                    Widgets.Label(new Rect(labelRect.x, labelRect.y + 6f, labelRect.width - 60f, 20f), labelText);
                    
                    // 显示修改标记
                    Texture2D modTex = TexCache.ApplyTex ?? Widgets.CheckboxOnTex;
                    Widgets.DrawTextureFitted(new Rect(labelRect.x - 20f, labelRect.y + 8f, 16f, 16f), modTex, 0.8f);
                    
                    GUI.color = Color.white;
                }
                else
                {
                    Widgets.Label(new Rect(labelRect.x, labelRect.y + 6f, labelRect.width - 60f, 20f), labelText);
                }

                // 为每个 kinddef 添加复制粘贴和应用按钮
                float btnX = labelRect.x + labelRect.width - 60f;
                float btnY = labelRect.y + 6f;
                float btnSize = 18f;
                float btnSpacing = 5f;

                // 复制按钮
                Texture2D kindCopyTex = TexCache.CopyTex ?? Widgets.CheckboxOnTex;
                Rect copyBtnRect = new Rect(btnX, btnY, btnSize, btnSize);
                if (Widgets.ButtonImage(copyBtnRect, kindCopyTex))
                {
                    if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(kindDef.defName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                        var kindData = factionData.GetOrCreateKindData(kindDef.defName);
                        copiedKindGearData = kindData.DeepCopy();
                        Messages.Message("Copied gear from " + labelText, MessageTypeDefOf.TaskCompletion, false);
                    }
                }
                TooltipHandler.TipRegion(copyBtnRect, "Copy this KindDef's gear");

                btnX += btnSize + btnSpacing;
                Texture2D kindPasteTex = TexCache.PasteTex ?? Widgets.CheckboxOnTex;
                Rect pasteBtnRect = new Rect(btnX, btnY, btnSize, btnSize);
                if (Widgets.ButtonImage(pasteBtnRect, kindPasteTex))
                {
                    if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(kindDef.defName) && copiedKindGearData != null)
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                        var targetKindData = factionData.GetOrCreateKindData(kindDef.defName);
                        targetKindData.CopyFrom(copiedKindGearData);
                        targetKindData.isModified = true;
                        isDraggingWeight = true;
                        pendingWeightSaveTime = Time.time + WeightSaveDelay;
                        Messages.Message("Pasted gear to " + labelText, MessageTypeDefOf.TaskCompletion, false);
                    }
                }
                TooltipHandler.TipRegion(pasteBtnRect, "Paste gear to this KindDef");

                // 点击兵种名称时设置为当前选中兵种
                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedKindDefName = kindDef.defName;
                    gearListScrollPos = Vector2.zero;
                    
                    // 只在该兵种没有任何装备数据时才加载默认装备
                    if (!string.IsNullOrEmpty(selectedFactionDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                        var kindData = factionData.GetOrCreateKindData(kindDef.defName);
                        
                        // 只有当兵种完全没有装备时才加载默认数据
                        if (!kindData.weapons.Any() && !kindData.meleeWeapons.Any() && 
                            !kindData.armors.Any() && !kindData.apparel.Any() && !kindData.others.Any())
                        {
                            FactionGearManager.LoadKindDefGear(kindDef, kindData);
                            FactionGearCustomizerMod.Settings.Write();
                        }
                    }
                }
                
                kindY += 32f;
            }
            Widgets.EndScrollView();
        }

        private static void DrawMiddlePanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            List<GearItem> gearItemsToDraw = new List<GearItem>();
            KindGearData currentKindData = null;
            if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                currentKindData = factionData.GetOrCreateKindData(selectedKindDefName);
                gearItemsToDraw = GetCurrentCategoryGear(currentKindData);
            }

            // 【修复 2】将 AA (Apply All) 按钮放在 Selected Gear 标题旁边
            Widgets.Label(new Rect(innerRect.x, innerRect.y, 120f, 24f), "Selected Gear");

            Rect clearButtonRect = new Rect(innerRect.xMax - 70f, innerRect.y, 70f, 20f);
            if (Widgets.ButtonText(clearButtonRect, "Clear All"))
            {
                if (currentKindData != null) { ClearAllGear(currentKindData); FactionGearCustomizerMod.Settings.Write(); }
            }

            Rect aaButtonRect = new Rect(clearButtonRect.x - 35f, innerRect.y, 30f, 20f);
            if (Widgets.ButtonText(aaButtonRect, "AA"))
            {
                if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
                {
                    // 添加二次确认对话框，防止误触
                    Find.WindowStack.Add(new Dialog_MessageBox(
                        "Are you sure you want to overwrite ALL Kind Defs in this faction with the current gear loadout?",
                        "Yes",
                        delegate
                        {
                            CopyKindDefGear(); // 复制当前兵种配置
                            ApplyToAllKindsInFaction(); // 应用到全局
                            Messages.Message("Applied current gear to ALL Kind Defs in this faction!", MessageTypeDefOf.PositiveEvent);
                        },
                        "No",
                        null
                    ));
                }
            }
            TooltipHandler.TipRegion(aaButtonRect, "Apply current gear to ALL other Kind Defs in this faction");

            Rect tabRect = new Rect(innerRect.x, innerRect.y + 24f, innerRect.width, 24f);
            DrawCategoryTabs(tabRect);

            float totalFixedHeight = 24f + 24f + 10f;
            float availableHeight = innerRect.height - totalFixedHeight;
            float previewHeight = Mathf.Clamp(availableHeight * 0.3f, 50f, 100f);
            float listHeight = Mathf.Max(availableHeight - previewHeight, 80f);
            Rect listOutRect = new Rect(innerRect.x, tabRect.yMax + 5f, innerRect.width, listHeight);

            // 【修复 1】动态计算展开和折叠的总高度
            float contentHeight = 0f;
            foreach (var item in gearItemsToDraw) {
                contentHeight += (item == expandedGearItem) ? 60f : 30f;
            }
            Rect listViewRect = new Rect(0, 0, listOutRect.width - 16f, Mathf.Max(contentHeight, listOutRect.height));

            Widgets.BeginScrollView(listOutRect, ref gearListScrollPos, listViewRect);
            if (gearItemsToDraw.Any() && currentKindData != null)
            {
                Listing_Standard gearListing = new Listing_Standard();
                gearListing.Begin(listViewRect);
                foreach (var gearItem in gearItemsToDraw.ToList())
                {
                    bool isExpanded = (gearItem == expandedGearItem);
                    // 根据是否展开，分配不同的行高
                    Rect rowRect = gearListing.GetRect(isExpanded ? 56f : 28f);
                    DrawGearItem(rowRect, gearItem, currentKindData, isExpanded);
                    gearListing.Gap(2f);
                }
                gearListing.End();
            }
            Widgets.EndScrollView();

            // 添加价值权重预览（放到底部，合并为一行）
            if (currentKindData != null)
            {
                // 计算预览区域位置，确保在面板底部
                float bottomPreviewHeight = 24f; // 仅一行高度
                float previewY = innerRect.y + innerRect.height - bottomPreviewHeight;
                float actualPreviewHeight = bottomPreviewHeight;
                
                if (actualPreviewHeight > 0 && previewY > listOutRect.yMax)
                {
                    Rect previewRect = new Rect(innerRect.x, previewY, innerRect.width, actualPreviewHeight);
                    // 合并为一行显示
                    float avgValue = GetAverageValue(currentKindData);
                    float avgWeight = GetAverageWeight(currentKindData);
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(previewRect, $"Avg Value: {avgValue:F0} | Avg Weight: {avgWeight:F1}");
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
            }
        }

        private static void DrawRightPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 24f), "Item Library");

            // 添加add all按钮
            Rect addAllButtonRect = new Rect(innerRect.x, innerRect.y + 24f, innerRect.width, 24f);
            if (Widgets.ButtonText(addAllButtonRect, "Add All"))
            {
                if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
                {
                    var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                    var kindData = factionData.GetOrCreateKindData(selectedKindDefName);
                    var currentCategory = GetCurrentCategoryGear(kindData);
                    var itemsToAdd = GetFilteredAndSortedItems();
                    
                    int addedCount = 0;
                    foreach (var thingDef in itemsToAdd)
                    {
                        if (!currentCategory.Any(g => g.thingDefName == thingDef.defName))
                        {
                            currentCategory.Add(new GearItem(thingDef.defName));
                            addedCount++;
                        }
                    }
                    
                    if (addedCount > 0)
                    {
                        kindData.isModified = true;
                        isDraggingWeight = true;
                        pendingWeightSaveTime = Time.time + WeightSaveDelay;
                        Messages.Message($"Added {addedCount} items to gear list", MessageTypeDefOf.TaskCompletion, false);
                    }
                }
            }

            // 筛选选项
            Rect filterRect = new Rect(innerRect.x, innerRect.y + 48f, innerRect.width, 120f);
            DrawFilters(filterRect);

            // 物品列表
            // 修复物品浏览ui布局没到底的问题
            Rect listOutRect = new Rect(innerRect.x, filterRect.yMax + 4f, innerRect.width, innerRect.height - filterRect.yMax - 4f);

            var itemsToDraw = GetFilteredAndSortedItems();
            // 紧凑型列表：进一步压缩物品高度
            float itemHeight = 28f;
            float gapHeight = 4f;
            // 确保列表视图高度正确计算，确保布局能够到底
            float totalHeight = itemsToDraw.Count * (itemHeight + gapHeight);
            // 确保至少有一个最小高度，防止布局异常
            totalHeight = Mathf.Max(totalHeight, listOutRect.height);
            Rect listViewRect = new Rect(0, 0, listOutRect.width - 16f, totalHeight);

            Widgets.BeginScrollView(listOutRect, ref libraryScrollPos, listViewRect);

            if (itemsToDraw.Any())
            {
                float currentY = 0f;
                float viewTop = libraryScrollPos.y;
                float viewBottom = libraryScrollPos.y + listOutRect.height;

                foreach (var thingDef in itemsToDraw)
                {
                    // 判断当前元素是否在可视区域内 (上下扩大半个元素的容错)
                    if (currentY + itemHeight >= viewTop - itemHeight && currentY <= viewBottom + itemHeight)
                    {
                        Rect rowRect = new Rect(0, currentY, listViewRect.width, itemHeight);
                        DrawItemButton(rowRect, thingDef);
                    }
                    
                    // 无论画不画，Y轴都要往下走，撑起滚动条的总高度
                    currentY += itemHeight + gapHeight;
                }
            }
            Widgets.EndScrollView();
        }

        private static void DrawCategoryTabs(Rect rect)
        {
            float tabWidth = rect.width / 5f;
            // 精简标签文本，减少拥挤
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, tabWidth, rect.height), "Ranged"))
            {
                if (selectedCategory != GearCategory.Weapons)
                {
                    selectedCategory = GearCategory.Weapons;
                    expandedGearItem = null; // 【新增】切换页签时自动收起所有展开项
                    CalculateMarketValueBounds();
                }
            }
            if (Widgets.ButtonText(new Rect(rect.x + tabWidth, rect.y, tabWidth, rect.height), "Melee"))
            {
                if (selectedCategory != GearCategory.MeleeWeapons)
                {
                    selectedCategory = GearCategory.MeleeWeapons;
                    expandedGearItem = null; // 【新增】切换页签时自动收起所有展开项
                    CalculateMarketValueBounds();
                }
            }
            if (Widgets.ButtonText(new Rect(rect.x + tabWidth * 2, rect.y, tabWidth, rect.height), "Armors"))
            {
                if (selectedCategory != GearCategory.Armors)
                {
                    selectedCategory = GearCategory.Armors;
                    expandedGearItem = null; // 【新增】切换页签时自动收起所有展开项
                    CalculateMarketValueBounds();
                }
            }
            if (Widgets.ButtonText(new Rect(rect.x + tabWidth * 3, rect.y, tabWidth, rect.height), "Clothes"))
            {
                if (selectedCategory != GearCategory.Apparel)
                {
                    selectedCategory = GearCategory.Apparel;
                    expandedGearItem = null; // 【新增】切换页签时自动收起所有展开项
                    CalculateMarketValueBounds();
                }
            }
            if (Widgets.ButtonText(new Rect(rect.x + tabWidth * 4, rect.y, tabWidth, rect.height), "Others"))
            {
                if (selectedCategory != GearCategory.Others)
                {
                    selectedCategory = GearCategory.Others;
                    expandedGearItem = null; // 【新增】切换页签时自动收起所有展开项
                    CalculateMarketValueBounds();
                }
            }
        }

        private static List<GearItem> GetCurrentCategoryGear(KindGearData kindData)
        {
            switch (selectedCategory)
            {
                case GearCategory.Weapons:
                    return kindData.weapons;
                case GearCategory.MeleeWeapons:
                    return kindData.meleeWeapons;
                case GearCategory.Armors:
                    return kindData.armors;
                case GearCategory.Apparel:
                    return kindData.apparel;
                case GearCategory.Others:
                    return kindData.others;
                default:
                    return new List<GearItem>();
            }
        }

        // 【修复 1】支持点击展开滑块的 UI 重构
        private static void DrawGearItem(Rect rect, GearItem gearItem, KindGearData kindData, bool isExpanded)
        {
            var thingDef = gearItem.ThingDef;
            if (thingDef == null) return;

            Widgets.DrawHighlightIfMouseover(rect);
            if (gearItem.weight != 1f)
            {
                GUI.color = new Color(1f, 0.8f, 0.4f, 0.3f);
                Widgets.DrawHighlight(rect);
                GUI.color = Color.white;
            }

            // 第一行：图标、info按钮、名称、极简权重文本（仅折叠时）、删除按钮
            // 优化布局，确保元素不会拥挤
            Rect row1 = new Rect(rect.x, rect.y, rect.width, 28f);
            Rect iconRect = new Rect(row1.x + 4f, row1.y + 2f, 24f, 24f);
            Rect infoButtonRect = new Rect(iconRect.xMax + 4f, row1.y + 4f, 16f, 16f);
            // 限制名称区域宽度，确保不会与其他元素重叠
            Rect nameRect = new Rect(infoButtonRect.xMax + 4f, row1.y, row1.width - 120f, 28f);
            Rect removeRect = new Rect(row1.xMax - 26f, row1.y + 2f, 24f, 24f);
            
            // 互交区域（点击空白处展开/折叠，避开删除按钮）
            Rect interactRect = new Rect(row1.x, row1.y, row1.width - 30f, 28f);
            if (Widgets.ButtonInvisible(interactRect))
            {
                expandedGearItem = (expandedGearItem == gearItem) ? null : gearItem;
            }

            // 绘制图标（懒加载）
            Texture2D icon = GetIconWithLazyLoading(thingDef);
            if (icon != null)
            {
                GUI.DrawTexture(iconRect, icon);
            }
            else if (thingDef.graphic?.MatSingle?.mainTexture != null)
            {
                GUI.DrawTexture(iconRect, thingDef.graphic.MatSingle.mainTexture);
            }

            // 添加信息按钮（放在物品名称前）
            if (Widgets.ButtonText(infoButtonRect, "i"))
            {
                if (Current.ProgramState == ProgramState.Playing)
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(thingDef));
                }
                else
                {
                    Messages.Message("Cannot open info card outside of gameplay", MessageTypeDefOf.RejectInput, false);
                }
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false; // 防止超长名字换行把 UI 撑爆
            Widgets.Label(nameRect, thingDef.LabelCap);
            Text.WordWrap = true;

            // 折叠状态时，在最右侧极简显示当前权重（仅当权重不等于默认值时显示）
            if (!isExpanded && gearItem.weight != 1f)
            {
                Text.Anchor = TextAnchor.MiddleRight;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(removeRect.x - 45f, row1.y, 40f, 28f), $"W:{gearItem.weight:F1}");
                GUI.color = Color.white;
            }
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonText(removeRect, "X"))
            {
                GetCurrentCategoryGear(kindData).Remove(gearItem);
                kindData.isModified = true;
                // 使用延迟保存机制
                isDraggingWeight = true;
                pendingWeightSaveTime = Time.time + WeightSaveDelay;
                if (expandedGearItem == gearItem) expandedGearItem = null;
            }

            // 第二行：仅在展开时绘制大尺寸精度滑块
            if (isExpanded)
            {
                Rect row2 = new Rect(rect.x, rect.y + 28f, rect.width, 28f);
                Rect sliderRect = new Rect(row2.x + 10f, row2.y + 4f, row2.width - 60f, 20f);
                Rect weightRect = new Rect(row2.xMax - 40f, row2.y, 35f, 28f);

                // 修复滑动条数值不赋值的问题
                float newWeight = Widgets.HorizontalSlider(sliderRect, gearItem.weight, 0.1f, 10f, true);
                if (newWeight != gearItem.weight)
                {
                    gearItem.weight = newWeight;
                    kindData.isModified = true;
                    isDraggingWeight = true;
                    pendingWeightSaveTime = Time.time + WeightSaveDelay;
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(weightRect, gearItem.weight.ToString("F1"));
                Text.Anchor = TextAnchor.UpperLeft;
            }

            // 传入 Func<string> 委托，只有当鼠标悬停超过 0.2 秒触发 Tip 时，才会执行计算！
            TooltipHandler.TipRegion(rect, new TipSignal(() => GetDetailedItemInfo(thingDef), thingDef.GetHashCode()));
        }

        private static void DrawItemButton(Rect rect, ThingDef thingDef)
        {
            // 绘制物品按钮
            // 确保所有元素都严格限制在rect内，防止溢出
            Rect iconRect = new Rect(rect.x, rect.y, 24f, 24f);
            Rect infoButtonRect = new Rect(iconRect.xMax + 6f, rect.y + 4f, 16f, 16f);
            // 限制标签和值的区域，确保不会溢出
            Rect labelRect = new Rect(infoButtonRect.xMax + 6f, rect.y, rect.width - 120f, 28f);

            // 绘制图标（懒加载）
            Texture2D icon = GetIconWithLazyLoading(thingDef);
            if (icon != null)
            {
                Widgets.DrawTextureFitted(iconRect, icon, 1f);
            }

            // 添加信息按钮（放在物品名称前）
            if (Widgets.ButtonText(infoButtonRect, "i"))
            {
                if (Current.ProgramState == ProgramState.Playing)
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(thingDef));
                }
                else
                {
                    Messages.Message("Cannot open info card outside of gameplay", MessageTypeDefOf.RejectInput, false);
                }
            }

            // 绘制名称
            Text.WordWrap = false;
            string itemName = thingDef.LabelCap;
            
            // 计算名称宽度
            float nameWidth = Text.CalcSize(itemName).x;
            float maxNameWidth = labelRect.width;
            
            // 确保名称不超过可用空间
            if (nameWidth > maxNameWidth)
            {
                // 缩短名称以适应空间
                itemName = GenText.Truncate(itemName, maxNameWidth);
            }
            
            // 绘制名称
            Widgets.Label(labelRect, itemName);
            Text.WordWrap = true;

            // 检查物品是否已添加
            bool isAdded = false;
            if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                var kindData = factionData.GetOrCreateKindData(selectedKindDefName);
                var currentCategory = GetCurrentCategoryGear(kindData);
                isAdded = currentCategory.Any(g => g.thingDefName == thingDef.defName);
            }

            // 添加按钮，使用原版图标
            Rect addButtonRect = new Rect(rect.xMax - 28f, rect.y + 2f, 24f, 24f);
            
            // 价格文字靠右对齐，在按钮左边
            Rect priceRect = new Rect(addButtonRect.x - 70f, rect.y, 65f, rect.height);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(priceRect, $"${thingDef.BaseMarketValue:F0}");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            
            // 绘制按钮
            if (isAdded)
            {
                // 如果已添加，显示原版的绿色勾选图标，点击则移除
                if (Widgets.ButtonImage(addButtonRect, Widgets.CheckboxOnTex))
                {
                    if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                        var kindData = factionData.GetOrCreateKindData(selectedKindDefName);
                        var currentCategory = GetCurrentCategoryGear(kindData);

                        // 移除物品
                        var existingItem = currentCategory.FirstOrDefault(g => g.thingDefName == thingDef.defName);
                        if (existingItem != null)
                        {
                            currentCategory.Remove(existingItem);
                            kindData.isModified = true;
                            // 使用延迟保存机制
                            isDraggingWeight = true;
                            pendingWeightSaveTime = Time.time + WeightSaveDelay;
                        }
                    }
                }
            }
            else
            {
                // 如果未添加，显示原版的加号图标 (TexButton.Plus)
                if (Widgets.ButtonImage(addButtonRect, TexButton.Plus))
                {
                    if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                        var kindData = factionData.GetOrCreateKindData(selectedKindDefName);
                        var currentCategory = GetCurrentCategoryGear(kindData);

                        // 添加物品
                        currentCategory.Add(new GearItem(thingDef.defName));
                        kindData.isModified = true;
                        // 使用延迟保存机制
                        isDraggingWeight = true;
                        pendingWeightSaveTime = Time.time + WeightSaveDelay;
                    }
                }
            }

            // 添加悬停提示
            // 传入 Func<string> 委托，只有当鼠标悬停超过 0.2 秒触发 Tip 时，才会执行计算！
            TooltipHandler.TipRegion(rect, new TipSignal(() => GetDetailedItemInfo(thingDef), thingDef.GetHashCode()));
        }

        // 缓存计算结果的数据结构 - 改为 struct，分配在栈上，不产生任何 GC 垃圾
        private struct FilteredItemsCacheKey
        {
            public string SearchText { get; set; }
            public GearCategory Category { get; set; }
            public string SortField { get; set; }
            public bool SortAscending { get; set; }
            public string ModSource { get; set; }
            public TechLevel? TechLevel { get; set; }
            public FloatRange Range { get; set; }
            public FloatRange Damage { get; set; }
            public FloatRange MarketValue { get; set; }
        }
        
        private static Dictionary<FilteredItemsCacheKey, List<ThingDef>> filteredItemsCache = new Dictionary<FilteredItemsCacheKey, List<ThingDef>>();
        
        private static List<ThingDef> GetFilteredAndSortedItems()
        {
            var cacheKey = new FilteredItemsCacheKey
            {
                SearchText = searchText,
                Category = selectedCategory,
                SortField = sortField,
                SortAscending = sortAscending,
                ModSource = selectedModSource,
                TechLevel = selectedTechLevel,
                Range = rangeFilter,
                Damage = damageFilter,
                MarketValue = marketValueFilter
            };
            
            // 检查缓存是否有效
            foreach (var cacheEntry in filteredItemsCache)
            {
                var key = cacheEntry.Key;
                if (key.SearchText == cacheKey.SearchText &&
                    key.Category == cacheKey.Category &&
                    key.SortField == cacheKey.SortField &&
                    key.SortAscending == cacheKey.SortAscending &&
                    key.ModSource == cacheKey.ModSource &&
                    key.TechLevel == cacheKey.TechLevel &&
                    Mathf.Approximately(key.Range.min, cacheKey.Range.min) &&
                    Mathf.Approximately(key.Range.max, cacheKey.Range.max) &&
                    Mathf.Approximately(key.Damage.min, cacheKey.Damage.min) &&
                    Mathf.Approximately(key.Damage.max, cacheKey.Damage.max) &&
                    Mathf.Approximately(key.MarketValue.min, cacheKey.MarketValue.min) &&
                    Mathf.Approximately(key.MarketValue.max, cacheKey.MarketValue.max))
                {
                    return cacheEntry.Value;
                }
            }

            // 获取基础物品列表 - 使用缓存方法避免重复查询
            List<ThingDef> items = new List<ThingDef>();
            switch (selectedCategory)
            {
                case GearCategory.Weapons:
                    items = GetCachedAllWeapons();
                    break;
                case GearCategory.MeleeWeapons:
                    items = GetCachedAllMeleeWeapons();
                    break;
                case GearCategory.Armors:
                    items = GetCachedAllArmors();
                    break;
                case GearCategory.Apparel:
                    items = GetCachedAllApparel();
                    break;
                case GearCategory.Others:
                    items = GetCachedAllOthers();
                    break;
            }

            // 应用搜索过滤 - 使用更高效的字符串比较
            if (!string.IsNullOrEmpty(searchText))
            {
                string lowerSearchText = searchText.ToLower();
                items = items.Where(t => t.label != null && t.label.ToLower().Contains(lowerSearchText)).ToList();
            }

            // 应用模组来源过滤
            if (selectedModSource != "All")
            {
                items = items.Where(t => FactionGearManager.GetModSource(t) == selectedModSource).ToList();
            }

            // 应用科技等级过滤
            if (selectedTechLevel.HasValue)
            {
                items = items.Where(t => t.techLevel == selectedTechLevel.Value).ToList();
            }

            // 应用数值范围过滤 - 缓存计算值 to improve performance
            items = items.Where(t =>
            {
                float range = FactionGearManager.GetWeaponRange(t);
                float damage = FactionGearManager.GetWeaponDamage(t);
                float marketValue = t.BaseMarketValue;

                return range >= rangeFilter.min && range <= rangeFilter.max &&
                       damage >= damageFilter.min && damage <= damageFilter.max &&
                       marketValue >= marketValueFilter.min && marketValue <= marketValueFilter.max;
            }).ToList();

            // 应用排序 - 使用缓存值来避免重复计算
            switch (sortField)
            {
                case "Name":
                    items = sortAscending ? items.OrderBy(t => t.label ?? "").ToList() : items.OrderByDescending(t => t.label ?? "").ToList();
                    break;
                case "MarketValue":
                    items = sortAscending ? items.OrderBy(t => t.BaseMarketValue).ToList() : items.OrderByDescending(t => t.BaseMarketValue).ToList();
                    break;
                case "Range":
                    // Cache range values to avoid repeated calculations
                    var rangePairs = items.Select(t => new { Thing = t, Range = FactionGearManager.GetWeaponRange(t) }).ToList();
                    rangePairs = sortAscending ? rangePairs.OrderBy(x => x.Range).ToList() : rangePairs.OrderByDescending(x => x.Range).ToList();
                    items = rangePairs.Select(x => x.Thing).ToList();
                    break;
                case "Accuracy":
                    // 精度排序 - 使用中距离精度作为基准
                    var accuracyPairs = items.Select(t => new { 
                        Thing = t, 
                        Accuracy = GetWeaponAccuracy(t) 
                    }).ToList();
                    accuracyPairs = sortAscending ? accuracyPairs.OrderBy(x => x.Accuracy).ToList() : accuracyPairs.OrderByDescending(x => x.Accuracy).ToList();
                    items = accuracyPairs.Select(x => x.Thing).ToList();
                    break;
                case "Damage":
                    // Cache damage values to avoid repeated calculations
                    var damagePairs = items.Select(t => new { Thing = t, Damage = FactionGearManager.GetWeaponDamage(t) }).ToList();
                    damagePairs = sortAscending ? damagePairs.OrderBy(x => x.Damage).ToList() : damagePairs.OrderByDescending(x => x.Damage).ToList();
                    items = damagePairs.Select(x => x.Thing).ToList();
                    break;
                case "DPS":
                    // 计算并缓存 DPS 值
                    var dpsPairs = items.Select(t => new { 
                        Thing = t, 
                        DPS = CalculateWeaponDPS(t) 
                    }).ToList();
                    dpsPairs = sortAscending ? dpsPairs.OrderBy(x => x.DPS).ToList() : dpsPairs.OrderByDescending(x => x.DPS).ToList();
                    items = dpsPairs.Select(x => x.Thing).ToList();
                    break;
                case "TechLevel":
                    items = sortAscending ? items.OrderBy(t => t.techLevel).ToList() : items.OrderByDescending(t => t.techLevel).ToList();
                    break;
                case "ModSource":
                    items = sortAscending ? items.OrderBy(t => FactionGearManager.GetModSource(t)).ToList() : items.OrderByDescending(t => FactionGearManager.GetModSource(t)).ToList();
                    break;
                case "Armor_Sharp":
                    // 锐器护甲排序
                    var sharpArmorPairs = items.Select(t => new { 
                        Thing = t, 
                        Armor = t.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) 
                    }).ToList();
                    sharpArmorPairs = sortAscending ? sharpArmorPairs.OrderBy(x => x.Armor).ToList() : sharpArmorPairs.OrderByDescending(x => x.Armor).ToList();
                    items = sharpArmorPairs.Select(x => x.Thing).ToList();
                    break;
                case "Armor_Blunt":
                    // 钝器护甲排序
                    var bluntArmorPairs = items.Select(t => new { 
                        Thing = t, 
                        Armor = t.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt) 
                    }).ToList();
                    bluntArmorPairs = sortAscending ? bluntArmorPairs.OrderBy(x => x.Armor).ToList() : bluntArmorPairs.OrderByDescending(x => x.Armor).ToList();
                    items = bluntArmorPairs.Select(x => x.Thing).ToList();
                    break;
            }

            // 限制缓存大小，避免内存泄漏
            if (filteredItemsCache.Count > 20)
            {
                var oldestKey = filteredItemsCache.Keys.First();
                filteredItemsCache.Remove(oldestKey);
            }
            
            // 更新缓存
            filteredItemsCache[cacheKey] = items;
            return items;
        }

        // 获取武器精度（中距离精度）
        private static float GetWeaponAccuracy(ThingDef weaponDef)
        {
            if (weaponDef == null)
                return 0f;
                
            try
            {
                return weaponDef.GetStatValueAbstract(StatDefOf.AccuracyMedium);
            }
            catch
            {
                return 0f;
            }
        }
        
        // 懒加载图标
        private static Texture2D GetIconWithLazyLoading(ThingDef thingDef)
        {
            if (thingDef == null)
                return null;
                
            string key = thingDef.defName;
            if (iconCache.TryGetValue(key, out Texture2D cachedIcon))
            {
                return cachedIcon;
            }
            
            // 检查缓存大小
            if (iconCache.Count >= maxCacheSize)
            {
                // 移除最早的缓存项
                var oldestKey = iconCache.Keys.First();
                iconCache.Remove(oldestKey);
            }
            
            // 加载图标
            Texture2D icon = thingDef.uiIcon;
            if (icon != null)
            {
                iconCache[key] = icon;
            }
            
            return icon;
        }
        
        // 派系排序优先级（数字越小越靠前）
        private static int GetFactionPriority(FactionDef f)
        {
            if (f == null) return 100;
            string name = f.defName.ToLower();
            if (name.Contains("player")) return 1;
            if (name.Contains("empire")) return 2;
            if (name.Contains("outlander")) return 3;
            if (name.Contains("tribe")) return 4;
            if (name.Contains("pirate")) return 5;
            if (name.Contains("mechanoid") || name.Contains("insect")) return 10;
            return 6; // 其他 Mod 派系
        }

        // 获取平均价值
        private static float GetAverageValue(KindGearData kindData)
        {
            List<GearItem> allGear = new List<GearItem>();
            allGear.AddRange(kindData.weapons);
            allGear.AddRange(kindData.meleeWeapons);
            allGear.AddRange(kindData.armors);
            allGear.AddRange(kindData.apparel);
            allGear.AddRange(kindData.others);

            if (!allGear.Any()) return 0f;

            float totalValue = 0f;
            int count = 0;

            foreach (var gearItem in allGear)
            {
                var thingDef = gearItem.ThingDef;
                if (thingDef != null)
                {
                    totalValue += thingDef.BaseMarketValue;
                    count++;
                }
            }

            return count > 0 ? totalValue / count : 0f;
        }

        // 获取平均权重
        private static float GetAverageWeight(KindGearData kindData)
        {
            List<GearItem> allGear = new List<GearItem>();
            allGear.AddRange(kindData.weapons);
            allGear.AddRange(kindData.meleeWeapons);
            allGear.AddRange(kindData.armors);
            allGear.AddRange(kindData.apparel);
            allGear.AddRange(kindData.others);

            if (!allGear.Any()) return 0f;

            float totalWeight = 0f;
            foreach (var gearItem in allGear)
            {
                totalWeight += gearItem.weight;
            }

            return totalWeight / allGear.Count;
        }
        
        // 计算武器DPS（伤害/冷却时间）
        private static float CalculateWeaponDPS(ThingDef weaponDef)
        {
            if (weaponDef == null)
                return 0f;
                
            float damage = FactionGearManager.GetWeaponDamage(weaponDef);
            float cooldown = 1f; // 默认冷却时间
            
            try
            {
                // 获取武器的射击/攻击间隔
                if (weaponDef.IsRangedWeapon && weaponDef.Verbs != null && weaponDef.Verbs.Count > 0)
                {
                    var verb = weaponDef.Verbs.FirstOrDefault(v => v.isPrimary) ?? weaponDef.Verbs.First();
                    // 使用预热时间作为主要冷却指标（避免访问可能不存在的 CooldownTime 属性）
                    cooldown = verb.warmupTime + 0.5f; // 假设射击间隔为 0.5 秒
                }
                else if (weaponDef.IsMeleeWeapon && weaponDef.tools != null && weaponDef.tools.Count > 0)
                {
                    // 近战武器使用工具的平均冷却时间
                    cooldown = weaponDef.tools.Average(tool => tool.cooldownTime);
                }
            }
            catch
            {
                cooldown = 1f;
            }
            
            if (cooldown <= 0)
                cooldown = 1f;
                
            return damage / cooldown;
        }
        
        // 计算当前分类物品的市场价值边界
        private static void CalculateMarketValueBounds()
        {
            // 获取当前分类的所有物品
            List<ThingDef> allItems = new List<ThingDef>();
            switch (selectedCategory)
            {
                case GearCategory.Weapons:
                    allItems = GetCachedAllWeapons();
                    break;
                case GearCategory.MeleeWeapons:
                    allItems = GetCachedAllMeleeWeapons();
                    break;
                case GearCategory.Armors:
                    allItems = GetCachedAllArmors();
                    break;
                case GearCategory.Apparel:
                    allItems = GetCachedAllApparel();
                    break;
                case GearCategory.Others:
                    allItems = GetCachedAllOthers();
                    break;
            }

            // 应用初步筛选（排除搜索和数值筛选，但保留mod和科技等级筛选）
            if (selectedModSource != "All")
            {
                allItems = allItems.Where(t => FactionGearManager.GetModSource(t) == selectedModSource).ToList();
            }

            if (selectedTechLevel.HasValue)
            {
                allItems = allItems.Where(t => t.techLevel == selectedTechLevel.Value).ToList();
            }

            // 计算市场价值边界
            if (allItems.Any())
            {
                minMarketValue = allItems.Min(t => t.BaseMarketValue);
                maxMarketValue = allItems.Max(t => t.BaseMarketValue);
                
                // 确保边界值合理
                if (minMarketValue == maxMarketValue)
                {
                    maxMarketValue = minMarketValue + 100f;
                }
                
                // 调整当前筛选范围以适应新边界
                if (marketValueFilter.min < minMarketValue)
                    marketValueFilter.min = minMarketValue;
                if (marketValueFilter.max > maxMarketValue)
                    marketValueFilter.max = maxMarketValue;
                
                // 确保最小值不大于最大值
                if (marketValueFilter.min > marketValueFilter.max)
                {
                    marketValueFilter.min = minMarketValue;
                    marketValueFilter.max = maxMarketValue;
                }
            }
            else
            {
                // 如果没有物品，使用默认边界
                minMarketValue = 0f;
                maxMarketValue = 10000f;
                marketValueFilter = new FloatRange(0f, 10000f);
            }
        }

        private static void DrawFilters(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            // 搜索框
            Rect searchRect = listing.GetRect(24f);
            Rect searchLabelRect = searchRect.LeftPartPixels(60f);
            Rect searchInputRect = searchRect.RightPartPixels(searchRect.width - 60f);
            Widgets.Label(searchLabelRect, "Search:");
            string newSearchText = Widgets.TextField(searchInputRect, searchText);
            if (newSearchText != searchText)
            {
                searchText = newSearchText;
            }
            
            // 搜索框一键清空按钮
            if (!string.IsNullOrEmpty(searchText))
            {
                Rect clearButtonRect = new Rect(searchInputRect.xMax - 18f, searchInputRect.y + 2f, 16f, 16f);
                if (Widgets.ButtonImage(clearButtonRect, Widgets.CheckboxOffTex))
                {
                    searchText = "";
                }
                TooltipHandler.TipRegion(clearButtonRect, "Clear search");
            }

            // 筛选区 - 50%宽度布局
            Rect filterRowRect = listing.GetRect(24f);
            Rect modSourceRect = new Rect(filterRowRect.x, filterRowRect.y, filterRowRect.width / 2 - 2f, filterRowRect.height);
            Rect techLevelRect = new Rect(filterRowRect.x + filterRowRect.width / 2 + 2f, filterRowRect.y, filterRowRect.width / 2 - 2f, filterRowRect.height);

            // 模组来源筛选 - 下拉菜单
            if (cachedModSources == null)
            {
                cachedModSources = GetUniqueModSources();
            }
            string[] modSourceOptions = new string[cachedModSources.Count + 1];
            modSourceOptions[0] = "All";
            cachedModSources.CopyTo(modSourceOptions, 1);
            if (Widgets.ButtonText(modSourceRect, $"Mod: {selectedModSource}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                for (int i = 0; i < modSourceOptions.Length; i++)
                {
                    string option = modSourceOptions[i];
                    options.Add(new FloatMenuOption(option, delegate
                    {
                        selectedModSource = option;
                        // 切换mod来源时重新计算市场价值边界
                        CalculateMarketValueBounds();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // 科技等级筛选 - 下拉菜单
            var techLevels = Enum.GetValues(typeof(TechLevel)).Cast<TechLevel>().ToArray();
            string[] techLevelOptions = new string[techLevels.Length + 1];
            techLevelOptions[0] = "All";
            for (int i = 0; i < techLevels.Length; i++)
            {
                techLevelOptions[i + 1] = techLevels[i].ToString();
            }
            if (Widgets.ButtonText(techLevelRect, $"Tech: {selectedTechLevel?.ToString() ?? "All"}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                for (int i = 0; i < techLevelOptions.Length; i++)
                {
                    string option = techLevelOptions[i];
                    options.Add(new FloatMenuOption(option, delegate
                    {
                        selectedTechLevel = i == 0 ? null : (TechLevel?)Enum.Parse(typeof(TechLevel), techLevelOptions[i]);
                        // 切换科技等级时重新计算市场价值边界
                        CalculateMarketValueBounds();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // 排序区 - 70% + 30%宽度布局
            Rect sortRowRect = listing.GetRect(24f);
            Rect sortFieldRect = new Rect(sortRowRect.x, sortRowRect.y, sortRowRect.width * 0.7f - 2f, sortRowRect.height);
            Rect sortOrderRect = new Rect(sortRowRect.x + sortRowRect.width * 0.7f + 2f, sortRowRect.y, sortRowRect.width * 0.3f - 2f, sortRowRect.height);

            // 智能动态排序字段 - 根据装备分类显示相应选项
            List<string> dynamicSortOptions = new List<string> { "Name", "MarketValue", "TechLevel", "ModSource" };
            if (selectedCategory == GearCategory.Weapons)
            {
                dynamicSortOptions.AddRange(new[] { "Range", "Accuracy", "Damage", "DPS" });
            }
            else if (selectedCategory == GearCategory.MeleeWeapons)
            {
                dynamicSortOptions.AddRange(new[] { "Damage", "DPS" });
            }
            else if (selectedCategory == GearCategory.Armors || selectedCategory == GearCategory.Apparel)
            {
                dynamicSortOptions.AddRange(new[] { "Armor_Sharp", "Armor_Blunt" });
            }

            int currentSortIndex = dynamicSortOptions.IndexOf(sortField);
            if (currentSortIndex == -1)
            {
                currentSortIndex = 0;
                sortField = dynamicSortOptions[0];
            }
            if (Widgets.ButtonText(sortFieldRect, $"Sort: {sortField}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (string option in dynamicSortOptions)
                {
                    options.Add(new FloatMenuOption(option, delegate
                    {
                        sortField = option;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // 升序/降序切换
            if (Widgets.ButtonText(sortOrderRect, sortAscending ? "↑" : "↓"))
            {
                sortAscending = !sortAscending;
            }

            // 滑块使用整数显示，去除冗余标签
            Rect marketValueRect = listing.GetRect(28f);
            // 使用整数显示，只保留滑块内部的数值
            Widgets.FloatRange(marketValueRect, 3, ref marketValueFilter, minMarketValue, maxMarketValue, null, ToStringStyle.Integer);

            // 根据装备分类显示特定的数值筛选
            if (selectedCategory == GearCategory.Weapons || selectedCategory == GearCategory.MeleeWeapons)
            {
                Rect rangeRect = listing.GetRect(32f);
                Widgets.FloatRange(rangeRect, 12346, ref rangeFilter, 0f, maxRange, null, ToStringStyle.Integer);

                Rect damageRect = listing.GetRect(32f);
                Widgets.FloatRange(damageRect, 12347, ref damageFilter, 0f, maxDamage, null, ToStringStyle.Integer);
            }

            listing.End();
        }

        private static List<ThingDef> GetCachedAllWeapons()
        {
            if (cachedAllWeapons == null || DateTime.Now - weaponsCacheTime > cacheExpiry)
            {
                cachedAllWeapons = FactionGearManager.GetAllWeapons();
                weaponsCacheTime = DateTime.Now;
            }
            return cachedAllWeapons;
        }
        
        private static List<ThingDef> GetCachedAllMeleeWeapons()
        {
            if (cachedAllMeleeWeapons == null || DateTime.Now - meleeCacheTime > cacheExpiry)
            {
                cachedAllMeleeWeapons = FactionGearManager.GetAllMeleeWeapons();
                meleeCacheTime = DateTime.Now;
            }
            return cachedAllMeleeWeapons;
        }
        
        private static List<ThingDef> GetCachedAllArmors()
        {
            if (cachedAllArmors == null || DateTime.Now - armorsCacheTime > cacheExpiry)
            {
                cachedAllArmors = FactionGearManager.GetAllArmors();
                armorsCacheTime = DateTime.Now;
            }
            return cachedAllArmors;
        }
        
        private static List<ThingDef> GetCachedAllApparel()
        {
            if (cachedAllApparel == null || DateTime.Now - apparelCacheTime > cacheExpiry)
            {
                cachedAllApparel = FactionGearManager.GetAllApparel();
                apparelCacheTime = DateTime.Now;
            }
            return cachedAllApparel;
        }
        
        private static List<ThingDef> GetCachedAllOthers()
        {
            if (cachedAllOthers == null || DateTime.Now - othersCacheTime > cacheExpiry)
            {
                cachedAllOthers = FactionGearManager.GetAllOthers();
                othersCacheTime = DateTime.Now;
            }
            return cachedAllOthers;
        }
        
        private static List<FactionDef> GetCachedSortedFactions()
        {
            if (cachedSortedFactions == null)
            {
                cachedSortedFactions = DefDatabase<FactionDef>.AllDefs
                    .OrderBy(f => f.label != null ? f.LabelCap.ToString() : f.defName)
                    .ToList();
            }
            return cachedSortedFactions;
        }
        
        private static List<PawnKindDef> GetCachedFactionKinds(string factionDefName)
        {
            if (string.IsNullOrEmpty(factionDefName))
                return new List<PawnKindDef>();
                
            if (!cachedFactionKinds.TryGetValue(factionDefName, out var kinds))
            {
                var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(factionDefName);
                kinds = new List<PawnKindDef>();
                
                if (factionDef != null && factionDef.pawnGroupMakers != null)
                {
                    var uniqueKindDefNames = new HashSet<string>();
                    foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                    {
                        if (pawnGroupMaker.options != null)
                        {
                            foreach (var option in pawnGroupMaker.options)
                            {
                                if (option.kind != null && uniqueKindDefNames.Add(option.kind.defName))
                                {
                                    kinds.Add(option.kind);
                                }
                            }
                        }
                    }
                }
                
                kinds = kinds.OrderBy(k => k.label != null ? k.LabelCap.ToString() : k.defName).ToList();
                cachedFactionKinds[factionDefName] = kinds;
            }
            return kinds;
        }
        
        private static List<string> GetUniqueModSources()
        {
            List<string> modSources = new List<string>();
            switch (selectedCategory)
            {
                case GearCategory.Weapons:
                    modSources.AddRange(GetCachedAllWeapons().Select(FactionGearManager.GetModSource).Distinct());
                    break;
                case GearCategory.MeleeWeapons:
                    modSources.AddRange(GetCachedAllMeleeWeapons().Select(FactionGearManager.GetModSource).Distinct());
                    break;
                case GearCategory.Armors:
                    modSources.AddRange(GetCachedAllArmors().Select(FactionGearManager.GetModSource).Distinct());
                    break;
                case GearCategory.Apparel:
                    modSources.AddRange(GetCachedAllApparel().Select(FactionGearManager.GetModSource).Distinct());
                    break;
                case GearCategory.Others:
                    modSources.AddRange(GetCachedAllOthers().Select(FactionGearManager.GetModSource).Distinct());
                    break;
            }
            return modSources.Distinct().ToList();
        }

        private static void DrawValueWeightPreview(Rect rect, KindGearData kindData)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(innerRect);

            // 计算平均市场价值
            var allGear = GetCurrentCategoryGear(kindData);
            if (allGear.Any())
            {
                float totalValue = 0f;
                int count = 0;
                foreach (var gear in allGear)
                {
                    var thingDef = gear.ThingDef;
                    if (thingDef != null)
                    {
                        totalValue += thingDef.BaseMarketValue * gear.weight;
                        count++;
                    }
                }
                float avgValue = totalValue / allGear.Count; // 使用总数而非非空数，保持一致性
                listing.Label($"Average Item Value: {avgValue:F2} silver");
            }
            else
            {
                listing.Label("Average Item Value: 0.00 silver");
            }

            // 显示权重分布
            if (allGear.Any())
            {
                float minWeight = allGear.Min(g => g.weight);
                float maxWeight = allGear.Max(g => g.weight);
                float avgWeight = allGear.Average(g => g.weight);
                listing.Label($"Weight Range: {minWeight:F2} - {maxWeight:F2}");
                listing.Label($"Average Weight: {avgWeight:F2}");
            }

            listing.End();
        }

        private static string GetDetailedItemInfo(ThingDef thingDef)
        {
            List<string> lines = new List<string>();

            // 物品名称
            lines.Add(thingDef.LabelCap.ToString());
            lines.Add("");

            // 基本信息
            lines.Add($"Tech Level: {thingDef.techLevel}");
            lines.Add($"Market Value: {thingDef.BaseMarketValue:F0} silver");
            lines.Add($"Mass: {thingDef.BaseMass:F2} kg");

            // 添加DPS信息
            if (thingDef.IsWeapon)
            {
                float dps = CalculateDPS(thingDef);
                lines.Add($"DPS: {dps:F2}");
                if (dps > 0)
                {
                    lines.Add($"Market Value per DPS: {(thingDef.BaseMarketValue / dps):F2} silver/DPS");
                }
            }

            // 武器信息
            if (thingDef.IsRangedWeapon)
            {
                lines.Add("");
                lines.Add("Weapon Info:");
                lines.Add($"Range: {FactionGearManager.GetWeaponRange(thingDef):F1} tiles");
                lines.Add($"Damage: {FactionGearManager.GetWeaponDamage(thingDef):F1} damage");
                
                // 【绝杀修复】去除错误的 verb.accuracyTouch 调用，使用官方 StatDef 方法获取
                lines.Add($"Accuracy (Touch): {thingDef.GetStatValueAbstract(StatDefOf.AccuracyTouch):P0}");
                lines.Add($"Accuracy (Short): {thingDef.GetStatValueAbstract(StatDefOf.AccuracyShort):P0}");
                lines.Add($"Accuracy (Medium): {thingDef.GetStatValueAbstract(StatDefOf.AccuracyMedium):P0}");
                lines.Add($"Accuracy (Long): {thingDef.GetStatValueAbstract(StatDefOf.AccuracyLong):P0}");

                if (thingDef.Verbs != null && thingDef.Verbs.Count > 0)
                {
                    var verb = thingDef.Verbs[0];
                    if (verb.defaultProjectile != null)
                    {
                        lines.Add($"Projectile: {verb.defaultProjectile.LabelCap}");
                    }
                }
            }
            else if (thingDef.IsMeleeWeapon)
            {
                lines.Add("");
                lines.Add("Melee Info:");
                lines.Add($"Damage: {FactionGearManager.GetWeaponDamage(thingDef):F1} damage");
                if (thingDef.tools != null && thingDef.tools.Count > 0)
                {
                    var tool = thingDef.tools[0];
                    lines.Add($"Damage: {tool.power:F2} damage");
                    lines.Add($"Cooldown: {tool.cooldownTime:F2}s");
                }
            }

            // 装备信息
            if (thingDef.IsApparel)
            {
                lines.Add("");
                lines.Add("Apparel Info:");
                lines.Add($"Blunt Armor: {thingDef.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt):F2}%");
                lines.Add($"Sharp Armor: {thingDef.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp):F2}%");
                lines.Add($"Heat Armor: {thingDef.GetStatValueAbstract(StatDefOf.ArmorRating_Heat):F2}%");
                lines.Add($"Insulation - Cold: {thingDef.GetStatValueAbstract(StatDefOf.Insulation_Cold):F1}°C");
                lines.Add($"Insulation - Heat: {thingDef.GetStatValueAbstract(StatDefOf.Insulation_Heat):F1}°C");
                if (thingDef.apparel != null)
                {
                    lines.Add($"Layer: {thingDef.apparel.layers.FirstOrDefault()?.LabelCap}");
                }
            }

            // Mod信息
            lines.Add("");
            lines.Add($"Mod Source: {FactionGearManager.GetModSource(thingDef)}");

            return string.Join("\n", lines);
        }

        private static float CalculateDPS(ThingDef thingDef)
        {
            if (!thingDef.IsWeapon) return 0f;

            float damage = FactionGearManager.GetWeaponDamage(thingDef);
            float speed = 1f; // 默认攻击速度

            if (thingDef.IsRangedWeapon && thingDef.Verbs != null && thingDef.Verbs.Count > 0)
            {
                var verb = thingDef.Verbs[0];
                speed = verb.defaultCooldownTime;
            }
            else if (thingDef.IsMeleeWeapon && thingDef.tools != null && thingDef.tools.Count > 0)
            {
                var tool = thingDef.tools[0];
                speed = tool.cooldownTime;
            }

            return speed > 0 ? damage / speed : 0f;
        }

        private static void ClearAllGear(KindGearData kindData)
        {
            kindData.weapons.Clear();
            kindData.meleeWeapons.Clear();
            kindData.armors.Clear();
            kindData.apparel.Clear();
            kindData.others.Clear();
            kindData.isModified = true;
        }

        private static void ResetCurrentFaction()
        {
            if (!string.IsNullOrEmpty(selectedFactionDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                factionData.ResetToDefault();
                FactionGearCustomizerMod.Settings.Write();
            }
        }

        private static void ResetCurrentKind()
        {
            if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                var kindData = factionData.GetOrCreateKindData(selectedKindDefName);
                kindData.ResetToDefault();
                FactionGearManager.LoadKindDefGear(DefDatabase<PawnKindDef>.GetNamedSilentFail(selectedKindDefName), kindData);
                FactionGearCustomizerMod.Settings.Write();
            }
        }

        private static void ResetFilters()
        {
            searchText = "";
            selectedModSource = "All";
            selectedTechLevel = null;
            rangeFilter = new FloatRange(0f, 100f);
            damageFilter = new FloatRange(0f, 100f);
            marketValueFilter = new FloatRange(0f, 10000f);
            sortField = "Name";
            sortAscending = true;
        }

        private static void AutoFillGear()
        {
            var allFactions = DefDatabase<FactionDef>.AllDefs;
            foreach (var factionDef in allFactions)
            {
                if (factionDef.pawnGroupMakers != null)
                {
                    var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(factionDef.defName);
                    foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                    {
                        if (pawnGroupMaker.options != null)
                        {
                            foreach (var option in pawnGroupMaker.options)
                            {
                                if (option.kind != null)
                                {
                                    var kindData = factionData.GetOrCreateKindData(option.kind.defName);
                                    // 只有当该兵种没有任何装备时才填充默认装备
                                    if (!kindData.weapons.Any() && !kindData.meleeWeapons.Any() && 
                                        !kindData.armors.Any() && !kindData.apparel.Any() && !kindData.others.Any())
                                    {
                                        FactionGearManager.LoadKindDefGear(option.kind, kindData);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            FactionGearCustomizerMod.Settings.Write();
        }

        private static void CopyKindDefGear()
        {
            if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                var kindData = factionData.GetOrCreateKindData(selectedKindDefName);
                copiedKindGearData = kindData.DeepCopy();
                Messages.Message("Copied KindDef gear!", MessageTypeDefOf.TaskCompletion, false);
            }
        }

        private static void PasteKindDefGear()
        {
            if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName) && copiedKindGearData != null)
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                var targetKindData = factionData.GetOrCreateKindData(selectedKindDefName);
                targetKindData.CopyFrom(copiedKindGearData);
                targetKindData.isModified = true;
                isDraggingWeight = true;
                pendingWeightSaveTime = Time.time + WeightSaveDelay;
                Messages.Message("Pasted gear to KindDef!", MessageTypeDefOf.TaskCompletion, false);
            }
        }

        private static void ApplyToAllKindsInFaction()
        {
            if (!string.IsNullOrEmpty(selectedFactionDefName) && copiedKindGearData != null)
            {
                var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(selectedFactionDefName);
                if (factionDef != null && factionDef.pawnGroupMakers != null)
                {
                    var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                    
                    foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                    {
                        if (pawnGroupMaker.options != null)
                        {
                            foreach (var option in pawnGroupMaker.options)
                            {
                                if (option.kind != null)
                                {
                                    var targetKindData = factionData.GetOrCreateKindData(option.kind.defName);
                                    targetKindData.CopyFrom(copiedKindGearData);
                                    targetKindData.isModified = true;
                                }
                            }
                        }
                    }
                    
                    isDraggingWeight = true;
                    pendingWeightSaveTime = Time.time + WeightSaveDelay;
                    Messages.Message("Applied to ALL KindDefs in Faction!", MessageTypeDefOf.TaskCompletion, false);
                }
            }
        }
    }
}