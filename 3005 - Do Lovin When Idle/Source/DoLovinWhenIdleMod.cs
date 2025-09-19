using RimWorld;
using UnityEngine;
using Verse;

namespace eqdseq
{
    public class DoLovinWhenIdleMod : Mod
    {
        public static DoLovinWhenIdleSettings Settings;
        public DoLovinWhenIdleMod(ModContentPack mod) : base(mod)
        {
            Settings = base.GetSettings<DoLovinWhenIdleSettings>();
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
                ref DoLovinWhenIdleMod.Settings.NoStopRecreation,
                "DoLovinWhenIdle.NoStopRecreation.Desc".Translate(),
                0f, 1f);
            listing_Standard.End();
            base.DoSettingsWindowContents(rect);
        }
    }
}