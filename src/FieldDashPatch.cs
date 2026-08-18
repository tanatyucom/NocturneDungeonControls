using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace NocturneDungeonControls
{
    [HarmonyPatch(typeof(fldPlayer), nameof(fldPlayer.fldPlayerCalc))]
    internal static class FieldDashPatch
    {
        private const int VirtualKeyDash = 0x50; // P
        private const float Multiplier = 1.60f;
        private const int NormalSpeedRva = 0x02AF1FF8;
        private const int AlternateSpeedRva = 0x028C7528;
        private const float ExpectedNormalSpeed = 29f;
        private const float ExpectedAlternateSpeed = 20f;
        private const uint PageExecuteReadWrite = 0x40;

        private static IntPtr _normalSpeedAddress;
        private static IntPtr _alternateSpeedAddress;
        private static bool _addressesValidated;
        private static bool _patchActive;
        private static bool _loggedHeld;
        private static bool _loggedUnsupported;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(
            IntPtr address,
            UIntPtr size,
            uint newProtection,
            out uint oldProtection);

        private static bool DashHeld => (GetAsyncKeyState(VirtualKeyDash) & 0x8000) != 0;

        private static void Prefix()
        {
            RestoreSpeeds();
            if (!DashHeld || !IsSafeNormalMovement())
            {
                LogRelease();
                return;
            }

            if (!ValidateAddresses())
            {
                return;
            }

            WriteFloat(_normalSpeedAddress, ExpectedNormalSpeed * Multiplier);
            WriteFloat(_alternateSpeedAddress, ExpectedAlternateSpeed * Multiplier);
            _patchActive = true;
            if (!_loggedHeld)
            {
                _loggedHeld = true;
                MelonLogger.Msg("[NocturneDungeonControls] Dash ON (P, native speed x1.60)");
            }
        }

        private static void Postfix()
        {
            RestoreSpeeds();
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            RestoreSpeeds();
            return __exception;
        }

        private static bool ValidateAddresses()
        {
            if (_addressesValidated)
            {
                return true;
            }

            IntPtr moduleBase = IntPtr.Zero;
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                if (string.Equals(
                    Path.GetFileName(module.FileName),
                    "GameAssembly.dll",
                    StringComparison.OrdinalIgnoreCase))
                {
                    moduleBase = module.BaseAddress;
                    break;
                }
            }
            if (moduleBase == IntPtr.Zero)
            {
                return false;
            }

            _normalSpeedAddress = IntPtr.Add(moduleBase, NormalSpeedRva);
            _alternateSpeedAddress = IntPtr.Add(moduleBase, AlternateSpeedRva);
            float normal = ReadFloat(_normalSpeedAddress);
            float alternate = ReadFloat(_alternateSpeedAddress);
            if (Math.Abs(normal - ExpectedNormalSpeed) > 0.001f ||
                Math.Abs(alternate - ExpectedAlternateSpeed) > 0.001f)
            {
                if (!_loggedUnsupported)
                {
                    _loggedUnsupported = true;
                    MelonLogger.Warning(
                        $"[NocturneDungeonControls] Unsupported game constants; dash disabled " +
                        $"(normal={normal}, alternate={alternate}).");
                }
                return false;
            }

            _addressesValidated = true;
            MelonLogger.Msg("[NocturneDungeonControls] Native movement speed constants validated (29/20).");
            return true;
        }

        private static float ReadFloat(IntPtr address)
        {
            return BitConverter.Int32BitsToSingle(Marshal.ReadInt32(address));
        }

        private static void WriteFloat(IntPtr address, float value)
        {
            if (!VirtualProtect(address, (UIntPtr)4, PageExecuteReadWrite, out uint oldProtection))
            {
                throw new InvalidOperationException("VirtualProtect failed: " + Marshal.GetLastWin32Error());
            }

            Marshal.WriteInt32(address, BitConverter.SingleToInt32Bits(value));
            VirtualProtect(address, (UIntPtr)4, oldProtection, out _);
        }

        private static void RestoreSpeeds()
        {
            if (!_patchActive)
            {
                return;
            }

            WriteFloat(_normalSpeedAddress, ExpectedNormalSpeed);
            WriteFloat(_alternateSpeedAddress, ExpectedAlternateSpeed);
            _patchActive = false;
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
