using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace NocturneDungeonControls
{
    /// <summary>
    /// Read-only telemetry for identifying the camera state changed by LB/RB.
    /// </summary>
    [HarmonyPatch(typeof(fldCamera), nameof(fldCamera.fldCamMain))]
    internal static class FieldCameraMainProbe
    {
        private static bool _logged;

        private static int _frame;

        private static void Prefix()
        {
            if (_logged)
            {
                return;
            }

            _logged = true;
            MelonLogger.Msg("[NocturneDungeonControls] EXTERNAL XINPUT probe active. Move Right Stick left/right/up/down.");
        }

        private static void Postfix()
        {
            _frame++;
            if ((_frame % 6) != 0)
            {
                return;
            }

            if (ExternalInputBridge.TryRead(
                out int x,
                out int y,
                out int api,
                out int user) &&
                (System.Math.Abs(x) >= 5000 || System.Math.Abs(y) >= 5000))
            {
                MelonLogger.Msg(
                    $"[NocturneDungeonControls] EXTERNAL_RSTICK api={api} " +
                    $"user={user} x={x} y={y}");
            }

        }
    }
}
