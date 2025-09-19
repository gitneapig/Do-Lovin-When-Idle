using Prepatcher;
using Verse;

namespace eqdseq
{
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
}