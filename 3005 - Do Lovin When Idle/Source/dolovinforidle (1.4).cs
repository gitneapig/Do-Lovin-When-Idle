using eqdseq;
using HarmonyLib;
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




    public static class PawnTempData
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
        public static extern ref bool layDownState(this Pawn target);

        [PrepatcherField]
        [Prepatcher.DefaultValue(false)]
        public static extern ref bool initLastTryTick(this Pawn target);

        [PrepatcherField]
        [Prepatcher.DefaultValue(1)]
        public static extern ref int lastTryCount(this Pawn target);
    }


    public class JobGiver_IdleLovin : ThinkNode_JobGiver
    {
        public JobGiver_IdleLovin() { }

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
            if (pawn.lastTryTick() > Find.TickManager.TicksGame)
            {
                return null;
            }

            int ticksGame = Find.TickManager.TicksGame;

            if (pawn.lastCheckTick() < ticksGame)
            {
                pawn.lastCheckTick() = ticksGame + 30;
            }

            Building_Bed ownedBed = pawn.ownership.OwnedBed;
            if (ownedBed == null)
            {
                pawn.lastTryTick() = ticksGame + 15000;
                return null;
            }
            if (ownedBed.OwnersForReading.Count == 1)
            {
                pawn.lastTryTick() = ticksGame + 30000;
                return null;
            }
            if (!ownedBed.Spawned)
            {
                pawn.lastTryTick() = ticksGame + 15000;
                return null;
            }

            int canLovinTick = pawn.mindState.canLovinTick;
            if (canLovinTick > ticksGame)
            {
                pawn.lastTryTick() = canLovinTick + 30;
                pawn.lastCheckTick() = Math.Max(pawn.lastCheckTick(), canLovinTick);
                return null;
            }

            Map ownedBedMap = ownedBed.Map;
            if (ownedBedMap != pawn.Map)
            {
                pawn.lastTryTick() = ticksGame + 15000;
                return null;
            }

            IntVec3 ownedBedPos = ownedBed.Position;
            foreach (IntVec3 item in ownedBed.OccupiedRect())
            {
                if (item.ContainsStaticFire(ownedBedMap))
                {
                    pawn.lastTryTick() = ticksGame + 5000;
                    return null;
                }
            }
            if (!pawn.SafeTemperatureAtCell(ownedBedPos, ownedBedMap))
            {
                pawn.lastTryTick() = ticksGame + 5000;
                return null;
            }
            //if (ownedBedPos.GetVacuum(ownedBedMap) >= 0.5f && pawn.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
            //{
            //    pawn.lastTryTick() = ticksGame + 10000;
            //    return null;
            //}


            if (pawn.needs == null)
            {
                pawn.lastTryTick() = int.MaxValue;
                pawn.lastCheckTick() = int.MaxValue;
                Log.Error($"[DoLovinWhenIdle]{pawn.LabelShortCap} currently has an error or is incompatible. Please reload the game or remove this mod. DoLovinWhenIdle will remain disabled until the game is reloaded.");
                return null;
            }
            if (pawn.needs.mood != null && pawn.needs.mood.CurLevel < 0.5f)
            {
                pawn.lastTryTick() = ticksGame + 3000;
                return null;
            }

            if (pawn.needs.food != null && pawn.needs.food.CurLevel < 0.3f)
            {
                pawn.lastTryTick() = ticksGame + 3000;
                return null;
            }

            if (pawn.needs.rest != null && pawn.needs.rest.CurLevel < 0.3f)
            {
                pawn.lastTryTick() = ticksGame + 3000;
                return null;
            }

            if (pawn.needs.joy != null && pawn.needs.joy.CurLevel < 0.5f)
            {
                pawn.lastTryTick() = ticksGame + 3000;
                return null;
            }


            if (pawn.health?.hediffSet == null)
            {
                pawn.lastTryTick() = int.MaxValue;
                pawn.lastCheckTick() = int.MaxValue;
                Log.Error($"[DoLovinWhenIdle]{pawn.LabelShortCap} currently has an error or is incompatible. Please reload the game or remove this mod. DoLovinWhenIdle will remain disabled until the game is reloaded.");
                return null;
            }

            if (pawn.health.hediffSet.BleedRateTotal > 0f)
            {
                pawn.lastTryTick() = ticksGame + 3000;
                return null;
            }

            if (pawn.laborCheckTick() < ticksGame)
            {
                if (!pawn.initLastTryTick())
                {
                    pawn.initLastTryTick() = true;
                    int numr = Rand.Range(500, 1000);
                    if (!ModLister.CheckBiotech("Human pregnancy"))
                    {
                        pawn.laborCheckTick() = int.MaxValue;
                        pawn.lastTryTick() = ticksGame + numr;
                        return null;
                    }

                    if (pawn.lastTryTick() == 60000)
                    {
                        pawn.lastCheckTick() += numr;
                        pawn.lastTryTick() = ticksGame + (5 * numr);
                        return null;
                    }
                }

                pawn.laborCheckTick() = ticksGame;
                if (pawn.health.hediffSet.InLabor(true))
                {
                    pawn.laborCheckTick() += 40000;
                    pawn.lastCheckTick() = ticksGame + 60000;
                    pawn.lastTryTick() = ticksGame + 60000;
                    return null;
                }
                int num = 360000;

                Hediff_Pregnant pregnancyHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnantHuman) as Hediff_Pregnant;
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
                                pawn.laborCheckTick() += 40000;
                                pawn.lastCheckTick() = ticksGame + 60000;
                                pawn.lastTryTick() = ticksGame + 60000;
                                return null;
                            }
                            num = 50000;
                            break;
                    }
                }
                pawn.laborCheckTick() += num;
            }

            IntVec3 sleepingSpot = RestUtility.GetBedSleepingSlotPosFor(pawn, ownedBed);
            int sharedBedNonSpouse = 0;
            int lastCheckTickTemp = pawn.lastTryTick();
            bool pawnCanReachCheck = false;
            pawn.lastTryTick() = ticksGame;


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

                if (pawn2.lastCheckTick() > ticksGame)
                {
                    lastCheckTickTemp = pawn2.lastCheckTick();
                    continue;
                }

                canLovinTick = pawn2.mindState.canLovinTick;
                if (canLovinTick > ticksGame)
                {
                    pawn2.lastCheckTick() = canLovinTick + 30;
                    lastCheckTickTemp = pawn2.lastCheckTick();
                    continue;
                }

                if (!pawn.CanReserve(pawn2, 1, -1, null, false) || !pawn2.CanReserve(pawn, 1, -1, null, false))
                {
                    pawn2.lastCheckTick() = ticksGame + 5000;
                    lastCheckTickTemp = pawn2.lastCheckTick();
                    continue;
                }

                if (pawn2.needs == null)
                {
                    pawn2.lastCheckTick() = int.MaxValue;
                    continue;
                }
                if (pawn2.needs.mood != null && pawn2.needs.mood.CurLevel < 0.45f)
                {
                    pawn2.lastCheckTick() = ticksGame + 5000;
                    lastCheckTickTemp = pawn2.lastCheckTick();
                    continue;
                }
                if (pawn2.needs.food != null && pawn2.needs.food.CurLevel < 0.3f)
                {
                    pawn2.lastCheckTick() = ticksGame + 2500;
                    lastCheckTickTemp = pawn2.lastCheckTick();
                    continue;
                }
                if (pawn2.needs.rest != null && pawn2.needs.rest.CurLevel < 0.3f)
                {
                    pawn2.lastCheckTick() = ticksGame + 2500;
                    lastCheckTickTemp = pawn2.lastCheckTick();
                    continue;
                }
                if (pawn2.needs.joy != null && pawn2.needs.joy.CurLevel < 0.5f)
                {
                    pawn2.lastCheckTick() = ticksGame + 2500;
                    lastCheckTickTemp = pawn2.lastCheckTick();
                    continue;
                }

                if (pawn2.health?.hediffSet == null)
                {
                    pawn2.lastCheckTick() = int.MaxValue;
                    continue;
                }

                if (pawn2.health.hediffSet.BleedRateTotal > 0f)
                {
                    pawn2.lastCheckTick() = ticksGame + 2500;
                    lastCheckTickTemp = pawn2.lastCheckTick();
                    continue;
                }

                if (!pawn2.SafeTemperatureAtCell(ownedBedPos, ownedBedMap))
                {
                    pawn2.lastCheckTick() = ticksGame + 5000;
                    lastCheckTickTemp = pawn2.lastCheckTick();
                    continue;
                }

                //if (ownedBedPos.GetVacuum(ownedBedMap) >= 0.5f && pawn2.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                //{
                //    pawn2.lastCheckTick() = ticksGame + 10000;
                //    lastCheckTickTemp = pawn2.lastCheckTick();
                //    continue;
                //}

                if (pawn2.laborCheckTick() < ticksGame)
                {
                    pawn2.laborCheckTick() = ticksGame;
                    if (pawn2.health.hediffSet.InLabor(true))
                    {
                        pawn2.laborCheckTick() += 40000;
                        pawn2.lastCheckTick() = ticksGame + 60000;
                        pawn2.lastTryTick() = ticksGame + 60000;
                        lastCheckTickTemp = pawn2.lastCheckTick();
                        continue;
                    }
                    int nums = 360000;

                    Hediff_Pregnant pregnancyHediff2 = pawn2.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnantHuman) as Hediff_Pregnant;
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
                                    pawn2.laborCheckTick() += 40000;
                                    pawn2.lastCheckTick() = ticksGame + 60000;
                                    pawn2.lastTryTick() = ticksGame + 60000;
                                    lastCheckTickTemp = pawn2.lastCheckTick();
                                    continue;
                                }
                                nums = 50000;
                                break;
                        }
                    }
                    pawn2.laborCheckTick() += nums;
                }

                IntVec3 sleepingSpot2 = RestUtility.GetBedSleepingSlotPosFor(pawn2, ownedBed);
                JobDef pawn2curJobdef = pawn2.jobs?.curJob?.def;
                if (pawn2curJobdef == null)
                {
                    pawn2.lastCheckTick() = ticksGame + Rand.Range(200, 750);
                    lastCheckTickTemp = pawn2.lastCheckTick();
                    continue;
                }

                JobTag pawn2JobTag = pawn2.mindState.lastJobTag;
                if (pawn2curJobdef == JobDefOf.Wait_Asleep && pawn2JobTag == JobTag.SatisfyingNeeds && pawn2.Position == sleepingSpot2 && pawn2.jobs.posture == PawnPosture.LayingInBed)
                {
                    if (!pawn2.health.capacities.CanBeAwake)
                    {
                        pawn2.lastCheckTick() = ticksGame + 30000;
                        lastCheckTickTemp = pawn2.lastCheckTick();
                        continue;
                    }
                    if (!pawnCanReachCheck)
                    {
                        if (!pawn.CanReach(sleepingSpot, PathEndMode.OnCell, Danger.Deadly))
                        {
                            pawn.lastCheckTick() = ticksGame + 5000;
                            pawn.lastTryTick() += 10000;
                            return null;
                        }
                        if (pawn.jobs?.curJob != null)
                        {
                            pawn.lastCheckTick() = ticksGame + 5000;
                            pawn.lastTryTick() += 10000;
                            return null;
                        }
                        pawnCanReachCheck = true;
                    }
                    pawn2.lastCheckTick() = ticksGame + 300;
                    pawn2.lastTryTick() = ticksGame + 900;
                    pawn.lastTryTick() += 2500;
                    Job newJob = JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn, ownedBed);
                    pawn2.jobs.StartJob(newJob, JobCondition.InterruptForced);
                    return JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn2, ownedBed);
                }


                bool pawn2Idle = false;
                if (pawn2JobTag == JobTag.Idle)
                {
                    if (pawn2curJobdef == JobDefOf.GotoWander || pawn2curJobdef == JobDefOf.Wait_Wander)
                    {
                        if (!pawn2.CanReach(sleepingSpot2, PathEndMode.OnCell, Danger.Deadly))
                        {
                            pawn2.lastCheckTick() = ticksGame + 2500;
                            pawn2.lastTryTick() = ticksGame + 5000;
                            lastCheckTickTemp = pawn2.lastCheckTick();
                            continue;
                        }

                        if (!pawnCanReachCheck)
                        {
                            if (!pawn.CanReach(sleepingSpot, PathEndMode.OnCell, Danger.Deadly))
                            {
                                pawn.lastCheckTick() = ticksGame + 5000;
                                pawn.lastTryTick() += 10000;
                                return null;
                            }
                            if (pawn.jobs?.curJob != null)
                            {
                                pawn.lastCheckTick() = ticksGame + 5000;
                                pawn.lastTryTick() += 10000;
                                return null;
                            }
                            pawnCanReachCheck = true;
                        }

                        pawn2.lastCheckTick() = ticksGame + 300;
                        pawn2.lastTryTick() = ticksGame + 900;
                        pawn.lastTryTick() += 2500;
                        Job newJob = JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn, ownedBed);
                        pawn2.jobs.StartJob(newJob, JobCondition.InterruptForced);
                        return JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn2, ownedBed);
                    }
                    pawn2Idle = true;
                }

                if ((pawn2JobTag == JobTag.SatisfyingNeeds || pawn2Idle) && pawn2curJobdef.joyKind != null)
                {
                    if (pawn2.jobs.curDriver == null)
                    {
                        pawn2.lastCheckTick() = ticksGame + 250;
                        lastCheckTickTemp = pawn2.lastCheckTick();
                        continue;
                    }
                    int num2 = pawn2.jobs.curDriver.ticksLeftThisToil;
                    if (num2 > 0 && num2 < 1000 && pawn2.jobs.curJob.count == -1)
                    {
                        if (!pawn2.CanReach(sleepingSpot2, PathEndMode.OnCell, Danger.Deadly))
                        {
                            pawn2.lastCheckTick() = ticksGame + 2500;
                            pawn2.lastTryTick() = ticksGame + 5000;
                            lastCheckTickTemp = pawn2.lastCheckTick();
                            continue;
                        }

                        if (!pawnCanReachCheck)
                        {
                            if (!pawn.CanReach(sleepingSpot, PathEndMode.OnCell, Danger.Deadly))
                            {
                                pawn.lastCheckTick() = ticksGame + 5000;
                                pawn.lastTryTick() += 10000;
                                return null;
                            }
                            if (pawn.jobs?.curJob != null)
                            {
                                pawn.lastCheckTick() = ticksGame + 5000;
                                pawn.lastTryTick() += 10000;
                                return null;
                            }
                            pawnCanReachCheck = true;
                        }
                        if (pawn2curJobdef.joyKind != JoyKindDefOf.Meditative)
                        {
                            JoyUtility.TryGainRecRoomThought(pawn2);
                        }
                        pawn2.lastCheckTick() = ticksGame + 300;
                        pawn2.lastTryTick() = ticksGame + 900;
                        pawn.lastTryTick() += 2500;
                        Job newJob = JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn, ownedBed);
                        pawn2.jobs.StartJob(newJob, JobCondition.InterruptForced);
                        return JobMaker.MakeJob(eJobDefOf.IdleLovin, pawn2, ownedBed);
                    }
                }
            }
            if (pawn.lastTryCount() > 5)
            {
                pawn.lastTryCount() = 0;
            }
            pawn.lastTryCount()++;
            if (sharedBedNonSpouse == 0)
            {
                pawn.lastTryTick() = lastCheckTickTemp + (pawn.lastTryCount() * Rand.Range(200, 750));
                return null;
            }
            pawn.lastTryTick() += (pawn.lastTryCount() * Rand.Range(200, 750)) + sharedBedNonSpouse;
            return null;
        }
    }

    public class JobDriver_IdleLovin : JobDriver
    {
        private int ticksLeft = 3200;
        private int ticksReady = 2200;

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

        public override string GetReport()
        {
            return JobDefOf.Lovin.reportString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //this.FailOnDespawnedOrNull(BedInd);
            //this.FailOnDespawnedOrNull(PartnerInd);

            if (Bed == null || !Bed.Spawned || Bed.Map != pawn.Map || Partner == null || !Partner.Spawned || Partner.Map != pawn.Map)
            {
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
            }
            else if ((pawn.Position == RestUtility.GetBedSleepingSlotPosFor(pawn, Bed)) || (pawn.needs != null && pawn.needs.rest != null && pawn.needs.rest.CurLevel < 0.34f))
            {
                this.KeepLyingDown(BedInd);
            }
            pawn.layDownState() = false;
            pawn.lastTryCount() = 0;

            Toil gotoBed = ToilMaker.MakeToil("GotoBed1");
            gotoBed.initAction = delegate
            {
                if (Bed == null || !Bed.Spawned || Bed.Map != pawn.Map || Partner == null || !Partner.Spawned || Partner.Map != pawn.Map)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
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
            gotoBed.AddPreTickAction(delegate
            {
                if (pawn.IsHashIntervalTick(100))
                {
                    if (Bed == null || !Bed.Spawned || Bed.Map != pawn.Map || Partner == null || !Partner.Spawned || Partner.Map != pawn.Map)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (!pawn.initLastTryTick())
                    {
                        pawn.lastTryTick() = Find.TickManager.TicksGame + Rand.Range(500, 5000);
                        pawn.initLastTryTick() = true;
                    }
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
                    Map map = Bed.Map;
                    foreach (IntVec3 item in Bed.OccupiedRect())
                    {
                        if (item.ContainsStaticFire(map))
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    //if (Bed.Position.GetVacuum(map) >= 0.5f && pawn.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                    //{
                    //    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    //    return;
                    //}
                }
            });
            gotoBed.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return gotoBed;

            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate
            {
                if (Bed == null || !Bed.Spawned || Bed.Map != pawn.Map || Partner == null || !Partner.Spawned || Partner.Map != pawn.Map)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
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
                if (Bed == null || !Bed.Spawned || Bed.Map != pawn.Map || Partner == null || !Partner.Spawned || Partner.Map != pawn.Map)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
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
                pawn.layDownState() = true;
                PortraitsCache.SetDirty(pawn);
            };
            toil1.AddPreTickAction(delegate
            {
                ticksReady--;
                if (Partner != null && Partner.layDownState())
                {
                    if (Bed == null || !Bed.Spawned || Bed.Map != pawn.Map || !Partner.Spawned || Partner.Map != pawn.Map)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    pawn.layDownState() = true;
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
                    //if (Bed.Position.GetVacuum(Bed.Map) >= 0.5f && pawn.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                    //{
                    //    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    //    return;
                    //}

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
                else if (pawn.IsHashIntervalTick(100))
                {
                    if (!pawn.initLastTryTick())
                    {
                        pawn.lastTryTick() = Find.TickManager.TicksGame + Rand.Range(500, 5000);
                        pawn.initLastTryTick() = true;
                        pawn.layDownState() = true;
                    }
                    if (Bed == null || !Bed.Spawned || Bed.Map != pawn.Map || Partner == null || !Partner.Spawned || Partner.Map != pawn.Map)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (Partner.CurJob == null || Partner.CurJob.def != eJobDefOf.IdleLovin || ticksReady < 0 || Partner.health == null || !Partner.health.capacities.CanBeAwake || !LovePartnerRelationUtility.LovePartnerRelationExists(pawn, Partner))
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }

                    IntVec3 pawnPos = pawn.Position;
                    if (Bed.IsOwner(pawn, out var assignedSleepingSlot))
                    {
                        if (pawnPos != Bed.GetSleepingSlotPos(assignedSleepingSlot.Value))
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
                    Map bedMap = Bed.Map;
                    IntVec3 bedPos = Bed.Position;
                    foreach (IntVec3 item in Bed.OccupiedRect())
                    {
                        if (item.ContainsStaticFire(bedMap))
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    if (!pawn.SafeTemperatureAtCell(bedPos, bedMap))
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    //if (bedPos.GetVacuum(bedMap) >= 0.5f && pawn.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                    //{
                    //    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    //    return;
                    //}

                    FleckMaker.ThrowMetaIcon(pawnPos, pawn.Map, FleckDefOf.Meditating);
                }
            });
            toil1.socialMode = RandomSocialMode.Normal;
            toil1.defaultCompleteMode = ToilCompleteMode.Never;
            yield return toil1;

            Toil toil2 = ToilMaker.MakeToil("LayDown2");
            toil2.initAction = delegate
            {
                if (Bed == null || !Bed.Spawned || Bed.Map != pawn.Map || Partner == null || !Partner.Spawned || Partner.Map != pawn.Map)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
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
                pawn.layDownState() = true;
                PortraitsCache.SetDirty(pawn);
                if (pawn.thingIDNumber < Partner.thingIDNumber)
                {
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.InitiatedLovin, pawn.Named(HistoryEventArgsNames.Doer)));
                    if (InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween_NewTemp(pawn, Partner) && InteractionWorker_RomanceAttempt.TryCreatePsychicBondBetween(pawn, Partner) && (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(Partner)))
                    {
                        Find.LetterStack.ReceiveLetter("LetterPsychicBondCreatedLovinLabel".Translate(), "LetterPsychicBondCreatedLovinText".Translate(pawn.Named("BONDPAWN"), Partner.Named("OTHERPAWN")), LetterDefOf.PositiveEvent, new LookTargets(pawn, Partner));
                    }
                }
            };
            toil2.AddPreTickAction(delegate
            {
                ticksLeft--;
                if (ticksLeft <= 0)
                {
                    ReadyForNextToil();
                }
                else if (pawn.IsHashIntervalTick(100))
                {
                    if (!pawn.initLastTryTick())
                    {
                        pawn.lastTryTick() = Find.TickManager.TicksGame + Rand.Range(500, 5000);
                        pawn.initLastTryTick() = true;
                        pawn.layDownState() = true;
                    }
                    if (Bed == null || !Bed.Spawned || Bed.Map != pawn.Map || Partner == null || !Partner.Spawned || Partner.Map != pawn.Map)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    if (Partner.CurJob == null || Partner.CurJob.def != eJobDefOf.IdleLovin || Partner.health == null || !Partner.health.capacities.CanBeAwake)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }

                    IntVec3 pawnPos = pawn.Position;
                    if (Bed.IsOwner(pawn, out var assignedSleepingSlot))
                    {
                        if (pawnPos != Bed.GetSleepingSlotPos(assignedSleepingSlot.Value))
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
                    Map bedMap = Bed.Map;
                    IntVec3 bedPos = Bed.Position;
                    foreach (IntVec3 item in Bed.OccupiedRect())
                    {
                        if (item.ContainsStaticFire(bedMap))
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                            return;
                        }
                    }
                    if (!pawn.SafeTemperatureAtCell(bedPos, bedMap))
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    //if (Bed.Position.GetVacuum(bedMap) >= 0.5f && pawn.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                    //{
                    //    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    //    return;
                    //}
                    FleckMaker.ThrowMetaIcon(pawnPos, pawn.Map, FleckDefOf.Heart);
                }
            });
            toil2.AddFinishAction(delegate
            {
                if (base.pawn.health == null || base.pawn.health.hediffSet == null || Partner == null || Partner.health == null || Partner.health.hediffSet == null)
                {
                    pawn.lastTryTick() = Find.TickManager.TicksGame + Rand.Range(500, 5000);
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

                pawn.lastTryTick() = Find.TickManager.TicksGame + GenerateRandomMinTicksToNextIdleLovin(base.pawn, Partner, nums);
                pawn.lastCheckTick() = Rand.Range(base.pawn.mindState.canLovinTick, pawn.lastTryTick());


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

            //foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            //{
            //    HediffComp_GiveLovinMTBFactor hediffComp_GiveLovinMTBFactor = hediff.TryGetComp<HediffComp_GiveLovinMTBFactor>();
            //    if (hediffComp_GiveLovinMTBFactor != null)
            //    {
            //        num *= hediffComp_GiveLovinMTBFactor.Props.lovinMTBFactor;
            //    }
            //}

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