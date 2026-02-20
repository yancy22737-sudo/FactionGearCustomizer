using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionGearCustomizer
{
    public static class TestScreen
    {
        public static void DrawTestScreen(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("=== Faction Gear Customizer 筛选与排序系统重构测试 ===");

            // 测试市场价值边界计算
            listing.GapLine();
            listing.Label("市场价值边界测试：");
            listing.Label($"当前分类: {FactionGearEditor.selectedCategory}");
            listing.Label($"边界值 - 最小: {FactionGearEditor.minMarketValue:F0}");
            listing.Label($"边界值 - 最大: {FactionGearEditor.maxMarketValue:F0}");
            listing.Label($"筛选范围: {FactionGearEditor.marketValueFilter.min:F0} - {FactionGearEditor.marketValueFilter.max:F0}");

            if (listing.ButtonText("重新计算边界"))
            {
                // 测试边界计算功能
                MethodInfo calculateBounds = typeof(FactionGearEditor).GetMethod("CalculateMarketValueBounds", 
                    BindingFlags.NonPublic | BindingFlags.Static);
                calculateBounds?.Invoke(null, null);
            }

            // 测试排序功能
            listing.GapLine();
            listing.Label("排序功能测试：");
            listing.Label($"当前排序字段: {FactionGearEditor.sortField}");
            listing.Label($"排序方向: {FactionGearEditor.sortAscending ? "升序" : "降序"}");

            // 测试获取武器数据
            listing.GapLine();
            listing.Label("武器属性测试：");
            
            var weapons = FactionGearManager.GetAllWeapons();
            if (weapons.Any())
            {
                var weapon = weapons.First();
                listing.Label($"测试武器: {weapon.label}");
                listing.Label($"伤害: {FactionGearManager.GetWeaponDamage(weapon):F1}");
                listing.Label($"精度: {FactionGearManager.GetWeaponAccuracy(weapon):F2}");
                listing.Label($"DPS: {FactionGearManager.CalculateWeaponDPS(weapon):F1}");
            }
            else
            {
                listing.Label("无武器数据可测试");
            }

            // 测试服装护甲
            listing.GapLine();
            listing.Label("服装护甲测试：");
            var armors = FactionGearManager.GetAllArmors();
            if (armors.Any())
            {
                var armor = armors.First();
                listing.Label($"测试护甲: {armor.label}");
                listing.Label($"锐器防护: {FactionGearManager.GetArmorRatingSharp(armor):F2}");
                listing.Label($"钝器防护: {FactionGearManager.GetArmorRatingBlunt(armor):F2}");
            }
            else
            {
                listing.Label("无护甲数据可测试");
            }

            listing.End();
        }
    }
}