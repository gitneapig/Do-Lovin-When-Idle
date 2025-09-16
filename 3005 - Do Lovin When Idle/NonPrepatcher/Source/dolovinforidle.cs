using eqdseq;
using HarmonyLib;
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
        static HarmonyPatches()
        {
            var harmony = new Harmony("eqdseq.dolovinforidle");
            if (LoadedModManager.RunningModsListForReading.Any(mod => mod.PackageIdPlayerFacing == "com.yayo.yayoAni.continued"))
            {
                try
                {
                    var yayoAnimationType = AccessTools.TypeByName("YayoAnimation.AnimationCore");
                    if (yayoAnimationType != null)
                    {
                        var method = AccessTools.Method(yayoAnimationType, "AniLaying");
                        if (method != null)
                        {
                            harmony.Patch(
                                original: method,
                                transpiler: new HarmonyMethod(typeof(HarmonyPatches_Transpiler), nameof(HarmonyPatches_Transpiler.YayoAnimation_AnimationCore_AniLaying))
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[Do Lovin' When Idle] Exception while patching : YayoAnimation.AnimationCore: {ex}");
                }
            }
            if (LoadedModManager.RunningModsListForReading.Any(mod => mod.PackageIdPlayerFacing == "telardo.RomanceOnTheRim"))
            {
                try
                {
                    var IdleLovinType = AccessTools.TypeByName("eqdseq.JobDriver_IdleLovin");
                    if (IdleLovinType != null)
                    {
                        var method = AccessTools.Method(IdleLovinType, "ModExtensionMessod");
                        if (method != null)
                        {
                            harmony.Patch(
                                original: method,
                                transpiler: new HarmonyMethod(typeof(HarmonyPatches_Transpiler), nameof(HarmonyPatches_Transpiler.RomanceOnTheRim_eqdseq_JobDriver_IdleLovin_ModExtensionMessod))
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[Do Lovin' When Idle] Exception while patching : eqdseq.JobDriver_IdleLovin: {ex}");
                }
            }
        }
    }
    public static class HarmonyPatches_Transpiler
    {
        public static IEnumerable<CodeInstruction> RimWorld_JoyUtility_JoyTickCheckEnd(IEnumerable<CodeInstruction> instructions)
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
        public static IEnumerable<CodeInstruction> YayoAnimation_AnimationCore_AniLaying(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_1 && codes[i + 2].opcode == OpCodes.Stfld && codes[i + 2].operand is FieldInfo fi && fi.Name == "jobName" && fi.DeclaringType?.Name == "PawnDrawData")
                {
                    Label skipSetLabel = ilGen.DefineLabel();
                    codes[i].labels.Add(skipSetLabel);
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_2, null));
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldstr, "IdleLovin"));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(string), "op_Equality")));
                    codes.Insert(i + 3, new CodeInstruction(OpCodes.Brfalse_S, skipSetLabel));
                    codes.Insert(i + 4, new CodeInstruction(OpCodes.Ldstr, "Lovin"));
                    codes.Insert(i + 5, new CodeInstruction(OpCodes.Starg_S, 2));
                    Log.Message("[Do Lovin' When Idle]: [Yayo's Animation (Continued)] was discovered and a compatibility patch has been applied. Target classes:: YayoAnimation.AnimationCore");
                    break;
                }
            }
            return codes;
        }
        public static IEnumerable<CodeInstruction> RomanceOnTheRim_eqdseq_JobDriver_IdleLovin_ModExtensionMessod(IEnumerable<CodeInstruction> instructions)
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
                    Log.Message("[Do Lovin' When Idle]: [Romance On The Rim] was discovered and a compatibility patch has been applied. Target classes:: eqdseq.JobDriver_IdleLovin");
                    break;
                }
            }
            return codes;
        }
    }
    public class DoLovinWhenIdleMod : Mod
    {
        public DoLovinWhenIdleMod(ModContentPack mod) : base(mod)
        {
            base.GetSettings<DoLovinWhenIdleSettings>();
            if (DoLovinWhenIdleSettings.NoStopRecreation)
            {
                var harmony = new Harmony("eqdseq.dolovinforidle");
                harmony.Patch(
                    original: AccessTools.Method(typeof(RimWorld.JoyUtility), "JoyTickCheckEnd"),
                    transpiler: new HarmonyMethod(typeof(HarmonyPatches_Transpiler), nameof(HarmonyPatches_Transpiler.RimWorld_JoyUtility_JoyTickCheckEnd))
                );
            }
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
                ref DoLovinWhenIdleSettings.NoStopRecreation,
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
            Scribe_Values.Look<bool>(ref DoLovinWhenIdleSettings.NoStopRecreation, "NoStopRecreation", true, false);
        }
        public static bool NoStopRecreation = true;
    }
    [DefOf]
    public static class eJobDefOf
    {
        public static JobDef IdleLovin;
    }
    public class IdleLovinUtility
    {
        public static float GetLovinMtbHours(Pawn pawn, Pawn partner)
        {
            float num = IdleLovinUtility.LovinMtbSinglePawnFactor(pawn);
            if (num <= 0f)
            {
                return -1f;
            }
            float num2 = IdleLovinUtility.LovinMtbSinglePawnFactor(partner);
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
        private static float LovinMtbSinglePawnFactor(Pawn pawn)
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
    public class DoLovinWhenIdleComponent : GameComponent
    {
        //private Game unusedMyComponent;
        public DoLovinWhenIdleComponent(Game game)
        {
            //unusedMyComponent = game;
            PawnTempDataManager.Reset();
        }
        //public override void LoadedGame()
        //{
        //    unusedMyComponent.components.Remove(this);
        //}
        public override void ExposeData()
        {
        }
    }
    public class PawnTempData
    {
        public int lastCheckTick = 0;
        public int laborCheckTick = 0;
        public int lastTryTick = 60000;
        public int lastTryCount = 0;
    }
    public static class PawnTempDataManager
    {
        private static Dictionary<int, PawnTempData> pawnData = new Dictionary<int, PawnTempData>();
        public static void Reset()
        {
            pawnData.Clear();
        }
        public static PawnTempData GetOrCreateData(int pawn)
        {
            if (pawnData.TryGetValue(pawn, out PawnTempData data))
            {
                return data;
            }
            PawnTempData newData = new PawnTempData();
            pawnData.Add(pawn, newData);
            newData.lastTryTick = Find.TickManager.TicksGame + Rand.Range(500, 5000);
            newData.lastCheckTick = Find.TickManager.TicksGame + Rand.Range(500, 1000);
            if (Prefs.DevMode)
            {
                newData.lastTryTick = Find.TickManager.TicksGame + 500;
                newData.lastCheckTick = Find.TickManager.TicksGame + 300;
            }
            if (!ModLister.CheckBiotech("Human pregnancy"))
            {
                newData.laborCheckTick = int.MaxValue;
            }
            return newData;
        }
    }
    public class JobGiver_IdleLovin : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Building_Bed ownedBed = pawn.ownership.OwnedBed;
            if (ownedBed == null)
            {
                return null;
            }
            if (ownedBed.OwnersForReading.Count == 1)
            {
                return null;
            }
            PawnTempData tempData = PawnTempDataManager.GetOrCreateData(pawn.thingIDNumber);
            if (tempData.lastCheckTick > Find.TickManager.TicksGame)
            {
                return null;
            }
            int ticksGame = Find.TickManager.TicksGame;
            if (tempData.lastCheckTick < ticksGame)
            {
                tempData.lastCheckTick = ticksGame + 30;
            }
            if (!ownedBed.Spawned)
            {
                tempData.lastTryTick = ticksGame + 15000 + (ticksGame % 2000);
                return null;
            }
            int canLovinTick = pawn.mindState.canLovinTick;
            if (canLovinTick > ticksGame)
            {
                tempData.lastTryTick = canLovinTick + 30;
                tempData.lastCheckTick = Math.Max(tempData.lastCheckTick, canLovinTick);
                return null;
            }
            Map ownedBedMap = ownedBed.Map;
            if (ownedBedMap != pawn.Map)
            {
                tempData.lastTryTick = ticksGame + 10000 + ((Rand.Range(1234, 5678)) * tempData.lastTryCount);
                if (tempData.lastTryCount > 5)
                {
                    tempData.lastTryCount = 0;
                }
                tempData.lastTryCount++;
                return null;
            }
            IntVec3 ownedBedPos = ownedBed.Position;
            foreach (IntVec3 item in ownedBed.OccupiedRect())
            {
                if (item.ContainsStaticFire(ownedBedMap))
                {
                    tempData.lastTryTick = ticksGame + 2900;
                    return null;
                }
            }
            if (!pawn.SafeTemperatureAtCell(ownedBedPos, ownedBedMap))
            {
                tempData.lastTryTick = ticksGame + 3900;
                return null;
            }
            if (ownedBedPos.GetVacuum(ownedBedMap) >= 0.5f && pawn.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
            {
                tempData.lastTryTick = ticksGame + 10000 + (ticksGame % 2000);
                return null;
            }
            Pawn_NeedsTracker needs = pawn.needs;
            if (needs == null)
            {
                tempData.lastTryTick = int.MaxValue;
                tempData.lastCheckTick = int.MaxValue;
                Log.Error($"[DoLovinWhenIdle]{pawn.LabelShortCap} currently has an error or is incompatible. Please reload the game or remove this mod. DoLovinWhenIdle will remain disabled until the game is reloaded.");
                return null;
            }
            if (needs.mood != null && needs.mood.CurLevel < 0.44f)
            {
                if (needs.rest != null && needs.rest.CurLevel < 0.3f)
                {
                    tempData.lastTryTick = ticksGame + (int)((1f - needs.rest.CurLevel) * 2900);
                    return null;
                }
                tempData.lastTryTick = ticksGame + (int)((1f - needs.mood.CurLevel) * 4400);
                return null;
            }
            if (needs.food != null && needs.food.CurLevel < 0.3f)
            {
                tempData.lastTryTick = ticksGame + (int)((1f - needs.food.CurLevel) * 2100);
                return null;
            }
            if (needs.joy != null && needs.joy.CurLevel < 0.43f)
            {
                tempData.lastTryTick = ticksGame + (int)((1f - needs.joy.CurLevel) * 2100);
                return null;
            }
            HediffSet hediffSet = pawn.health?.hediffSet;
            if (hediffSet == null)
            {
                tempData.lastTryTick = int.MaxValue;
                tempData.lastCheckTick = int.MaxValue;
                Log.Error($"[DoLovinWhenIdle]{pawn.LabelShortCap} currently has an error or is incompatible. Please reload the game or remove this mod. DoLovinWhenIdle will remain disabled until the game is reloaded.");
                return null;
            }
            if (hediffSet.BleedRateTotal > 0f)
            {
                tempData.lastTryTick = ticksGame + 1500;
                return null;
            }
            if (tempData.laborCheckTick < ticksGame)
            {
                tempData.laborCheckTick = ticksGame;
                if (hediffSet.InLabor(true))
                {
                    tempData.laborCheckTick += 40000;
                    tempData.lastCheckTick = ticksGame + 60000;
                    tempData.lastTryTick = ticksGame + 61000;
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
                                tempData.laborCheckTick += 40000;
                                tempData.lastCheckTick = ticksGame + 60000;
                                tempData.lastTryTick = ticksGame + 61000;
                                return null;
                            }
                            num = 50000;
                            break;
                    }
                }
                tempData.laborCheckTick += num;
            }
            IntVec3 sleepingSpot = RestUtility.GetBedSleepingSlotPosFor(pawn, ownedBed);
            int sharedBedNonSpouse = 0;
            int lastCheckTickTemp = tempData.lastTryTick;
            bool pawnCanReachCheck = false;
            tempData.lastTryTick = ticksGame;
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
                PawnTempData tempData2 = PawnTempDataManager.GetOrCreateData(pawn2.thingIDNumber);
                if (tempData2.lastCheckTick > ticksGame)
                {
                    lastCheckTickTemp = Math.Min(tempData2.lastCheckTick, ticksGame + 6000);
                    continue;
                }
                canLovinTick = pawn2.mindState.canLovinTick;
                if (canLovinTick > ticksGame)
                {
                    tempData2.lastCheckTick = canLovinTick + 30;
                    lastCheckTickTemp = tempData2.lastCheckTick;
                    continue;
                }
                if (!pawn.CanReserve(pawn2, 1, -1, null, false) || !pawn2.CanReserve(pawn, 1, -1, null, false))
                {
                    tempData2.lastCheckTick = ticksGame + 2500;
                    lastCheckTickTemp = tempData2.lastCheckTick;
                    continue;
                }
                needs = pawn2.needs;
                if (needs == null)
                {
                    tempData2.lastCheckTick = int.MaxValue;
                    continue;
                }
                if (needs.mood != null && needs.mood.CurLevel < 0.42f)
                {
                    if (needs.rest != null && needs.rest.CurLevel < 0.27f)
                    {
                        tempData2.lastCheckTick = ticksGame + (int)((1f - needs.rest.CurLevel) * 2700);
                        lastCheckTickTemp = tempData2.lastCheckTick;
                        continue;
                    }
                    tempData2.lastCheckTick = ticksGame + (int)((1f - needs.mood.CurLevel) * 4100);
                    lastCheckTickTemp = tempData2.lastCheckTick;
                    continue;
                }
                if (needs.food != null && needs.food.CurLevel < 0.26f)
                {
                    tempData2.lastCheckTick = ticksGame + (int)((1f - needs.food.CurLevel) * 2200);
                    lastCheckTickTemp = tempData2.lastCheckTick;
                    continue;
                }
                if (needs.joy != null && needs.joy.CurLevel < 0.41f)
                {
                    tempData2.lastCheckTick = ticksGame + (int)((1f - needs.joy.CurLevel) * 2100);
                    lastCheckTickTemp = tempData2.lastCheckTick;
                    continue;
                }
                hediffSet = pawn2.health?.hediffSet;
                if (hediffSet == null)
                {
                    tempData2.lastCheckTick = int.MaxValue;
                    continue;
                }
                if (hediffSet.BleedRateTotal > 0f)
                {
                    tempData2.lastCheckTick = ticksGame + 2500;
                    lastCheckTickTemp = tempData2.lastCheckTick;
                    continue;
                }
                if (!pawn2.SafeTemperatureAtCell(ownedBedPos, ownedBedMap))
                {
                    tempData2.lastCheckTick = ticksGame + 3900;
                    lastCheckTickTemp = tempData2.lastCheckTick;
                    continue;
                }
                if (ownedBedPos.GetVacuum(ownedBedMap) >= 0.5f && pawn2.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                {
                    tempData2.lastCheckTick = ticksGame + 10000 + (ticksGame % 2000);
                    lastCheckTickTemp = tempData2.lastCheckTick;
                    continue;
                }
                if (tempData2.laborCheckTick < ticksGame)
                {
                    tempData2.laborCheckTick = ticksGame;
                    if (hediffSet.InLabor(true))
                    {
                        tempData2.laborCheckTick += 40000;
                        tempData2.lastCheckTick = ticksGame + 60000;
                        tempData2.lastTryTick = ticksGame + 61000;
                        lastCheckTickTemp = tempData2.lastCheckTick;
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
                                    tempData2.laborCheckTick += 40000;
                                    tempData2.lastCheckTick = ticksGame + 60000;
                                    tempData2.lastTryTick = ticksGame + 61000;
                                    lastCheckTickTemp = tempData2.lastCheckTick;
                                    continue;
                                }
                                nums = 50000;
                                break;
                        }
                    }
                    tempData2.laborCheckTick += nums;
                }
                IntVec3 sleepingSpot2 = RestUtility.GetBedSleepingSlotPosFor(pawn2, ownedBed);
                Pawn_JobTracker jobs2 = pawn2.jobs;
                JobDef pawn2curJobdef = jobs2.curJob?.def;
                if (pawn2curJobdef == null)
                {
                    tempData2.lastCheckTick = ticksGame + Rand.Range(200, 750);
                    lastCheckTickTemp = tempData2.lastCheckTick;
                    continue;
                }
                if (jobs2.curDriver == null)
                {
                    tempData2.lastCheckTick = ticksGame + 999;
                    lastCheckTickTemp = tempData2.lastCheckTick;
                    continue;
                }
                JobTag lastJobTag = pawn2.mindState.lastJobTag;
                if (jobs2.curDriver.asleep && lastJobTag == JobTag.SatisfyingNeeds && sleepingSpot2 == pawn2.Position && jobs2.posture == PawnPosture.LayingInBed)
                {
                    if (!pawn2.health.capacities.CanBeAwake)
                    {
                        tempData2.lastCheckTick = ticksGame + 30000;
                        lastCheckTickTemp = tempData2.lastCheckTick;
                        continue;
                    }
                    if (!pawnCanReachCheck)
                    {
                        if (!pawn.CanReach(sleepingSpot, PathEndMode.OnCell, Danger.Deadly))
                        {
                            tempData.lastCheckTick = ticksGame + 2700;
                            tempData.lastTryTick += 6000;
                            return null;
                        }
                        if (pawn.jobs?.curJob != null)
                        {
                            tempData.lastCheckTick = ticksGame + 1900;
                            tempData.lastTryTick += 3500;
                            return null;
                        }
                        pawnCanReachCheck = true;
                    }
                    tempData2.lastCheckTick = ticksGame + 300;
                    tempData2.lastTryTick = ticksGame + 900;
                    tempData2.lastTryCount = 0;
                    tempData.lastTryTick += 2500;
                    tempData.lastTryCount = 0;
                    jobs2.StartJob(JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn, ownedBed), JobCondition.InterruptForced);
                    return JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn2, ownedBed);
                }
                if (lastJobTag == JobTag.Idle)
                {
                    if (pawn2curJobdef == JobDefOf.GotoWander || pawn2curJobdef == JobDefOf.Wait_Wander)
                    {
                        if (!pawn2.CanReach(sleepingSpot2, PathEndMode.OnCell, Danger.Deadly))
                        {
                            tempData2.lastCheckTick = ticksGame + 2300;
                            tempData2.lastTryTick = ticksGame + 2800;
                            lastCheckTickTemp = tempData2.lastCheckTick;
                            continue;
                        }
                        if (!pawnCanReachCheck)
                        {
                            if (!pawn.CanReach(sleepingSpot, PathEndMode.OnCell, Danger.Deadly))
                            {
                                tempData.lastCheckTick = ticksGame + 2700;
                                tempData.lastTryTick += 6000;
                                return null;
                            }
                            if (pawn.jobs?.curJob != null)
                            {
                                tempData.lastCheckTick = ticksGame + 1900;
                                tempData.lastTryTick += 3500;
                                return null;
                            }
                            pawnCanReachCheck = true;
                        }
                        tempData2.lastCheckTick = ticksGame + 300;
                        tempData2.lastTryTick = ticksGame + 900;
                        tempData2.lastTryCount = 0;
                        tempData.lastTryTick += 2500;
                        tempData.lastTryCount = 0;
                        jobs2.StartJob(JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn, ownedBed), JobCondition.InterruptForced);
                        return JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn2, ownedBed);
                    }
                }
                if (((lastJobTag == JobTag.SatisfyingNeeds && pawn2curJobdef.joyKind != null) || (pawn2curJobdef != eJobDefOf.IdleLovin && lastJobTag == JobTag.Idle)))
                {
                    int num2 = pawn2.jobs.curDriver.ticksLeftThisToil;
                    if (num2 > 0 && num2 < 1400 && jobs2.curJob.count == -1)
                    {
                        if (!pawn2.CanReach(sleepingSpot2, PathEndMode.OnCell, Danger.Deadly))
                        {
                            tempData2.lastCheckTick = ticksGame + 2300;
                            tempData2.lastTryTick = ticksGame + 2800;
                            lastCheckTickTemp = tempData2.lastCheckTick;
                            continue;
                        }
                        if (!pawnCanReachCheck)
                        {
                            if (!pawn.CanReach(sleepingSpot, PathEndMode.OnCell, Danger.Deadly))
                            {
                                tempData.lastCheckTick = ticksGame + 2700;
                                tempData.lastTryTick += 6000;
                                return null;
                            }
                            if (pawn.jobs?.curJob != null)
                            {
                                tempData.lastCheckTick = ticksGame + 1900;
                                tempData.lastTryTick += 3500;
                                return null;
                            }
                            pawnCanReachCheck = true;
                        }
                        if (pawn2curJobdef.joyKind != JoyKindDefOf.Meditative)
                        {
                            JoyUtility.TryGainRecRoomThought(pawn2);
                        }
                        tempData2.lastCheckTick = ticksGame + 300;
                        tempData2.lastTryTick = ticksGame + 900;
                        tempData2.lastTryCount = 0;
                        tempData.lastTryTick += 2500;
                        tempData.lastTryCount = 0;
                        jobs2.StartJob(JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn, ownedBed), JobCondition.InterruptForced);
                        return JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn2, ownedBed);
                    }
                }
            }
            if (tempData.lastTryCount > 5)
            {
                tempData.lastTryCount = 0;
            }
            tempData.lastTryCount++;
            if (sharedBedNonSpouse == 0)
            {
                tempData.lastTryTick = lastCheckTickTemp + (tempData.lastTryCount * Rand.Range(200, 650));
                return null;
            }
            tempData.lastTryTick += (tempData.lastTryCount * Rand.Range(200, 650)) + sharedBedNonSpouse;
            return null;
        }
    }
    public class JobDriver_IdleLovin : JobDriver
    {
        private TargetIndex PartnerInd = TargetIndex.A;
        private TargetIndex BedInd = TargetIndex.B;
        private static readonly SimpleCurve LovinIntervalHoursFromAgeCurve = new SimpleCurve
    {
        new CurvePoint(16f, 1.5f),
        new CurvePoint(22f, 1.5f),
        new CurvePoint(30f, 4f),
        new CurvePoint(50f, 12f),
        new CurvePoint(75f, 36f)
    };
        private Pawn Partner => (Pawn)(Thing)job.GetTarget(PartnerInd);
        private Building_Bed Bed => (Building_Bed)(Thing)job.GetTarget(BedInd);
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
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
        private void ModExtensionMessod(Pawn pawn)
        {
            return;
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
                PawnTempData tempData = PawnTempDataManager.GetOrCreateData(actor.thingIDNumber);
                if (hediffSet == null || hediffSet2 == null)
                {
                    tempData.lastTryTick = Find.TickManager.TicksGame + Rand.Range(2500, 5000);
                    return;
                }
                ModExtensionMessod(actor);
                Thought_Memory thought_Memory = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDefOf.GotSomeLovin);
                if ((hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)) || (hediffSet2.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)))
                {
                    thought_Memory.moodPowerFactor = 1.5f;
                }
                actor.needs?.mood?.thoughts.memories.TryGainMemory(thought_Memory, actor2);
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.GotLovin, actor.Named(HistoryEventArgsNames.Doer)));
                HistoryEventDef def = (actor.relations.DirectRelationExists(PawnRelationDefOf.Spouse, actor2) ? HistoryEventDefOf.GotLovin_Spouse : HistoryEventDefOf.GotLovin_NonSpouse);
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(def, actor.Named(HistoryEventArgsNames.Doer)));
                float nums = GenerateRandomMinTicksToNextLovin(actor);
                int canLovinTick = ticksGame + (int)(nums * 2500f);
                actor.mindState.canLovinTick = canLovinTick;
                tempData.lastTryTick = ticksGame + GenerateRandomMinTicksToNextIdleLovin(actor, actor2, nums);
                tempData.lastCheckTick = Rand.Range(canLovinTick, tempData.lastTryTick);
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
        private float GenerateRandomMinTicksToNextLovin(Pawn pawn)
        {
            float num = LovinIntervalHoursFromAgeCurve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
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
        private int GenerateRandomMinTicksToNextIdleLovin(Pawn pawn, Pawn Partner, float num)
        {
            float num2 = IdleLovinUtility.GetLovinMtbHours(pawn, Partner);
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
    }
}