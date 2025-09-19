using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace eqdseq
{
    public class JobDriver_IdleLovin : JobDriver
    {
        private TargetIndex PartnerInd = TargetIndex.A;
        private TargetIndex BedInd = TargetIndex.B;
        private Pawn Partner => (Pawn)(Thing)job.GetTarget(PartnerInd);
        private Building_Bed Bed => (Building_Bed)(Thing)job.GetTarget(BedInd);
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Map != null && pawn.Map == Bed.Map)
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
                //Log.Warning($"[{pawn.Label} at tick {Find.TickManager.TicksGame}] gotoBed initaction start");
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
                //gotoBed.AddPreTickAction(delegate
                //{
                //    if (pawn.IsHashIntervalTick(100))
                {
                    //Log.Warning($"[{pawn.Label} at tick {Find.TickManager.TicksGame}] gotoBed delta start");
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
                //Log.Warning($"[{pawn.Label} at tick {Find.TickManager.TicksGame}] toil1 initacion start");
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
            //toil1.AddPreTickAction(delegate
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
                    //Log.Warning($"[{pawn.Label} at tick {Find.TickManager.TicksGame}] toil1 layingbed start");
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
                //else if (actor.IsHashIntervalTick(100))
                {
                    //Log.Warning($"[{pawn.Label} at tick {Find.TickManager.TicksGame}] toil1 else if start");
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
                PortraitsCache.SetDirty(actor);
                ticksLeftThisToil = (int)(2500f * Mathf.Clamp(Rand.Range(0.6f, 1.1f), 0.1f, 2f));
                if (actor.thingIDNumber < partner.thingIDNumber)
                {
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.InitiatedLovin, actor.Named(HistoryEventArgsNames.Doer)));
                    if (InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween(actor, partner) && InteractionWorker_RomanceAttempt.TryCreatePsychicBondBetween(actor, partner) && (PawnUtility.ShouldSendNotificationAbout(actor) || PawnUtility.ShouldSendNotificationAbout(partner)))
                    {
                        Find.LetterStack.ReceiveLetter("LetterPsychicBondCreatedLovinLabel".Translate(), "LetterPsychicBondCreatedLovinText".Translate(actor.Named("BONDPAWN"), partner.Named("OTHERPAWN")), LetterDefOf.PositiveEvent, new LookTargets(actor, partner));
                    }
                    //if (InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween_NewTemp(actor, partner) && InteractionWorker_RomanceAttempt.TryCreatePsychicBondBetween(actor, partner) && (PawnUtility.ShouldSendNotificationAbout(actor) || PawnUtility.ShouldSendNotificationAbout(partner)))
                    //{
                    //    Find.LetterStack.ReceiveLetter("LetterPsychicBondCreatedLovinLabel".Translate(), "LetterPsychicBondCreatedLovinText".Translate(actor.Named("BONDPAWN"), partner.Named("OTHERPAWN")), LetterDefOf.PositiveEvent, new LookTargets(actor, partner));
                    //}
                }
            };
            toil2.AddPreTickIntervalAction(delegate (int delta)
            {
                if (ticksLeftThisToil <= 0)
                {
                    ReadyForNextToil();
                }
                else if (pawn.IsHashIntervalTick(100, delta))
                //toil2.AddPreTickAction(delegate
                //{
                //    if (ticksLeftThisToil <= 0)
                //    {
                //        ReadyForNextToil();
                //    }
                //    else if (pawn.IsHashIntervalTick(100))
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
                bool enhancer = false;
                if ((hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)) || (hediffSet2.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)))
                {
                    thought_Memory.moodPowerFactor = 1.5f;
                    enhancer = true;
                }
                if (!enhancer)
                {
                    thought_Memory.durationTicksOverride = 90000;
                }
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
}