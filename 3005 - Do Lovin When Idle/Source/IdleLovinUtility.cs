using RimWorld;
using UnityEngine;
using Verse;

namespace eqdseq
{
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
}