using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FactionGearCustomizer
{
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
}