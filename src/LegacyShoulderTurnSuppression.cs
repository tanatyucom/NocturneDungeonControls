using Il2Cpp;
using MelonLoader;

namespace NocturneModernController
{
    internal static class LegacyShoulderTurnSuppression
    {
        private static bool _loggedFieldLeft;
        private static bool _loggedFieldRight;
        private static bool _loggedPuzzleLeft;
        private static bool _loggedPuzzleRight;

        internal static bool IsSuppressedExplorationAction(SIActionName action)
        {
            return action == SIActionName.FD_Turn_Left ||
                   action == SIActionName.FD_Turn_Right ||
                   action == SIActionName.FD_Camera_Left ||
                   action == SIActionName.FD_Camera_Right ||
                   action == SIActionName.PZL_MapRot_Left ||
                   action == SIActionName.PZL_MapRot_Right;
        }

        internal static void LogSuppressed(SIActionName action)
        {
            if (action != SIActionName.FD_Turn_Left &&
                action != SIActionName.FD_Turn_Right &&
                action != SIActionName.PZL_MapRot_Left &&
                action != SIActionName.PZL_MapRot_Right)
            {
                return;
            }

            ref bool logged = ref GetLogFlag(action);
            if (logged)
            {
                return;
            }

            logged = true;
            MelonLogger.Msg(
                $"[NocturneModernController] Q5 legacy shoulder turn route suppressed: {action}.");
        }

        private static ref bool GetLogFlag(SIActionName action)
        {
            if (action == SIActionName.FD_Turn_Left)
            {
                return ref _loggedFieldLeft;
            }
            if (action == SIActionName.FD_Turn_Right)
            {
                return ref _loggedFieldRight;
            }
            if (action == SIActionName.PZL_MapRot_Left)
            {
                return ref _loggedPuzzleLeft;
            }
            return ref _loggedPuzzleRight;
        }
    }
}
