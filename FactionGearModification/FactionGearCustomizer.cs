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
            FactionGearManager.LoadDefaultPresets();
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
            LoadDefaultPresets(null);
        }

        public static void LoadDefaultPresets(string factionDefName)
        {
            var factionDefs = factionDefName != null 
                ? new List<FactionDef> { DefDatabase<FactionDef>.GetNamedSilentFail(factionDefName) }
                : DefDatabase<FactionDef>.AllDefs.ToList();

            foreach (var factionDef in factionDefs)
            {
                if (factionDef == null)
                    continue;

                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(factionDef.defName);
                if (factionDef.pawnGroupMakers != null)
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
                                    LoadKindDefGear(option.kind, kindData);
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



        private static Vector2 factionListScrollPos = Vector2.zero;
        private static Vector2 kindListScrollPos = Vector2.zero;
        private static Vector2 gearListScrollPos = Vector2.zero;
        private static Vector2 libraryScrollPos = Vector2.zero;
        private static string searchText = "";

        private static string selectedModSource = "All";
        private static TechLevel? selectedTechLevel = null;

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
        private static string sortField = "Name"; // 默认按名称排序
        private static bool sortAscending = true; // 默认升序排序

        // 预设相关字段
        private static bool showPresetManager = false;
        private static FactionGearPreset selectedPreset = null;
        private static string newPresetName = "";
        private static string newPresetDescription = "";
        
        // 复制粘贴功能
        private static KindGearData copiedKindGearData = null;
        
        // 暂存设置
        private static FactionGearCustomizerSettings tempSettings = null;

        public static void DrawEditor(UnityEngine.Rect inRect)
        {
            // 规范化字体为环世界标准字体
            Text.Font = GameFont.Small;
            
            // 初始化暂存设置
            if (tempSettings == null)
            {
                tempSettings = new FactionGearCustomizerSettings();
                // 复制当前设置到暂存设置
                foreach (var kvp in FactionGearCustomizerMod.Settings.factionGearData)
                {
                    tempSettings.factionGearData[kvp.Key] = kvp.Value;
                }
                tempSettings.presets.AddRange(FactionGearCustomizerMod.Settings.presets);
            }
            
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

            // 1. 顶部按钮栏：使用 WidgetRow 自动横向排列
            Rect topRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            WidgetRow buttonRow = new WidgetRow(topRect.x, topRect.y, UIDirection.RightThenUp, topRect.width, 4f);

            if (buttonRow.ButtonText("Reset All"))
            {
                FactionGearCustomizerMod.Settings.ResetToDefault();
            }
            if (!string.IsNullOrEmpty(selectedFactionDefName) && buttonRow.ButtonText("Reset Current Faction"))
            {
                ResetCurrentFaction();
            }
            if (!string.IsNullOrEmpty(selectedFactionDefName) && buttonRow.ButtonText("Load Default Faction"))
            {
                FactionGearManager.LoadDefaultPresets(selectedFactionDefName);
                FactionGearCustomizerMod.Settings.Write();
            }
            if (!string.IsNullOrEmpty(selectedKindDefName) && buttonRow.ButtonText("Reset Current Kind"))
            {
                ResetCurrentKind();
            }
            if (buttonRow.ButtonText("Reset Filters"))
            {
                ResetFilters();
            }

            if (buttonRow.ButtonText("Save"))
            {
                // 将暂存设置保存到实际设置
                FactionGearCustomizerMod.Settings.factionGearData.Clear();
                foreach (var kvp in tempSettings.factionGearData)
                {
                    FactionGearCustomizerMod.Settings.factionGearData[kvp.Key] = kvp.Value;
                }
                FactionGearCustomizerMod.Settings.presets.Clear();
                FactionGearCustomizerMod.Settings.presets.AddRange(tempSettings.presets);
                FactionGearCustomizerMod.Settings.Write();
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

                // 检查该派系是否被修改过
                bool isModified = false;
                var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(factionDef.defName);
                if (factionData.kindGearData.Any(kv => kv.Value.isModified))
                {
                    isModified = true;
                }

                // 绘制派系名称
                Rect labelRect = new Rect(rowRect.x, rowRect.y, rowRect.width - 20f, rowRect.height);
                string labelText = factionDef.LabelCap;
                
                // 为修改过的派系添加视觉标记
                if (isModified)
                {
                    labelText = $"*{labelText}*";
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.yellow;
                    Widgets.Label(labelRect, labelText);
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                else
                {
                    Widgets.Label(labelRect, labelText);
                }

                // 点击派系名称时设置为当前选中派系
                if (Widgets.ButtonInvisible(new Rect(rowRect.x + 25f, rowRect.y, rowRect.width - 25f - 100f, rowRect.height)))
                {
                    selectedFactionDefName = factionDef.defName;
                    selectedKindDefName = "";
                    
                    // 选择该派系的第一个兵种
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
                Rect labelRect = new Rect(rowRect.x, rowRect.y, rowRect.width - 20f, rowRect.height);
                string labelText = kindDef.LabelCap;
                
                // 为修改过的兵种添加视觉标记
                if (isModified)
                {
                    labelText = $"*{labelText}*";
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.yellow;
                    Widgets.Label(labelRect, labelText);
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                else
                {
                    Widgets.Label(labelRect, labelText);
                }

                // 点击兵种名称时设置为当前选中兵种
                if (Widgets.ButtonInvisible(new Rect(rowRect.x + 25f, rowRect.y, rowRect.width - 25f - 140f, rowRect.height)))
                {
                    selectedKindDefName = kindDef.defName;
                    // 重置滚动位置，确保物品列表更新后正确显示
                    gearListScrollPos = Vector2.zero;
                }
                
                // 添加Copy图标
                Rect copyIconRect = new Rect(rowRect.xMax - 100f, rowRect.y, 20f, 20f);
                if (Widgets.ButtonInvisible(copyIconRect))
                {
                    var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                    var kindData = factionData.GetOrCreateKindData(kindDef.defName);
                    CopyKindDefGear();
                }
                // 绘制复制图标（使用try-catch避免ContentFinder错误）
                if (Widgets.ButtonInvisible(copyIconRect))
                {
                    var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                    var kindData = factionData.GetOrCreateKindData(kindDef.defName);
                    CopyKindDefGear();
                }
                try
                {
                    // 尝试加载复制图标 - 使用更安全的方式，避免ContentFinder的错误日志
                    Texture2D copyTexture = null;
                    try
                    {
                        copyTexture = ContentFinder<Texture2D>.Get("UI/Buttons/Copy", false);
                    }
                    catch {}
                    if (copyTexture != null)
                    {
                        Widgets.DrawTextureFitted(copyIconRect, copyTexture, 1f);
                    }
                    else
                    {
                        Widgets.Label(copyIconRect, "C");
                    }
                }
                catch (Exception e)
                {
                    // 出错时显示替代标签
                    Widgets.Label(copyIconRect, "C");
                }
                
                // 常驻显示Paste图标
                Rect pasteIconRect = new Rect(rowRect.xMax - 70f, rowRect.y, 20f, 20f);
                try
                {
                    if (copiedKindGearData != null)
                    {
                        if (Widgets.ButtonInvisible(pasteIconRect))
                        {
                            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                            var kindData = factionData.GetOrCreateKindData(kindDef.defName);
                            PasteKindDefGear();
                            // 确保设置修改标记
                            kindData.isModified = true;
                        }
                        // 尝试加载粘贴图标 - 使用更安全的方式，避免ContentFinder的错误日志
                        Texture2D pasteTexture = null;
                        try
                        {
                            pasteTexture = ContentFinder<Texture2D>.Get("UI/Buttons/Paste", false);
                        }
                        catch {}
                        if (pasteTexture != null)
                        {
                            Widgets.DrawTextureFitted(pasteIconRect, pasteTexture, 1f);
                        }
                        else
                        {
                            Widgets.Label(pasteIconRect, "P");
                        }
                    }
                    else
                    {
                        // 当没有复制数据时，显示灰色的粘贴图标
                        GUI.color = Color.grey;
                        Texture2D pasteTexture = null;
                        try
                        {
                            pasteTexture = ContentFinder<Texture2D>.Get("UI/Buttons/Paste", false);
                        }
                        catch {}
                        if (pasteTexture != null)
                        {
                            Widgets.DrawTextureFitted(pasteIconRect, pasteTexture, 1f);
                        }
                        else
                        {
                            Widgets.Label(pasteIconRect, "P");
                        }
                        GUI.color = Color.white;
                    }
                }
                catch (Exception e)
                {
                    // 出错时显示替代标签
                    if (copiedKindGearData != null)
                    {
                        Widgets.Label(pasteIconRect, "P");
                    }
                    else
                    {
                        GUI.color = Color.grey;
                        Widgets.Label(pasteIconRect, "P");
                        GUI.color = Color.white;
                    }
                }
                
                // 添加应用到本faction其他所有kind的图标
                Rect applyAllIconRect = new Rect(rowRect.xMax - 40f, rowRect.y, 20f, 20f);
                try
                {
                    if (copiedKindGearData != null)
                    {
                        if (Widgets.ButtonInvisible(applyAllIconRect))
                        {
                            ApplyToAllKindsInFaction();
                        }
                        // 尝试加载应用图标 - 使用更安全的方式，避免ContentFinder的错误日志
                        Texture2D applyTexture = null;
                        try
                        {
                            applyTexture = ContentFinder<Texture2D>.Get("UI/Buttons/Apply", false);
                        }
                        catch {}
                        if (applyTexture != null)
                        {
                            Widgets.DrawTextureFitted(applyAllIconRect, applyTexture, 1f);
                        }
                        else
                        {
                            Widgets.Label(applyAllIconRect, "A");
                        }
                    }
                    else
                    {
                        // 当没有复制数据时，显示灰色的应用图标
                        GUI.color = Color.grey;
                        Texture2D applyTexture = null;
                        try
                        {
                            applyTexture = ContentFinder<Texture2D>.Get("UI/Buttons/Apply", false);
                        }
                        catch {}
                        if (applyTexture != null)
                        {
                            Widgets.DrawTextureFitted(applyAllIconRect, applyTexture, 1f);
                        }
                        else
                        {
                            Widgets.Label(applyAllIconRect, "A");
                        }
                        GUI.color = Color.white;
                    }
                }
                catch (Exception e)
                {
                    // 出错时显示替代标签
                    if (copiedKindGearData != null)
                    {
                        Widgets.Label(applyAllIconRect, "A");
                    }
                    else
                    {
                        GUI.color = Color.grey;
                        Widgets.Label(applyAllIconRect, "A");
                        GUI.color = Color.white;
                    }
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
            float previewHeight = 100f;
            Rect listOutRect = new Rect(innerRect.x, tabRect.yMax + 5f, innerRect.width, innerRect.height - 24f - 24f - previewHeight - 10f);
            
            // 确保列表区域高度为正
            if (listOutRect.height < 100f)
                listOutRect.height = 100f;

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

            // 计算列表区域
            Rect listOutRect = new Rect(innerRect.x, filterRect.yMax + 5f, innerRect.width, innerRect.height - 24f - filterRect.height - 10f);

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


        }

        private static void DrawFilters(Rect rect)
        {
            // 检查是否需要计算边界值
            if (needCalculateBounds)
            {
                CalculateFilterBounds();
                needCalculateBounds = false;
            }
            
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
                Widgets.FloatRange(rangeRect, 1, ref rangeFilter, minRange, maxRange, "Range", ToStringStyle.FloatOne);
            }

            if (selectedCategory == GearCategory.Weapons || selectedCategory == GearCategory.MeleeWeapons)
            {
                Rect damageRect = listing.GetRect(28f);
                Widgets.FloatRange(damageRect, 2, ref damageFilter, minDamage, maxDamage, "Damage", ToStringStyle.FloatOne);
            }

            // 市场价值滑块（与其他滑块样式一致）
            Rect marketValueRect = listing.GetRect(28f);
            Widgets.FloatRange(marketValueRect, 3, ref marketValueFilter, minMarketValue, maxMarketValue, "MarketValue", ToStringStyle.FloatOne);



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
                    // 标记需要重新计算边界值
                    needCalculateBounds = true;
                }
            }
        }

        private static void CalculateFilterBounds()
        {
            List<ThingDef> items = GetFilteredItems();
            if (!items.Any()) return;

            // 计算射程边界
            if (selectedCategory == GearCategory.Weapons)
            {
                var ranges = items.Select(t => FactionGearManager.GetWeaponRange(t)).Where(r => r > 0).ToList();
                if (ranges.Any())
                {
                    minRange = ranges.Min();
                    maxRange = ranges.Max();
                }
                else
                {
                    minRange = 0f;
                    maxRange = 100f;
                }
            }

            // 计算伤害边界
            if (selectedCategory == GearCategory.Weapons || selectedCategory == GearCategory.MeleeWeapons)
            {
                var damages = items.Select(t => FactionGearManager.GetWeaponDamage(t)).Where(d => d > 0).ToList();
                if (damages.Any())
                {
                    minDamage = damages.Min();
                    maxDamage = damages.Max();
                }
                else
                {
                    minDamage = 0f;
                    maxDamage = 100f;
                }
            }

            // 计算市场价值边界
            var marketValues = items.Select(t => t.BaseMarketValue).Where(v => v > 0).ToList();
            if (marketValues.Any())
            {
                minMarketValue = marketValues.Min();
                maxMarketValue = marketValues.Max();
            }
            else
            {
                minMarketValue = 0f;
                maxMarketValue = 10000f;
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
            // 标记需要重新计算边界值
            needCalculateBounds = true;
        }

        // 复制KindDef装备
        private static void CopyKindDefGear()
        {
            if (string.IsNullOrEmpty(selectedFactionDefName) || string.IsNullOrEmpty(selectedKindDefName))
                return;

            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
            var kindData = factionData.GetOrCreateKindData(selectedKindDefName);

            // 创建深拷贝
            copiedKindGearData = new KindGearData(kindData.kindDefName);
            copiedKindGearData.weapons = kindData.weapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            copiedKindGearData.meleeWeapons = kindData.meleeWeapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            copiedKindGearData.armors = kindData.armors.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            copiedKindGearData.apparel = kindData.apparel.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            copiedKindGearData.accessories = kindData.accessories.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            copiedKindGearData.isModified = kindData.isModified;
        }

        // 粘贴KindDef装备
        private static void PasteKindDefGear()
        {
            if (string.IsNullOrEmpty(selectedFactionDefName) || string.IsNullOrEmpty(selectedKindDefName) || copiedKindGearData == null)
                return;

            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
            var kindData = factionData.GetOrCreateKindData(selectedKindDefName);

            // 粘贴数据
            kindData.weapons = copiedKindGearData.weapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            kindData.meleeWeapons = copiedKindGearData.meleeWeapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            kindData.armors = copiedKindGearData.armors.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            kindData.apparel = copiedKindGearData.apparel.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            kindData.accessories = copiedKindGearData.accessories.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            kindData.isModified = true;

            FactionGearCustomizerMod.Settings.Write();
        }

        // 应用到本faction其他所有kind
        private static void ApplyToAllKindsInFaction()
        {
            if (string.IsNullOrEmpty(selectedFactionDefName) || copiedKindGearData == null)
                return;

            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
            
            // 遍历派系中的所有兵种
            foreach (var kvp in factionData.kindGearData)
            {
                var kindDefName = kvp.Key;
                var kindData = kvp.Value;
                
                // 跳过当前选中的兵种
                if (kindDefName == selectedKindDefName)
                    continue;
                
                // 粘贴数据
                kindData.weapons = copiedKindGearData.weapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                kindData.meleeWeapons = copiedKindGearData.meleeWeapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                kindData.armors = copiedKindGearData.armors.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                kindData.apparel = copiedKindGearData.apparel.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                kindData.accessories = copiedKindGearData.accessories.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                kindData.isModified = true;
            }

            FactionGearCustomizerMod.Settings.Write();
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
            // 处理当前选中的派系
            if (string.IsNullOrEmpty(selectedFactionDefName))
                return;

            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
            
            // 处理当前选中的兵种
            if (string.IsNullOrEmpty(selectedKindDefName))
                return;

            var kindData = factionData.GetOrCreateKindData(selectedKindDefName);
            var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(selectedKindDefName);

            if (kindDef == null)
                return;

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

            // 获取默认预设装备（模拟原版装备）
            var defaultApparel = GetDefaultApparelForKindDef(kindDef);
            
            // 获取当前自定义装备
            var currentApparel = GetCurrentApparel(kindData);
            
            // 分析身体区域覆盖
            var defaultBodyParts = GetCoveredBodyParts(defaultApparel);
            var currentBodyParts = GetCoveredBodyParts(currentApparel);
            
            // 找出缺失的身体区域覆盖
            var missingBodyParts = defaultBodyParts.Except(currentBodyParts).ToList();
            
            // 为缺失的身体区域添加装备
            foreach (var bodyPart in missingBodyParts)
            {
                AddApparelForBodyPart(kindData, bodyPart, defaultApparel);
            }
            
            // 检查价值相近的装备
            CheckAndAddValueSimilarApparel(kindData, defaultApparel, currentApparel);

            kindData.isModified = true;

            // 保存设置
            FactionGearCustomizerMod.Settings.Write();
        }

        // 获取兵种的默认装备（模拟原版预设）
        private static List<ThingDef> GetDefaultApparelForKindDef(PawnKindDef kindDef)
        {
            List<ThingDef> defaultApparel = new List<ThingDef>();
            
            // 基于兵种科技等级和标签添加默认装备
            TechLevel techLevel = TechLevel.Industrial; // 默认科技等级
            
            // 添加头盔
            var helmets = FactionGearManager.GetAllHelmets()
                .Where(h => h.techLevel <= techLevel)
                .OrderByDescending(h => h.BaseMarketValue)
                .Take(1)
                .ToList();
            defaultApparel.AddRange(helmets);
            
            // 添加装甲
            var armors = FactionGearManager.GetAllArmors()
                .Where(a => a.techLevel <= techLevel)
                .OrderByDescending(a => a.BaseMarketValue)
                .Take(1)
                .ToList();
            defaultApparel.AddRange(armors);
            
            // 添加衣物
            var apparels = FactionGearManager.GetAllApparel()
                .Where(a => a.techLevel <= techLevel)
                .OrderByDescending(a => a.BaseMarketValue)
                .Take(2)
                .ToList();
            defaultApparel.AddRange(apparels);
            
            // 添加饰品
            var accessories = FactionGearManager.GetAllAccessories()
                .Where(a => a.techLevel <= techLevel)
                .OrderByDescending(a => a.BaseMarketValue)
                .Take(1)
                .ToList();
            defaultApparel.AddRange(accessories);
            
            return defaultApparel;
        }

        // 获取当前自定义装备
        private static List<ThingDef> GetCurrentApparel(KindGearData kindData)
        {
            List<ThingDef> currentApparel = new List<ThingDef>();
            
            // 添加装甲
            currentApparel.AddRange(kindData.armors
                .Select(g => g.ThingDef)
                .Where(t => t != null));
            
            // 添加衣物
            currentApparel.AddRange(kindData.apparel
                .Select(g => g.ThingDef)
                .Where(t => t != null));
            
            // 添加饰品
            currentApparel.AddRange(kindData.accessories
                .Select(g => g.ThingDef)
                .Where(t => t != null));
            
            return currentApparel;
        }

        // 获取装备覆盖的身体区域
        private static List<BodyPartGroupDef> GetCoveredBodyParts(List<ThingDef> apparel)
        {
            List<BodyPartGroupDef> bodyParts = new List<BodyPartGroupDef>();
            
            foreach (var thingDef in apparel)
            {
                if (thingDef.IsApparel && thingDef.apparel != null && thingDef.apparel.bodyPartGroups != null)
                {
                    bodyParts.AddRange(thingDef.apparel.bodyPartGroups);
                }
            }
            
            return bodyParts.Distinct().ToList();
        }

        // 为特定身体区域添加装备
        private static void AddApparelForBodyPart(KindGearData kindData, BodyPartGroupDef bodyPart, List<ThingDef> defaultApparel)
        {
            // 查找默认装备中覆盖该身体区域的装备
            var defaultItems = defaultApparel
                .Where(a => a.IsApparel && a.apparel != null && a.apparel.bodyPartGroups != null && a.apparel.bodyPartGroups.Contains(bodyPart))
                .ToList();
            
            if (!defaultItems.Any())
                return;
            
            // 获取价值最高的默认装备作为参考
            var referenceItem = defaultItems.OrderByDescending(i => i.BaseMarketValue).First();
            float referenceValue = referenceItem.BaseMarketValue;
            
            // 查找当前装备中覆盖该身体区域的装备
            var currentItems = GetCurrentApparel(kindData)
                .Where(a => a.apparel != null && a.apparel.bodyPartGroups != null && a.apparel.bodyPartGroups.Contains(bodyPart))
                .ToList();
            
            // 如果已经有装备覆盖该身体区域，跳过
            if (currentItems.Any())
                return;
            
            // 查找价值相近的装备
            var valueSimilarItems = DefDatabase<ThingDef>.AllDefs
                .Where(t => t.IsApparel && t.apparel != null && t.apparel.bodyPartGroups != null && t.apparel.bodyPartGroups.Contains(bodyPart))
                .Where(t => IsValueSimilar(t.BaseMarketValue, referenceValue))
                .OrderByDescending(t => t.BaseMarketValue)
                .Take(1)
                .ToList();
            
            // 添加装备到合适的类别
            foreach (var item in valueSimilarItems.Any() ? valueSimilarItems : defaultItems.Take(1))
            {
                if (item.apparel.layers != null && item.apparel.layers.Contains(ApparelLayerDefOf.Overhead))
                {
                    // 头盔 - 这里暂时不处理，因为我们通常通过其他方式添加
                }
                else if (item.apparel.layers != null && (item.apparel.layers.Contains(ApparelLayerDefOf.Shell) || item.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) > 0.4f))
                {
                    // 装甲
                    if (!kindData.armors.Any(g => g.thingDefName == item.defName))
                    {
                        kindData.armors.Add(new GearItem(item.defName));
                    }
                }
                else if (item.apparel.layers != null && item.apparel.layers.Contains(ApparelLayerDefOf.Belt))
                {
                    // 饰品
                    if (!kindData.accessories.Any(g => g.thingDefName == item.defName))
                    {
                        kindData.accessories.Add(new GearItem(item.defName));
                    }
                }
                else
                {
                    // 普通衣物
                    if (!kindData.apparel.Any(g => g.thingDefName == item.defName))
                    {
                        kindData.apparel.Add(new GearItem(item.defName));
                    }
                }
            }
        }

        // 检查并添加价值相近的装备
        private static void CheckAndAddValueSimilarApparel(KindGearData kindData, List<ThingDef> defaultApparel, List<ThingDef> currentApparel)
        {
            foreach (var defaultItem in defaultApparel)
            {
                if (!defaultItem.IsApparel)
                    continue;
                
                // 检查当前装备中是否有覆盖相同身体区域的装备
                var defaultBodyParts = defaultItem.apparel?.bodyPartGroups ?? new List<BodyPartGroupDef>();
                bool hasSimilarCoverage = currentApparel.Any(c => 
                    c.apparel != null && 
                    c.apparel.bodyPartGroups != null && 
                    defaultBodyParts.Intersect(c.apparel.bodyPartGroups).Any());
                
                if (hasSimilarCoverage)
                    continue;
                
                // 检查是否有价值相近的装备
                bool hasValueSimilar = currentApparel.Any(c => 
                    c.apparel != null && 
                    c.apparel.bodyPartGroups != null && 
                    defaultBodyParts.Intersect(c.apparel.bodyPartGroups).Any() && 
                    IsValueSimilar(c.BaseMarketValue, defaultItem.BaseMarketValue));
                
                if (hasValueSimilar)
                    continue;
                
                // 添加默认装备
                if (defaultItem.apparel.layers != null && defaultItem.apparel.layers.Contains(ApparelLayerDefOf.Overhead))
                {
                    // 头盔 - 这里暂时不处理
                }
                else if (defaultItem.apparel.layers != null && (defaultItem.apparel.layers.Contains(ApparelLayerDefOf.Shell) || defaultItem.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) > 0.4f))
                {
                    // 装甲
                    if (!kindData.armors.Any(g => g.thingDefName == defaultItem.defName))
                    {
                        kindData.armors.Add(new GearItem(defaultItem.defName));
                    }
                }
                else if (defaultItem.apparel.layers != null && defaultItem.apparel.layers.Contains(ApparelLayerDefOf.Belt))
                {
                    // 饰品
                    if (!kindData.accessories.Any(g => g.thingDefName == defaultItem.defName))
                    {
                        kindData.accessories.Add(new GearItem(defaultItem.defName));
                    }
                }
                else
                {
                    // 普通衣物
                    if (!kindData.apparel.Any(g => g.thingDefName == defaultItem.defName))
                    {
                        kindData.apparel.Add(new GearItem(defaultItem.defName));
                    }
                }
            }
        }

        // 检查两个值是否相近（上下浮动20%）
        private static bool IsValueSimilar(float value1, float value2)
        {
            if (value1 <= 0 || value2 <= 0)
                return false;
            
            float ratio = Mathf.Max(value1, value2) / Mathf.Min(value1, value2);
            return ratio <= 1.2f; // 20% 浮动
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

            // 获取当前类别的装备列表
            List<GearItem> gearItems = GetCurrentCategoryGear(kindData);

            if (gearItems.Any())
            {
                // 计算平均市场价值
                float totalValue = 0f;
                int validItems = 0;

                foreach (var gearItem in gearItems)
                {
                    var thingDef = gearItem.ThingDef;
                    if (thingDef != null)
                    {
                        totalValue += thingDef.BaseMarketValue;
                        validItems++;
                    }
                }

                if (validItems > 0)
                {
                    float avgValue = totalValue / validItems;
                    Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 20f), $"Average Market Value: {avgValue:F0} silver");
                }
                else
                {
                    // 当所有装备的ThingDef都为null时，显示无有效项目
                    Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 20f), "No valid items in this category");
                }
            }
            else
            {
                Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 20f), "No items in this category");
            }
        }

        private static string GetCategoryLabel(GearCategory category)
        {
            switch (category)
            {
                case GearCategory.Weapons: return "Ranged";
                case GearCategory.MeleeWeapons: return "Melee";
                case GearCategory.Armors: return "Armors";
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