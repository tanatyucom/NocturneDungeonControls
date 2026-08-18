using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace NocturneDungeonControls
{
    /// <summary>
    /// Adds Right Stick X as a digital binding for the game's existing
    /// FIELD/DUNGEON turn and PUZZLE map-rotation actions.
    /// Existing bindings remain intact; BATTLE actions are not handled.
    /// </summary>
    internal static class RightStickTurnInput
    {
        private const int PadNumber = 0;
        private const int RightStick = 1;
        private const int XAxis = 0;
        private const int CipNumber = 1;
        private const int Center = 128;

        private static TurnState _loggedState;
        private static bool _hasLoggedState;
        private static volatile int _sampledState;
        private static bool _inputReady;
        private static int _retryFrames;
        private static bool _loggedWaiting;

        internal static bool LeftHeld => _sampledState == (int)TurnState.Left;
        internal static bool RightHeld => _sampledState == (int)TurnState.Right;

        internal static void TrySample()
        {
            if (_retryFrames > 0)
            {
                _retryFrames--;
                return;
            }

            byte raw;
            try
            {
                raw = dds3PadManager.GetPadAnalog(
                    PadNumber,
                    RightStick,
                    XAxis,
                    CipNumber);
            }
            catch (Exception exception)
            {
                _sampledState = (int)TurnState.Neutral;
                _retryFrames = 120;
                if (!_loggedWaiting)
                {
                    MelonLogger.Msg(
                        "[NocturneDungeonControls] Pad input is not ready; waiting without retry spam (" +
                        exception.GetType().Name + ").");
                    _loggedWaiting = true;
                }
                return;
            }

            int deadzone;
            try
            {
                deadzone = dds3ConfigGamePadSteam.GetAnalogAdjust();
            }
            catch (Exception)
            {
                _sampledState = (int)TurnState.Neutral;
                _retryFrames = 120;
                return;
            }
            if (!_inputReady)
            {
                // A startup value of 0 was observed before the pad manager was
                // usable. Do not arm until the stick has first been seen near
                // its real center, preventing a false permanent left input.
                int armWindow = Math.Max(24, Math.Min(deadzone, 56));
                if (Math.Abs(raw - Center) > armWindow)
                {
                    _sampledState = (int)TurnState.Neutral;
                    _retryFrames = 30;
                    return;
                }

                _inputReady = true;
                MelonLogger.Msg($"[NocturneDungeonControls] Pad input ready (center sample={raw}, deadzone={deadzone}).");
            }

            int leftThreshold = Center - deadzone;
            int rightThreshold = Center + deadzone;

            TurnState state = raw < leftThreshold
                ? TurnState.Left
                : raw > rightThreshold
                    ? TurnState.Right
                    : TurnState.Neutral;

            _sampledState = (int)state;
            LogTransition(state, raw, deadzone);
        }

        internal static bool IsHeld(SIActionName action)
        {
            TurnState state = (TurnState)_sampledState;
            return action switch
            {
                SIActionName.FD_Turn_Left => state == TurnState.Left,
                SIActionName.PZL_MapRot_Left => state == TurnState.Left,
                SIActionName.FD_Turn_Right => state == TurnState.Right,
                SIActionName.PZL_MapRot_Right => state == TurnState.Right,
                _ => false
            };
        }

        internal static bool IsHeldForPadMap(Il2Cpplibsdf_H.SDF_PADMAP map)
        {
            return map switch
            {
                Il2Cpplibsdf_H.SDF_PADMAP.SDF_PADMAP_L1 => LeftHeld,
                Il2Cpplibsdf_H.SDF_PADMAP.SDF_PADMAP_R1 => RightHeld,
                _ => false
            };
        }

        private static void LogTransition(TurnState state, byte raw, int deadzone)
        {
            if (_hasLoggedState && state == _loggedState)
            {
                return;
            }

            if (_hasLoggedState)
            {
                if (_loggedState == TurnState.Left)
                {
                    MelonLogger.Msg("[NocturneDungeonControls] TurnLeft OFF");
                }
                else if (_loggedState == TurnState.Right)
                {
                    MelonLogger.Msg("[NocturneDungeonControls] TurnRight OFF");
                }
            }

            if (state == TurnState.Left)
            {
                MelonLogger.Msg($"[NocturneDungeonControls] TurnLeft ON (raw={raw}, deadzone={deadzone})");
            }
            else if (state == TurnState.Right)
            {
                MelonLogger.Msg($"[NocturneDungeonControls] TurnRight ON (raw={raw}, deadzone={deadzone})");
            }

            _loggedState = state;
            _hasLoggedState = true;
        }

        private enum TurnState
        {
            Neutral,
            Left,
            Right
        }
    }

    [HarmonyPatch(
        typeof(SteamInputAssign),
        nameof(SteamInputAssign.padcheck),
        new Type[] { typeof(int), typeof(SIActionName), typeof(SIPressType) })]
    internal static class FieldTurnPadCheckPatch
    {
        private static void Postfix(
            SIActionName __1,
            SIPressType __2,
            ref bool __result)
        {
            if (__result || __2 != SIPressType.DOWN)
            {
                return;
            }

            // FIELD/DUNGEON does not use this path. Keep this logical-action
            // augmentation only for the separately mapped puzzle actions.
            if (__1 != SIActionName.PZL_MapRot_Left &&
                __1 != SIActionName.PZL_MapRot_Right)
            {
                return;
            }

            try
            {
                __result = RightStickTurnInput.IsHeld(__1);
            }
            catch (Exception exception)
            {
                MelonLogger.Warning(
                    "[NocturneDungeonControls] Right-stick turn check failed: " +
                    exception.GetType().Name + ": " + exception.Message);
            }
        }
    }
}
