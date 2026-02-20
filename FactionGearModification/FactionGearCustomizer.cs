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
        public string name = "New Preset";
        public string description = "";
        // 存储包含修改的派系数据
        public List<FactionGearData> factionGearData = new List<FactionGearData>();
        // 存储该预设所需的模组列表
        public List<string> requiredMods = new List<string>();

        public FactionGearPreset() { }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name", "New Preset");
            Scribe_Values.Look(ref description, "description", "");
            Scribe_Collections.Look(ref factionGearData, "factionGearData", LookMode.Deep);
            Scribe_Collections.Look(ref requiredMods, "requiredMods", LookMode.Value);

            if (factionGearData == null) factionGearData = new List<FactionGearData>();
            if (requiredMods == null) requiredMods = new List<string>();
        }

        // 自动计算并更新所需的模组列表
        public void CalculateRequiredMods()
        {
            HashSet<string> mods = new HashSet<string>();
            foreach (var faction in factionGearData)
            {
                foreach (var kind in faction.kindGearData)
                {
                    var allGear = kind.weapons.Concat(kind.meleeWeapons).Concat(kind.armors).Concat(kind.apparel).Concat(kind.others);
                    foreach (var gear in allGear)
                    {
                        var def = gear.ThingDef;
                        if (def != null && def.modContentPack != null && !def.modContentPack.IsCoreMod)
                        {
                            mods.Add(def.modContentPack.Name);
                        }
                    }
                }
            }
            requiredMods = mods.ToList();
        }

        // 从当前游戏设置中抓取被修改的数据并深拷贝保存
        public void SaveFromCurrentSettings(List<FactionGearData> currentSettingsData)
        {
            factionGearData.Clear();
            foreach (var faction in currentSettingsData)
            {
                // 只保存有修改兵种的派系
                var modifiedKinds = faction.kindGearData.Where(k => k.isModified).ToList();
                if (modifiedKinds.Any())
                {
                    // 创建深拷贝
                    FactionGearData newFactionData = new FactionGearData(faction.factionDefName);
                    foreach (var kind in modifiedKinds)
                    {
                        KindGearData newKindData = new KindGearData(kind.kindDefName)
                        {
                            isModified = true,
                            weapons = kind.weapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                            meleeWeapons = kind.meleeWeapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                            armors = kind.armors.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                            apparel = kind.apparel.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                            others = kind.others.Select(g => new GearItem(g.thingDefName, g.weight)).ToList()
                        };
                        newFactionData.kindGearData.Add(newKindData);
                    }
                    factionGearData.Add(newFactionData);
                }
            }
            CalculateRequiredMods();
        }
    }

    // 预设导入/导出管理器
    public static class PresetIOManager
    {
        // 导出预设为 Base64 字符串
        public static string ExportToBase64(FactionGearPreset preset)
        {
            string path = System.IO.Path.Combine(GenFilePaths.ConfigFolderPath, "TempPresetExport.xml");
            try
            {
                Scribe.saver.InitSaving(path, "FactionGearPreset");
                Scribe_Deep.Look(ref preset, "Preset");
                Scribe.saver.FinalizeSaving();
                
                string xml = System.IO.File.ReadAllText(path);
                return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(xml));
            }
            catch (System.Exception e)
            {
                Log.Error("[FactionGearCustomizer] Export failed: " + e.Message);
                return null;
            }
        }

        // 从 Base64 字符串导入预设
        public static FactionGearPreset ImportFromBase64(string base64)
        {
            string path = System.IO.Path.Combine(GenFilePaths.ConfigFolderPath, "TempPresetImport.xml");
            try
            {
                string xml = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(base64));
                System.IO.File.WriteAllText(path, xml);

                FactionGearPreset preset = null;
                Scribe.loader.InitLoading(path);
                Scribe_Deep.Look(ref preset, "Preset");
                Scribe.loader.FinalizeLoading();

                return preset;
            }
            catch
            {
                return null; // 导入失败（字符串不合法）
            }
        }
    }

    public class FactionGearCustomizerSettings : ModSettings
    {
        // 版本控制
        private int version = 2;
        public List<FactionGearData> factionGearData = new List<FactionGearData>();
        public List<FactionGearPreset> presets = new List<FactionGearPreset>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref version, "version", 2);
            
            // 处理不同版本的数据结构
            if (version == 1)
            {
                // 旧版本使用字典，需要转换
                Dictionary<string, FactionGearData> oldFactionGearData = new Dictionary<string, FactionGearData>();
                Scribe_Collections.Look(ref oldFactionGearData, "factionGearData", LookMode.Value, LookMode.Deep);
                if (oldFactionGearData != null)
                {
                    factionGearData = oldFactionGearData.Values.ToList();
                }
            }
            else
            {
                // 新版本使用列表
                Scribe_Collections.Look(ref factionGearData, "factionGearData", LookMode.Deep);
            }
            
            if (factionGearData == null)
                factionGearData = new List<FactionGearData>();

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
            var data = factionGearData.FirstOrDefault(f => f.factionDefName == factionDefName);
            if (data == null)
            {
                data = new FactionGearData(factionDefName);
                factionGearData.Add(data);
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
        public List<KindGearData> kindGearData = new List<KindGearData>();

        public FactionGearData() { }

        public FactionGearData(string factionDefName)
        {
            this.factionDefName = factionDefName;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref factionDefName, "factionDefName");
            Scribe_Collections.Look(ref kindGearData, "kindGearData", LookMode.Deep);
            if (kindGearData == null)
                kindGearData = new List<KindGearData>();
        }

        public KindGearData GetOrCreateKindData(string kindDefName)
        {
            var data = kindGearData.FirstOrDefault(k => k.kindDefName == kindDefName);
            if (data == null)
            {
                data = new KindGearData(kindDefName);
                kindGearData.Add(data);
            }
            return data;
        }

        public void ResetToDefault()
        {
            kindGearData.Clear();
        }
        
        // 添加或更新兵种数据
        public void AddOrUpdateKindData(KindGearData data)
        {
            var existing = kindGearData.FirstOrDefault(k => k.kindDefName == data.kindDefName);
            if (existing != null)
            {
                kindGearData.Remove(existing);
            }
            kindGearData.Add(data);
        }
        
        // 获取指定兵种的数据
        public KindGearData GetKindData(string kindDefName)
        {
            return kindGearData.FirstOrDefault(k => k.kindDefName == kindDefName);
        }
    }

    public class KindGearData : IExposable
    {
        public string kindDefName;
        public List<GearItem> weapons = new List<GearItem>();
        public List<GearItem> meleeWeapons = new List<GearItem>();
        public List<GearItem> armors = new List<GearItem>();
        public List<GearItem> apparel = new List<GearItem>();
        public List<GearItem> others = new List<GearItem>();
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
            Scribe_Collections.Look(ref others, "others", LookMode.Deep);
            Scribe_Values.Look(ref isModified, "isModified", false);

            if (weapons == null) weapons = new List<GearItem>();
            if (meleeWeapons == null) meleeWeapons = new List<GearItem>();
            if (armors == null) armors = new List<GearItem>();
            if (apparel == null) apparel = new List<GearItem>();
            if (others == null) others = new List<GearItem>();
        }

        public void ResetToDefault()
        {
            weapons.Clear();
            meleeWeapons.Clear();
            armors.Clear();
            apparel.Clear();
            others.Clear();
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
    // 静态构造函数类，用于提前缓存贴图资源
    [StaticConstructorOnStartup]
    public static class TexCache
    {
        public static readonly Texture2D CopyTex;
        public static readonly Texture2D PasteTex;
        public static readonly Texture2D ApplyTex;

        static TexCache()
        {
            // 提前加载并缓存贴图，避免在UI循环中实时读取硬盘
            // 尝试加载图标，如果失败则使用 null 安全处理
            CopyTex = TryLoadTexture("UI/Buttons/Copy");
            PasteTex = TryLoadTexture("UI/Buttons/Paste");
            ApplyTex = TryLoadTexture("UI/Buttons/Confirm"); // 使用 Confirm 代替 Apply，这个更可能存在
        }

        private static Texture2D TryLoadTexture(string path)
        {
            try
            {
                return ContentFinder<Texture2D>.Get(path, false);
            }
            catch
            {
                return null;
            }
        }
    }

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
            // 1. 读取原版武器标签
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

            // 2. 【核心修复】读取原版服装标签 (apparelTags)
            if (kindDef.apparelTags != null)
            {
                var apparels = DefDatabase<ThingDef>.AllDefs.Where(t => t.IsApparel && t.apparel.tags != null && t.apparel.tags.Intersect(kindDef.apparelTags).Any()).ToList();
                foreach (var app in apparels)
                {
                    if (app.apparel.layers != null)
                    {
                        if (app.apparel.layers.Contains(ApparelLayerDefOf.Shell) || app.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) > 0.4f)
                        {
                            if (!kindData.armors.Any(g => g.thingDefName == app.defName)) kindData.armors.Add(new GearItem(app.defName));
                        }
                        else if (app.apparel.layers.Contains(ApparelLayerDefOf.Belt))
                        {
                            if (!kindData.others.Any(g => g.thingDefName == app.defName)) kindData.others.Add(new GearItem(app.defName));
                        }
                        else
                        {
                            if (!kindData.apparel.Any(g => g.thingDefName == app.defName)) kindData.apparel.Add(new GearItem(app.defName));
                        }
                    }
                }
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

        // 4. 获取其他物品（饰品/腰带类）
        public static List<ThingDef> GetAllOthers() =>
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

        // [新增] 获取合并后的 Mod 分组名称
        public static string GetModGroup(ThingDef thingDef)
        {
            string rawName = GetModSource(thingDef);
            
            // 1. Combat Extended 系列
            if (rawName.StartsWith("Combat Extended", StringComparison.OrdinalIgnoreCase))
                return "Combat Extended (Group)";
                
            // 2. Vanilla Expanded 系列 (涵盖 VWE, VAE, VFE 等所有原版扩展)
            if (rawName.StartsWith("Vanilla", StringComparison.OrdinalIgnoreCase) && rawName.Contains("Expanded"))
                return "Vanilla Expanded (Group)";
                
            // 3. Alpha 系列 (Alpha Animals, Alpha Biomes 等)
            if (rawName.StartsWith("Alpha ", StringComparison.OrdinalIgnoreCase))
                return "Alpha Series (Group)";
                
            // 4. Rimsenal 系列 (边缘军工)
            if (rawName.StartsWith("Rimsenal", StringComparison.OrdinalIgnoreCase))
                return "Rimsenal (Group)";
                
            // 如果你还有其他想合并的模组，可以在这里继续照猫画虎添加 if 判断
            // ...
                
            // 如果没有匹配到任何热门系列，则返回原始名称
            return rawName;
        }
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

            if (!string.IsNullOrEmpty(kindDefName))
            {
                var kindData = factionData.GetKindData(kindDefName);
                if (kindData != null)
                {
                    ApplyWeapons(pawn, kindData);
                    ApplyApparel(pawn, kindData);
                }
            }
        }

        private static void ApplyWeapons(Pawn pawn, KindGearData kindData)
        {
            // 只销毁常规武器，保留特殊物品
            if (pawn.equipment != null)
            {
                var equipmentToDestroy = pawn.equipment.AllEquipmentListForReading
                    .Where(eq => ShouldDestroyItem(eq))
                    .ToList();
                
                foreach (var equipment in equipmentToDestroy)
                {
                    pawn.equipment.TryDropEquipment(equipment, out _, pawn.Position, true);
                    equipment.Destroy();
                }
            }

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
        
        // 检查物品是否应该被销毁
        private static bool ShouldDestroyItem(Thing item)
        {
            if (item == null)
                return false;
            
            // 保留destroyOnDrop为false的物品（通常是重要物品）
            if (!item.def.destroyOnDrop)
                return false;
            
            // 其他物品可以销毁
            return true;
        }

        private static void ApplyApparel(Pawn pawn, KindGearData kindData)
        {
            // 保底检查：如果自定义数据里什么都没有，就不要脱掉原版的衣服，防止裸体
            if (kindData.armors.NullOrEmpty() && kindData.apparel.NullOrEmpty() && kindData.others.NullOrEmpty())
            {
                return;
            }

            // 只销毁常规服装，保留特殊物品
            var apparelToDestroy = pawn.apparel.WornApparel
                .Where(app => ShouldDestroyItem(app))
                .ToList();
            
            foreach (var apparel in apparelToDestroy)
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
            EquipApparelList(kindData.others);
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
        private static string sortField = "Name"; // 默认按名称排序
        private static bool sortAscending = true; // 默认升序排序

        // 预设相关字段
        private static bool showPresetManager = false;
        private static FactionGearPreset selectedPreset = null;
        private static string newPresetName = "";
        private static string newPresetDescription = "";
        
        // 复制粘贴功能
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
        
        // 暂存设置


        public static void DrawEditor(UnityEngine.Rect inRect)
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

            if (buttonRow.ButtonText("Apply & Save to Game"))
            {
                // [修复] 彻底抛弃 tempSettings，直接保存当前状态
                FactionGearCustomizerMod.Settings.Write();
                Messages.Message("Settings saved successfully!", MessageTypeDefOf.PositiveEvent);

                // [新功能] 如果用户没有任何预设，自动弹出预设管理器并给出强烈建议
                if (FactionGearCustomizerMod.Settings.presets.Count == 0)
                {
                    showPresetManager = true;
                    Messages.Message("Tip: Please create a preset to safely back up your hard work!", MessageTypeDefOf.NeutralEvent);
                }
            }

            if (buttonRow.ButtonText("Presets"))
            {
                showPresetManager = !showPresetManager;
            }

            if (buttonRow.ButtonText("Auto Fill"))
            {
                AutoFillGear();
            }

            // 2. 划分三大主面板：为上面两行的按钮腾出 70f 的空间，防止面板重叠
            Rect mainRect = new Rect(inRect.x, inRect.y + 70f, inRect.width, inRect.height - 70f);
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
            // 显示新的预设管理器窗口
            Find.WindowStack.Add(new PresetManagerWindow());
            // 重置标志位，避免重复创建窗口
            showPresetManager = false;
        }

        // 预设管理器窗口类
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
                presetSearchText = Widgets.TextField(new Rect(innerRect.x, innerRect.y + 24f, innerRect.width, 24f), presetSearchText);
                
                // 2. 列表区域只占上方 80%
                Rect listOutRect = new Rect(innerRect.x, innerRect.y + 55f, innerRect.width, innerRect.height - 150f);
                
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
                    // 绘制列表项... [保持你现有的高亮和点击代码不变]
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
                
                // 【新增】将新建预设移到左侧底部
                float bottomY = innerRect.yMax - 85f;
                Widgets.DrawLineHorizontal(innerRect.x, bottomY, innerRect.width);
                Widgets.Label(new Rect(innerRect.x, bottomY + 5f, innerRect.width, 24f), "Create New:");
                newPresetName = Widgets.TextField(new Rect(innerRect.x, bottomY + 30f, innerRect.width - 70f, 24f), newPresetName);
                
                if (Widgets.ButtonText(new Rect(innerRect.xMax - 65f, bottomY + 30f, 65f, 24f), "Add"))
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
                Listing_Standard listing = new Listing_Standard();
                listing.Begin(innerRect);

                listing.Label("Preset Details:");
                selectedPreset.name = listing.TextEntry(selectedPreset.name);
                selectedPreset.description = listing.TextEntry(selectedPreset.description, 2);
                
                // 把那两个容易混淆的按钮名称改掉
                if (listing.ButtonText("1. Save Meta Info (Name/Desc)")) SavePreset();
                listing.Gap(10f);
                
                GUI.color = Color.cyan;
                if (listing.ButtonText("2. Overwrite Preset With Current Active Gear")) SaveFromCurrentSettings();
                GUI.color = Color.white;
                listing.GapLine();

                // 自动计算高度，不再硬编码 y += 160f
                listing.Label("Required Mods:");
                // 这里调用你原本画Mod的方法，但高度改小点
                DrawModList(listing.GetRect(80f)); 
                listing.Gap(10f);

                listing.Label("Faction Preview:");
                DrawFactionPreview(listing.GetRect(200f)); 
                
                listing.GapLine();
                // 危险操作放最底下
                Rect dangerRect = listing.GetRect(30f);
                if (Widgets.ButtonText(new Rect(dangerRect.x, dangerRect.y, 100f, 30f), "Load/Apply")) ApplyPreset();
                if (Widgets.ButtonText(new Rect(dangerRect.x + 110f, dangerRect.y, 100f, 30f), "Export to Clipboard")) ExportPreset();
                
                GUI.color = Color.red;
                if (Widgets.ButtonText(new Rect(dangerRect.xMax - 100f, dangerRect.y, 100f, 30f), "Delete Preset")) DeletePreset();
                GUI.color = Color.white;

                listing.End();
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
                        return;
                    }
                    
                    // 使用新的 PresetIOManager 导入预设
                    FactionGearPreset newPreset = PresetIOManager.ImportFromBase64(base64Content);
                    if (newPreset != null)
                    {
                        // 添加到预设列表
                        FactionGearCustomizerMod.Settings.AddPreset(newPreset);
                        selectedPreset = newPreset;
                        Log.Message("[FactionGearCustomizer] Preset imported successfully!");
                        Messages.Message($"Preset '{newPreset.name}' imported from clipboard!", MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Log.Message("[FactionGearCustomizer] Import failed: Invalid preset data!");
                    }
                }
                catch (System.Exception e)
                {
                    Log.Message("[FactionGearCustomizer] Import failed: " + e.Message);
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
                    
                    Widgets.BeginScrollView(innerRect, ref modListScrollPos, new Rect(0, 0, innerRect.width - 16f, selectedPreset.requiredMods.Count * 20f));
                    float y = 0;
                    
                    foreach (var mod in selectedPreset.requiredMods)
                    {
                        Widgets.Label(new Rect(0, y, innerRect.width, 20f), mod);
                        y += 20f;
                    }
                    
                    Widgets.EndScrollView();
                }
                else
                {
                    Widgets.Label(rect, "No required mods.");
                }
            }
        }

        private static void DrawPresetManagerWindow(int windowID)
        {
            // 旧窗口实现，保留以确保兼容性
            Rect inRect = new Rect(10, 30, 780, 560);
            Widgets.Label(inRect, "Please use the new Preset Manager Window.");
            
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

            // 1. 绘制派系列表（使用紧凑的原版风格）
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 30f), "Factions");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 派系列表占60%高度
            float factionListHeight = innerRect.height * 0.6f;
            Rect factionListOutRect = new Rect(innerRect.x, innerRect.y + 35f, innerRect.width, factionListHeight - 35f);

            // ================= 派系列表绘制 =================
            // 【修复 1】安全排序：防止因某些隐藏派系没有 label 导致报错
            var allFactions = DefDatabase<FactionDef>.AllDefs
                .OrderBy(f => f.label != null ? f.LabelCap.ToString() : f.defName)
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

                // 【需求更新】去除原版长篇介绍，改为分两行显示 "中文名称" 和 "存档代码(defName)"
                Rect infoRect = new Rect(rowRect.x + 36f, rowRect.y, rowRect.width - 120f, rowRect.height);
                string factionLabel = factionDef.label != null ? factionDef.LabelCap.ToString() : factionDef.defName;
                
                Text.Anchor = TextAnchor.MiddleLeft;
                // 使用富文本将 defName 显示为灰色，方便排查和对应存档
                Widgets.Label(infoRect, $"{factionLabel}\n<color=#aaaaaa>ID: {factionDef.defName}</color>");
                Text.Anchor = TextAnchor.UpperLeft;

                // 信息按钮
                Rect infoButtonRect = new Rect(rowRect.x + rowRect.width - 100f, rowRect.y + 8f, 16f, 16f);
                if (Widgets.ButtonInvisible(infoButtonRect))
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(factionDef));
                }
                Widgets.Label(infoButtonRect, "i");

                // 【修复 4】严格限制好感度的获取时机，仅在真正游玩 (Playing) 时获取
                Rect statusRect = new Rect(rowRect.x + rowRect.width - 80f, rowRect.y, 80f, rowRect.height);
                string statusText = "Neutral";
                Color statusColor = Color.cyan;

                if (Current.ProgramState == ProgramState.Playing && Find.FactionManager != null)
                {
                    var worldFaction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def == factionDef);
                    if (worldFaction != null && Faction.OfPlayer != null && worldFaction != Faction.OfPlayer)
                    {
                        try
                        {
                            float goodwill = worldFaction.PlayerGoodwill;
                            if (goodwill > 75f) { statusText = "Allied"; statusColor = Color.green; }
                            else if (goodwill > 0f) { statusText = "Friendly"; statusColor = new Color(0.4f, 1f, 0.4f); }
                            else if (goodwill > -75f) { statusText = "Neutral"; statusColor = Color.cyan; }
                            else { statusText = "Hostile"; statusColor = Color.red; }
                        }
                        catch { /* 忽略任何突发的底层计算异常 */ }
                    }
                }

                GUI.color = statusColor;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(statusRect, statusText);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;

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

            // 【修复】恢复使用 TexCache 中的图标，但添加安全检查
            Rect btnRect = new Rect(innerRect.xMax - 75f, kindListY + 5f, 20f, 20f);
            
            // 使用复制图标，添加安全检查
            Texture2D copyTex = TexCache.CopyTex ?? Widgets.CheckboxOnTex;
            if (Widgets.ButtonImage(btnRect, copyTex))
            {
                CopyKindDefGear();
                Messages.Message("Copied KindDef gear!", MessageTypeDefOf.TaskCompletion, false);
            }
            TooltipHandler.TipRegion(btnRect, "Copy selected KindDef's gear");

            btnRect.x += 25f;
            // 使用粘贴图标，添加安全检查
            Texture2D pasteTex = TexCache.PasteTex ?? Widgets.CheckboxOnTex;
            if (Widgets.ButtonImage(btnRect, pasteTex))
            {
                PasteKindDefGear();
                Messages.Message("Pasted gear to KindDef!", MessageTypeDefOf.TaskCompletion, false);
            }
            TooltipHandler.TipRegion(btnRect, "Paste gear to selected KindDef");

            btnRect.x += 25f;
            // 使用应用图标，添加安全检查
            Texture2D applyTex = TexCache.ApplyTex ?? Widgets.CheckboxOnTex;
            if (Widgets.ButtonImage(btnRect, applyTex))
            {
                ApplyToAllKindsInFaction();
                Messages.Message("Applied to ALL KindDefs in Faction!", MessageTypeDefOf.TaskCompletion, false);
            }
            TooltipHandler.TipRegion(btnRect, "Apply copied gear to ALL KindDefs in this Faction");

            Rect kindListOutRect = new Rect(innerRect.x, kindListY + 35f, innerRect.width, kindListHeight - 35f);

            List<PawnKindDef> kindDefsToDraw = new List<PawnKindDef>();
            if (!string.IsNullOrEmpty(selectedFactionDefName))
            {
                var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(selectedFactionDefName);
                if (factionDef != null && factionDef.pawnGroupMakers != null)
                {
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
                // 【修复 5】同样防止兵种列表因为 null label 崩溃
                kindDefsToDraw = DefDatabase<PawnKindDef>.AllDefs
                    .OrderBy(k => k.label != null ? k.LabelCap.ToString() : k.defName)
                    .ToList();
            }

            Rect kindListViewRect = new Rect(0, 0, kindListOutRect.width - 16f, kindDefsToDraw.Count * 32f);
            Widgets.BeginScrollView(kindListOutRect, ref kindListScrollPos, kindListViewRect);
            float kindY = 0;
            
            // 【修复 5.1】应用安全排序
            var sortedKindDefs = kindDefsToDraw.OrderBy(k => k.label != null ? k.LabelCap.ToString() : k.defName);
            
            foreach (var kindDef in sortedKindDefs)
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
                    // 复制当前兵种的装备
                    if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(kindDef.defName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                        var kindData = factionData.GetOrCreateKindData(kindDef.defName);
                        // 复制到剪贴板
                        copiedKindGearData = new KindGearData(kindDef.defName)
                        {
                            isModified = kindData.isModified,
                            weapons = kindData.weapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                            meleeWeapons = kindData.meleeWeapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                            armors = kindData.armors.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                            apparel = kindData.apparel.Select(g => new GearItem(g.thingDefName, g.weight)).ToList(),
                            others = kindData.others.Select(g => new GearItem(g.thingDefName, g.weight)).ToList()
                        };
                        Messages.Message("Copied gear from " + labelText, MessageTypeDefOf.TaskCompletion, false);
                    }
                }
                TooltipHandler.TipRegion(copyBtnRect, "Copy this KindDef's gear");

                // 粘贴按钮
                btnX += btnSize + btnSpacing;
                Texture2D kindPasteTex = TexCache.PasteTex ?? Widgets.CheckboxOnTex;
                Rect pasteBtnRect = new Rect(btnX, btnY, btnSize, btnSize);
                if (Widgets.ButtonImage(pasteBtnRect, kindPasteTex))
                {
                    // 粘贴装备到当前兵种
                    if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(kindDef.defName) && copiedKindGearData != null)
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                        var targetKindData = factionData.GetOrCreateKindData(kindDef.defName);
                        
                        // 复制装备数据
                        targetKindData.isModified = true;
                        targetKindData.weapons = copiedKindGearData.weapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                        targetKindData.meleeWeapons = copiedKindGearData.meleeWeapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                        targetKindData.armors = copiedKindGearData.armors.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                        targetKindData.apparel = copiedKindGearData.apparel.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                        targetKindData.others = copiedKindGearData.others.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                        
                        FactionGearCustomizerMod.Settings.Write();
                        Messages.Message("Pasted gear to " + labelText, MessageTypeDefOf.TaskCompletion, false);
                    }
                }
                TooltipHandler.TipRegion(pasteBtnRect, "Paste gear to this KindDef");

                // 点击兵种名称时设置为当前选中兵种
                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedKindDefName = kindDef.defName;
                    // 重置滚动位置，确保物品列表更新后正确显示
                    gearListScrollPos = Vector2.zero;
                    
                    // 加载该兵种的默认装备数据
                    if (!string.IsNullOrEmpty(selectedFactionDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                        var kindData = factionData.GetOrCreateKindData(kindDef.defName);
                        // 加载默认装备数据
                        FactionGearManager.LoadKindDefGear(kindDef, kindData);
                        // 保存设置
                        FactionGearCustomizerMod.Settings.Write();
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

            // 智能计算高度，确保在低分辨率下不会溢出
            float totalFixedHeight = 24f + 24f + 10f; // 标题 + 标签页 + 边距
            float availableHeight = innerRect.height - totalFixedHeight;
            
            // 计算价值权重预览区域的高度（最大100f，最小50f）
            float previewHeight = Mathf.Clamp(availableHeight * 0.3f, 50f, 100f);
            
            // 计算列表区域高度（最小80f）
            float listHeight = Mathf.Max(availableHeight - previewHeight, 80f);
            
            Rect listOutRect = new Rect(innerRect.x, tabRect.yMax + 5f, innerRect.width, listHeight);

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

            // 添加价值权重预览（放到底部）
            if (currentKindData != null)
            {
                // 计算预览区域位置，确保在面板底部
                float bottomPreviewHeight = Mathf.Clamp(innerRect.height * 0.2f, 40f, 80f);
                float previewY = innerRect.y + innerRect.height - bottomPreviewHeight;
                float actualPreviewHeight = bottomPreviewHeight;
                
                if (actualPreviewHeight > 0 && previewY > listOutRect.yMax)
                {
                    Rect previewRect = new Rect(innerRect.x, previewY, innerRect.width, actualPreviewHeight);
                    DrawValueWeightPreview(previewRect, currentKindData);
                }
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

            // 智能计算过滤器区域高度，确保在低分辨率下不会溢出
            float totalFixedHeight = 24f + 10f; // 标题 + 边距
            float availableHeight = innerRect.height - totalFixedHeight;
            
            // 计算过滤器区域高度（最大180f，最小100f）
            float filterHeight = Mathf.Clamp(availableHeight * 0.4f, 100f, 180f);
            
            Rect filterRect = new Rect(innerRect.x, innerRect.y + 24f, innerRect.width, filterHeight);
            
            // 绘制过滤器并获取实际使用的高度
            float actualFilterHeight = DrawFilters(filterRect);
            
            // 计算列表区域高度（最小80f）
            float listHeight = Mathf.Max(availableHeight - actualFilterHeight, 80f);

            // 计算列表区域
            Rect listOutRect = new Rect(innerRect.x, filterRect.y + actualFilterHeight + 5f, innerRect.width, listHeight);

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

        private static float DrawFilters(Rect rect)
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

            // Mod 筛选按钮（带自动横滚功能）
            if (Widgets.ButtonInvisible(modRect))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (var mod in GetAllModSources())
                {
                    options.Add(new FloatMenuOption(mod, () => selectedModSource = mod));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            // 绘制按钮背景
            Widgets.DrawOptionBackground(modRect, false);
            // 使用标签绘制文本
            Widgets.Label(modRect, $"Mod: {selectedModSource}");

            // 科技等级筛选按钮
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
            
            // 返回实际使用的高度
            return Mathf.Min(listing.CurHeight, rect.height);
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

            // 重新设计布局，确保各元素有足够空间
            float iconSize = 24f;
            float namePadding = 8f;
            float sliderWidth = 60f;
            float weightWidth = 30f;
            float removeButtonSize = 24f;
            float elementSpacing = 8f;
            
            // 计算各元素位置
            Rect iconRect = new Rect(rect.x, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize);
            
            // 右侧控件总宽度
            float rightControlsWidth = sliderWidth + elementSpacing + weightWidth + elementSpacing + removeButtonSize;
            
            // 名称区域宽度 = 总宽度 - 图标宽度 - 名称左边距 - 右侧控件宽度
            float nameWidth = rect.width - iconSize - namePadding - rightControlsWidth;
            
            Rect nameRect = new Rect(rect.x + iconSize + namePadding, rect.y, nameWidth, rect.height);
            
            // 右侧控件起始位置
            float rightStartX = rect.x + iconSize + namePadding + nameWidth;
            
            Rect sliderRect = new Rect(rightStartX, rect.y + (rect.height - 20f) / 2f, sliderWidth, 20f);
            Rect weightRect = new Rect(rightStartX + sliderWidth + elementSpacing, rect.y, weightWidth, rect.height);
            Rect removeRect = new Rect(rightStartX + sliderWidth + elementSpacing + weightWidth + elementSpacing, rect.y + (rect.height - removeButtonSize) / 2f, removeButtonSize, removeButtonSize);

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

            // 绘制物品名称（单行显示，不换行）
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(nameRect, thingDef.LabelCap);
            Text.WordWrap = true;

            // 绘制滑块和权重
            gearItem.weight = Widgets.HorizontalSlider(sliderRect, gearItem.weight, 0.1f, 10f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(weightRect, gearItem.weight.ToString("0.0"));
            Text.Anchor = TextAnchor.UpperLeft;

            // 绘制删除按钮
            if (Widgets.ButtonText(removeRect, "X"))
            {
                RemoveGearItem(gearItem, kindData);
                FactionGearCustomizerMod.Settings.Write();
            }
        }

        // 检查物品是否已经被添加到当前选中的兵种中
        private static bool IsItemAlreadyAdded(ThingDef thingDef)
        {
            if (string.IsNullOrEmpty(selectedFactionDefName) || string.IsNullOrEmpty(selectedKindDefName))
                return false;

            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
            var kindData = factionData.GetOrCreateKindData(selectedKindDefName);

            // 根据物品类型，检查是否已经存在于相应的列表中
            switch (selectedCategory)
            {
                case GearCategory.Weapons:
                    return kindData.weapons.Any(g => g.thingDefName == thingDef.defName);
                case GearCategory.MeleeWeapons:
                    return kindData.meleeWeapons.Any(g => g.thingDefName == thingDef.defName);
                case GearCategory.Armors:
                    return kindData.armors.Any(g => g.thingDefName == thingDef.defName);
                case GearCategory.Apparel:
                    return kindData.apparel.Any(g => g.thingDefName == thingDef.defName);
                case GearCategory.Others:
                    return kindData.others.Any(g => g.thingDefName == thingDef.defName);
                default:
                    return false;
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

            // 判断是否载入游戏，如果是则显示信息按钮，否则不显示
            bool isGameLoaded = Current.ProgramState == ProgramState.Playing || Find.World != null;
            if (isGameLoaded)
            {
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

            // 检查物品是否已经被添加到当前选中的兵种中
            bool isItemAdded = IsItemAlreadyAdded(thingDef);
            
            // 根据物品是否已添加，调整按钮的显示效果
            if (isItemAdded)
            {
                // 已添加的物品，显示按下去的效果
                GUI.color = new Color(0.7f, 0.7f, 0.7f); // 灰色
                Widgets.DrawHighlight(addRect);
                if (Widgets.ButtonText(addRect, "Added"))
                {
                    // 已添加的物品，点击可以移除
                    if (!string.IsNullOrEmpty(selectedFactionDefName) && !string.IsNullOrEmpty(selectedKindDefName))
                    {
                        var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
                        var kindData = factionData.GetOrCreateKindData(selectedKindDefName);
                        var gearItem = GetGearItemFromThingDef(thingDef, kindData);
                        if (gearItem != null)
                        {
                            RemoveGearItem(gearItem, kindData);
                            FactionGearCustomizerMod.Settings.Write();
                        }
                    }
                }
                GUI.color = Color.white;
            }
            else
            {
                // 未添加的物品，显示普通添加按钮
                if (Widgets.ButtonText(addRect, "Add"))
                {
                    AddGearItem(thingDef);
                    FactionGearCustomizerMod.Settings.Write();
                }
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

                // [修改] 筛选Mod来源 (将 GetModSource 改为 GetModGroup)
                if (filterByMod && FactionGearManager.GetModGroup(item) != selectedModSource)
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
            copiedKindGearData.others = kindData.others.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
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
            kindData.others = copiedKindGearData.others.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
            kindData.isModified = true;

            FactionGearCustomizerMod.Settings.Write();
        }

        // 应用到本faction其他所有kind
        private static void ApplyToAllKindsInFaction()
        {
            if (string.IsNullOrEmpty(selectedFactionDefName) || copiedKindGearData == null)
                return;

            var factionData = FactionGearCustomizerMod.Settings.GetOrCreateFactionData(selectedFactionDefName);
            var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(selectedFactionDefName);
            
            if (factionDef == null)
                return;
            
            // 获取派系的所有兵种
            List<string> allKindDefNames = new List<string>();
            if (factionDef.pawnGroupMakers != null)
            {
                foreach (var pawnGroupMaker in factionDef.pawnGroupMakers)
                {
                    if (pawnGroupMaker.options != null)
                    {
                        foreach (var option in pawnGroupMaker.options)
                        {
                            if (option.kind != null && !allKindDefNames.Contains(option.kind.defName))
                                allKindDefNames.Add(option.kind.defName);
                        }
                    }
                }
            }
            
            // 遍历派系中的所有兵种
            foreach (var kindDefName in allKindDefNames)
            {
                // 跳过当前选中的兵种
                if (kindDefName == selectedKindDefName)
                    continue;
                
                // 获取或创建兵种数据
                var kindData = factionData.GetOrCreateKindData(kindDefName);
                
                // 粘贴数据
                kindData.weapons = copiedKindGearData.weapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                kindData.meleeWeapons = copiedKindGearData.meleeWeapons.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                kindData.armors = copiedKindGearData.armors.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                kindData.apparel = copiedKindGearData.apparel.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                kindData.others = copiedKindGearData.others.Select(g => new GearItem(g.thingDefName, g.weight)).ToList();
                kindData.isModified = true;
            }

            FactionGearCustomizerMod.Settings.Write();
        }

        private static List<string> GetAllModSources()
        {
            if (cachedModSources == null)
            {
                var modSources = new HashSet<string> { "All" };
                // 只遍历武器和衣服，减少循环量
                foreach (var thingDef in DefDatabase<ThingDef>.AllDefs.Where(t => t.IsWeapon || t.IsApparel))
                {
                    if (thingDef.modContentPack != null)
                    {
                        // [修改] 使用我们刚写的分组方法，替代原始的 thingDef.modContentPack.Name
                        modSources.Add(FactionGearManager.GetModGroup(thingDef));
                    }
                }
                cachedModSources = modSources.OrderBy(s => s).ToList();
            }
            return cachedModSources;
        }

        private static List<GearItem> GetCurrentCategoryGear(KindGearData kindData)
        {
            switch (selectedCategory)
            {
                case GearCategory.Weapons: return kindData.weapons;
                case GearCategory.MeleeWeapons: return kindData.meleeWeapons;
                case GearCategory.Armors: return kindData.armors;
                case GearCategory.Apparel: return kindData.apparel;
                case GearCategory.Others: return kindData.others;
                default: return new List<GearItem>();
            }
        }

        private static List<ThingDef> GetFilteredItems()
        {
            // 检查是否需要刷新缓存
            bool needRefresh = cachedFilteredItems == null ||
                              searchText != lastSearchText ||
                              selectedCategory != lastCategory ||
                              sortField != lastSortField ||
                              sortAscending != lastSortAscending ||
                              selectedModSource != lastSelectedModSource ||
                              selectedTechLevel != lastSelectedTechLevel ||
                              rangeFilter != lastRangeFilter ||
                              damageFilter != lastDamageFilter ||
                              marketValueFilter != lastMarketValueFilter;

            if (!needRefresh)
            {
                return cachedFilteredItems;
            }

            // 重新计算筛选结果
            List<ThingDef> filteredItems = new List<ThingDef>();
            switch (selectedCategory)
            {
                case GearCategory.Weapons: filteredItems = FactionGearManager.GetAllWeapons(); break;
                case GearCategory.MeleeWeapons: filteredItems = FactionGearManager.GetAllMeleeWeapons(); break;
                case GearCategory.Armors: filteredItems = FactionGearManager.GetAllArmors(); break;
                case GearCategory.Apparel: filteredItems = FactionGearManager.GetAllApparel(); break;
                case GearCategory.Others: filteredItems = FactionGearManager.GetAllOthers(); break;
            }

            filteredItems = ApplyFilters(filteredItems);
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredItems = filteredItems.Where(t => (t.label ?? t.defName).ToLower().Contains(searchText.ToLower())).ToList();
            }

            // 添加排序逻辑
            filteredItems = ApplySorting(filteredItems);

            // 更新缓存和缓存键
            cachedFilteredItems = filteredItems;
            lastSearchText = searchText;
            lastCategory = selectedCategory;
            lastSortField = sortField;
            lastSortAscending = sortAscending;
            lastSelectedModSource = selectedModSource;
            lastSelectedTechLevel = selectedTechLevel;
            lastRangeFilter = rangeFilter;
            lastDamageFilter = damageFilter;
            lastMarketValueFilter = marketValueFilter;

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
                    case GearCategory.Others:
                        if (!kindData.others.Any(g => g.thingDefName == thingDef.defName))
                        {
                            kindData.others.Add(new GearItem(thingDef.defName));
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
                        case GearCategory.Others:
                            if (!kindData.others.Any(g => g.thingDefName == thingDef.defName))
                            {
                                kindData.others.Add(new GearItem(thingDef.defName));
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
            RemoveInvalidGearItems(kindData.others);

            ClampGearWeights(kindData.weapons);
            ClampGearWeights(kindData.meleeWeapons);
            ClampGearWeights(kindData.armors);
            ClampGearWeights(kindData.apparel);
            ClampGearWeights(kindData.others);
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
            
            // 添加其他物品
            var others = FactionGearManager.GetAllOthers()
                .Where(a => a.techLevel <= techLevel)
                .OrderByDescending(a => a.BaseMarketValue)
                .Take(1)
                .ToList();
            defaultApparel.AddRange(others);
            
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
            
            // 添加其他物品
            currentApparel.AddRange(kindData.others
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
                    // 其他物品
                    if (!kindData.others.Any(g => g.thingDefName == item.defName))
                    {
                        kindData.others.Add(new GearItem(item.defName));
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
                    // 其他物品
                    if (!kindData.others.Any(g => g.thingDefName == defaultItem.defName))
                    {
                        kindData.others.Add(new GearItem(defaultItem.defName));
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
                case GearCategory.Weapons:
                    kindData.weapons.Remove(gearItem);
                    break;
                case GearCategory.MeleeWeapons:
                    kindData.meleeWeapons.Remove(gearItem);
                    break;
                case GearCategory.Armors:
                    kindData.armors.Remove(gearItem);
                    break;
                case GearCategory.Apparel:
                    kindData.apparel.Remove(gearItem);
                    break;
                case GearCategory.Others:
                    kindData.others.Remove(gearItem);
                    break;
            }
            kindData.isModified = true;
        }

        private static GearItem GetGearItemFromThingDef(ThingDef thingDef, KindGearData kindData)
        {
            switch (selectedCategory)
            {
                case GearCategory.Weapons:
                    return kindData.weapons.FirstOrDefault(g => g.thingDefName == thingDef.defName);
                case GearCategory.MeleeWeapons:
                    return kindData.meleeWeapons.FirstOrDefault(g => g.thingDefName == thingDef.defName);
                case GearCategory.Armors:
                    return kindData.armors.FirstOrDefault(g => g.thingDefName == thingDef.defName);
                case GearCategory.Apparel:
                    return kindData.apparel.FirstOrDefault(g => g.thingDefName == thingDef.defName);
                case GearCategory.Others:
                    return kindData.others.FirstOrDefault(g => g.thingDefName == thingDef.defName);
                default:
                    return null;
            }
        }

        private static void ClearAllGear(KindGearData kindData)
        {
            switch (selectedCategory)
            {
                case GearCategory.Weapons: kindData.weapons.Clear(); break;
                case GearCategory.MeleeWeapons: kindData.meleeWeapons.Clear(); break;
                case GearCategory.Armors: kindData.armors.Clear(); break;
                case GearCategory.Apparel: kindData.apparel.Clear(); break;
                case GearCategory.Others: kindData.others.Clear(); break;
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
                case GearCategory.Others: return "Others";
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
    }

    public enum GearCategory
    {
        Weapons,
        MeleeWeapons,
        Armors,
        Apparel,
        Others
    }
}