using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace eqdseq
{
    public static class DoLovinWhenIdle_Patches
    {
        public static IEnumerable<CodeInstruction> JoyTickCheckEnd_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand is MethodInfo method && method.DeclaringType == typeof(RimWorld.Need) && method.Name == "get_CurLevel" && (float)codes[i + 1].operand == 0.9999f)
                {
                    var jumpTarget = codes[i + 3].operand;
                    codes.Insert(i + 4, new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Insert(i + 5, new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(Pawn), "mindState")));
                    codes.Insert(i + 6, new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(Pawn_MindState), "lastJobTag")));
                    codes.Insert(i + 7, new CodeInstruction(OpCodes.Ldc_I4_3));
                    codes.Insert(i + 8, new CodeInstruction(OpCodes.Beq_S, jumpTarget));
                    break;
                }
            }
            return codes;
        }
        public static IEnumerable<CodeInstruction> DLWIMF_CanReach_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            MethodInfo method = typeof(MultiFloors.StairPathFinderUtility).GetMethod("CanReachAcrossLevel");
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_0)
                {
                    codes.Insert(i + 0, new CodeInstruction(OpCodes.Ldarg_0, null));
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_1, null));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldarg_2, null));
                    codes.Insert(i + 3, new CodeInstruction(OpCodes.Ldarg_3, null));
                    codes[i + 4] = new CodeInstruction(OpCodes.Call, method);
                    break;
                }
            }
            return codes;
        }
        public static IEnumerable<CodeInstruction> DLWIRR_TryRomanceNeed_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            MethodInfo method = typeof(RomanceOnTheRim.RomanceUtility).GetMethod("TryAffectRomanceNeedLevel");
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ret)
                {
                    codes.Insert(i + 0, new CodeInstruction(OpCodes.Ldarg_0, null));
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(JobDriver), "pawn")));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldc_R4, 0.5f));
                    codes.Insert(i + 3, new CodeInstruction(OpCodes.Call, method));
                    break;
                }
            }
            return codes;
        }
        //public static IEnumerable<CodeInstruction> Ani1_Transpiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    var codes = new List<CodeInstruction>(instructions);
        //    Label targetLabel = default;
        //    for (int i = 0; i < codes.Count; i++)
        //    {
        //        if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand is string str && str == "Lovin")
        //        {
        //            targetLabel = (Label)codes[i + 2].operand;
        //            break;
        //        }
        //    }

        //    if (targetLabel == default)
        //    {
        //        Log.Error("[Do Lovin' When Idle] Yayo's Animation was not applied. Contact the Lovin' modder for a solution.");
        //        return instructions;
        //    }

        //    for (int i = 0; i < codes.Count; i++)
        //    {
        //        if (codes[i + 1].opcode == OpCodes.Ldstr && codes[i + 1].operand is string str && str == "UseHotTub" && targetLabel != default)
        //        {
        //            codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldstr, "IdleLovin"));
        //            codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), "op_Equality")));
        //            codes.Insert(i + 3, new CodeInstruction(OpCodes.Brtrue, targetLabel));
        //            codes.Insert(i + 4, new CodeInstruction(OpCodes.Ldarg_2));
        //            break;
        //        }
        //    }
        //    return codes;
        //}
        public static void AniLaying_Prefix(ref string defName)
        {
            if (defName == "IdleLovin")
            {
                defName = "Lovin";
            }
        }
        public static bool JoyTickCheckEnd_Prefix(ref bool __result, Pawn pawn, ref int delta, JoyTickFullJoyAction fullJoyAction = JoyTickFullJoyAction.EndJob, float extraJoyGainFactor = 1f, Building joySource = null)
        {
            if (pawn.mindState.lastJobTag == JobTag.Idle)
            {
                if (!pawn.IsHashIntervalTick(150, delta))
                {
                    __result = false;
                    return false;
                }
                delta = 150;
                Job curJob = pawn.CurJob;
                JoyKindDef joyKind = curJob.def.joyKind;
                if (joyKind == null)
                {
                    Log.Warning("This method can only be called for jobs with joyKind.");
                    __result = false;
                    return false;
                }
                if (joySource != null)
                {
                    if (joySource.def.building.joyKind != null && joyKind != joySource.def.building.joyKind)
                    {
                        Log.ErrorOnce("Joy source joyKind and jobDef.joyKind are not the same. building=" + joySource.ToStringSafe() + ", jobDef=" + pawn.CurJob.def.ToStringSafe(), joySource.thingIDNumber ^ 0x343FD5CC);
                    }
                    extraJoyGainFactor *= joySource.GetStatValue(StatDefOf.JoyGainFactor);
                }
                Need_Joy joy = pawn.needs.joy;
                if (joy == null)
                {
                    if (curJob.doUntilGatheringEnded)
                    {
                        if (curJob.def.joySkill != null)
                        {
                            pawn.skills.GetSkill(curJob.def.joySkill).Learn(curJob.def.joyXpPerTick * (float)delta);
                        }
                    }
                    __result = false;
                    return false;
                }
                joy.GainJoy(extraJoyGainFactor * curJob.def.joyGainRate * 0.36f / 2500f * (float)delta, joyKind);
                SkillDef joySkill = curJob.def.joySkill;
                if (joySkill != null)
                {
                    pawn.skills.GetSkill(joySkill).Learn(curJob.def.joyXpPerTick * (float)delta);
                }
                if (curJob.doUntilGatheringEnded)
                {
                    if (fullJoyAction != JoyTickFullJoyAction.None)
                    {
                        __result = true;
                        return false;
                    }
                    __result = false;
                    return false;
                }
                if (!curJob.ignoreJoyTimeAssignment && !pawn.GetTimeAssignment().allowJoy)
                {
                    pawn.jobs.curDriver.EndJobWith(JobCondition.InterruptForced);
                    __result = true;
                    return false;
                }
                //Log.Warning($"[{pawn.Label} at tick {Find.TickManager.TicksGame}] delta finish");
                __result = false;
                return false;
            }
            return true;
        }
        public static bool JoyTickCheckEnd_Prefix2(ref bool __result, Pawn pawn, ref int delta, JoyTickFullJoyAction fullJoyAction = JoyTickFullJoyAction.EndJob, float extraJoyGainFactor = 1f, Building joySource = null)
        {
            if (!pawn.IsHashIntervalTick(150, delta))
            {
                __result = false;
                return false;
            }
            delta = 150;
            return true;
        }
    }
}