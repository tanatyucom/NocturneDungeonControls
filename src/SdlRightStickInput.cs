using MelonLoader;

namespace NocturneModernController
{
    internal static class SdlRightStickInput
    {
        private const int EngageThreshold = 10000;
        private const int ReleaseThreshold = 6000;
        private static TurnState _state;
        private static VerticalState _verticalState;

        internal static void Initialize(MelonLogger.Instance logger)
        {
            ExternalInputBridge.Start(logger);
        }

        internal static void Sample()
        {
            if (!ExternalInputBridge.TryRead(out int x, out int y))
            {
                VanillaTurnInvocationPoc.SetHeldState(
                    left: false, right: false, up: false, down: false);
                return;
            }

            TurnState next = _state switch
            {
                TurnState.Left when x < -ReleaseThreshold => TurnState.Left,
                TurnState.Right when x > ReleaseThreshold => TurnState.Right,
                _ when x < -EngageThreshold => TurnState.Left,
                _ when x > EngageThreshold => TurnState.Right,
                _ => TurnState.Neutral
            };
            VerticalState nextVertical = _verticalState switch
            {
                VerticalState.Up when y < -ReleaseThreshold => VerticalState.Up,
                VerticalState.Down when y > ReleaseThreshold => VerticalState.Down,
                _ when y < -EngageThreshold => VerticalState.Up,
                _ when y > EngageThreshold => VerticalState.Down,
                _ => VerticalState.Neutral
            };
            VanillaTurnInvocationPoc.SetHeldState(
                left: next == TurnState.Left,
                right: next == TurnState.Right,
                up: nextVertical == VerticalState.Up,
                down: nextVertical == VerticalState.Down);

            if (next != _state)
            {
                MelonLogger.Msg($"[NocturneModernController] Q4 right stick {next} (x={x}, engage={EngageThreshold}, release={ReleaseThreshold}).");
                _state = next;
            }
            if (nextVertical != _verticalState)
            {
                MelonLogger.Msg($"[NocturneModernController] Q4 right stick vertical {nextVertical} (y={y}).");
                _verticalState = nextVertical;
            }
        }

        internal static void Shutdown()
        {
            VanillaTurnInvocationPoc.SetHeldState(
                left: false, right: false, up: false, down: false);
            ExternalInputBridge.Stop();
        }

        private enum TurnState
        {
            Neutral,
            Left,
            Right
        }

        private enum VerticalState
        {
            Neutral,
            Up,
            Down
        }
    }
}
