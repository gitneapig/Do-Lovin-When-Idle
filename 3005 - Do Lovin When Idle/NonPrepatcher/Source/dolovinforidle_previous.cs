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
                    Log.Error($"[Do Lovin' When Idle] Exception while patching : [Yayo Animation]: {ex}");
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
                    Log.Error($"[Do Lovin' When Idle] Exception while patching : [Romance On The Rim]: {ex}");
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

                    Log.Message("Do Lovin' When Idle + Yayo's Animation (Continued) = Success.");
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
                    Log.Message("Do Lovin' When Idle + RomanceOnTheRim = Success.");
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

        static eJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(eJobDefOf));
        }
    }


    public class DoLovinWhenIdleComponent : GameComponent
    {
        private Game unusedMyComponent;
        public DoLovinWhenIdleComponent(Game game)
        {
            unusedMyComponent = game;
            PawnTempDataManager.Reset();
        }

        public override void LoadedGame()
        {
            unusedMyComponent.components.Remove(this);
        }

        public override void ExposeData()
        {

        }
    }

    public class PawnTempData
    {
        public int lastCheckTick = 0;
        public int laborCheckTick = 0;
        public int sharedBedSpouse = 0;
        public int lastTryTick = 1;

        public int lovinCount = 0;
    }

    public static class PawnTempDataManager
    {
        private static Dictionary<int, PawnTempData> pawnData = new Dictionary<int, PawnTempData>();

        public static void Reset()
        {
            pawnData.Clear();
            //Log.Message("PawnTempDataManager를 초기화합니다.");
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

    public class JobGiver_IdleLovin : ThinkNode_JobGiver
    {
        private readonly int radius = 70;
        public JobGiver_IdleLovin() { }

        public override float GetPriority(Pawn pawn)
        {
            if (pawn.needs == null)
            {
                return 0f;
            }

            if (pawn.needs.joy == null)
            {
                return 0f;
            }
            if (pawn.needs.joy.CurLevel < 0.8f)
            {
                return 0f;
            }
            return pawn.needs.joy.CurLevel * 10f;
        }

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
            if (Find.TickManager.TicksGame < pawn.mindState.canLovinTick)
            {
                return null;
            }
            if (!ownedBed.Spawned)
            {
                return null;
            }
            if (ownedBed.Map != pawn.Map)
            {
                return null;
            }
            if (ownedBed.Medical)
            {
                return null;
            }


            PawnTempData tempData = PawnTempDataManager.GetOrCreateData(pawn.thingIDNumber);
            int ticksGame = Find.TickManager.TicksGame;

            if (ticksGame < tempData.lastTryTick)
            {
                return null;
            }
            tempData.lastTryTick = ticksGame + 180;

            if (ticksGame < tempData.lastCheckTick)
            {
                return null;
            }
            tempData.lastCheckTick = ticksGame;

            int diff = Mathf.RoundToInt((pawn.Position - ownedBed.Position).LengthHorizontalSquared - (radius * radius));
            if (diff > 0)
            {
                tempData.lastTryTick += (9 * radius);
                tempData.lastCheckTick += (6 * radius);
                return null;
            }

            foreach (IntVec3 item in ownedBed.OccupiedRect())
            {
                if (item.ContainsStaticFire(ownedBed.Map))
                {
                    tempData.lastTryTick += 5000;
                    tempData.lastCheckTick += 2500;
                    return null;
                }
            }
            if (!pawn.SafeTemperatureAtCell(ownedBed.Position, ownedBed.Map))
            {
                tempData.lastTryTick += 5000;
                tempData.lastCheckTick += 2500;
                return null;
            }

            if (ownedBed.Position.GetVacuum(ownedBed.Map) >= 0.5f && pawn.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
            {
                tempData.lastTryTick += 10000;
                tempData.lastCheckTick += 5000;
                return null;
            }
            tempData.lastCheckTick += 20;

            if (pawn.needs == null)
            {
                tempData.lastTryTick = int.MaxValue;
                tempData.lastCheckTick = int.MaxValue;
                Log.Error($"[DoLovinWhenIdle]{pawn.LabelShortCap} currently has an error or is incompatible. Please reload the game or remove this mod. DoLovinWhenIdle will remain disabled until the game is reloaded.");
                return null;
            }
            if (pawn.needs.mood != null && pawn.needs.mood.CurLevel < 0.5)
            {
                tempData.lastTryTick = ticksGame + 3000;
                tempData.lastCheckTick += 1500;
                return null;
            }

            if (pawn.needs.food != null && pawn.needs.food.CurLevel < 0.3)
            {
                tempData.lastTryTick = ticksGame + 3000;
                tempData.lastCheckTick += 1500;
                return null;
            }

            if (pawn.needs.rest != null && pawn.needs.rest.CurLevel < 0.3)
            {
                tempData.lastTryTick = ticksGame + 3000;
                return null;
            }

            if (pawn.needs.joy != null && pawn.needs.joy.CurLevel < 0.5)
            {
                tempData.lastTryTick = ticksGame + 3000;
                return null;
            }

            if (pawn.health?.hediffSet == null)
            {
                tempData.lastTryTick = int.MaxValue;
                tempData.lastCheckTick = int.MaxValue;
                Log.Error($"[DoLovinWhenIdle]{pawn.LabelShortCap} currently has an error or is incompatible. Please reload the game or remove this mod. DoLovinWhenIdle will remain disabled until the game is reloaded.");
                return null;
            }

            if (pawn.health.hediffSet.BleedRateTotal > 0f)
            {
                tempData.lastTryTick = ticksGame + 3000;
                tempData.lastCheckTick += 1500;
                return null;
            }

            if (tempData.laborCheckTick < ticksGame)
            {
                if (pawn.health.hediffSet.InLabor(true))
                {
                    tempData.laborCheckTick = ticksGame + 40000;
                    tempData.lastCheckTick += 60000;
                    return null;
                }
                tempData.laborCheckTick = ticksGame + 360000;

                Hediff_Pregnant pregnancyHediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.PregnantHuman) as Hediff_Pregnant;
                if (pregnancyHediff != null)
                {
                    switch (pregnancyHediff.CurStageIndex)
                    {
                        case 0:
                            tempData.laborCheckTick = ticksGame + 360000;
                            break;
                        case 1:
                            tempData.laborCheckTick = ticksGame + 180000;
                            break;
                        case 2:
                            float severity = pregnancyHediff.Severity;
                            if (severity > 0.95f)
                            {
                                tempData.laborCheckTick = ticksGame + 40000;
                                tempData.lastCheckTick += 60000;
                                return null;
                            }
                            else
                            {
                                tempData.laborCheckTick = ticksGame + 50000;
                            }
                            break;
                    }
                }
            }

            IntVec3 sleepingSpot = RestUtility.GetBedSleepingSlotPosFor(pawn, ownedBed);
            int sharedBedNonSpouse = 0;
            int lastCheckTickTemp = int.MaxValue;
            bool doItLater = false;


            foreach (Pawn pawn2 in ownedBed.OwnersForReading)
            {
                if (pawn2 == pawn)
                {
                    continue;
                }

                if (!LovePartnerRelationUtility.LovePartnerRelationExists(pawn, pawn2))
                {
                    sharedBedNonSpouse++;
                    continue;
                }

                if (ticksGame < pawn2.mindState.canLovinTick)
                {
                    continue;
                }

                if (pawn2.needs == null)
                {
                    doItLater = true;
                    continue;
                }

                if (pawn2.needs.mood != null && pawn2.needs.mood.CurLevel < 0.45)
                {
                    doItLater = true;
                    continue;
                }
                if (pawn2.needs.food != null && pawn2.needs.food.CurLevel < 0.3)
                {
                    doItLater = true;
                    continue;
                }
                if (pawn2.needs.rest != null && pawn2.needs.rest.CurLevel < 0.3)
                {
                    doItLater = true;
                    continue;
                }
                if (pawn2.needs.joy != null && pawn2.needs.joy.CurLevel < 0.5)
                {
                    doItLater = true;
                    continue;
                }

                if (pawn2.health?.hediffSet == null)
                {
                    doItLater = true;
                    continue;
                }

                if (pawn2.health.hediffSet.BleedRateTotal > 0f)
                {
                    doItLater = true;
                    continue;
                }

                PawnTempData tempData2 = PawnTempDataManager.GetOrCreateData(pawn2.thingIDNumber);
                if (ticksGame < tempData2.lastCheckTick)
                {
                    if (lastCheckTickTemp > tempData2.lastCheckTick)
                    {
                        lastCheckTickTemp = tempData2.lastCheckTick;
                    }
                    continue;
                }
                tempData2.lastCheckTick = ticksGame + 30;

                float diff2 = (pawn2.Position - ownedBed.Position).LengthHorizontalSquared - (radius * radius);

                if (diff2 > 0)
                {
                    doItLater = true;
                    continue;
                }

                if (!pawn2.SafeTemperatureAtCell(ownedBed.Position, ownedBed.Map))
                {
                    doItLater = true;
                    continue;
                }

                if (ownedBed.Position.GetVacuum(ownedBed.Map) >= 0.5f && pawn2.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                {
                    doItLater = true;
                    continue;
                }

                JobDriver pawn2curDriver = pawn2.jobs.curDriver;
                if (pawn2curDriver == null)
                {
                    continue;
                }

                if (!pawn.CanReserve(pawn2, 1, -1, null, false) || !pawn2.CanReserve(pawn, 1, -1, null, false))
                {
                    doItLater = true;
                    continue;
                }

                if (pawn2.mindState.lastJobTag == JobTag.SatisfyingNeeds)
                {
                    if (pawn2curDriver.asleep)
                    {
                        if (pawn2.CurrentBed() == null || pawn2.CurrentBed().Medical || !pawn2.health.capacities.CanBeAwake)
                        {
                            tempData2.lastCheckTick += 2500;
                            continue;
                        }
                        if (!pawn.CanReach(sleepingSpot, PathEndMode.OnCell, Danger.Deadly))
                        {
                            tempData.lastCheckTick += 1500;
                            tempData.lastTryTick += 3000;
                            return null;
                        }
                        tempData2.lastCheckTick += 300;
                        tempData2.lastTryTick += 900;
                        tempData.lastTryTick += 2500;
                        Job newJob = JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn, ownedBed);
                        pawn2.jobs.StartJob(newJob, JobCondition.InterruptForced);
                        return JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn2, ownedBed);
                    }
                }

                if (tempData2.laborCheckTick < ticksGame)
                {
                    if (pawn2.health.hediffSet.InLabor(true))
                    {
                        tempData2.laborCheckTick = ticksGame + 40000;
                        tempData2.lastCheckTick += 60000;
                        continue;
                    }
                    tempData2.laborCheckTick = ticksGame + 360000;

                    Hediff_Pregnant pregnancyHediff2 = pawn2.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnantHuman) as Hediff_Pregnant;
                    if (pregnancyHediff2 != null)
                    {
                        switch (pregnancyHediff2.CurStageIndex)
                        {
                            case 0:
                                tempData2.laborCheckTick = ticksGame + 360000;
                                break;
                            case 1:
                                tempData2.laborCheckTick = ticksGame + 180000;
                                break;
                            case 2:
                                float severity = pregnancyHediff2.Severity;
                                if (severity > 0.95f)
                                {
                                    tempData2.laborCheckTick = ticksGame + 40000;
                                    tempData2.lastCheckTick += 60000;
                                    continue;
                                }
                                else
                                {
                                    tempData2.laborCheckTick = ticksGame + 50000;
                                }
                                break;
                        }
                    }
                }

                JobDef pawn2curJobdef = pawn2.jobs.curJob?.def;
                if (pawn2curJobdef == null)
                {
                    continue;
                }

                IntVec3 sleepingSpot2 = RestUtility.GetBedSleepingSlotPosFor(pawn2, ownedBed);
                if (pawn2curDriver.ticksLeftThisToil > 0)
                {
                    if ((pawn2.mindState.lastJobTag == JobTag.SatisfyingNeeds || pawn2.mindState.lastJobTag == JobTag.Idle) && pawn2curJobdef.joyKind != null && pawn2curDriver.ticksLeftThisToil < 1000 && pawn2.jobs.curJob.count == -1)
                    {
                        if (!pawn2.CanReach(sleepingSpot2, PathEndMode.OnCell, Danger.Deadly))
                        {
                            tempData2.lastCheckTick += 3000;
                            doItLater = true;
                            continue;
                        }

                        if (!pawn.CanReach(sleepingSpot, PathEndMode.OnCell, Danger.Deadly))
                        {
                            tempData.lastCheckTick += 1500;
                            tempData.lastTryTick += 3000;
                            return null;
                        }

                        if (pawn2curJobdef.joyKind != JoyKindDefOf.Meditative)
                        {
                            JoyUtility.TryGainRecRoomThought(pawn2);
                        }
                        tempData2.lastCheckTick += 300;
                        tempData2.lastTryTick += 900;
                        tempData.lastTryTick += 2500;
                        Job newJob = JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn, ownedBed);
                        pawn2.jobs.StartJob(newJob, JobCondition.InterruptForced);
                        return JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn2, ownedBed);
                    }
                }

                if (pawn2curJobdef == JobDefOf.GotoWander || pawn2curJobdef == JobDefOf.Wait_Wander)
                {

                    if (!pawn2.CanReach(sleepingSpot2, PathEndMode.OnCell, Danger.Deadly))
                    {
                        tempData2.lastCheckTick += 3000;
                        doItLater = true;
                        continue;
                    }

                    if (!pawn.CanReach(sleepingSpot, PathEndMode.OnCell, Danger.Deadly))
                    {
                        tempData.lastCheckTick += 1500;
                        tempData.lastTryTick += 3000;
                        return null;
                    }
                    tempData2.lastCheckTick += 300;
                    tempData2.lastTryTick += 900;
                    tempData.lastTryTick += 2500;
                    Job newJob = JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn, ownedBed);
                    pawn2.jobs.StartJob(newJob, JobCondition.InterruptForced);
                    return JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn2, ownedBed);
                }
            }


            if (sharedBedNonSpouse > 0)
            {
                tempData.lastTryTick = ticksGame + 30000;
            }
            else if (doItLater)
            {
                tempData.lastTryTick = ticksGame + 2500;
            }
            else
            {
                tempData.lastTryTick = ticksGame + Rand.Range(200, 500);
            }

            if (lastCheckTickTemp != int.MaxValue)
            {
                tempData.lastTryTick = lastCheckTickTemp + 30;
            }
            return null;
        }
    }

    public class JobDriver_IdleLovin : JobDriver
    {
        private int ticksLeft = 3200;
        private int ticksReady = 1500;

        private TargetIndex PartnerInd = TargetIndex.A;

        private TargetIndex BedInd = TargetIndex.B;

        private const int TicksBetweenHeartMotes = 100;

        private static float PregnancyChance = 0.01f;

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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 2250);
        }

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
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(BedInd);
            this.FailOnDespawnedOrNull(PartnerInd);
            if ((pawn.Position == RestUtility.GetBedSleepingSlotPosFor(pawn, Bed)) || pawn.needs != null && pawn.needs.rest != null && pawn.needs.rest.CurLevel < 0.34f)
            {
                this.KeepLyingDown(BedInd);
            }

            Toil gotoBed = ToilMaker.MakeToil("GotoBed1");
            gotoBed.initAction = delegate
            {
                IntVec3 bedSleepingSlotPosFor = RestUtility.GetBedSleepingSlotPosFor(pawn, Bed);
                if (pawn.Position == bedSleepingSlotPosFor)
                {
                    pawn.jobs.curDriver.ReadyForNextToil();
                    return;
                }
                else
                {
                    pawn.pather.StartPath(bedSleepingSlotPosFor, PathEndMode.OnCell);
                }
            };
            gotoBed.AddPreTickIntervalAction(delegate (int delta)
            {
                if (pawn.IsHashIntervalTick(100, delta))
                {
                    if (Partner.CurJob == null || Partner.CurJob.def != eJobDefOf.IdleLovin || Partner.health == null || !Partner.health.capacities.CanBeAwake)
                    {
                        if (pawn.jobs.curDriver.ticksLeftThisToil != 0)
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }

                    if (!Bed.IsOwner(pawn))
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    foreach (IntVec3 item in Bed.OccupiedRect())
                    {
                        if (item.ContainsStaticFire(Bed.Map))
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    if (Bed.Position.GetVacuum(Bed.Map) >= 0.5f && pawn.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                }
            });
            gotoBed.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return gotoBed;

            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate
            {
                if (Partner.jobs.posture != PawnPosture.LayingInBed)
                {
                    ticksLeft = (int)(2500f * Mathf.Clamp(Rand.Range(0.6f, 1.1f), 0.1f, 2f));
                }
                else
                {
                    ticksLeft = 3100;
                }
                ticksReady = ticksLeft + 500;
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;


            Toil toil1 = ToilMaker.MakeToil("LayDown1");
            toil1.initAction = delegate
            {
                Pawn_PathFollower pather = pawn.pather;
                if (pather != null)
                {
                    pather.StopDead();
                }

                if (!Bed.OccupiedRect().Contains(pawn.Position))
                {
                    string text = "Can't start LayDown toil because pawn is not in the bed. pawn=";
                    Log.Error(text + ((pawn != null) ? pawn.ToString() : null));
                    pawn.jobs.EndCurrentJob(JobCondition.Errored, true, true);
                    return;
                }
                pawn.jobs.posture = PawnPosture.LayingInBed;
                PortraitsCache.SetDirty(pawn);
            };
            toil1.AddPreTickIntervalAction(delegate (int delta)
            {
                ticksReady -= delta;
                if (Partner.jobs.posture == PawnPosture.LayingInBed)
                {
                    if (pawn.Map != Bed.Map)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (Bed.IsOwner(pawn, out var assignedSleepingSlot))
                    {
                        if (pawn.Position != Bed.GetSleepingSlotPos(assignedSleepingSlot.Value))
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    else
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    foreach (IntVec3 item in Bed.OccupiedRect())
                    {
                        if (item.ContainsStaticFire(Bed.Map))
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    if (Bed.Position.GetVacuum(Bed.Map) >= 0.5f && pawn.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }

                    if (Partner.CurJob != null && Partner.CurJob.def == eJobDefOf.IdleLovin && Partner.health != null && Partner.health.capacities.CanBeAwake && LovePartnerRelationUtility.LovePartnerRelationExists(pawn, Partner))
                    {
                        ReadyForNextToil();
                    }
                    else
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                }
                else if (pawn.IsHashIntervalTick(100, delta))
                {
                    if (Partner.CurJob == null || Partner.CurJob.def != eJobDefOf.IdleLovin || ticksReady < 0 || Partner.health == null || !Partner.health.capacities.CanBeAwake || !LovePartnerRelationUtility.LovePartnerRelationExists(pawn, Partner))
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (pawn.Map != Bed.Map)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (Bed.IsOwner(pawn, out var assignedSleepingSlot))
                    {
                        if (pawn.Position != Bed.GetSleepingSlotPos(assignedSleepingSlot.Value))
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    else
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    foreach (IntVec3 item in Bed.OccupiedRect())
                    {
                        if (item.ContainsStaticFire(Bed.Map))
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    if (!pawn.SafeTemperatureAtCell(Bed.Position, Bed.Map))
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (Bed.Position.GetVacuum(Bed.Map) >= 0.5f && pawn.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }

                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Meditating);
                }
            });
            toil1.socialMode = RandomSocialMode.Normal;
            toil1.defaultCompleteMode = ToilCompleteMode.Never;
            yield return toil1;

            Toil toil2 = ToilMaker.MakeToil("LayDown2");
            toil2.initAction = delegate
            {
                Pawn_PathFollower pather = pawn.pather;
                if (pather != null)
                {
                    pather.StopDead();
                }
                if (!Bed.OccupiedRect().Contains(pawn.Position))
                {
                    string text = "Can't start LayDown toil because pawn is not in the bed. pawn=";
                    Log.Error(text + ((pawn != null) ? pawn.ToString() : null));
                    pawn.jobs.EndCurrentJob(JobCondition.Errored, true, true);
                    return;
                }
                pawn.jobs.posture = PawnPosture.LayingInBed;
                PortraitsCache.SetDirty(pawn);
                if (pawn.thingIDNumber < Partner.thingIDNumber)
                {
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.InitiatedLovin, pawn.Named(HistoryEventArgsNames.Doer)));
                    if (InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween(pawn, Partner) && InteractionWorker_RomanceAttempt.TryCreatePsychicBondBetween(pawn, Partner) && (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(Partner)))
                    {
                        Find.LetterStack.ReceiveLetter("LetterPsychicBondCreatedLovinLabel".Translate(), "LetterPsychicBondCreatedLovinText".Translate(pawn.Named("BONDPAWN"), Partner.Named("OTHERPAWN")), LetterDefOf.PositiveEvent, new LookTargets(pawn, Partner));
                    }
                }
            };
            toil2.AddPreTickIntervalAction(delegate (int delta)
            {
                ticksLeft -= delta;
                if (ticksLeft <= 0)
                {
                    ReadyForNextToil();
                }
                else if (pawn.IsHashIntervalTick(100, delta))
                {
                    if (Partner.CurJob == null || Partner.CurJob.def != eJobDefOf.IdleLovin || Partner.health == null || !Partner.health.capacities.CanBeAwake)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (pawn.Map != Bed.Map)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (Bed.IsOwner(pawn, out var assignedSleepingSlot))
                    {
                        if (pawn.Position != Bed.GetSleepingSlotPos(assignedSleepingSlot.Value))
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    else
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    foreach (IntVec3 item in Bed.OccupiedRect())
                    {
                        if (item.ContainsStaticFire(Bed.Map))
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    if (!pawn.SafeTemperatureAtCell(Bed.Position, Bed.Map))
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (Bed.Position.GetVacuum(Bed.Map) >= 0.5f && pawn.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
                }
            });
            toil2.AddFinishAction(delegate
            {
                if (base.pawn.health == null || base.pawn.health.hediffSet == null || Partner == null || Partner.health == null || Partner.health.hediffSet == null)
                {
                    return;
                }

                ModExtensionMessod(this.pawn);

                Thought_Memory thought_Memory = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDefOf.GotSomeLovin);
                if ((base.pawn.health != null && base.pawn.health.hediffSet != null && base.pawn.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)) || (Partner.health != null && Partner.health.hediffSet != null && Partner.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)))
                {
                    thought_Memory.moodPowerFactor = 1.5f;
                }

                if (base.pawn.needs.mood != null)
                {
                    base.pawn.needs.mood.thoughts.memories.TryGainMemory(thought_Memory, Partner);
                }

                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.GotLovin, base.pawn.Named(HistoryEventArgsNames.Doer)));
                HistoryEventDef def = (base.pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, Partner) ? HistoryEventDefOf.GotLovin_Spouse : HistoryEventDefOf.GotLovin_NonSpouse);
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(def, base.pawn.Named(HistoryEventArgsNames.Doer)));
                float nums = GenerateRandomMinTicksToNextLovin(base.pawn);
                base.pawn.mindState.canLovinTick = Find.TickManager.TicksGame + (int)(nums * 2500f);

                PawnTempData tempData = PawnTempDataManager.GetOrCreateData(pawn.thingIDNumber);
                tempData.lastTryTick = Find.TickManager.TicksGame + GenerateRandomMinTicksToNextIdleLovin(base.pawn, Partner, nums);
                tempData.lastCheckTick = Rand.Range(base.pawn.mindState.canLovinTick, tempData.lastTryTick);


                //if (Prefs.DevMode)
                //{
                //    tempData.lovinCount++;
                //    Log.Warning(
                //        $"{pawn.LabelShortCap} = {tempData.lovinCount:F0}회, " +
                //        $"{(pawn.mindState.canLovinTick - Find.TickManager.TicksGame) / 2500f:F2}, " +
                //        $"{(tempData.lastCheckTick - Find.TickManager.TicksGame) / 2500f:F2}"
                //    );
                //}

                if (ModsConfig.BiotechActive)
                {
                    Pawn pawn = ((base.pawn.gender == Gender.Male) ? base.pawn : ((Partner.gender == Gender.Male) ? Partner : null));
                    Pawn pawn2 = ((base.pawn.gender == Gender.Female) ? base.pawn : ((Partner.gender == Gender.Female) ? Partner : null));
                    if (pawn != null && pawn2 != null && Rand.Chance(PregnancyChance * PregnancyUtility.PregnancyChanceForPartners(pawn2, pawn)))
                    {
                        bool success;
                        GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(pawn, pawn2, out success);
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
            if (DebugSettings.alwaysDoLovin)
            {
                return 100;
            }

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

            return num;
        }

        private int GenerateRandomMinTicksToNextIdleLovin(Pawn pawn, Pawn Partner, float num)
        {
            float num2 = IdleLovinUtility.GetLovinMtbHours(pawn, Partner);
            //Log.Warning($"{pawn.LabelShortCap} = {num2:F2}:계수");
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