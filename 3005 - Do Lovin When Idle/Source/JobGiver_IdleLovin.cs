using RimWorld;
using RimWorld.Planet;
using System;
using Verse;
using Verse.AI;

namespace eqdseq
{
    public class JobGiver_IdleLovin : ThinkNode_JobGiver
    {
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
                int numr = ((((ticksGame % 1000) + (pawn.thingIDNumber % 1000)) % 1000) + Rand.Range(1400, 8000));
                pawn.SetCheckTick(ticksGame + numr);
                pawn.SetTryTick(ticksGame + numr + (int)(2500 * IdleLovinUtility.GenerateRandomMinTicksToNextCanLovin(pawn)));
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
                    lastCheckTickTemp = Math.Min(pawn2.GetCheckTick() + 10000, canLovinTick);
                    continue;
                }
                if (!pawn.CanReserve(pawn2, 1, -1, null, false) || !pawn2.CanReserve(pawn, 1, -1, null, false))
                {
                    lastCheckTickTemp = ticksGame + 2500;
                    continue;
                }
                needs = pawn2.needs;
                if (needs == null)
                {
                    continue;
                }
                if (needs.mood != null && needs.mood.CurLevel < 0.42f)
                {
                    if (needs.rest != null && needs.rest.CurLevel < 0.27f)
                    {
                        lastCheckTickTemp = ticksGame + (int)((1f - needs.rest.CurLevel) * 2700);
                        continue;
                    }
                    lastCheckTickTemp = ticksGame + (int)((1f - needs.mood.CurLevel) * 4100);
                    continue;
                }
                if (needs.food != null && needs.food.CurLevel < 0.26f)
                {
                    lastCheckTickTemp = ticksGame + (int)((1f - needs.food.CurLevel) * 2200);
                    continue;
                }
                if (needs.joy != null && needs.joy.CurLevel < 0.41f)
                {
                    lastCheckTickTemp = ticksGame + (int)((1f - needs.joy.CurLevel) * 2100);
                    continue;
                }
                hediffSet = pawn2.health?.hediffSet;
                if (hediffSet == null)
                {
                    lastCheckTickTemp = ticksGame + 10000;
                    continue;
                }
                if (hediffSet.BleedRateTotal > 0f)
                {
                    lastCheckTickTemp = ticksGame + 2500;
                    continue;
                }
                if (!pawn2.SafeTemperatureAtCell(ownedBedPos, ownedBedMap))
                {
                    lastCheckTickTemp = ticksGame + 3900;
                    continue;
                }
                if (ownedBedPos.GetVacuum(ownedBedMap) >= 0.5f && pawn2.GetStatValue(StatDefOf.VacuumResistance, true, 60) < 1f)
                {
                    lastCheckTickTemp = ticksGame + 10000 + (ticksGame % 2000);
                    continue;
                }
                if (pawn2.GetLaborCheckTick() < ticksGame)
                {
                    if (hediffSet.InLabor(true))
                    {
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
                                    lastCheckTickTemp = ticksGame + 60000;
                                    continue;
                                }
                                nums = 50000;
                                break;
                        }
                    }
                    lastCheckTickTemp = ticksGame + nums;
                }
                IntVec3 sleepingSpot2 = RestUtility.GetBedSleepingSlotPosFor(pawn2, ownedBed);
                Pawn_JobTracker jobs2 = pawn2.jobs;
                JobDef pawn2curJobdef = jobs2.curJob?.def;
                if (pawn2curJobdef == null)
                {
                    lastCheckTickTemp = ticksGame + Rand.Range(200, 750);
                    continue;
                }
                if (flagm)
                {
                    if (!DoLovinWhenIdle_Manager.DLWIMF_CanReach(pawn2, ownedBed, ownedBedMap, false))
                    {
                        lastCheckTickTemp = ticksGame + 2500;
                        continue;
                    }
                }
                if (jobs2.curDriver == null)
                {
                    lastCheckTickTemp = ticksGame + 999;
                    continue;
                }
                JobTag lastJobTag = pawn2.mindState.lastJobTag;
                if (!flagm && jobs2.curDriver.asleep && lastJobTag == JobTag.SatisfyingNeeds && sleepingSpot2 == pawn2.Position && jobs2.posture == PawnPosture.LayingInBed)
                {
                    if (!pawn2.health.capacities.CanBeAwake)
                    {
                        lastCheckTickTemp = ticksGame + 30000;
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
                            lastCheckTickTemp = ticksGame + 2300;
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
                        pawn.SetCheckTick(ticksGame + 8100);
                        pawn.SetTryTick(ticksGame + 8200);
                        if (flagm)
                        {
                            //Log.Warning($"[{pawn.Label} at tick {Find.TickManager.TicksGame}] b2 start");
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
                            lastCheckTickTemp = ticksGame + 2300;
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
                        pawn.SetCheckTick(ticksGame + 8100);
                        pawn.SetTryTick(ticksGame + 8200);
                        if (flagm)
                        {
                            //Log.Warning($"[{pawn.Label} at tick {Find.TickManager.TicksGame}] b3 start");
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
}