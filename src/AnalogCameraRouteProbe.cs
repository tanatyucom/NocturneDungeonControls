using HarmonyLib;
using Il2Cpp;

namespace NocturneModernController
{
    [HarmonyPatch(typeof(dds3PadManager), nameof(dds3PadManager.GetPadAnalog))]
    internal static class NativeRightStickVerticalPatch
    {
        private static void Postfix(
            int __0,
            int __1,
            int __2,
            int __3,
            ref byte __result)
        {
            // The game's native dungeon camera consumes the right-stick Y axis
            // through this exact channel. Steam recording happened to expose
            // the physical value here; provide the same value from our SDL
            // sampler so the native spherical orbit, limits and smoothing run.
            if (FieldDashPatch.IsExplorationActive &&
                __0 == 0 && __1 == 1 && __2 == 1 && __3 == 1)
            {
                if (VanillaTurnInvocationPoc.UpHeld)
                {
                    __result = byte.MaxValue;
                }
                else if (VanillaTurnInvocationPoc.DownHeld)
                {
                    __result = byte.MinValue;
                }
            }

        }
    }
}
