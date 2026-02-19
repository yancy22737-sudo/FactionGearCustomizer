using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public class FactionGearCustomizerMod : Mod
    {
        public static FactionGearCustomizerSettings Settings;

        public FactionGearCustomizerMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<FactionGearCustomizerSettings>();
            Log.Message("[FactionGearCustomizer] Loading success!");
            var harmony = new Harmony("yancy.factiongearcustomizer");
            harmony.PatchAll();
        }

        public override string SettingsCategory() => "Faction Gear Customizer";

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            FactionGearEditor.DrawEditor(inRect);
        }
    }

    public class FactionGearPreset : IExposable
    {
        public string name;
        public string description;
        public Dictionary<string, KindGearData> kindGearData = new Dictionary<string, KindGearData>();

        public FactionGearPreset() { }

        public FactionGearPreset(string name, string description = "")
        {
            this.name = name;
            this.description = description;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref description, "description");
            Scribe_Collections.Look(ref kindGearData, "kindGearData", LookMode.Value, LookMode.Deep);
            if (kindGearData == null)
                kindGearData = new Dictionary<string, KindGearData>();
        }
    }

    public class FactionGearCustomizerSettings : ModSettings
    {
        public Dictionary<string, FactionGearData> factionGearData = new Dictionary<string, FactionGearData>();
        public List<FactionGearPreset> presets = new List<FactionGearPreset>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref factionGearData, "factionGearData", LookMode.Value, LookMode.Deep);
            if (factionGearData == null)
                factionGearData = new Dictionary<string, FactionGearData>();

            Scribe_Collections.Look(ref presets, "presets", LookMode.Deep);
            if (presets == null)
                presets = new List<FactionGearPreset>();
        }

        public void ResetToDefault()
        {
            factionGearData.Clear();
            Write();
        }

        public FactionGearData GetOrCreateFactionData(string factionDefName)
        {
            if (!factionGearData.TryGetValue(factionDefName, out var data))
            {
                data = new FactionGearData(factionDefName);
                factionGearData[factionDefName] = data;
            }
            return data;
        }

        public void AddPreset(FactionGearPreset preset)
        {
            presets.Add(preset);
            Write();
        }

        public void RemovePreset(FactionGearPreset preset)
        {
            presets.Remove(preset);
            Write();
        }

        public void UpdatePreset(FactionGearPreset preset)
        {
            Write();
        }
    }

    public class FactionGearData : IExposable
    {
        public string factionDefName;
        public Dictionary<string, KindGearData> kindGearData = new Dictionary<string, KindGearData>();

        public FactionGearData() { }

        public FactionGearData(string factionDefName)
        {
            this.factionDefName = factionDefName;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref factionDefName, "factionDefName");
            Scribe_Collections.Look(ref kindGearData, "kindGearData", LookMode.Value, LookMode.Deep);
            if (kindGearData == null)
                kindGearData = new Dictionary<string, KindGearData>();
        }

        public KindGearData GetOrCreateKindData(string kindDefName)
        {
            if (!kindGearData.TryGetValue(kindDefName, out var data))
            {
                data = new KindGearData(kindDefName);
                kindGearData[kindDefName] = data;
            }
            return data;
        }

        public void ResetToDefault()
        {
            kindGearData.Clear();
        }
    }

    public class KindGearData : IExposable
    {
        public string kindDefName;
        public List<GearItem> weapons = new List<GearItem>();
        public List<GearItem> meleeWeapons = new List<GearItem>();
        public List<GearItem> armors = new List<GearItem>();
        public List<GearItem> apparel = new List<GearItem>();
        public List<GearItem> accessories = new List<GearItem>();
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
            Scribe_Collections.Look(ref accessories, "accessories", LookMode.Deep);
            Scribe_Values.Look(ref isModified, "isModified", false);

            if (weapons == null) weapons = new List<GearItem>();
            if (meleeWeapons == null) meleeWeapons = new List<GearItem>();
            if (armors == null) armors = new List<GearItem>();
            if (apparel == null) apparel = new List<GearItem>();
            if (accessories == null) accessories = new List<GearItem>();
        }

        public void ResetToDefault()
        {
            weapons.Clear();
            meleeWeapons.Clear();
            armors.Clear();
            apparel.Clear();
            accessories.Clear();
            isModified = false;
        }
    }

    public class GearItem : IExposable
    {
        public string thingDefName;
        public float weight = 1f;

        public GearItem() { }

        public GearItem(string thingDefName, float weight = 1f)
        {
            this.thingDefName = thingDefName;
            this.weight = weight;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref thingDefName, "thingDefName");
            Scribe_Values.Look(ref weight, "weight", 1f);
        }

        public ThingDef ThingDef => DefDatabase<ThingDef>.GetNamedSilentFail(thingDefName);
    }

    // [修复] 类名从 FactionGearCustomizer 改为 FactionGearManager，解决调用错误
    public static class FactionGearManager
    {
        public static void LoadDefaultPresets()
        {
            foreach (var factionDef in DefDatabase<FactionDef>.AllDefs)
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(factionDef.defName);
                if (factionDef.pawnGroupMakers != null)
                {
                    foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                    {
                        if (pawnGroupMaker.options != null)
                        {
                            foreach (var option in pawnGroupMaker.options)
                            {
                                if (option.kind != null) // [修复] 错误位置：178行，错误内容：PawnGenOption未包含kindDef的定义
                                {
                                    var kindData = factionData.GetOrCreateKindData(option.kind.defName); // [修复] 错误位置：180行，错误内容：PawnGenOption未包含kindDef的定义
                                    LoadKindDefGear(option.kind, kindData); // [修复] 错误位置：181行，错误内容：PawnGenOption未包含kindDef的定义
                                }
                            }
                        }
                    }
                }
            }
        }

        // [修复] 将 LoadKindDefGear 改为 public，供重置单兵种时重新抓取数据使用
        public static void LoadKindDefGear(PawnKindDef kindDef, KindGearData kindData)
        {
            if (kindDef.weaponTags != null)
            {
                foreach (var tag in kindDef.weaponTags)
                {
                    var weapons = DefDatabase<ThingDef>.AllDefs.Where(t => t.IsWeapon && t.weaponTags != null && t.weaponTags.Contains(tag)).ToList();
                    foreach (var weapon in weapons)
                    {
                        if (weapon.IsRangedWeapon)
                        {
                            if (!kindData.weapons.Any(g => g.thingDefName == weapon.defName))
                                kindData.weapons.Add(new GearItem(weapon.defName));
                        }
                        else
                        {
                            if (!kindData.meleeWeapons.Any(g => g.thingDefName == weapon.defName))
                                kindData.meleeWeapons.Add(new GearItem(weapon.defName));
                        }
                    }
                }
            }

            // [修复] 错误位置：214行，错误内容：PawnKindDef未包含apparelRequirements的定义
            // 改为加载默认装备
            LoadDefaultApparel(kindData);
        }

        // [修复] 添加加载默认装备的方法
        private static void LoadDefaultApparel(KindGearData kindData)
        {
            // 加载默认装甲（使用新的筛选逻辑）
            var armors = GetAllArmors().Take(5).ToList();
            foreach (var armor in armors)
            {
                if (!kindData.armors.Any(g => g.thingDefName == armor.defName))
                    kindData.armors.Add(new GearItem(armor.defName));
            }

            // 加载默认衣服（使用新的筛选逻辑）
            var apparels = GetAllApparel().Take(5).ToList();
            foreach (var apparel in apparels)
            {
                if (!kindData.apparel.Any(g => g.thingDefName == apparel.defName))
                    kindData.apparel.Add(new GearItem(apparel.defName));
            }

            // 加载默认饰品（只包含腰带）
            var accessories = GetAllAccessories().Take(5).ToList();
            foreach (var accessory in accessories)
            {
                if (!kindData.accessories.Any(g => g.thingDefName == accessory.defName))
                    kindData.accessories.Add(new GearItem(accessory.defName));
            }
        }

        public static List<ThingDef> GetAllWeapons() => DefDatabase<ThingDef>.AllDefs.Where(t => t.IsRangedWeapon).ToList();
        public static List<ThingDef> GetAllMeleeWeapons() => DefDatabase<ThingDef>.AllDefs.Where(t => t.IsMeleeWeapon).ToList();
        // 1. 获取所有头盔（头部装备）
        public static List<ThingDef> GetAllHelmets() =>
            DefDatabase<ThingDef>.AllDefs.Where(t => t.IsApparel && t.apparel.layers != null && 
            t.apparel.layers.Contains(ApparelLayerDefOf.Overhead)).ToList();

        // 2. 获取所有装甲（通常是外层护甲或高防御装备）
        public static List<ThingDef> GetAllArmors() =>
            DefDatabase<ThingDef>.AllDefs.Where(t => t.IsApparel && t.apparel.layers != null && 
            (t.apparel.layers.Contains(ApparelLayerDefOf.Shell) || // 外层
             t.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) > 0.4f)).ToList(); // 或者是锐器防御较高的装备

        // 3. 获取普通衣物（躯干及下肢的内层、中层衣物）
        public static List<ThingDef> GetAllApparel() =>
            DefDatabase<ThingDef>.AllDefs.Where(t => t.IsApparel && t.apparel.layers != null && 
            !t.apparel.layers.Contains(ApparelLayerDefOf.Overhead) && // 不是头盔
            !t.apparel.layers.Contains(ApparelLayerDefOf.Belt) &&     // 不是腰带/饰品
            (t.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) || t.apparel.layers.Contains(ApparelLayerDefOf.Middle)) &&
            t.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) < 0.4f).ToList(); // 排除高防御护甲

        // 4. 获取饰品/腰带类
        public static List<ThingDef> GetAllAccessories() =>
            DefDatabase<ThingDef>.AllDefs.Where(t => t.IsApparel && t.apparel.layers != null && 
            t.apparel.layers.Contains(ApparelLayerDefOf.Belt)).ToList();

        public static float GetWeaponRange(ThingDef weaponDef)
        {
            // 检查武器定义是否有效
            if (weaponDef == null)
                return 0f;

            // 检查武器是否有Verbs属性
            if (weaponDef.Verbs != null && weaponDef.Verbs.Count > 0)
            {
                // 返回第一个Verb的射程
                return weaponDef.Verbs[0].range;
            }

            // 对于没有Verbs的武器（如某些特殊武器），返回0
            return 0f;
        }

        public static float GetWeaponDamage(ThingDef weaponDef)
        {
            // 检查武器定义是否有效
            if (weaponDef == null)
                return 0f;

            float maxDamage = 0f;

            // 1. 读取远程武器的子弹伤害
            if (weaponDef.IsRangedWeapon && weaponDef.Verbs != null && weaponDef.Verbs.Count > 0)
            {
                var verb = weaponDef.Verbs[0];
                if (verb != null)
                {
                    // 检查是否有默认投射物
                    var projectileDef = verb.defaultProjectile;
                    if (projectileDef != null && projectileDef.projectile != null)
                    {
                        try
                        {
                            // 适配 1.5/1.6 版本，使用 GetDamageAmount 方法
                            maxDamage = projectileDef.projectile.GetDamageAmount(null);
                        }
                        catch
                        {
                            // 兼容旧版本
                            maxDamage = 0f;
                        }
                    }
                }
            }

            // 2. 读取武器的近战伤害 (近战武器，或是远程武器的枪托砸击)
            if (weaponDef.tools != null && weaponDef.tools.Count > 0)
            {
                float meleeDamage = weaponDef.tools.Max(tool => tool.power);
                if (meleeDamage > maxDamage)
                {
                    maxDamage = meleeDamage;
                }
            }

            return maxDamage;
        }

        public static TechLevel GetTechLevel(ThingDef thingDef) => thingDef.techLevel;
        public static string GetModSource(ThingDef thingDef) => thingDef.modContentPack?.Name ?? "Unknown";
    }


    // 恢复最标准的 Harmony 拦截格式，不再使用动态 TargetMethod
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    public static class Patch_GeneratePawn
    {
        // 移除 request 前面的 ref 关键字
        public static void Postfix(Pawn __result, PawnGenerationRequest request)
        {
            if (__result != null && request.Faction != null && __result.RaceProps != null && __result.RaceProps.Humanlike)
            {
                GearApplier.ApplyCustomGear(__result, request.Faction);
            }
        }
    }

    public static class GearApplier
    {
        public static void ApplyCustomGear(Pawn pawn, Faction faction)
        {
            if (pawn == null || faction == null) return;

            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(faction.def.defName);
            var kindDefName = pawn.kindDef?.defName;

            if (!string.IsNullOrEmpty(kindDefName) && factionData.kindGearData.TryGetValue(kindDefName, out var kindData))
            {
                ApplyWeapons(pawn, kindData);
                ApplyApparel(pawn, kindData);
            }
        }

        private static void ApplyWeapons(Pawn pawn, KindGearData kindData)
        {
            pawn.equipment?.DestroyAllEquipment();

            if (kindData.weapons.Any())
            {
                var weaponItem = GetRandomGearItem(kindData.weapons);
                var weaponDef = weaponItem?.ThingDef;
                if (weaponDef != null)
                {
                    // [修复] 添加材料Stuff，防止报错
                    ThingDef stuff = weaponDef.MadeFromStuff ? GenStuff.RandomStuffFor(weaponDef) : null;
                    var weapon = (ThingWithComps)ThingMaker.MakeThing(weaponDef, stuff);

                    // [优化] 添加武器质量（基于派系科技等级生成）
                    CompQuality compQuality = weapon.TryGetComp<CompQuality>();
                    if (compQuality != null)
                    {
                        // [修复] 错误位置：334行，错误内容：参数 1: 无法从“RimWorld.TechLevel”转换为“RimWorld.QualityGenerator”
                        // 简化实现，直接设置为Normal质量
                        compQuality.SetQuality(QualityCategory.Normal, ArtGenerationContext.Outsider);
                    }

                    pawn.equipment?.AddEquipment(weapon);
                }
            }

            if (kindData.meleeWeapons.Any())
            {
                var meleeItem = GetRandomGearItem(kindData.meleeWeapons);
                var meleeDef = meleeItem?.ThingDef;
                if (meleeDef != null)
                {
                    ThingDef stuff = meleeDef.MadeFromStuff ? GenStuff.RandomStuffFor(meleeDef) : null;
                    var meleeWeapon = (ThingWithComps)ThingMaker.MakeThing(meleeDef, stuff);

                    CompQuality compQuality = meleeWeapon.TryGetComp<CompQuality>();
                    if (compQuality != null)
                    {
                        // [修复] 错误位置：351行，错误内容：参数 1: 无法从“RimWorld.TechLevel”转换为“RimWorld.QualityGenerator”
                        // 简化实现，直接设置为Normal质量
                        compQuality.SetQuality(QualityCategory.Normal, ArtGenerationContext.Outsider);
                    }

                    pawn.equipment?.AddEquipment(meleeWeapon);
                }
            }
        }

        private static void ApplyApparel(Pawn pawn, KindGearData kindData)
        {
            // 保底检查：如果自定义数据里什么都没有，就不要脱掉原版的衣服，防止裸体
            if (kindData.armors.NullOrEmpty() && kindData.apparel.NullOrEmpty() && kindData.accessories.NullOrEmpty())
            {
                return;
            }

            foreach (var apparel in pawn.apparel.WornApparel.ToList())
            {
                pawn.apparel.Remove(apparel);
                apparel.Destroy();
            }

            // [优化] 封装衣服生成逻辑，减少重复代码
            void EquipApparelList(List<GearItem> gearList)
            {
                if (!gearList.Any()) return;
                var item = GetRandomGearItem(gearList);
                var def = item?.ThingDef;
                if (def != null)
                {
                    ThingDef stuff = def.MadeFromStuff ? GenStuff.RandomStuffFor(def) : null;
                    var apparel = (Apparel)ThingMaker.MakeThing(def, stuff);

                    CompQuality compQuality = apparel.TryGetComp<CompQuality>();
                    if (compQuality != null)
                    {
                        // [修复] 错误位置：379行，错误内容：参数 1: 无法从“RimWorld.TechLevel”转换为“RimWorld.QualityGenerator”
                        // 简化实现，直接设置为Normal质量
                        compQuality.SetQuality(QualityCategory.Normal, ArtGenerationContext.Outsider);
                    }

                    pawn.apparel.Wear(apparel, false);
                }
            }

            EquipApparelList(kindData.armors);
            EquipApparelList(kindData.apparel);
            EquipApparelList(kindData.accessories);
        }

        private static GearItem GetRandomGearItem(List<GearItem> items)
        {
            // 检查列表是否为空
            if (!items.Any()) return null;

            // 计算总权重
            var totalWeight = items.Sum(item => item.weight);

            // 如果总权重为0，随机返回一个
            if (totalWeight <= 0f)
            {
                return items.RandomElement();
            }

            // 基于权重随机选择
            var randomValue = Rand.Value * totalWeight;
            var currentWeight = 0f;

            foreach (var item in items)
            {
                currentWeight += item.weight;
                if (randomValue <= currentWeight)
                {
                    return item;
                }
            }

            // 作为后备，返回第一个元素
            return items.First();
        }

        // [修复] 移除未使用的方法，所有调用处已改为直接设置QualityCategory.Normal
        // private static QualityGenerator GetQualityGeneratorFromTechLevel(TechLevel techLevel)
        // {
        //     // [修复] 错误位置：415行，错误内容：无法将null转换为QualityGenerator
        //     // 简化实现，直接返回Random
        //     return QualityGenerator.Random;
        // }
    }

    public static class FactionGearEditor
    {
        private static string selectedFactionDefName = "";
        private static string selectedKindDefName = "";
        private static GearCategory selectedCategory = GearCategory.Weapons;

        // 多选功能
        private static List<string> selectedFactionDefNames = new List<string>();
        private static List<string> selectedKindDefNames = new List<string>();

        private static Vector2 factionListScrollPos = Vector2.zero;
        private static Vector2 kindListScrollPos = Vector2.zero;
        private static Vector2 gearListScrollPos = Vector2.zero;
        private static Vector2 libraryScrollPos = Vector2.zero;
        private static string searchText = "";

        private static string selectedModSource = "All";
        private static TechLevel? selectedTechLevel = null;

        // [重构] 使用环世界官方的区间滑块
        private static FloatRange rangeFilter = new FloatRange(0f, 100f);
        private static FloatRange damageFilter = new FloatRange(0f, 100f);
        private static FloatRange marketValueFilter = new FloatRange(0f, 10000f); // 市场价值筛选

        // 排序相关字段
        private static string sortField = "Name"; // 默认按名称排序
        private static bool sortAscending = true; // 默认升序排序

        // 预设相关字段
        private static bool showPresetManager = false;
        private static FactionGearPreset selectedPreset = null;
        private static string newPresetName = "";
        private static string newPresetDescription = "";

        public static void DrawEditor(UnityEngine.Rect inRect)
        {
            // 规范化字体为环世界标准字体
            Text.Font = GameFont.Small;

            // 1. 顶部按钮栏：使用 WidgetRow 自动横向排列
            Rect topRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            WidgetRow buttonRow = new WidgetRow(topRect.x, topRect.y, UIDirection.RightThenUp, topRect.width, 4f);

            if (buttonRow.ButtonText("Load Default Presets"))
            {
                FactionGearManager.LoadDefaultPresets();
                FactionGearCustomizerMod.Settings.Write();
            }
            if (buttonRow.ButtonText("Reset All to Default"))
            {
                FactionGearCustomizerMod.Settings.ResetToDefault();
            }
            if (!string.IsNullOrEmpty(selectedFactionDefName) && buttonRow.ButtonText("Reset Current Faction"))
            {
                ResetCurrentFaction();
            }
            if (!string.IsNullOrEmpty(selectedKindDefName) && buttonRow.ButtonText("Reset Current Kind"))
            {
                ResetCurrentKind();
            }
            if (buttonRow.ButtonText("Reset Filters"))
            {
                ResetFilters();
            }

            if (buttonRow.ButtonText("Presets"))
            {
                showPresetManager = !showPresetManager;
            }

            if (buttonRow.ButtonText("Auto Fill"))
            {
                AutoFillGear();
            }

            // 2. 划分三大主面板
            Rect mainRect = new Rect(inRect.x, inRect.y + 35f, inRect.width, inRect.height - 35f);
            float panelWidth = (mainRect.width - 20f) / 3f;

            Rect leftPanel = new Rect(mainRect.x, mainRect.y, panelWidth, mainRect.height);
            Rect middlePanel = new Rect(leftPanel.xMax + 10f, mainRect.y, panelWidth, mainRect.height);
            Rect rightPanel = new Rect(middlePanel.xMax + 10f, mainRect.y, panelWidth, mainRect.height);

            DrawLeftPanel(leftPanel);
            DrawMiddlePanel(middlePanel);
            DrawRightPanel(rightPanel);

            // 绘制预设管理器
            if (showPresetManager)
            {
                DrawPresetManager();
            }
        }

        private static void DrawPresetManager()
        {
            Rect windowRect = new Rect(100, 100, 800, 600);
            windowRect = UnityEngine.GUI.Window(12345, windowRect, DrawPresetManagerWindow, "Gear Presets");
        }

        private static void DrawPresetManagerWindow(int windowID)
        {
            Rect inRect = new Rect(10, 30, 780, 560);
            Rect listRect = new Rect(inRect.x, inRect.y, 300, inRect.height);
            Rect detailsRect = new Rect(inRect.x + 310, inRect.y, inRect.width - 310, inRect.height);

            // 绘制预设列表
            Widgets.DrawMenuSection(listRect);
            Rect listInnerRect = listRect.ContractedBy(5f);
            Widgets.Label(new Rect(listInnerRect.x, listInnerRect.y, listInnerRect.width, 24f), "Saved Presets");

            Rect listOutRect = new Rect(listInnerRect.x, listInnerRect.y + 24f, listInnerRect.width, listInnerRect.height - 24f);
            List<FactionGearPreset> presets = FactionGearCustomizerMod.Settings.presets;
            Rect listViewRect = new Rect(0, 0, listOutRect.width - 16f, presets.Count * 30f);

            Widgets.BeginScrollView(listOutRect, ref factionListScrollPos, listViewRect);
            float y = 0;
            foreach (var preset in presets)
            {
                Rect rowRect = new Rect(0, y, listViewRect.width, 24f);
                if (selectedPreset == preset)
                    Widgets.DrawHighlightSelected(rowRect);
                else if (Mouse.IsOver(rowRect))
                    Widgets.DrawHighlight(rowRect);

                Widgets.Label(rowRect, preset.name);
                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedPreset = preset;
                }
                y += 30f;
            }
            Widgets.EndScrollView();

            // 绘制预设详情和操作
            Widgets.DrawMenuSection(detailsRect);
            Rect detailsInnerRect = detailsRect.ContractedBy(5f);

            // 新建预设
            Widgets.Label(new Rect(detailsInnerRect.x, detailsInnerRect.y, detailsInnerRect.width, 24f), "Create New Preset:");
            newPresetName = Widgets.TextField(new Rect(detailsInnerRect.x, detailsInnerRect.y + 30f, detailsInnerRect.width - 100f, 24f), newPresetName);
            if (Widgets.ButtonText(new Rect(detailsInnerRect.x + detailsInnerRect.width - 95f, detailsInnerRect.y + 30f, 90f, 24f), "Create"))
            {
                if (!string.IsNullOrEmpty(newPresetName) && !presets.Any(p => p.name == newPresetName))
                {
                    var newPreset = new FactionGearPreset(newPresetName, newPresetDescription);
                    if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                        var kindData = factionData.GetOrCreateKindData(selectedKindDefName);
                        newPreset.kindGearData[selectedKindDefName] = kindData;
                    }
                    FactionGearCustomizerMod.Settings.AddPreset(newPreset);
                    selectedPreset = newPreset;
                    newPresetName = "";
                    newPresetDescription = "";
                }
            }

            // 预设详情
            if (selectedPreset != null)
            {
                Widgets.Label(new Rect(detailsInnerRect.x, detailsInnerRect.y + 70f, detailsInnerRect.width, 24f), "Preset Details:");
                selectedPreset.name = Widgets.TextField(new Rect(detailsInnerRect.x, detailsInnerRect.y + 100f, detailsInnerRect.width, 24f), selectedPreset.name);
                selectedPreset.description = Widgets.TextField(new Rect(detailsInnerRect.x, detailsInnerRect.y + 130f, detailsInnerRect.width, 24f), selectedPreset.description);

                // 操作按钮
                if (Widgets.ButtonText(new Rect(detailsInnerRect.x, detailsInnerRect.y + 170f, 100f, 30f), "Apply"))
                {
                    if (!string.IsNullOrEmpty(selectedFactionDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                        foreach (var kvp in selectedPreset.kindGearData)
                        {
                            factionData.kindGearData[kvp.Key] = kvp.Value;
                        }
                        FactionGearCustomizerMod.Settings.Write();
                    }
                }

                if (Widgets.ButtonText(new Rect(detailsInnerRect.x + 110f, detailsInnerRect.y + 170f, 100f, 30f), "Save"))
                {
                    FactionGearCustomizerMod.Settings.UpdatePreset(selectedPreset);
                }

                if (Widgets.ButtonText(new Rect(detailsInnerRect.x + 220f, detailsInnerRect.y + 170f, 100f, 30f), "Delete"))
                {
                    FactionGearCustomizerMod.Settings.RemovePreset(selectedPreset);
                    selectedPreset = null;
                }
            }

            // 关闭按钮
            if (Widgets.ButtonText(new Rect(inRect.x + inRect.width - 80f, inRect.y + inRect.height - 40f, 70f, 30f), "Close"))
            {
                showPresetManager = false;
            }

            UnityEngine.GUI.DragWindow();
        }

        private static void DrawLeftPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect); // 环世界官方暗色背景板
            Rect innerRect = rect.ContractedBy(5f);

            Rect factionRect = new Rect(innerRect.x, innerRect.y, innerRect.width, (innerRect.height / 2f) - 5f);
            Rect kindRect = new Rect(innerRect.x, factionRect.yMax + 10f, innerRect.width, (innerRect.height / 2f) - 5f);

            // -- 派系列表 --
            Widgets.Label(new Rect(factionRect.x, factionRect.y, factionRect.width, 24f), "Factions");
            Rect factionListOutRect = new Rect(factionRect.x, factionRect.y + 24f, factionRect.width, factionRect.height - 24f);

            var allFactions = DefDatabase<FactionDef>.AllDefs.OrderBy(f => f.LabelCap.ToString()).ToList();
            Rect factionListViewRect = new Rect(0, 0, factionListOutRect.width - 16f, allFactions.Count * 26f);

            Widgets.BeginScrollView(factionListOutRect, ref factionListScrollPos, factionListViewRect);
            Listing_Standard factionListing = new Listing_Standard();
            factionListing.Begin(factionListViewRect);
            foreach (var factionDef in allFactions)
            {
                Rect rowRect = factionListing.GetRect(24f);

                // 悬停与选中特效
                if (selectedFactionDefName == factionDef.defName)
                    Widgets.DrawHighlightSelected(rowRect);
                else if (Mouse.IsOver(rowRect))
                    Widgets.DrawHighlight(rowRect);

                // 绘制复选框
                Vector2 checkBoxPos = new Vector2(rowRect.x, rowRect.y + 2f);
                bool isSelected = selectedFactionDefNames.Contains(factionDef.defName);
                bool originalIsSelected = isSelected;
                Widgets.Checkbox(checkBoxPos, ref isSelected);
                if (isSelected != originalIsSelected)
                {
                    if (isSelected)
                        selectedFactionDefNames.Add(factionDef.defName);
                    else
                        selectedFactionDefNames.Remove(factionDef.defName);
                }

                // 绘制派系名称
                Rect labelRect = new Rect(rowRect.x + 25f, rowRect.y, rowRect.width - 25f, rowRect.height);
                Widgets.Label(labelRect, factionDef.LabelCap);

                // 点击派系名称时设置为当前选中派系
                if (Widgets.ButtonInvisible(new Rect(rowRect.x + 25f, rowRect.y, rowRect.width - 25f, rowRect.height)))
                {
                    selectedFactionDefName = factionDef.defName;
                    selectedKindDefName = "";
                    // 重置滚动位置，确保列表更新后正确显示
                    kindListScrollPos = Vector2.zero;
                    gearListScrollPos = Vector2.zero;
                }
                factionListing.Gap(2f);
            }
            factionListing.End();
            Widgets.EndScrollView();

            // -- 兵种列表 --
            Widgets.Label(new Rect(kindRect.x, kindRect.y, kindRect.width, 24f), "Kind Defs");
            Rect kindListOutRect = new Rect(kindRect.x, kindRect.y + 24f, kindRect.width, kindRect.height - 24f);

            List<PawnKindDef> kindDefsToDraw = new List<PawnKindDef>();
            if (!string.IsNullOrEmpty(selectedFactionDefName))
            {
                var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(selectedFactionDefName);
                if (factionDef != null && factionDef.pawnGroupMakers != null)
                {
                    // 清空列表，确保每次选择派系时重新生成
                    kindDefsToDraw.Clear();
                    foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                    {
                        if (pawnGroupMaker.options != null)
                        {
                            foreach (var option in pawnGroupMaker.options)
                            {
                                if (option.kind != null && !kindDefsToDraw.Contains(option.kind))
                                    kindDefsToDraw.Add(option.kind);
                            }
                        }
                    }
                }
            }
            else
            {
                // 如果没有选择派系，显示所有兵种
                kindDefsToDraw = DefDatabase<PawnKindDef>.AllDefs.OrderBy(k => k.LabelCap.ToString()).ToList();
            }

            Rect kindListViewRect = new Rect(0, 0, kindListOutRect.width - 16f, kindDefsToDraw.Count * 26f);
            Widgets.BeginScrollView(kindListOutRect, ref kindListScrollPos, kindListViewRect);
            Listing_Standard kindListing = new Listing_Standard();
            kindListing.Begin(kindListViewRect);
            foreach (var kindDef in kindDefsToDraw.OrderBy(k => k.LabelCap.ToString()))
            {
                Rect rowRect = kindListing.GetRect(24f);

                if (selectedKindDefName == kindDef.defName)
                    Widgets.DrawHighlightSelected(rowRect);
                else if (Mouse.IsOver(rowRect))
                    Widgets.DrawHighlight(rowRect);

                // 绘制复选框
                Vector2 checkBoxPos = new Vector2(rowRect.x, rowRect.y + 2f);
                bool isSelected = selectedKindDefNames.Contains(kindDef.defName);
                bool originalIsSelected = isSelected;
                Widgets.Checkbox(checkBoxPos, ref isSelected);
                if (isSelected != originalIsSelected)
                {
                    if (isSelected)
                        selectedKindDefNames.Add(kindDef.defName);
                    else
                        selectedKindDefNames.Remove(kindDef.defName);
                }

                // 检查该兵种是否被修改过
                bool isModified = false;
                if (!string.IsNullOrEmpty(selectedFactionDefName))
                {
                    var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                    if (factionData.kindGearData.TryGetValue(kindDef.defName, out var kindData))
                    {
                        isModified = kindData.isModified;
                    }
                }

                // 绘制兵种名称
                Rect labelRect = new Rect(rowRect.x + 25f, rowRect.y, rowRect.width - 25f - 20f, rowRect.height);
                Widgets.Label(labelRect, kindDef.LabelCap);

                // 为修改过的兵种添加视觉标记
                if (isModified)
                {
                    Rect modIconRect = new Rect(rowRect.xMax - 20f, rowRect.y + 2f, 16f, 16f);
                    GUI.color = Color.yellow;
                    Widgets.DrawBox(modIconRect);
                    GUI.color = Color.white;
                }

                // 点击兵种名称时设置为当前选中兵种
                if (Widgets.ButtonInvisible(new Rect(rowRect.x + 25f, rowRect.y, rowRect.width - 25f, rowRect.height)))
                {
                    selectedKindDefName = kindDef.defName;
                    // 重置滚动位置，确保物品列表更新后正确显示
                    gearListScrollPos = Vector2.zero;
                }
                kindListing.Gap(2f);
            }
            kindListing.End();
            Widgets.EndScrollView();
        }

        private static void DrawMiddlePanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 24f), "Selected Gear");

            // 添加一键全部删除按钮
            Rect clearButtonRect = new Rect(innerRect.x + innerRect.width - 100f, innerRect.y, 90f, 20f);
            if (Widgets.ButtonText(clearButtonRect, "Clear All"))
            {
                if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
                {
                    var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                    var kindData = factionData.GetOrCreateKindData(selectedKindDefName);
                    ClearAllGear(kindData);
                    FactionGearCustomizerMod.Settings.Write();
                }
            }

            Rect tabRect = new Rect(innerRect.x, innerRect.y + 24f, innerRect.width, 24f);
            DrawCategoryTabs(tabRect);

            // 计算价值权重预览区域的高度
            float previewHeight = 80f;
            Rect listOutRect = new Rect(innerRect.x, tabRect.yMax + 5f, innerRect.width, innerRect.height - 24f - 24f - previewHeight - 10f);

            List<GearItem> gearItemsToDraw = new List<GearItem>();
            KindGearData currentKindData = null;
            if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                currentKindData = factionData.GetOrCreateKindData(selectedKindDefName);
                gearItemsToDraw = GetCurrentCategoryGear(currentKindData);
            }

            Rect listViewRect = new Rect(0, 0, listOutRect.width - 16f, gearItemsToDraw.Count * 32f);
            Widgets.BeginScrollView(listOutRect, ref gearListScrollPos, listViewRect);

            if (gearItemsToDraw.Any() && currentKindData != null)
            {
                Listing_Standard gearListing = new Listing_Standard();
                gearListing.Begin(listViewRect);
                foreach (var gearItem in gearItemsToDraw.ToList())
                {
                    DrawGearItem(gearListing.GetRect(28f), gearItem, currentKindData);
                    gearListing.Gap(4f);
                }
                gearListing.End();
            }
            Widgets.EndScrollView();

            // 添加价值权重预览
            if (currentKindData != null)
            {
                Rect previewRect = new Rect(innerRect.x, listOutRect.yMax + 5f, innerRect.width, previewHeight);
                DrawValueWeightPreview(previewRect, currentKindData);
            }
        }

        private static void DrawRightPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 24f), "Item Library");

            // 添加一键全部添加按钮
            Rect addAllButtonRect = new Rect(innerRect.x + innerRect.width - 100f, innerRect.y, 90f, 20f);
            if (Widgets.ButtonText(addAllButtonRect, "Add All"))
            {
                if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
                {
                    var items = GetFilteredItems();
                    AddAllItems(items);
                    FactionGearCustomizerMod.Settings.Write();
                }
            }

            Rect filterRect = new Rect(innerRect.x, innerRect.y + 24f, innerRect.width, 150f);
            DrawFilters(filterRect);

            // 计算列表和滑块区域
            float sliderHeight = 40f;
            Rect listOutRect = new Rect(innerRect.x, filterRect.yMax + 5f, innerRect.width, innerRect.height - 24f - filterRect.height - sliderHeight - 10f);
            Rect marketValueSliderRect = new Rect(innerRect.x, listOutRect.yMax + 5f, innerRect.width, sliderHeight);

            var libraryItems = GetFilteredItems();
            Rect listViewRect = new Rect(0, 0, listOutRect.width - 16f, libraryItems.Count * 46f);

            Widgets.BeginScrollView(listOutRect, ref libraryScrollPos, listViewRect);
            Listing_Standard libListing = new Listing_Standard();
            libListing.Begin(listViewRect);
            foreach (var item in libraryItems)
            {
                DrawLibraryItem(libListing.GetRect(42f), item);
                libListing.Gap(4f);
            }
            libListing.End();
            Widgets.EndScrollView();

            // 绘制Market Value滑块
            Widgets.Label(new Rect(marketValueSliderRect.x, marketValueSliderRect.y, 100f, 20f), "Market Value");
            Rect sliderRect = new Rect(marketValueSliderRect.x + 100f, marketValueSliderRect.y, marketValueSliderRect.width - 160f, 20f);
            marketValueFilter.max = Widgets.HorizontalSlider(sliderRect, marketValueFilter.max, 0f, 10000f);
            Widgets.Label(new Rect(marketValueSliderRect.x + marketValueSliderRect.width - 60f, marketValueSliderRect.y, 60f, 20f), $"{marketValueFilter.max:F0}");
        }

        private static void DrawFilters(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            // 搜索框
            searchText = listing.TextEntry(searchText);

            // Mod 与 科技等级下拉菜单 并排显示
            Rect dropdownsRect = listing.GetRect(24f);
            Rect modRect = dropdownsRect.LeftHalf().ContractedBy(2f);
            Rect techRect = dropdownsRect.RightHalf().ContractedBy(2f);

            if (Widgets.ButtonText(modRect, $"Mod: {selectedModSource}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (var mod in GetAllModSources())
                {
                    options.Add(new FloatMenuOption(mod, () => selectedModSource = mod));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            string techLabel = selectedTechLevel.HasValue ? selectedTechLevel.Value.ToString() : "All";
            if (Widgets.ButtonText(techRect, $"Tech: {techLabel}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("All", () => selectedTechLevel = null));
                foreach (TechLevel level in Enum.GetValues(typeof(TechLevel)))
                {
                    options.Add(new FloatMenuOption(level.ToString(), () => selectedTechLevel = level));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // 排序选项
            Rect sortRect = listing.GetRect(24f);
            Rect sortFieldRect = sortRect.LeftHalf().ContractedBy(2f);
            Rect sortOrderRect = sortRect.RightHalf().ContractedBy(2f);

            if (Widgets.ButtonText(sortFieldRect, $"Sort: {sortField}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("Name", () => sortField = "Name"));
                options.Add(new FloatMenuOption("Range", () => sortField = "Range"));
                options.Add(new FloatMenuOption("Damage", () => sortField = "Damage"));
                options.Add(new FloatMenuOption("DPS", () => sortField = "DPS"));
                options.Add(new FloatMenuOption("MarketValue", () => sortField = "MarketValue"));
                options.Add(new FloatMenuOption("TechLevel", () => sortField = "TechLevel"));
                Find.WindowStack.Add(new FloatMenu(options));
            }

            if (Widgets.ButtonText(sortOrderRect, $"Order: {(sortAscending ? "Asc" : "Desc")}"))
            {
                sortAscending = !sortAscending;
            }

            // [重构] 官方双向区间滑块 (FloatRange)
            if (selectedCategory == GearCategory.Weapons)
            {
                Rect rangeRect = listing.GetRect(28f);
                Widgets.FloatRange(rangeRect, 1, ref rangeFilter, 0f, 100f, "Range", ToStringStyle.FloatOne);
            }

            if (selectedCategory == GearCategory.Weapons || selectedCategory == GearCategory.MeleeWeapons)
            {
                Rect damageRect = listing.GetRect(28f);
                Widgets.FloatRange(damageRect, 2, ref damageFilter, 0f, 100f, "Damage", ToStringStyle.FloatOne);
            }



            listing.End();
        }

        private static void DrawCategoryTabs(Rect rect)
        {
            int tabCount = 5;
            float tabWidth = rect.width / tabCount;

            for (int i = 0; i < tabCount; i++)
            {
                Rect tab = new Rect(rect.x + i * tabWidth, rect.y, tabWidth, rect.height);
                GearCategory category = (GearCategory)i;
                string label = GetCategoryLabel(category);

                if (selectedCategory == category)
                    Widgets.DrawHighlightSelected(tab);
                else if (Mouse.IsOver(tab))
                    Widgets.DrawHighlight(tab);

                // 绘制边框
                GUI.color = Color.grey;
                Widgets.DrawBox(tab);
                GUI.color = Color.white;

                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(tab, label);
                Text.Anchor = TextAnchor.UpperLeft;

                if (Widgets.ButtonInvisible(tab))
                {
                    selectedCategory = category;
                }
            }
        }

        private static void DrawGearItem(Rect rect, GearItem gearItem, KindGearData kindData)
        {
            var thingDef = gearItem.ThingDef;
            if (thingDef == null) return;

            Widgets.DrawHighlightIfMouseover(rect);

            // 为修改过的装备添加视觉提示
            if (gearItem.weight != 1f)
            {
                GUI.color = new Color(1f, 0.8f, 0.4f); // 金色高亮
                Widgets.DrawHighlight(rect);
                GUI.color = Color.white;
            }

            Rect nameRect = new Rect(rect.x, rect.y, rect.width - 140f, rect.height);
            Rect sliderRect = new Rect(rect.xMax - 135f, rect.y + 6f, 70f, 20f);
            Rect weightRect = new Rect(rect.xMax - 60f, rect.y + 2f, 30f, rect.height);
            Rect removeRect = new Rect(rect.xMax - 25f, rect.y + 2f, 24f, 24f);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(nameRect, thingDef.LabelCap);

            gearItem.weight = Widgets.HorizontalSlider(sliderRect, gearItem.weight, 0.1f, 10f);

            Widgets.Label(weightRect, gearItem.weight.ToString("0.0"));
            Text.Anchor = TextAnchor.UpperLeft;

            // 官方原生按钮 UI
            if (Widgets.ButtonText(removeRect, "X"))
            {
                RemoveGearItem(gearItem, kindData);
                FactionGearCustomizerMod.Settings.Write();
            }
        }

        private static void DrawLibraryItem(Rect rect, ThingDef thingDef)
        {
            Widgets.DrawHighlightIfMouseover(rect);

            // 信息按钮矩形
            Rect infoButtonRect = new Rect(rect.x, rect.y + 10f, 20f, 20f);
            // 缩略图矩形
            Rect iconRect = new Rect(rect.x + 25f, rect.y, 40f, 40f);
            // 文本矩形
            Rect textRect = new Rect(rect.x + 70f, rect.y, rect.width - 130f, rect.height);
            // 添加按钮矩形
            Rect addRect = new Rect(rect.xMax - 55f, rect.y + 8f, 50f, 24f);

            // 绘制信息按钮（大i字体）
            Rect infoLabelRect = new Rect(rect.x + 5f, rect.y + 8f, 20f, 20f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Widgets.Label(infoLabelRect, "i");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 点击区域
            if (Widgets.ButtonInvisible(infoButtonRect))
            {
                // 打开物品的百科界面
                Find.WindowStack.Add(new Dialog_InfoCard(thingDef));
            }

            // 绘制物品缩略图
            if (thingDef.uiIcon != null)
            {
                GUI.DrawTexture(iconRect, thingDef.uiIcon);
            }
            else if (thingDef.graphic != null)
            {
                // 尝试使用graphic获取纹理
                var graphic = thingDef.graphic;
                if (graphic != null)
                {
                    var texture = graphic.MatSingle.mainTexture;
                    if (texture != null)
                    {
                        GUI.DrawTexture(iconRect, texture);
                    }
                }
            }

            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(textRect.x, textRect.y, textRect.width, 20f), thingDef.LabelCap);

            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(textRect.x, textRect.y + 20f, textRect.width, 20f), GetItemInfo(thingDef));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // 添加鼠标悬浮显示完整参数的功能
            string tooltip = GetDetailedItemInfo(thingDef);
            TooltipHandler.TipRegion(rect, tooltip);

            if (Widgets.ButtonText(addRect, "Add"))
            {
                AddGearItem(thingDef);
                FactionGearCustomizerMod.Settings.Write();
            }
        }

        private static List<ThingDef> ApplyFilters(List<ThingDef> items)
        {
            // 预计算筛选条件，避免重复计算
            bool filterByMod = selectedModSource != "All";
            bool filterByTechLevel = selectedTechLevel.HasValue;
            bool filterByRange = selectedCategory == GearCategory.Weapons;
            bool filterByDamage = selectedCategory == GearCategory.Weapons || selectedCategory == GearCategory.MeleeWeapons;
            bool filterByMarketValue = true; // 始终应用市场价值筛选

            // 使用LINQ进行筛选，提高可读性和性能
            return items.Where(item => {
                // 检查物品是否有效
                if (item == null) return false;

                // 筛选Mod来源
                if (filterByMod && FactionGearManager.GetModSource(item) != selectedModSource)
                    return false;

                // 筛选科技等级
                if (filterByTechLevel && item.techLevel != selectedTechLevel.Value)
                    return false;

                // 筛选射程（仅武器）
                if (filterByRange)
                {
                    float range = FactionGearManager.GetWeaponRange(item);
                    if (range < rangeFilter.min || range > rangeFilter.max)
                        return false;
                }

                // 筛选伤害（武器和近战武器）
                if (filterByDamage)
                {
                    float damage = FactionGearManager.GetWeaponDamage(item);
                    if (damage < damageFilter.min || damage > damageFilter.max)
                        return false;
                }

                // 筛选市场价值
                if (filterByMarketValue)
                {
                    float marketValue = item.BaseMarketValue;
                    if (marketValue < marketValueFilter.min || marketValue > marketValueFilter.max)
                        return false;
                }

                return true;
            }).ToList();
        }

        private static void ResetFilters()
        {
            selectedModSource = "All";
            selectedTechLevel = null;
            rangeFilter = new FloatRange(0f, 100f);
            damageFilter = new FloatRange(0f, 100f);
            marketValueFilter = new FloatRange(0f, 10000f);
            searchText = "";
            sortField = "Name";
            sortAscending = true;
        }

        private static List<string> GetAllModSources()
        {
            var modSources = new HashSet<string> { "All" };
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.modContentPack != null)
                {
                    modSources.Add(thingDef.modContentPack.Name);
                }
            }
            return modSources.OrderBy(s => s).ToList();
        }

        private static List<GearItem> GetCurrentCategoryGear(KindGearData kindData)
        {
            switch (selectedCategory)
            {
                case GearCategory.Weapons: return kindData.weapons;
                case GearCategory.MeleeWeapons: return kindData.meleeWeapons;
                case GearCategory.Armors: return kindData.armors;
                case GearCategory.Apparel: return kindData.apparel;
                case GearCategory.Accessories: return kindData.accessories;
                default: return new List<GearItem>();
            }
        }

        private static List<ThingDef> GetFilteredItems()
        {
            List<ThingDef> filteredItems = new List<ThingDef>();
            switch (selectedCategory)
            {
                case GearCategory.Weapons: filteredItems = FactionGearManager.GetAllWeapons(); break;
                case GearCategory.MeleeWeapons: filteredItems = FactionGearManager.GetAllMeleeWeapons(); break;
                case GearCategory.Armors: filteredItems = FactionGearManager.GetAllArmors(); break;
                case GearCategory.Apparel: filteredItems = FactionGearManager.GetAllApparel(); break;
                case GearCategory.Accessories: filteredItems = FactionGearManager.GetAllAccessories(); break;
            }

            filteredItems = ApplyFilters(filteredItems);
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredItems = filteredItems.Where(t => (t.label ?? t.defName).ToLower().Contains(searchText.ToLower())).ToList();
            }

            // 添加排序逻辑
            filteredItems = ApplySorting(filteredItems);

            return filteredItems;
        }

        private static List<ThingDef> ApplySorting(List<ThingDef> items)
        {
            switch (sortField)
            {
                case "Name":
                    items = sortAscending
                        ? items.OrderBy(t => t.LabelCap.ToString()).ToList()
                        : items.OrderByDescending(t => t.LabelCap.ToString()).ToList();
                    break;
                case "Range":
                    items = sortAscending
                        ? items.OrderBy(t => FactionGearManager.GetWeaponRange(t)).ToList()
                        : items.OrderByDescending(t => FactionGearManager.GetWeaponRange(t)).ToList();
                    break;
                case "Damage":
                    items = sortAscending
                        ? items.OrderBy(t => FactionGearManager.GetWeaponDamage(t)).ToList()
                        : items.OrderByDescending(t => FactionGearManager.GetWeaponDamage(t)).ToList();
                    break;
                case "DPS":
                    items = sortAscending
                        ? items.OrderBy(t => CalculateDPS(t)).ToList()
                        : items.OrderByDescending(t => CalculateDPS(t)).ToList();
                    break;
                case "MarketValue":
                    items = sortAscending
                        ? items.OrderBy(t => t.BaseMarketValue).ToList()
                        : items.OrderByDescending(t => t.BaseMarketValue).ToList();
                    break;
                case "TechLevel":
                    items = sortAscending
                        ? items.OrderBy(t => t.techLevel).ToList()
                        : items.OrderByDescending(t => t.techLevel).ToList();
                    break;
            }
            return items;
        }

        private static float CalculateDPS(ThingDef thingDef)
        {
            if (!thingDef.IsWeapon)
                return 0f;

            float damage = FactionGearManager.GetWeaponDamage(thingDef);
            float cooldown = 1f; // 默认冷却时间

            // 对于近战武器，使用tool的冷却时间
            if (thingDef.IsMeleeWeapon && thingDef.tools != null && thingDef.tools.Count > 0)
            {
                cooldown = thingDef.tools.Min(tool => tool.cooldownTime);
            }
            // 对于远程武器，使用verb的冷却时间
            else if (thingDef.IsRangedWeapon && thingDef.Verbs != null && thingDef.Verbs.Count > 0)
            {
                cooldown = thingDef.Verbs.Min(verb => verb.warmupTime);
            }

            // 避免除以零
            if (cooldown <= 0f)
                cooldown = 1f;

            return damage / cooldown;
        }

        private static void AddGearItem(ThingDef thingDef)
        {
            if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                var kindData = factionData.GetOrCreateKindData(selectedKindDefName);

                switch (selectedCategory)
                {
                    case GearCategory.Weapons:
                        if (!kindData.weapons.Any(g => g.thingDefName == thingDef.defName))
                        {
                            kindData.weapons.Add(new GearItem(thingDef.defName));
                            kindData.isModified = true;
                        }
                        break;
                    case GearCategory.MeleeWeapons:
                        if (!kindData.meleeWeapons.Any(g => g.thingDefName == thingDef.defName))
                        {
                            kindData.meleeWeapons.Add(new GearItem(thingDef.defName));
                            kindData.isModified = true;
                        }
                        break;
                    case GearCategory.Armors:
                        if (!kindData.armors.Any(g => g.thingDefName == thingDef.defName))
                        {
                            kindData.armors.Add(new GearItem(thingDef.defName));
                            kindData.isModified = true;
                        }
                        break;
                    case GearCategory.Apparel:
                        if (!kindData.apparel.Any(g => g.thingDefName == thingDef.defName))
                        {
                            kindData.apparel.Add(new GearItem(thingDef.defName));
                            kindData.isModified = true;
                        }
                        break;
                    case GearCategory.Accessories:
                        if (!kindData.accessories.Any(g => g.thingDefName == thingDef.defName))
                        {
                            kindData.accessories.Add(new GearItem(thingDef.defName));
                            kindData.isModified = true;
                        }
                        break;
                }
                ValidateGearData(kindData);
            }
        }

        private static void AddAllItems(List<ThingDef> items)
        {
            if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                var kindData = factionData.GetOrCreateKindData(selectedKindDefName);
                bool addedAny = false;

                foreach (var thingDef in items)
                {
                    switch (selectedCategory)
                    {
                        case GearCategory.Weapons:
                            if (!kindData.weapons.Any(g => g.thingDefName == thingDef.defName))
                            {
                                kindData.weapons.Add(new GearItem(thingDef.defName));
                                addedAny = true;
                            }
                            break;
                        case GearCategory.MeleeWeapons:
                            if (!kindData.meleeWeapons.Any(g => g.thingDefName == thingDef.defName))
                            {
                                kindData.meleeWeapons.Add(new GearItem(thingDef.defName));
                                addedAny = true;
                            }
                            break;
                        case GearCategory.Armors:
                            if (!kindData.armors.Any(g => g.thingDefName == thingDef.defName))
                            {
                                kindData.armors.Add(new GearItem(thingDef.defName));
                                addedAny = true;
                            }
                            break;
                        case GearCategory.Apparel:
                            if (!kindData.apparel.Any(g => g.thingDefName == thingDef.defName))
                            {
                                kindData.apparel.Add(new GearItem(thingDef.defName));
                                addedAny = true;
                            }
                            break;
                        case GearCategory.Accessories:
                            if (!kindData.accessories.Any(g => g.thingDefName == thingDef.defName))
                            {
                                kindData.accessories.Add(new GearItem(thingDef.defName));
                                addedAny = true;
                            }
                            break;
                    }
                }
                if (addedAny) kindData.isModified = true;
                ValidateGearData(kindData);
            }
        }

        private static void ValidateGearData(KindGearData kindData)
        {
            RemoveInvalidGearItems(kindData.weapons);
            RemoveInvalidGearItems(kindData.meleeWeapons);
            RemoveInvalidGearItems(kindData.armors);
            RemoveInvalidGearItems(kindData.apparel);
            RemoveInvalidGearItems(kindData.accessories);

            ClampGearWeights(kindData.weapons);
            ClampGearWeights(kindData.meleeWeapons);
            ClampGearWeights(kindData.armors);
            ClampGearWeights(kindData.apparel);
            ClampGearWeights(kindData.accessories);
        }

        private static void AutoFillGear()
        {
            // 处理所有选中的派系
            var factionDefNames = selectedFactionDefNames.Any() ? selectedFactionDefNames : new List<string> { selectedFactionDefName };
            
            foreach (var factionDefName in factionDefNames)
            {
                if (string.IsNullOrEmpty(factionDefName))
                    continue;

                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(factionDefName);
                
                // 处理所有选中的兵种
                var kindDefNames = selectedKindDefNames.Any() ? selectedKindDefNames : new List<string> { selectedKindDefName };
                
                foreach (var kindDefName in kindDefNames)
                {
                    if (string.IsNullOrEmpty(kindDefName))
                        continue;

                    var kindData = factionData.GetOrCreateKindData(kindDefName);
                    var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindDefName);

                    if (kindDef == null)
                        continue;

                    // 基于兵种标签添加武器
                    if (kindDef.weaponTags != null && kindDef.weaponTags.Any())
                    {
                        foreach (var tag in kindDef.weaponTags)
                        {
                            var weapons = DefDatabase<ThingDef>.AllDefs.Where(t => t.IsWeapon && t.weaponTags != null && t.weaponTags.Contains(tag)).Take(3).ToList();
                            foreach (var weapon in weapons)
                            {
                                if (weapon.IsRangedWeapon && !kindData.weapons.Any(g => g.thingDefName == weapon.defName))
                                {
                                    kindData.weapons.Add(new GearItem(weapon.defName));
                                }
                                else if (weapon.IsMeleeWeapon && !kindData.meleeWeapons.Any(g => g.thingDefName == weapon.defName))
                                {
                                    kindData.meleeWeapons.Add(new GearItem(weapon.defName));
                                }
                            }
                        }
                    }
                    else
                    {
                        // 如果没有武器标签，添加一些基本武器
                        if (!kindData.weapons.Any())
                        {
                            var basicWeapons = DefDatabase<ThingDef>.AllDefs.Where(t => t.IsRangedWeapon).Take(2).ToList();
                            foreach (var weapon in basicWeapons)
                            {
                                kindData.weapons.Add(new GearItem(weapon.defName));
                            }
                        }
                        if (!kindData.meleeWeapons.Any())
                        {
                            var basicMeleeWeapons = DefDatabase<ThingDef>.AllDefs.Where(t => t.IsMeleeWeapon).Take(2).ToList();
                            foreach (var weapon in basicMeleeWeapons)
                            {
                                kindData.meleeWeapons.Add(new GearItem(weapon.defName));
                            }
                        }
                    }

                    // 添加基本装甲
                    if (!kindData.armors.Any())
                    {
                        var armors = DefDatabase<ThingDef>.AllDefs.Where(t => t.IsApparel && t.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso)).Take(2).ToList();
                        foreach (var armor in armors)
                        {
                            kindData.armors.Add(new GearItem(armor.defName));
                        }
                    }

                    // 添加基本衣物
                    if (!kindData.apparel.Any())
                    {
                        var apparels = DefDatabase<ThingDef>.AllDefs.Where(t => t.IsApparel && !t.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) &&
                            (t.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) || t.apparel.layers.Contains(ApparelLayerDefOf.Middle))).Take(2).ToList();
                        foreach (var apparel in apparels)
                        {
                            kindData.apparel.Add(new GearItem(apparel.defName));
                        }
                    }

                    // 添加基本饰品
                    if (!kindData.accessories.Any())
                    {
                        var accessories = DefDatabase<ThingDef>.AllDefs.Where(t => t.IsApparel && t.apparel.layers.Contains(ApparelLayerDefOf.Belt)).Take(1).ToList();
                        foreach (var accessory in accessories)
                        {
                            kindData.accessories.Add(new GearItem(accessory.defName));
                        }
                    }

                    kindData.isModified = true;
                }
            }

            // 保存设置
            FactionGearCustomizerMod.Settings.Write();
        }

        private static void RemoveInvalidGearItems(List<GearItem> gearItems)
        {
            for (int i = gearItems.Count - 1; i >= 0; i--)
            {
                if (gearItems[i].ThingDef == null)
                {
                    gearItems.RemoveAt(i);
                }
            }
        }

        private static void ClampGearWeights(List<GearItem> gearItems)
        {
            foreach (var gearItem in gearItems)
            {
                gearItem.weight = Mathf.Clamp(gearItem.weight, 0.1f, 10f);
            }
        }

        private static void RemoveGearItem(GearItem gearItem, KindGearData kindData)
        {
            switch (selectedCategory)
            {
                case GearCategory.Weapons: kindData.weapons.Remove(gearItem); break;
                case GearCategory.MeleeWeapons: kindData.meleeWeapons.Remove(gearItem); break;
                case GearCategory.Armors: kindData.armors.Remove(gearItem); break;
                case GearCategory.Apparel: kindData.apparel.Remove(gearItem); break;
                case GearCategory.Accessories: kindData.accessories.Remove(gearItem); break;
            }
            kindData.isModified = true;
        }

        private static void ClearAllGear(KindGearData kindData)
        {
            switch (selectedCategory)
            {
                case GearCategory.Weapons: kindData.weapons.Clear(); break;
                case GearCategory.MeleeWeapons: kindData.meleeWeapons.Clear(); break;
                case GearCategory.Armors: kindData.armors.Clear(); break;
                case GearCategory.Apparel: kindData.apparel.Clear(); break;
                case GearCategory.Accessories: kindData.accessories.Clear(); break;
            }
            kindData.isModified = true;
        }

        private static void ResetCurrentFaction()
        {
            if (!string.IsNullOrEmpty(selectedFactionDefName))
            {
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                factionData.ResetToDefault();

                var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(selectedFactionDefName);
                if (factionDef != null && factionDef.pawnGroupMakers != null)
                {
                    foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                    {
                        if (pawnGroupMaker.options != null)
                        {
                            foreach (var option in pawnGroupMaker.options)
                            {
                                if (option.kind != null)
                                {
                                    var kindData = factionData.GetOrCreateKindData(option.kind.defName);
                                    FactionGearManager.LoadKindDefGear(option.kind, kindData);
                                }
                            }
                        }
                    }
                }
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

                var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(selectedKindDefName);
                if (kindDef != null)
                {
                    FactionGearManager.LoadKindDefGear(kindDef, kindData);
                }

                FactionGearCustomizerMod.Settings.Write();
            }
        }

        private static void DrawValueWeightPreview(Rect rect, KindGearData kindData)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);

            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 20f), "Value & Weight Preview");

            // 获取当前类别的装备列表
            List<GearItem> gearItems = GetCurrentCategoryGear(kindData);

            if (gearItems.Any())
            {
                // 计算价值和权重统计
                float totalValue = 0f;
                float totalWeight = 0f;
                int validItems = 0;

                foreach (var gearItem in gearItems)
                {
                    var thingDef = gearItem.ThingDef;
                    if (thingDef != null)
                    {
                        totalValue += thingDef.BaseMarketValue;
                        totalWeight += gearItem.weight;
                        validItems++;
                    }
                }

                if (validItems > 0)
                {
                    float avgValue = totalValue / validItems;
                    float avgWeight = totalWeight / validItems;
                    float totalValuePerWeight = totalValue / totalWeight;

                    // 绘制统计信息
                    float y = innerRect.y + 24f;
                    float lineHeight = 16f;

                    Widgets.Label(new Rect(innerRect.x, y, innerRect.width, lineHeight), $"Total Value: {totalValue:F0} silver");
                    y += lineHeight;
                    Widgets.Label(new Rect(innerRect.x, y, innerRect.width, lineHeight), $"Average Value: {avgValue:F0} silver");
                    y += lineHeight;
                    Widgets.Label(new Rect(innerRect.x, y, innerRect.width, lineHeight), $"Total Weight: {totalWeight:F2}");
                    y += lineHeight;
                    Widgets.Label(new Rect(innerRect.x, y, innerRect.width, lineHeight), $"Average Weight: {avgWeight:F2}");
                    y += lineHeight;
                    Widgets.Label(new Rect(innerRect.x, y, innerRect.width, lineHeight), $"Value per Weight: {totalValuePerWeight:F2} silver/weight");
                }
            }
            else
            {
                Widgets.Label(new Rect(innerRect.x, innerRect.y + 24f, innerRect.width, 20f), "No items in this category");
            }
        }

        private static string GetCategoryLabel(GearCategory category)
        {
            switch (category)
            {
                case GearCategory.Weapons: return "Weapons";
                case GearCategory.MeleeWeapons: return "Melee";
                case GearCategory.Armors: return "装甲衣物";
                case GearCategory.Apparel: return "Apparel";
                case GearCategory.Accessories: return "Accessories";
                default: return "Unknown";
            }
        }

        private static string GetItemInfo(ThingDef thingDef)
        {
            List<string> infoParts = new List<string>();

            // 科技等级
            infoParts.Add(thingDef.techLevel.ToString());

            // 射程（仅武器）
            if (thingDef.IsRangedWeapon)
            {
                float range = FactionGearManager.GetWeaponRange(thingDef);
                if (range > 0)
                {
                    infoParts.Add($"Range: {range:F1}");
                }
            }

            // 伤害（武器和近战武器）
            if (thingDef.IsWeapon)
            {
                float damage = FactionGearManager.GetWeaponDamage(thingDef);
                if (damage > 0)
                {
                    infoParts.Add($"Damage: {damage:F1}");
                }
            }

            // 市场价值
            infoParts.Add($"Value: {thingDef.BaseMarketValue:F0}");

            // 护甲值（仅装甲和衣服）
            if (thingDef.IsApparel)
            {
                float armorRating = thingDef.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt);
                if (armorRating > 0)
                {
                    infoParts.Add($"Armor: {armorRating:F2}");
                }
            }

            // Mod来源
            string modSource = FactionGearManager.GetModSource(thingDef);
            if (!string.IsNullOrEmpty(modSource))
            {
                infoParts.Add(modSource);
            }

            return string.Join(" | ", infoParts);
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
                if (thingDef.Verbs != null && thingDef.Verbs.Count > 0)
                {
                    var verb = thingDef.Verbs[0];
                    lines.Add($"Accuracy: {verb.accuracyTouch:F2}, {verb.accuracyShort:F2}, {verb.accuracyMedium:F2}, {verb.accuracyLong:F2}");
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
    }

    public enum GearCategory
    {
        Weapons,
        MeleeWeapons,
        Armors,
        Apparel,
        Accessories
    }
}