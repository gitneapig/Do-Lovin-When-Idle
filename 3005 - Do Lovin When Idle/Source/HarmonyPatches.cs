using HarmonyLib;
using System;
using System.Linq;
using Verse;

namespace eqdseq
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        public static readonly bool IsPrepatcherLoaded;
        static HarmonyPatches()
        {
            var harmony = new Harmony("eqdseq.dolovinforidle");
            IsPrepatcherLoaded = LoadedModManager.RunningModsListForReading.Any(m => m.PackageId.ToLower() == "zetrith.prepatcher")
                   || LoadedModManager.RunningModsListForReading.Any(m => m.PackageId.ToLower() == "jikulopo.prepatcher");
            TryPatch(
                harmony: harmony,
                modId: "telardo.MultiFloors",
                targetMethodString: "eqdseq.DoLovinWhenIdle_Manager:DLWIMF_CanReach",
                transpiler: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.DLWIMF_CanReach_Transpiler))
            );
            TryPatch(
                harmony: harmony,
                modId: "telardo.RomanceOnTheRim",
                targetMethodString: "eqdseq.DoLovinWhenIdle_Manager:DLWIRR_TryRomanceNeed",
                transpiler: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.DLWIRR_TryRomanceNeed_Transpiler))
            );
            TryPatch(
                harmony: harmony,
                modId: "com.yayo.yayoAni.continued",
                targetMethodString: "YayoAnimation.AnimationCore:AniLaying",
                prefix: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.AniLaying_Prefix))
            );
            //TryPatch(
            //    harmony: harmony,
            //    modId: "com.yayo.yayoAni.continued",
            //    targetMethodString: "yayoAni.Yayo:Ani1",
            //    transpiler: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.Ani1_Transpiler))
            //);
            if (DoLovinWhenIdleMod.Settings.NoStopRecreation)
            {
                //harmony.Patch(
                //    original: AccessTools.Method(typeof(RimWorld.JoyUtility), "JoyTickCheckEnd"),
                //    transpiler: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.JoyTickCheckEnd_Transpiler))
                //);
                if (LoadedModManager.RunningModsListForReading.Any(mod => mod.PackageIdPlayerFacing.ToLower() == "Arkymn.SlowerPawnTickRate".ToLower()))
                {
                    harmony.Patch(
                        original: AccessTools.Method(typeof(RimWorld.JoyUtility), "JoyTickCheckEnd"),
                        transpiler: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.JoyTickCheckEnd_Transpiler))
                    );
                }
                else
                {
                    if (LoadedModManager.RunningModsListForReading.Any(mod => mod.PackageIdPlayerFacing.ToLower() == "seobongzu.endlessRecreation".ToLower()))
                    {
                        harmony.Patch(
                            original: AccessTools.Method(typeof(RimWorld.JoyUtility), "JoyTickCheckEnd"),
                            prefix: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.JoyTickCheckEnd_Prefix2))
                        );
                    }
                    else
                    {
                        harmony.Patch(
                            original: AccessTools.Method(typeof(RimWorld.JoyUtility), "JoyTickCheckEnd"),
                            prefix: new HarmonyMethod(typeof(DoLovinWhenIdle_Patches), nameof(DoLovinWhenIdle_Patches.JoyTickCheckEnd_Prefix))
                        );
                    }
                }
            }
        }
        private static void TryPatch(Harmony harmony, string modId, string targetMethodString, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null, HarmonyMethod finalizer = null)
        {
            if (!LoadedModManager.RunningModsListForReading.Any(mod => mod.PackageIdPlayerFacing.ToLower() == modId.ToLower()))
            {
                return;
            }
            if (prefix == null && postfix == null && transpiler == null && finalizer == null)
            {
                Log.Warning($"[Do Lovin' When Idle] Patch attempt on '{targetMethodString}' was called without any patch methods (prefix, postfix, etc.).");
                return;
            }
            try
            {
                var methodToPatch = AccessTools.Method(targetMethodString);
                if (methodToPatch == null)
                {
                    Log.Warning($"[Do Lovin' When Idle] Could not find method '{targetMethodString}' for patching. The compatibility patch will not be applied.");
                    return;
                }
                harmony.Patch(
                original: methodToPatch,
                prefix: prefix,
                postfix: postfix,
                transpiler: transpiler,
                finalizer: finalizer);
                Log.Message($"[Do Lovin' When Idle] Successfully applied compatibility patch for '{modId}' on method '{targetMethodString}'.");
            }
            catch (Exception ex)
            {
                Log.Error($"[Do Lovin' When Idle] An unexpected error occurred while patching '{targetMethodString}' for mod '{modId}'. Details: {ex}");
            }
        }
    }
}