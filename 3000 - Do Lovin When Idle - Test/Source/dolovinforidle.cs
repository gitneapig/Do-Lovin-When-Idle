using eqdseq;
using HarmonyLib;
using MultiFloors;
using MultiFloors.Jobs;
using Prepatcher;
using RimWorld;
using RimWorld.Planet;
using RomanceOnTheRim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;

namespace eqdseq
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        public static readonly bool IsPrepatcherLoaded;
        static HarmonyPatches()
        {
            var harmony = new Harmony("eqdseq.dolovinforidle");
            IsPrepatcherLoaded = LoadedModManager.RunningModsListForReading.Any(m => m.PackageId.ToLower() == "zetrith.prepatcher")
                   || LoadedModManager.RunningModsListForReading.Any(m => m.PackageId.ToLower() == "jikulopo.prepatcher");
            TryPatch(
                harmony: harmony,
                modId: "telardo.MultiFloors",
                targetMethodString: "eqdseq.DoLovinWhenIdle_Manager:DLWIMF_CanReach",
                transpiler: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.DLWIMF_CanReach_Transpiler))
            );
            TryPatch(
                harmony: harmony,
                modId: "telardo.RomanceOnTheRim",
                targetMethodString: "eqdseq.DoLovinWhenIdle_Manager:DLWIRR_TryRomanceNeed",
                transpiler: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.DLWIRR_TryRomanceNeed_Transpiler))
            );
            TryPatch(
                harmony: harmony,
                modId: "com.yayo.yayoAni.continued",
                targetMethodString: "YayoAnimation.AnimationCore:AniLaying",
                transpiler: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.AniLaying_Transpiler))
            );
            if (DoLovinWhenIdleMod.Settings.NoStopRecreation)
            {
                if (LoadedModManager.RunningModsListForReading.Any(mod => mod.PackageIdPlayerFacing.ToLower() == "Arkymn.SlowerPawnTickRate".ToLower()))
                {
                    harmony.Patch(
                        original: AccessTools.Method(typeof(RimWorld.JoyUtility), "JoyTickCheckEnd"),
                        transpiler: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.JoyTickCheckEnd_Transpiler))
                    );
                }
                else
                {
                    if (LoadedModManager.RunningModsListForReading.Any(mod => mod.PackageIdPlayerFacing.ToLower() == "seobongzu.endlessRecreation".ToLower()))
                    {
                        harmony.Patch(
                            original: AccessTools.Method(typeof(RimWorld.JoyUtility), "JoyTickCheckEnd"),
                            prefix: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.JoyTickCheckEnd_Prefix2))
                        );
                    }
                    else
                    {
                        harmony.Patch(
                            original: AccessTools.Method(typeof(RimWorld.JoyUtility), "JoyTickCheckEnd"),
                            prefix: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.JoyTickCheckEnd_Prefix))
                        );
                    }
                }
            }
        }
        private static void TryPatch(Harmony harmony, string modId, string targetMethodString, HarmonyMethod transpiler = null)
        {
            if (!LoadedModManager.RunningModsListForReading.Any(mod => mod.PackageIdPlayerFacing.ToLower() == modId.ToLower()))
            {
                return;
            }
            try
            {
                var methodToPatch = AccessTools.Method(targetMethodString);
                if (methodToPatch == null)
                {
                    Log.Warning($"[Do Lovin' When Idle] Could not find method '{targetMethodString}' for patching. The compatibility patch will not be applied.");
                    return;
                }
                harmony.Patch(
                    original: methodToPatch,
                    transpiler: transpiler);
                Log.Message($"[Do Lovin' When Idle] Successfully applied compatibility patch for '{modId}' on method '{targetMethodString}'.");
            }
            catch (Exception ex)
            {
                Log.Error($"[Do Lovin' When Idle] An unexpected error occurred while patching '{targetMethodString}' for mod '{modId}'. Details: {ex}");
            }
        }
    }
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
        public static IEnumerable<CodeInstruction> AniLaying_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_1 && codes[i + 2].opcode == OpCodes.Stfld && codes[i + 2].operand is FieldInfo fi && fi.Name == "jobName" && fi.DeclaringType?.Name == "PawnDrawData")
                {
                    Label skipSetLabel = ilGen.DefineLabel();
                    codes[i].labels.Add(skipSetLabel);

                    var newInstructions = new List<CodeInstruction>()
                    {
                    };
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_2, null));
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldstr, "IdleLovin"));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), "op_Equality")));
                    codes.Insert(i + 3, new CodeInstruction(OpCodes.Brfalse_S, skipSetLabel));
                    codes.Insert(i + 4, new CodeInstruction(OpCodes.Ldstr, "Lovin"));
                    codes.Insert(i + 5, new CodeInstruction(OpCodes.Starg_S, 2));
                    break;
                }
            }
            return codes;
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
    public class DoLovinWhenIdleMod : Mod
    {
        public static DoLovinWhenIdleSettings Settings;
        public DoLovinWhenIdleMod(ModContentPack mod) : base(mod)
        {
            Settings = base.GetSettings<DoLovinWhenIdleSettings>();
        }
        public override string SettingsCategory()
        {
            return base.Content.Name;
        }
        public override void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(rect);
            listing_Standard.Gap(10f);
            listing_Standard.CheckboxLabeled("DoLovinWhenIdle.NoStopRecreation".Translate(),
                ref DoLovinWhenIdleMod.Settings.NoStopRecreation,
                "DoLovinWhenIdle.NoStopRecreation.Desc".Translate(),
                0f, 1f);
            listing_Standard.End();
            base.DoSettingsWindowContents(rect);
        }
    }
    public class DoLovinWhenIdleSettings : ModSettings
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.NoStopRecreation, "NoStopRecreation", true, false);
        }
        public bool NoStopRecreation = true;
    }
    public class DoLovinWhenIdle_Component : GameComponent
    {
        private Game unusedMyComponent;
        public DoLovinWhenIdle_Component(Game game)
        {
            unusedMyComponent = game;
            DLWI_DictionaryField_Manager.Reset();
        }
        public override void LoadedGame()
        {
            unusedMyComponent.components.Remove(this);
        }
        public override void ExposeData()
        {

        }
    }
    public class DLWI_DictionaryField
    {
        public int lastCheckTick = 0;
        public int laborCheckTick = 0;
        public int lastTryTick = 60000;
        public int lastTryCount = 1;
        public bool initLastTryTick = true;
    }
    public static class DLWI_DictionaryField_Manager
    {
        private static Dictionary<int, DLWI_DictionaryField> pawnData = new Dictionary<int, DLWI_DictionaryField>();
        public static void Reset()
        {
            pawnData.Clear();
        }
        public static DLWI_DictionaryField GetOrCreateData(int pawn)
        {
            if (pawnData.TryGetValue(pawn, out DLWI_DictionaryField data))
            {
                return data;
            }
            DLWI_DictionaryField newData = new DLWI_DictionaryField();
            pawnData.Add(pawn, newData);
            newData.lastTryTick = Find.TickManager.TicksGame + Rand.Range(1500, 60000);
            newData.lastCheckTick = Find.TickManager.TicksGame + Rand.Range(500, 30000);
            //if (Prefs.DevMode)
            //{
            //    newData.lastTryTick = Find.TickManager.TicksGame + 500;
            //    newData.lastCheckTick = Find.TickManager.TicksGame + 300;
            //}
            if (!ModLister.CheckBiotech("Human pregnancy"))
            {
                newData.laborCheckTick = int.MaxValue;
            }
            return newData;
        }
    }

    public static class DLWI_PrepatcherField
    {
        [PrepatcherField]
        [Prepatcher.DefaultValue(0)]
        public static extern ref int lastCheckTick(this Pawn target);

        [PrepatcherField]
        [Prepatcher.DefaultValue(0)]
        public static extern ref int laborCheckTick(this Pawn target);

        [PrepatcherField]
        [Prepatcher.DefaultValue(60000)]
        public static extern ref int lastTryTick(this Pawn target);

        [PrepatcherField]
        [Prepatcher.DefaultValue(false)]
        public static extern ref bool initLastTryTick(this Pawn target);

        [PrepatcherField]
        [Prepatcher.DefaultValue(1)]
        public static extern ref int lastTryCount(this Pawn target);

    }

    public static class DoLovinWhenIdle_Manager
    {
        public static bool DLWIMF_CanReach(Pawn p, Thing b, Map m, bool f)
        {
            //return MultiFloors.StairPathFinderUtility.CanReachAcrossLevel(p, b, m , f);
            return false;
        }
        public static Job DLWIMF_StairJob(Pawn p, Pawn pt, Thing b, Map m, bool f)
        {
            Job job2 = JobMaker.MakeJob(eJobDefOf.IdleLovin, p, b);
            ThinkResult thinkResult2 = new ThinkResult(job2, null, new JobTag?(JobTag.Misc), false);
            MultiFloors.PrepatcherFields.NextJobThinkResult(pt) = thinkResult2;
            Job job3 = MultiFloors.Jobs.CrossLevelJobFactory.MakeChangeLevelThroughStairJob(b, m, null);
            pt.jobs.StartJob(job3, JobCondition.InterruptForced); //
            Job job = JobMaker.MakeJob(eJobDefOf.IdleLovin, pt, b);
            ThinkResult thinkResult = new ThinkResult(job, null, new JobTag?(JobTag.Misc), false);
            MultiFloors.PrepatcherFields.NextJobThinkResult(p) = thinkResult;
            return MultiFloors.Jobs.CrossLevelJobFactory.MakeChangeLevelThroughStairJob(b, m, null);
        }
        public static void DLWIRR_TryRomanceNeed(Pawn pawn)
        {
            return;
        }
        public static int GetTryTick(this Pawn pawn)
        {
            if (HarmonyPatches.IsPrepatcherLoaded)
            {
                return pawn.lastTryTick();
            }
            return DLWI_DictionaryField_Manager.GetOrCreateData(pawn.thingIDNumber).lastTryTick;
        }
        public static int GetCheckTick(this Pawn pawn)
        {
            if (HarmonyPatches.IsPrepatcherLoaded)
            {
                return pawn.lastCheckTick();
            }
            return DLWI_DictionaryField_Manager.GetOrCreateData(pawn.thingIDNumber).lastCheckTick;
        }
        public static int GetTryCount(this Pawn pawn)
        {
            if (HarmonyPatches.IsPrepatcherLoaded)
            {
                return pawn.lastTryCount();
            }
            return DLWI_DictionaryField_Manager.GetOrCreateData(pawn.thingIDNumber).lastTryCount;
        }
        public static int GetLaborCheckTick(this Pawn pawn)
        {
            if (HarmonyPatches.IsPrepatcherLoaded)
            {
                return pawn.laborCheckTick();
            }
            return DLWI_DictionaryField_Manager.GetOrCreateData(pawn.thingIDNumber).laborCheckTick;
        }
        public static void SetTryTick(this Pawn pawn, int value)
        {
            if (HarmonyPatches.IsPrepatcherLoaded)
            {
                pawn.lastTryTick() = value;
            }
            else
            {
                DLWI_DictionaryField_Manager.GetOrCreateData(pawn.thingIDNumber).lastTryTick = value;
            }
        }
        public static void SetCheckTick(this Pawn pawn, int value)
        {
            if (HarmonyPatches.IsPrepatcherLoaded)
            {
                pawn.lastCheckTick() = value;
            }
            else
            {
                DLWI_DictionaryField_Manager.GetOrCreateData(pawn.thingIDNumber).lastCheckTick = value;
            }
        }
        public static void SetTryCount(this Pawn pawn, int value)
        {
            if (HarmonyPatches.IsPrepatcherLoaded)
            {
                pawn.lastTryCount() = value;
            }
            else
            {
                DLWI_DictionaryField_Manager.GetOrCreateData(pawn.thingIDNumber).lastTryCount = value;
            }
        }
        public static void SetLaborCheckTick(this Pawn pawn, int value)
        {
            if (HarmonyPatches.IsPrepatcherLoaded)
            {
                pawn.laborCheckTick() = value;
            }
            else
            {
                DLWI_DictionaryField_Manager.GetOrCreateData(pawn.thingIDNumber).laborCheckTick = value;
            }
        }
        public static bool GetInitLastTryTick(this Pawn pawn)
        {
            if (HarmonyPatches.IsPrepatcherLoaded)
            {
                return pawn.initLastTryTick();
            }
            return true;
        }
        public static void SetInitLastTryTick(this Pawn pawn, bool value)
        {
            if (HarmonyPatches.IsPrepatcherLoaded)
            {
                pawn.initLastTryTick() = value;
            }
        }
    }
    public class JobGiver_IdleLovin : ThinkNode_JobGiver
    {
        //public JobGiver_IdleLovin() { }
        //public override float GetPriority(Pawn pawn)
        //{
        //    if (pawn.lastTryTick() > Find.TickManager.TicksGame)
        //    {
        //        return 0f;
        //    }
        //    if (pawn.needs == null)
        //    {
        //        pawn.lastTryTick() = int.MaxValue;
        //        pawn.lastCheckTick() = int.MaxValue;
        //        Log.Error($"[DoLovinWhenIdle]{pawn.LabelShortCap} currently has an error or is incompatible. Please reload the game or remove this mod. DoLovinWhenIdle will remain disabled until the game is reloaded.");
        //        return 0f;
        //    }
        //    if (pawn.needs.joy != null && pawn.needs.joy.CurLevel < 0.5f)
        //    {
        //        pawn.lastTryTick() = Find.TickManager.TicksGame + 3000;
        //        return 0f;
        //    }
        //    return 9f;
        //}
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.GetTryTick() > Find.TickManager.TicksGame)
            {
                return null;
            }
            int ticksGame = Find.TickManager.TicksGame;
            bool flagm = false;
            if (!pawn.GetInitLastTryTick())
            {
                pawn.SetInitLastTryTick(true);
                int numr = ((((ticksGame % 1000) + (pawn.thingIDNumber % 1000)) % 1000) + Rand.Range(500, 5000));
                pawn.SetCheckTick(ticksGame + numr);
                pawn.SetTryTick(ticksGame + (numr * 2));
                if (!ModLister.CheckBiotech("Human pregnancy"))
                {
                    pawn.SetLaborCheckTick(int.MaxValue);
                    return null;
                }
                return null;
            }
            if (pawn.GetCheckTick() < ticksGame)
            {
                pawn.SetCheckTick(ticksGame + 30);
            }
            Building_Bed ownedBed = pawn.ownership?.OwnedBed;
            if (ownedBed == null)
            {
                pawn.SetTryTick(ticksGame + 15000 + (ticksGame % 2000));
                return null;
            }
            if (ownedBed.OwnersForReading.Count == 1)
            {
                pawn.SetTryTick(ticksGame + 30000 + (ticksGame % 2000));
                return null;
            }
            if (!ownedBed.Spawned)
            {
                pawn.SetTryTick(ticksGame + 15000 + (ticksGame % 2000));
                return null;
            }
            int canLovinTick = pawn.mindState.canLovinTick;
            if (canLovinTick > ticksGame)
            {
                pawn.SetTryTick(canLovinTick + 30);
                pawn.SetCheckTick(Math.Max(pawn.GetCheckTick(), canLovinTick));
                return null;
            }
            Map ownedBedMap = ownedBed.Map;
            if (ownedBedMap != pawn.Map)
            {
                flagm = DoLovinWhenIdle_Manager.DLWIMF_CanReach(pawn, ownedBed, ownedBedMap, false);
                if (!flagm)
                {
                    pawn.SetTryTick(ticksGame + (Rand.Range(1234, 5678) * pawn.GetTryCount()));
                    if (pawn.GetTryCount() > 5)
                    {
                        pawn.SetTryCount(1);
                    }
                    else
                    {
                        pawn.SetTryCount(pawn.GetTryCount() + 1);
                    }
                    return null;
                }
                if (pawn.jobs?.curJob != null)
                {
                    pawn.SetCheckTick(ticksGame + 1900);
                    pawn.SetTryTick(ticksGame + 3500);
                    return null;
                }
            }
            IntVec3 ownedBedPos = ownedBed.Position;
            foreach (IntVec3 item in ownedBed.OccupiedRect())
            {
                if (item.ContainsStaticFire(ownedBedMap))
                {
                    pawn.SetTryTick(ticksGame + 2900);
                    return null;
                }
            }
            if (!pawn.SafeTemperatureAtCell(ownedBedPos, ownedBedMap))
            {
                pawn.SetTryTick(ticksGame + 3900);
                return null;
            }
            if (ownedBedPos.GetVacuum(ownedBedMap) >= 0.5f && pawn.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
            {
                pawn.SetTryTick(ticksGame + 10000 + (ticksGame % 2000));
                return null;
            }
            Pawn_NeedsTracker needs = pawn.needs;
            if (needs == null)
            {
                pawn.SetTryTick(int.MaxValue);
                pawn.SetCheckTick(int.MaxValue);
                Log.Error($"[DoLovinWhenIdle]{pawn.LabelShortCap} currently has an error or is incompatible. Please reload the game or remove this mod. DoLovinWhenIdle will remain disabled until the game is reloaded.");
                return null;
            }
            if (needs.mood != null && needs.mood.CurLevel < 0.44f)
            {
                if (needs.rest != null && needs.rest.CurLevel < 0.3f)
                {
                    pawn.SetTryTick(ticksGame + (int)((1f - needs.rest.CurLevel) * 2900));
                    return null;
                }
                pawn.SetTryTick(ticksGame + (int)((1f - needs.mood.CurLevel) * 4400));
                return null;
            }
            if (needs.food != null && needs.food.CurLevel < 0.3f)
            {
                pawn.SetTryTick(ticksGame + (int)((1f - needs.food.CurLevel) * 2100));
                return null;
            }
            if (needs.joy != null && needs.joy.CurLevel < 0.43f)
            {
                pawn.SetTryTick(ticksGame + (int)((1f - needs.joy.CurLevel) * 2100));
                return null;
            }
            HediffSet hediffSet = pawn.health?.hediffSet;
            if (hediffSet == null)
            {
                pawn.SetTryTick(int.MaxValue);
                pawn.SetCheckTick(int.MaxValue);
                Log.Error($"[DoLovinWhenIdle]{pawn.LabelShortCap} currently has an error or is incompatible. Please reload the game or remove this mod. DoLovinWhenIdle will remain disabled until the game is reloaded.");
                return null;
            }
            if (hediffSet.BleedRateTotal > 0f)
            {
                pawn.SetTryTick(ticksGame + 1500);
                return null;
            }
            if (pawn.GetLaborCheckTick() < ticksGame)
            {
                //if (!pawn.GetInitLastTryTick())
                //{
                //    pawn.SetInitLastTryTick(true);
                //    int numr = ((((ticksGame % 1000) + (pawn.thingIDNumber % 1000)) % 1000) + Rand.Range(500, 50000));
                    //if (pawn.GetTryTick() == 60000)
                    //{
                    //    pawn.SetCheckTick(ticksGame + numr);
                    //    pawn.SetTryTick(ticksGame + numr);
                    //    return null;
                    //}
                //}
                if (hediffSet.InLabor(true))
                {
                    pawn.SetLaborCheckTick(ticksGame + 40000);
                    pawn.SetCheckTick(ticksGame + 60000);
                    pawn.SetTryTick(ticksGame + 61000);
                    return null;
                }
                int num = 360000;
                Hediff_Pregnant pregnancyHediff = hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnantHuman) as Hediff_Pregnant;
                if (pregnancyHediff != null)
                {
                    switch (pregnancyHediff.CurStageIndex)
                    {
                        case 0:
                            break;
                        case 1:
                            num = 180000;
                            break;
                        case 2:
                            float severity = pregnancyHediff.Severity;
                            if (severity > 0.95f)
                            {
                                pawn.SetLaborCheckTick(ticksGame + 40000);
                                pawn.SetCheckTick(ticksGame + 60000);
                                pawn.SetTryTick(ticksGame + 61000);
                                return null;
                            }
                            num = 50000;
                            break;
                    }
                }
                pawn.SetLaborCheckTick(ticksGame + num);
            }
            IntVec3 sleepingSpot = RestUtility.GetBedSleepingSlotPosFor(pawn, ownedBed);
            int sharedBedNonSpouse = 0;
            int lastCheckTickTemp = pawn.GetTryTick();
            bool pawnCanReachCheck = flagm;
            foreach (Pawn pawn2 in ownedBed.OwnersForReading)
            {
                if (pawn2 == pawn)
                {
                    continue;
                }
                if (!LovePartnerRelationUtility.LovePartnerRelationExists(pawn, pawn2))
                {
                    sharedBedNonSpouse += 5000;
                    continue;
                }
                if (pawn2.GetCheckTick() > ticksGame)
                {
                    lastCheckTickTemp = Math.Min(pawn2.GetCheckTick(), ticksGame + 6000);
                    continue;
                }
                canLovinTick = pawn2.mindState.canLovinTick;
                if (canLovinTick > ticksGame)
                {
                    //pawn2.lastCheckTick() = canLovinTick + 30;
                    lastCheckTickTemp = Math.Min(pawn2.GetCheckTick() + 10000, canLovinTick);
                    continue;
                }
                if (!pawn.CanReserve(pawn2, 1, -1, null, false) || !pawn2.CanReserve(pawn, 1, -1, null, false))
                {
                    //pawn2.lastCheckTick() = ticksGame + 2500;
                    lastCheckTickTemp = ticksGame + 2500;
                    continue;
                }
                needs = pawn2.needs;
                if (needs == null)
                {
                    //pawn2.lastCheckTick() = int.MaxValue;
                    continue;
                }
                if (needs.mood != null && needs.mood.CurLevel < 0.42f)
                {
                    if (needs.rest != null && needs.rest.CurLevel < 0.27f)
                    {
                        //pawn2.lastCheckTick() = ticksGame + (int)((1f - needs.rest.CurLevel) * 2700);
                        lastCheckTickTemp = /*pawn2.lastCheckTick()*/ ticksGame + (int)((1f - needs.rest.CurLevel) * 2700);
                        continue;
                    }
                    //pawn2.lastCheckTick() = ticksGame + (int)((1f - needs.mood.CurLevel) * 4100);
                    lastCheckTickTemp = /*pawn2.lastCheckTick()*/ ticksGame + (int)((1f - needs.mood.CurLevel) * 4100);
                    continue;
                }
                if (needs.food != null && needs.food.CurLevel < 0.26f)
                {
                    //pawn2.lastCheckTick() = ticksGame + (int)((1f - needs.food.CurLevel) * 2200);
                    lastCheckTickTemp = /*pawn2.lastCheckTick()*/ ticksGame + (int)((1f - needs.food.CurLevel) * 2200);
                    continue;
                }
                if (needs.joy != null && needs.joy.CurLevel < 0.41f)
                {
                    //pawn2.lastCheckTick() = ticksGame + (int)((1f - needs.joy.CurLevel) * 2100);
                    lastCheckTickTemp = /*pawn2.lastCheckTick() */ ticksGame + (int)((1f - needs.joy.CurLevel) * 2100);
                    continue;
                }
                hediffSet = pawn2.health?.hediffSet;
                if (hediffSet == null)
                {
                    //pawn2.lastCheckTick() = int.MaxValue;
                    continue;
                }
                if (hediffSet.BleedRateTotal > 0f)
                {
                    //pawn2.lastCheckTick() = ticksGame + 2500;
                    lastCheckTickTemp = /*pawn2.lastCheckTick()*/ ticksGame + 2500;
                    continue;
                }
                if (!pawn2.SafeTemperatureAtCell(ownedBedPos, ownedBedMap))
                {
                    //pawn2.lastCheckTick() = ticksGame + 3900;
                    lastCheckTickTemp = /*pawn2.lastCheckTick()*/ ticksGame + 3900;
                    continue;
                }
                if (ownedBedPos.GetVacuum(ownedBedMap) >= 0.5f && pawn2.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                {
                    //pawn2.lastCheckTick() = ticksGame + 10000 + (ticksGame % 2000);
                    lastCheckTickTemp = /*pawn2.lastCheckTick()*/ ticksGame + 10000 + (ticksGame % 2000);
                    continue;
                }
                if (pawn2.GetLaborCheckTick() < ticksGame)
                {
                    //pawn2.laborCheckTick() = ticksGame;
                    if (hediffSet.InLabor(true))
                    {
                        //pawn2.laborCheckTick() += 40000;
                        //pawn2.lastCheckTick() = ticksGame + 60000;
                        //pawn2.lastTryTick() = ticksGame + 61000;
                        //lastCheckTickTemp = pawn2.lastCheckTick();
                        lastCheckTickTemp = ticksGame + 60000;
                        continue;
                    }
                    int nums = 360000;
                    Hediff_Pregnant pregnancyHediff2 = hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnantHuman) as Hediff_Pregnant;
                    if (pregnancyHediff2 != null)
                    {
                        switch (pregnancyHediff2.CurStageIndex)
                        {
                            case 0:
                                break;
                            case 1:
                                nums = 180000;
                                break;
                            case 2:
                                if (pregnancyHediff2.Severity > 0.95f)
                                {
                                    //pawn2.laborCheckTick() += 40000;
                                    //pawn2.lastCheckTick() = ticksGame + 60000;
                                    //pawn2.lastTryTick() = ticksGame + 61000;
                                    //lastCheckTickTemp = pawn2.lastCheckTick();
                                    lastCheckTickTemp = ticksGame + 60000;
                                    continue;
                                }
                                nums = 50000;
                                break;
                        }
                    }
                    //pawn2.laborCheckTick() += nums;
                    lastCheckTickTemp = ticksGame + nums;
                }
                IntVec3 sleepingSpot2 = RestUtility.GetBedSleepingSlotPosFor(pawn2, ownedBed);
                Pawn_JobTracker jobs2 = pawn2.jobs;
                JobDef pawn2curJobdef = jobs2.curJob?.def;
                if (pawn2curJobdef == null)
                {
                    //pawn2.lastCheckTick() = ticksGame + Rand.Range(200, 750);
                    lastCheckTickTemp = /*pawn2.lastCheckTick()*/ ticksGame + Rand.Range(200, 750);
                    continue;
                }
                if (flagm)
                {
                    if (!DoLovinWhenIdle_Manager.DLWIMF_CanReach(pawn2, ownedBed, ownedBedMap, false))
                    {
                        //pawn2.lastCheckTick() = ticksGame + 2500;
                        //pawn2.lastTryTick() = ticksGame + 5000;
                        lastCheckTickTemp = /*pawn2.lastCheckTick()*/ ticksGame + 2500;
                        continue;
                    }
                }
                if (jobs2.curDriver == null)
                {
                    //pawn2.lastCheckTick() = ticksGame + 999;
                    lastCheckTickTemp = /*pawn2.lastCheckTick()*/ ticksGame + 999;
                    continue;
                }
                JobTag lastJobTag = pawn2.mindState.lastJobTag;
                if (!flagm && jobs2.curDriver.asleep && lastJobTag == JobTag.SatisfyingNeeds && sleepingSpot2 == pawn2.Position && jobs2.posture == PawnPosture.LayingInBed)
                {
                    if (!pawn2.health.capacities.CanBeAwake)
                    {
                        //pawn2.lastCheckTick() = ticksGame + 30000;
                        lastCheckTickTemp = /*pawn2.lastCheckTick()*/ ticksGame + 30000;
                        continue;
                    }
                    if (!pawnCanReachCheck)
                    {
                        if (!pawn.CanReach(sleepingSpot, PathEndMode.OnCell, Danger.Deadly))
                        {
                            pawn.SetCheckTick(ticksGame + 2700);
                            pawn.SetTryTick(ticksGame + 6000);
                            return null;
                        }
                        if (pawn.jobs?.curJob != null)
                        {
                            pawn.SetCheckTick(ticksGame + 1900);
                            pawn.SetTryTick(ticksGame + 3500);
                            return null;
                        }
                        pawnCanReachCheck = true;
                    }
                    //pawn2.lastCheckTick() = ticksGame + 300;
                    //pawn2.lastTryTick() = ticksGame + 900;
                    pawn.SetCheckTick(ticksGame + 8100);
                    pawn.SetTryTick(ticksGame + 8200);
                    jobs2.StartJob(JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn, ownedBed), JobCondition.InterruptForced);
                    //Log.Warning($"[{pawn.Label} at tick {Find.TickManager.TicksGame}] b1 start");
                    return JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn2, ownedBed);
                }
                if (lastJobTag == JobTag.Idle)
                {
                    if (pawn2curJobdef == JobDefOf.GotoWander || pawn2curJobdef == JobDefOf.Wait_Wander)
                    {
                        if (!flagm && !pawn2.CanReach(sleepingSpot2, PathEndMode.OnCell, Danger.Deadly))
                        {
                            //pawn2.lastCheckTick() = ticksGame + 2300;
                            //pawn2.lastTryTick() = ticksGame + 2800;
                            lastCheckTickTemp = /*pawn2.lastCheckTick()*/ ticksGame + 2300;
                            continue;
                        }
                        if (!pawnCanReachCheck)
                        {
                            if (!pawn.CanReach(sleepingSpot, PathEndMode.OnCell, Danger.Deadly))
                            {
                                pawn.SetCheckTick(ticksGame + 2700);
                                pawn.SetTryTick(ticksGame + 6000);
                                return null;
                            }
                            if (pawn.jobs?.curJob != null)
                            {
                                pawn.SetCheckTick(ticksGame + 1900);
                                pawn.SetTryTick(ticksGame + 3500);
                                return null;
                            }
                            pawnCanReachCheck = true;
                        }
                        //pawn2.lastCheckTick() = ticksGame + 300;
                        //pawn2.lastTryTick() = ticksGame + 900;
                        pawn.SetCheckTick(ticksGame + 8100);
                        pawn.SetTryTick(ticksGame + 8200);
                        if (flagm)
                        {
                            return DoLovinWhenIdle_Manager.DLWIMF_StairJob(pawn, pawn2, ownedBed, ownedBedMap, false);
                        }
                        jobs2.StartJob(JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn, ownedBed), JobCondition.InterruptForced);
                        //Log.Warning($"[{pawn.Label} at tick {Find.TickManager.TicksGame}] b2 start");
                        return JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn2, ownedBed);
                    }
                }
                if (((lastJobTag == JobTag.SatisfyingNeeds && pawn2curJobdef.joyKind != null) || (pawn2curJobdef != eJobDefOf.IdleLovin && lastJobTag == JobTag.Idle)))
                {
                    int num2 = pawn2.jobs.curDriver.ticksLeftThisToil;
                    if (num2 > 0 && num2 < 1350 && jobs2.curJob.count == -1)
                    {
                        if (!flagm && !pawn2.CanReach(sleepingSpot2, PathEndMode.OnCell, Danger.Deadly))
                        {
                            //pawn2.lastCheckTick() = ticksGame + 2300;
                            //pawn2.lastTryTick() = ticksGame + 2800;
                            lastCheckTickTemp = /*pawn2.lastCheckTick()*/ ticksGame + 2300;
                            continue;
                        }
                        if (!pawnCanReachCheck)
                        {
                            if (!pawn.CanReach(sleepingSpot, PathEndMode.OnCell, Danger.Deadly))
                            {
                                pawn.SetCheckTick(ticksGame + 2700);
                                pawn.SetTryTick(ticksGame + 6000);
                                return null;
                            }
                            if (pawn.jobs?.curJob != null)
                            {
                                pawn.SetCheckTick(ticksGame + 1900);
                                pawn.SetTryTick(ticksGame + 3500);
                                return null;
                            }
                            pawnCanReachCheck = true;
                        }
                        if (pawn2curJobdef.joyKind != JoyKindDefOf.Meditative)
                        {
                            JoyUtility.TryGainRecRoomThought(pawn2);
                        }
                        //pawn2.lastCheckTick() = ticksGame + 300;
                        //pawn2.lastTryTick() = ticksGame + 900;
                        pawn.SetCheckTick(ticksGame + 8100);
                        pawn.SetTryTick(ticksGame + 8200);
                        if (flagm)
                        {
                            return DoLovinWhenIdle_Manager.DLWIMF_StairJob(pawn, pawn2, ownedBed, ownedBedMap, false);
                        }
                        jobs2.StartJob(JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn, ownedBed), JobCondition.InterruptForced);
                        //Log.Warning($"[{pawn.Label} at tick {Find.TickManager.TicksGame}] b3 start");
                        return JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn2, ownedBed);
                    }
                }
            }
            if (pawn.GetTryCount() > 5)
            {
                pawn.SetTryCount(1);
            }
            else
            {
                pawn.SetTryCount(pawn.GetTryCount() + 1);
            }
            if (sharedBedNonSpouse == 0)
            {
                pawn.SetTryTick(lastCheckTickTemp + (pawn.GetTryCount() * Rand.Range(200, 650)));
                return null;
            }
            pawn.SetTryTick(ticksGame + (pawn.GetTryCount() * Rand.Range(2000, 6500)) + sharedBedNonSpouse);
            return null;
        }
    }
    public class JobDriver_IdleLovin : JobDriver
    {
        private TargetIndex PartnerInd = TargetIndex.A;
        private TargetIndex BedInd = TargetIndex.B;
        private Pawn Partner => (Pawn)(Thing)job.GetTarget(PartnerInd);
        private Building_Bed Bed => (Building_Bed)(Thing)job.GetTarget(BedInd);
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Map != null && pawn.Map != Bed.Map)
            {
                return pawn.Reserve(Bed, job, Bed.SleepingSlotsCount, 0, null, errorOnFailed);
            }
            if (pawn.Reserve(Partner, job, 1, -1, null, errorOnFailed))
            {
                return pawn.Reserve(Bed, job, Bed.SleepingSlotsCount, 0, null, errorOnFailed);
            }
            return false;
        }
        public override bool CanBeginNowWhileLyingDown()
        {
            return JobInBedUtility.InBedOrRestSpotNow(pawn, job.GetTarget(BedInd));
        }
        public override string GetReport()
        {
            return JobDefOf.Lovin.reportString;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil gotoBed = ToilMaker.MakeToil("GotoBed1");
            gotoBed.initAction = delegate
            {
                Building_Bed bed = this.Bed;
                Pawn actor = this.pawn;
                if (actor.GetTryTick() < Find.TickManager.TicksGame)
                {
                    actor.SetTryTick(Find.TickManager.TicksGame + 8200);
                    actor.SetCheckTick(Find.TickManager.TicksGame + 8100);
                }
                if (bed == null || !bed.Spawned || bed.Map == null || bed.Map != actor.Map)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                Pawn partner = this.Partner;
                if (partner == null || !partner.Spawned)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                actor.SetTryCount(1);
                ticksLeftThisToil = 8000;
                IntVec3 bedSleepingSlotPosFor = RestUtility.GetBedSleepingSlotPosFor(actor, bed);
                if (bedSleepingSlotPosFor == actor.Position)
                {
                    this.KeepLyingDown(BedInd);
                    actor.jobs.posture = PawnPosture.LayingInBed;
                    actor.jobs.curDriver.ReadyForNextToil();
                }
                else
                {
                    actor.pather.StartPath(bedSleepingSlotPosFor, PathEndMode.OnCell);
                }
            };
            gotoBed.AddPreTickIntervalAction(delegate (int delta)
            {
                if (pawn.IsHashIntervalTick(100, delta))
                {
                    Pawn actor = this.pawn;
                    if (!actor.GetInitLastTryTick())
                    {
                        actor.SetTryTick(Find.TickManager.TicksGame + Rand.Range(1500, 3000));
                        actor.SetInitLastTryTick(true);
                    }
                    Building_Bed bed = this.Bed;
                    if (bed == null || !bed.Spawned)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    Map map = actor.Map;
                    if (map != bed.Map)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    Pawn partner = this.Partner;
                    if (partner == null || !partner.Spawned || !partner.health.capacities.CanBeAwake)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    Job job2 = partner.CurJob;
                    if (job2 == null)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (job2.def != eJobDefOf.IdleLovin && bed != job2.targetC)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (!bed.IsOwner(actor))
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    foreach (IntVec3 item in bed.OccupiedRect())
                    {
                        if (item.ContainsStaticFire(map))
                        {
                            actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    if (bed.Position.GetVacuum(map) >= 0.5f && actor.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (ticksLeftThisToil < 0)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    IntVec3 bedSleepingSlotPosFor = RestUtility.GetBedSleepingSlotPosFor(actor, bed);
                    if (bedSleepingSlotPosFor == actor.Position)
                    {
                        actor.jobs.posture = PawnPosture.LayingInBed;
                        actor.jobs.curDriver.ReadyForNextToil();
                    }
                }
            });
            gotoBed.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return gotoBed;
            Toil toil1 = ToilMaker.MakeToil("LayDown1");
            toil1.initAction = delegate
            {
                Building_Bed bed = this.Bed;
                Pawn actor = this.pawn;
                if (bed == null || !bed.Spawned || bed.Map == null || bed.Map != actor.Map)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                Pawn partner = this.Partner;
                if (partner == null || !partner.Spawned || !partner.health.capacities.CanBeAwake)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                Pawn_PathFollower pather = actor.pather;
                pather?.StopDead();
                if (!bed.OccupiedRect().Contains(actor.Position))
                {
                    string text = "Can't start LayDown toil because pawn is not in the bed. pawn=";
                    Log.Error(text + (actor?.ToString()));
                    actor.jobs.EndCurrentJob(JobCondition.Errored, true, true);
                    return;
                }
                actor.jobs.posture = PawnPosture.LayingInBed;
                PortraitsCache.SetDirty(actor);
                ticksLeftThisToil = (int)(2500f * Mathf.Clamp(Rand.Range(0.6f, 1.1f), 0.1f, 2f));
            };
            toil1.AddPreTickIntervalAction(delegate (int delta)
            {
                Pawn actor = this.pawn;
                Pawn partner = this.Partner;
                if (partner == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                if (partner.jobs.posture == PawnPosture.LayingInBed)
                {
                    if (!actor.GetInitLastTryTick())
                    {
                        actor.SetTryTick(Find.TickManager.TicksGame + Rand.Range(1500, 3000));
                        actor.SetInitLastTryTick(true);
                    }
                    if (!partner.Spawned || !partner.health.capacities.CanBeAwake)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    Building_Bed bed = this.Bed;
                    if (bed == null || !bed.Spawned)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    Map map = actor.Map;
                    if (map != bed.Map || map != partner.Map)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    JobDef job2def = partner.CurJobDef;
                    if (job2def != eJobDefOf.IdleLovin)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    IntVec3 pos = actor.Position;
                    if (bed.IsOwner(actor, out var assignedSleepingSlot))
                    {
                        if (pos != bed.GetSleepingSlotPos(assignedSleepingSlot.Value))
                        {
                            actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    else
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    foreach (IntVec3 item in bed.OccupiedRect())
                    {
                        if (item.ContainsStaticFire(map))
                        {
                            actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    if (pos.GetVacuum(map) >= 0.5f && actor.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (!LovePartnerRelationUtility.LovePartnerRelationExists(actor, partner))
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    ReadyForNextToil();
                }
                else if (actor.IsHashIntervalTick(100, delta))
                {
                    if (!actor.GetInitLastTryTick())
                    {
                        actor.SetTryTick(Find.TickManager.TicksGame + Rand.Range(1500, 3000));
                        actor.SetInitLastTryTick(true);
                    }
                    if (!partner.Spawned || !partner.health.capacities.CanBeAwake)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    Building_Bed bed = this.Bed;
                    if (bed == null || !bed.Spawned)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    Map map = actor.Map;
                    if (map != bed.Map)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    Job job2 = partner.CurJob;
                    if (job2 == null)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (job2.def != eJobDefOf.IdleLovin && bed != job2.targetC)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (ticksLeftThisToil < 0 || !LovePartnerRelationUtility.LovePartnerRelationExists(actor, partner))
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    IntVec3 pos = actor.Position;
                    if (bed.IsOwner(pawn, out var assignedSleepingSlot))
                    {
                        if (pos != bed.GetSleepingSlotPos(assignedSleepingSlot.Value))
                        {
                            actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    else
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    foreach (IntVec3 item in bed.OccupiedRect())
                    {
                        if (item.ContainsStaticFire(map))
                        {
                            actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    if (!actor.SafeTemperatureAtCell(pos, map))
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (pos.GetVacuum(map) >= 0.5f && actor.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    FleckMaker.ThrowMetaIcon(pos, map, FleckDefOf.Meditating);
                }
            });
            toil1.defaultCompleteMode = ToilCompleteMode.Never;
            yield return toil1;
            Toil toil2 = ToilMaker.MakeToil("LayDown2");
            toil2.initAction = delegate
            {
                Building_Bed bed = this.Bed;
                Pawn actor = this.pawn;
                if (bed == null || !bed.Spawned)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                Map map = actor.Map;
                if (map != bed.Map)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                Pawn partner = this.Partner;
                if (partner == null || !partner.Spawned || map != partner.Map)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                Pawn_PathFollower pather = actor.pather;
                pather?.StopDead();
                if (!bed.OccupiedRect().Contains(actor.Position))
                {
                    string text = "Can't start LayDown toil because pawn is not in the bed. pawn=";
                    Log.Error(text + (actor?.ToString()));
                    actor.jobs.EndCurrentJob(JobCondition.Errored, true, true);
                    return;
                }
                actor.jobs.posture = PawnPosture.LayingInBed;
                //actor.layDownState() = true;
                PortraitsCache.SetDirty(actor);
                ticksLeftThisToil = (int)(2500f * Mathf.Clamp(Rand.Range(0.6f, 1.1f), 0.1f, 2f));
                if (actor.thingIDNumber < partner.thingIDNumber)
                {
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.InitiatedLovin, actor.Named(HistoryEventArgsNames.Doer)));
                    if (InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween(actor, partner) && InteractionWorker_RomanceAttempt.TryCreatePsychicBondBetween(actor, partner) && (PawnUtility.ShouldSendNotificationAbout(actor) || PawnUtility.ShouldSendNotificationAbout(partner)))
                    {
                        Find.LetterStack.ReceiveLetter("LetterPsychicBondCreatedLovinLabel".Translate(), "LetterPsychicBondCreatedLovinText".Translate(actor.Named("BONDPAWN"), partner.Named("OTHERPAWN")), LetterDefOf.PositiveEvent, new LookTargets(actor, partner));
                    }
                }
            };
            toil2.AddPreTickIntervalAction(delegate (int delta)
            {
                if (ticksLeftThisToil <= 0)
                {
                    ReadyForNextToil();
                }
                else if (pawn.IsHashIntervalTick(100, delta))
                {
                    Pawn actor = this.pawn;
                    if (!actor.GetInitLastTryTick())
                    {
                        actor.SetTryTick(Find.TickManager.TicksGame + Rand.Range(1500, 3000));
                        actor.SetInitLastTryTick(true);
                    }
                    Building_Bed bed = this.Bed;
                    if (bed == null || !bed.Spawned)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    Map map = actor.Map;
                    if (map != bed.Map)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    Pawn partner = this.Partner;
                    if (partner == null || !partner.Spawned || map != partner.Map || !partner.health.capacities.CanBeAwake)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    Job job2 = partner.CurJob;
                    if (job2 == null || job2.def != eJobDefOf.IdleLovin)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    IntVec3 pos = actor.Position;
                    if (bed.IsOwner(actor, out var assignedSleepingSlot))
                    {
                        if (pos != bed.GetSleepingSlotPos(assignedSleepingSlot.Value))
                        {
                            actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    else
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    foreach (IntVec3 item in bed.OccupiedRect())
                    {
                        if (item.ContainsStaticFire(map))
                        {
                            actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    if (!actor.SafeTemperatureAtCell(pos, map))
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (pos.GetVacuum(map) >= 0.5f && actor.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                    {
                        actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    FleckMaker.ThrowMetaIcon(pos, map, FleckDefOf.Heart);
                }
            });
            toil2.AddFinishAction(delegate
            {
                Pawn actor = base.pawn;
                Pawn actor2 = Partner;
                HediffSet hediffSet = actor.health?.hediffSet;
                HediffSet hediffSet2 = actor2?.health?.hediffSet;
                int ticksGame = Find.TickManager.TicksGame;
                if (hediffSet == null || hediffSet2 == null)
                {
                    actor.SetTryTick(ticksGame + Rand.Range(2500, 5000));
                    actor.SetCheckTick(ticksGame + 2400);
                    return;
                }
                DoLovinWhenIdle_Manager.DLWIRR_TryRomanceNeed(actor);
                Thought_Memory thought_Memory = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDefOf.GotSomeLovin);
                //int numm = 60000;
                if ((hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)) || (hediffSet2.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)))
                {
                    thought_Memory.moodPowerFactor = 1.5f;
                    //numm = 120000;
                }
                //thought_Memory.durationTicksOverride = numm;
                actor.needs?.mood?.thoughts.memories.TryGainMemory(thought_Memory, actor2);
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.GotLovin, actor.Named(HistoryEventArgsNames.Doer)));
                HistoryEventDef def = (actor.relations.DirectRelationExists(PawnRelationDefOf.Spouse, actor2) ? HistoryEventDefOf.GotLovin_Spouse : HistoryEventDefOf.GotLovin_NonSpouse);
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(def, actor.Named(HistoryEventArgsNames.Doer)));
                float nums = IdleLovinUtility.GenerateRandomMinTicksToNextCanLovin(actor);
                int canLovinTick = ticksGame + (int)(nums * 2500f);
                actor.mindState.canLovinTick = canLovinTick;
                actor.SetTryTick(ticksGame + IdleLovinUtility.GenerateRandomMinTicksToNextIdleLovin(actor, actor2, nums));
                actor.SetCheckTick(Rand.Range(canLovinTick, actor.GetTryTick()));
                if (ModsConfig.BiotechActive)
                {
                    Pawn pawn = ((actor.gender == Gender.Male) ? actor : ((actor2.gender == Gender.Male) ? actor2 : null));
                    Pawn pawn2 = ((actor.gender == Gender.Female) ? actor : ((actor2.gender == Gender.Female) ? actor2 : null));
                    if (pawn != null && pawn2 != null && Rand.Chance(0.005f * PregnancyUtility.PregnancyChanceForPartners(pawn2, pawn)))
                    {
                        GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(pawn, pawn2, out bool success);
                        if (success)
                        {
                            Hediff_Pregnant hediff_Pregnant = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, pawn2);
                            hediff_Pregnant.SetParents(null, pawn, inheritedGeneSet);
                            pawn2.health.AddHediff(hediff_Pregnant);
                        }
                        else if (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(pawn2))
                        {
                            Messages.Message("MessagePregnancyFailed".Translate(pawn.Named("FATHER"), pawn2.Named("MOTHER")) + ": " + "CombinedGenesExceedMetabolismLimits".Translate(), new LookTargets(pawn, pawn2), MessageTypeDefOf.NegativeEvent);
                        }
                    }
                }
            });
            toil2.socialMode = RandomSocialMode.Off;
            toil2.defaultCompleteMode = ToilCompleteMode.Never;
            yield return toil2;
        }
    }
    public class IdleLovinUtility
    {
        public static readonly SimpleCurve IdleLovinIntervalHoursFromAgeCurve = new SimpleCurve
    {
        new CurvePoint(16f, 1.5f),
        new CurvePoint(22f, 1.5f),
        new CurvePoint(30f, 4f),
        new CurvePoint(50f, 12f),
        new CurvePoint(75f, 36f)
    };
        public static float GenerateRandomMinTicksToNextCanLovin(Pawn pawn)
        {
            float num = IdleLovinIntervalHoursFromAgeCurve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                foreach (Gene item in pawn.genes.GenesListForReading)
                {
                    num *= item.def.lovinMTBFactor;
                }
            }
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                HediffComp_GiveLovinMTBFactor hediffComp_GiveLovinMTBFactor = hediff.TryGetComp<HediffComp_GiveLovinMTBFactor>();
                if (hediffComp_GiveLovinMTBFactor != null)
                {
                    num *= hediffComp_GiveLovinMTBFactor.Props.lovinMTBFactor;
                }
            }
            num = Rand.Gaussian(num, 0.3f);
            if (num < 0.5f)
            {
                num = 0.5f;
            }
            if (num > 36f)
            {
                num = 36f;
            }
            return num;
        }
        public static int GenerateRandomMinTicksToNextIdleLovin(Pawn pawn, Pawn Partner, float num)
        {
            float num2 = IdleLovinUtility.GetIdleLovinMtbHours(pawn, Partner);
            if (num2 <= 0)
            {
                num += 16f;
                return (int)(Rand.Gaussian(num, 8f) * 2500f);
            }
            float sum = num2 + num;
            if (sum <= 6f)
            {
                float t = sum / 6f;
                sum = Mathf.Lerp(2f, 6f, t);
            }
            else
            {
                sum = Mathf.Lerp(sum, 6f, 0.4f);
            }
            float result = Mathf.Clamp(Rand.Gaussian(sum, 3f), num, Mathf.Max(9f, sum));
            if (result > 8f)
            {
                result = Mathf.Lerp(result, 8f, 0.4f);
            }
            return (int)(result * 2500f);
        }
        public static float GetIdleLovinMtbHours(Pawn pawn, Pawn partner)
        {
            float num = IdleLovinUtility.IdleLovinMtbSinglePawnFactor(pawn);
            if (num <= 0f)
            {
                return -1f;
            }
            float num2 = IdleLovinUtility.IdleLovinMtbSinglePawnFactor(partner);
            if (num2 <= 0f)
            {
                return -1f;
            }
            float num3 = 12f;
            num3 *= num;
            num3 *= num2;
            num3 /= Mathf.Max(pawn.relations.SecondaryLovinChanceFactor(partner), 0.1f);
            num3 /= Mathf.Max(partner.relations.SecondaryLovinChanceFactor(pawn), 0.1f);
            num3 *= GenMath.LerpDouble(-100f, 100f, 1.3f, 0.7f, (float)pawn.relations.OpinionOf(partner));
            num3 *= GenMath.LerpDouble(-100f, 100f, 1.3f, 0.7f, (float)partner.relations.OpinionOf(pawn));
            if (pawn.health.hediffSet.HasHediff(HediffDefOf.PsychicLove, false))
            {
                num3 /= 4f;
            }
            else if (partner.health.hediffSet.HasHediff(HediffDefOf.PsychicLove, false))
            {
                num3 /= 2f;
            }
            return num3;
        }
        private static float IdleLovinMtbSinglePawnFactor(Pawn pawn)
        {
            float num = 1f;
            float painFactor = 1f - pawn.health.hediffSet.PainTotal;
            if (painFactor <= 0f)
            {
                return -1f;
            }
            num /= painFactor;
            float level = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
            if (level < 0.5f)
            {
                if (level <= 0f)
                {
                    return -1f;
                }
                num /= level * 2f;
            }
            float ageFactor = GenMath.FlatHill(0f, 14f, 16f, 25f, 80f, 0.2f, pawn.ageTracker.AgeBiologicalYearsFloat);
            if (ageFactor <= 0f)
            {
                return -1f;
            }
            return num / ageFactor;
        }
    }
    [DefOf]
    public static class eJobDefOf
    {
        public static JobDef IdleLovin;
    }
}