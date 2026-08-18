using System.Runtime.InteropServices;

namespace NocturneDungeonControls
{
    internal static class XInputReader
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Gamepad
        {
            internal ushort Buttons;
            internal byte LeftTrigger;
            internal byte RightTrigger;
            internal short ThumbLX;
            internal short ThumbLY;
            internal short ThumbRX;
            internal short ThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct State
        {
            internal uint PacketNumber;
            internal Gamepad Gamepad;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern uint GetState14(uint userIndex, out State state);

        [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")]
        private static extern uint GetState13(uint userIndex, out State state);

        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        private static extern uint GetState910(uint userIndex, out State state);

        internal static int FindActiveRightStickAllApis(
            out int foundUser,
            out short x,
            out short y)
        {
            for (int api = 0; api < 3; api++)
            {
                for (uint userIndex = 0; userIndex < 4; userIndex++)
                {
                    uint result = api == 0
                        ? GetState14(userIndex, out State state)
                        : api == 1
                            ? GetState13(userIndex, out state)
                            : GetState910(userIndex, out state);
                    if (result == 0 &&
                        (System.Math.Abs((int)state.Gamepad.ThumbRX) >= 5000 ||
                         System.Math.Abs((int)state.Gamepad.ThumbRY) >= 5000))
                    {
                        foundUser = (int)userIndex;
                        x = state.Gamepad.ThumbRX;
                        y = state.Gamepad.ThumbRY;
                        return api;
                    }
                }
            }

            foundUser = -1;
            x = 0;
            y = 0;
            return -1;
        }
    }
}
