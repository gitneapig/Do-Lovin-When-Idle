using Verse;

namespace eqdseq
{
    public class DoLovinWhenIdleSettings : ModSettings
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.NoStopRecreation, "NoStopRecreation", true, false);
        }
        public bool NoStopRecreation = true;
    }
}