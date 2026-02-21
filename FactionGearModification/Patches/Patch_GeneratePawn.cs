using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
    // 恢复最标准的 Harmony 拦截格式，不再使用动态 TargetMethod
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    [HarmonyPriority(Priority.Last)] // 确保在其他 Mod 之后运行，覆盖装备
    public static class Patch_GeneratePawn
    {
        // 移除 request 前面的 ref 关键字
        public static void Postfix(Pawn __result, PawnGenerationRequest request)
        {
            if (__result != null && __result.RaceProps != null && __result.RaceProps.Humanlike)
            {
                Faction faction = request.Faction ?? __result.Faction;
                if (faction != null)
                {
                    // 添加日志以便调试
                    // Log.Message($"[FactionGearCustomizer] Checking pawn {__result.Name} of faction {faction.def.defName}");
                    GearApplier.ApplyCustomGear(__result, faction);
                }
            }
        }
    }
}