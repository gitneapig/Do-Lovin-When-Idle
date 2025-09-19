using Verse;
using Verse.AI;

namespace eqdseq
{
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
}