using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace NocturneModernController
{
    [HarmonyPatch(typeof(fldCamera), nameof(fldCamera.fldCamMain))]
    internal static class VerticalCameraStateProbe
    {
        private static int _lastLogTick;
        private static int _lastMoveUd = int.MinValue;
        private static float _lastCameraMoveUd = float.NaN;
        private static int _lastDirection = int.MinValue;
        private static int _lastInputRead = int.MinValue;

        private static void Postfix()
        {
            if (!ExternalInputBridge.TryRead(out _, out int y))
            {
                return;
            }

            int moveUd = fldCamera.mMoveUD;
            float cameraMoveUd = fldCamera.CameraMoveUD;
            int direction = fldCamera.mDirection;
            int inputRead = fldCamera.calcCamNormal_inp_rd;
            bool changed = moveUd != _lastMoveUd ||
                           Math.Abs(cameraMoveUd - _lastCameraMoveUd) > 0.0001f ||
                           direction != _lastDirection ||
                           inputRead != _lastInputRead;
            int now = Environment.TickCount;
            if (changed && unchecked(now - _lastLogTick) >= 100)
            {
                _lastLogTick = now;
                MelonLogger.Msg(
                    $"[NocturneModernController] CAMERA-UD y={y} " +
                    $"mMoveUD={moveUd} CameraMoveUD={cameraMoveUd:F4} " +
                    $"mMoveLR={fldCamera.mMoveLR} CameraMoveLR={fldCamera.CameraMoveLR:F4} " +
                    $"dir={direction} moveDir={fldCamera.CameraMoveDir:F4} inp={inputRead}");
            }
            _lastMoveUd = moveUd;
            _lastCameraMoveUd = cameraMoveUd;
            _lastDirection = direction;
            _lastInputRead = inputRead;
        }
    }
}
