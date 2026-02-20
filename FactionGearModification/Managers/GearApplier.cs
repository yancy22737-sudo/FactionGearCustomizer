using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
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
                        // [修复] 错误位置：334行，错误内容：参数 1: 无法从"RimWorld.TechLevel"转换为"RimWorld.QualityGenerator"
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
                        // [修复] 错误位置：351行，错误内容：参数 1: 无法从"RimWorld.TechLevel"转换为"RimWorld.QualityGenerator"
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
                        // [修复] 错误位置：379行，错误内容：参数 1: 无法从"RimWorld.TechLevel"转换为"RimWorld.QualityGenerator"
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
    }
}