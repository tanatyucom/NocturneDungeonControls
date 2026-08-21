using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace NocturneModernController
{
    /// <summary>
    /// Q3 temporary input source. F6/F7 deliberately remain separate from the
    /// SDL right-stick reader so native turn invocation can be proven first.
    /// </summary>
    internal static class VanillaTurnInvocationPoc
    {
        private static bool _leftHeld;
        private static bool _rightHeld;
        private static bool _loggedLogicalLeft;
        private static bool _loggedLogicalRight;

        internal static bool LeftHeld => _leftHeld && !_rightHeld;
        internal static bool RightHeld => _rightHeld && !_leftHeld;

        internal static void SampleTemporaryInput()
        {
            bool left = Input.GetKey(KeyCode.F6);
            bool right = Input.GetKey(KeyCode.F7);

            if (left != _leftHeld)
            {
                MelonLogger.Msg($"[NocturneModernController] Q3 TurnLeft {(left ? "ON" : "OFF")} (temporary F6)");
            }

            if (right != _rightHeld)
            {
                MelonLogger.Msg($"[NocturneModernController] Q3 TurnRight {(right ? "ON" : "OFF")} (temporary F7)");
            }

            _leftHeld = left;
            _rightHeld = right;
        }

        internal static void LogLogicalInjection(bool left, SIActionName action)
        {
            if (left ? _loggedLogicalLeft : _loggedLogicalRight)
            {
                return;
            }

            if (left)
            {
                _loggedLogicalLeft = true;
            }
            else
            {
                _loggedLogicalRight = true;
            }

            MelonLogger.Msg($"[NocturneModernController] Q3 logical {action} injection observed.");
        }
    }

    [HarmonyPatch(
        typeof(SteamInputAssign),
        nameof(SteamInputAssign.padcheck),
        new Type[] { typeof(int), typeof(SIActionName), typeof(SIPressType) })]
    internal static class PuzzleLogicalTurnPatch
    {
        private static void Postfix(
            int __0,
            SIActionName __1,
            SIPressType __2,
            ref bool __result)
        {
            if (__result || __0 != 0 || __2 != SIPressType.DOWN)
            {
                return;
            }

            if (__1 == SIActionName.FD_Turn_Left ||
                __1 == SIActionName.PZL_MapRot_Left)
            {
                __result = VanillaTurnInvocationPoc.LeftHeld;
                if (__result)
                {
                    VanillaTurnInvocationPoc.LogLogicalInjection(left: true, action: __1);
                }
            }
            else if (__1 == SIActionName.FD_Turn_Right ||
                     __1 == SIActionName.PZL_MapRot_Right)
            {
                __result = VanillaTurnInvocationPoc.RightHeld;
                if (__result)
                {
                    VanillaTurnInvocationPoc.LogLogicalInjection(left: false, action: __1);
                }
            }
        }
    }
}
