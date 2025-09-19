namespace eqdseq
{
    public class DLWI_DictionaryField
    {
        public int lastCheckTick = 0;
        public int laborCheckTick = 0;
        public int lastTryTick = 60000;
        public int lastTryCount = 1;
        public bool initLastTryTick = true;
    }
}