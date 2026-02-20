using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    public static class FactionGearManager
    {
        // 静态缓存 - 只在游戏启动时计算一次，大幅提升性能
        private static List<ThingDef> cachedAllWeapons = null;
        private static List<ThingDef> cachedAllMeleeWeapons = null;
        private static List<ThingDef> cachedAllHelmets = null;
        private static List<ThingDef> cachedAllArmors = null;
        private static List<ThingDef> cachedAllApparel = null;
        private static List<ThingDef> cachedAllOthers = null;
        private static bool cacheInitialized = false;
        private static readonly object cacheLock = new object();

        // 确保缓存已初始化
        private static void EnsureCacheInitialized()
        {
            if (cacheInitialized) return;
            
            lock (cacheLock)
            {
                if (cacheInitialized) return;
                
                // 一次性初始化所有缓存，避免重复遍历DefDatabase
                cachedAllWeapons = DefDatabase<ThingDef>.AllDefs
                    .Where(t => t.IsRangedWeapon).ToList();
                
                cachedAllMeleeWeapons = DefDatabase<ThingDef>.AllDefs
                    .Where(t => t.IsMeleeWeapon).ToList();
                
                cachedAllHelmets = DefDatabase<ThingDef>.AllDefs
                    .Where(t => t.IsApparel && t.apparel != null && t.apparel.layers != null && 
                           t.apparel.layers.Contains(ApparelLayerDefOf.Overhead)).ToList();
                
                cachedAllArmors = DefDatabase<ThingDef>.AllDefs
                    .Where(t => t.IsApparel && t.apparel != null && t.apparel.layers != null && 
                           (t.apparel.layers.Contains(ApparelLayerDefOf.Shell) || 
                            t.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) > 0.4f)).ToList();
                
                cachedAllApparel = DefDatabase<ThingDef>.AllDefs
                    .Where(t => t.IsApparel && t.apparel != null && t.apparel.layers != null && 
                           !t.apparel.layers.Contains(ApparelLayerDefOf.Overhead) && 
                           !t.apparel.layers.Contains(ApparelLayerDefOf.Belt) &&
                           (t.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) || 
                            t.apparel.layers.Contains(ApparelLayerDefOf.Middle)) &&
                           t.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp) < 0.4f).ToList();
                
                cachedAllOthers = DefDatabase<ThingDef>.AllDefs
                    .Where(t => t.IsApparel && t.apparel != null && t.apparel.layers != null && 
                           t.apparel.layers.Contains(ApparelLayerDefOf.Belt)).ToList();
                
                cacheInitialized = true;
            }
        }

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


        public static List<ThingDef> GetAllWeapons() { EnsureCacheInitialized(); return cachedAllWeapons; }
        public static List<ThingDef> GetAllMeleeWeapons() { EnsureCacheInitialized(); return cachedAllMeleeWeapons; }
        public static List<ThingDef> GetAllHelmets() { EnsureCacheInitialized(); return cachedAllHelmets; }
        public static List<ThingDef> GetAllArmors() { EnsureCacheInitialized(); return cachedAllArmors; }
        public static List<ThingDef> GetAllApparel() { EnsureCacheInitialized(); return cachedAllApparel; }
        public static List<ThingDef> GetAllOthers() { EnsureCacheInitialized(); return cachedAllOthers; }

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

        public static float GetWeaponAccuracy(ThingDef weaponDef)
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
        
        public static float CalculateWeaponDPS(ThingDef weaponDef)
        {
            if (weaponDef == null)
                return 0f;
                
            float damage = GetWeaponDamage(weaponDef);
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
        
        public static float GetArmorRatingSharp(ThingDef apparelDef)
        {
            if (apparelDef == null)
                return 0f;
                
            try
            {
                return apparelDef.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp);
            }
            catch
            {
                return 0f;
            }
        }
        
        public static float GetArmorRatingBlunt(ThingDef apparelDef)
        {
            if (apparelDef == null)
                return 0f;
                
            try
            {
                return apparelDef.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt);
            }
            catch
            {
                return 0f;
            }
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
}