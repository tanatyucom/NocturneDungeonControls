using System;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace NocturneDungeonControls
{
    [HarmonyPatch(typeof(fldPlayer), nameof(fldPlayer.fldPlayerCalcForUnity))]
    internal static class FieldDashPatch
    {
        private const int VirtualKeyF15 = 0x7E;
        private const float Multiplier = 1.35f;
        private const float MaximumNormalFrameDistance = 0.35f;

        private static Vector3 _before;
        private static bool _sampleValid;
        private static bool _loggedHeld;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        private static bool DashHeld => (GetAsyncKeyState(VirtualKeyF15) & 0x8000) != 0;

        private static void Prefix()
        {
            _sampleValid = false;
            if (!DashHeld || !IsSafeNormalMovement())
            {
                LogRelease();
                return;
            }

            GameObject player = fldPlayer.fldPlayerObj;
            if (player == null)
            {
                return;
            }

            _before = player.transform.position;
            _sampleValid = true;
            if (!_loggedHeld)
            {
                _loggedHeld = true;
                MelonLogger.Msg("[NocturneDungeonControls] Dash ON (F15, x1.35)");
            }
        }

        private static void Postfix()
        {
            if (!_sampleValid || !DashHeld || !IsSafeNormalMovement())
            {
                return;
            }

            GameObject player = fldPlayer.fldPlayerObj;
            if (player == null)
            {
                return;
            }

            Transform transform = player.transform;
            Vector3 after = transform.position;
            Vector3 delta = after - _before;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance <= 0.00001f || distance > MaximumNormalFrameDistance)
            {
                return;
            }

            transform.position = after + delta * (Multiplier - 1f);
        }

        private static bool IsSafeNormalMovement()
        {
            try
            {
                return fldPlayer.playerRun &&
                    fldPlayer.gfldPlayerHasiCnt == 0 &&
                    fldPlayer.gfldPlayerAnaCnt == 0 &&
                    fldPlayer.gfldPlayerDamegeCnt == 0 &&
                    !fldPlayer.bRestoreMode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void LogRelease()
        {
            if (!_loggedHeld)
            {
                return;
            }

            _loggedHeld = false;
            MelonLogger.Msg("[NocturneDungeonControls] Dash OFF");
        }
    }
}
