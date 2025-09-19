using System.Collections.Generic;
using Verse;

namespace eqdseq
{
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
            newData.lastTryTick = Find.TickManager.TicksGame + Rand.Range(2000, 34000);
            newData.lastCheckTick = Find.TickManager.TicksGame + Rand.Range(2000, 20000);
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
}