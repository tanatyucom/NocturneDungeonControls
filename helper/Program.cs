using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using HidSharp;
using Windows.Gaming.Input;

internal static class Program
{
    private const string MapName = "NocturneDungeonControls_XInput_v1";
    private const int Magic = 0x4E444331;
    private static readonly object HidLock = new object();
    private static readonly byte[] HidReport = new byte[64];
    private static int _hidLength;
    private static int _hidSequence;
    private static int _hidDeviceCount;
    private static int _hidOpenCount;
    private static int _gamingInputCount;
    private static int _gamingLeftX;
    private static int _gamingLeftY;
    private static int _gamingButtons;
    private static int _gamingTriggers;

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
    private static extern uint GetState14(uint index, out State state);

    [DllImport("xinput1_3.dll", EntryPoint = "XInputGetState")]
    private static extern uint GetState13(uint index, out State state);

    [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
    private static extern uint GetState910(uint index, out State state);

    private static void Main()
    {
        var hidThread = new Thread(ReadXboxHid) { IsBackground = true };
        hidThread.Start();

        using MemoryMappedFile map = MemoryMappedFile.CreateOrOpen(MapName, 128);
        using MemoryMappedViewAccessor view = map.CreateViewAccessor();
        while (true)
        {
            bool connected = TryReadGamingInput(out int rx, out int ry);
            int api;
            int user;
            State state = default;
            if (connected)
            {
                api = 3;
                user = 0;
            }
            else
            {
                connected = TryRead(out api, out user, out state);
                rx = connected ? state.Gamepad.ThumbRX : 0;
                ry = connected ? state.Gamepad.ThumbRY : 0;
            }
            view.Write(4, connected ? 1 : 0);
            view.Write(8, rx);
            view.Write(12, ry);
            view.Write(16, connected ? api : -1);
            view.Write(20, connected ? user : -1);
            lock (HidLock)
            {
                view.Write(24, _hidLength);
                view.Write(28, _hidSequence);
                view.WriteArray(32, HidReport, 0, HidReport.Length);
                view.Write(96, _hidDeviceCount);
                view.Write(100, _hidOpenCount);
                view.Write(104, _gamingInputCount);
                view.Write(108, _gamingLeftX);
                view.Write(112, _gamingLeftY);
                view.Write(116, _gamingButtons);
                view.Write(120, _gamingTriggers);
            }
            view.Write(0, Magic);
            Thread.Sleep(8);
        }
    }

    private static bool TryReadGamingInput(out int x, out int y)
    {
        _gamingInputCount = Windows.Gaming.Input.Gamepad.Gamepads.Count;
        if (_gamingInputCount == 0)
        {
            x = y = 0;
            return false;
        }

        GamepadReading reading = Windows.Gaming.Input.Gamepad.Gamepads[0].GetCurrentReading();
        _gamingLeftX = (int)(reading.LeftThumbstickX * 32767.0);
        _gamingLeftY = (int)(reading.LeftThumbstickY * 32767.0);
        _gamingButtons = unchecked((int)reading.Buttons);
        _gamingTriggers = ((int)(reading.LeftTrigger * 255.0) & 0xFF) |
            (((int)(reading.RightTrigger * 255.0) & 0xFF) << 8);
        x = (int)(reading.RightThumbstickX * 32767.0);
        y = (int)(reading.RightThumbstickY * 32767.0);
        return true;
    }

    private static void ReadXboxHid()
    {
        while (true)
        {
            HidDevice[] devices = new System.Collections.Generic.List<HidDevice>(
                DeviceList.Local.GetHidDevices(0x045E)).ToArray();
            _hidDeviceCount = devices.Length;
            foreach (HidDevice device in devices)
            {
                if (!device.TryOpen(out HidStream stream))
                {
                    continue;
                }

                _hidOpenCount++;

                using (stream)
                {
                    stream.ReadTimeout = 250;
                    byte[] buffer = new byte[device.GetMaxInputReportLength()];
                    while (true)
                    {
                        try
                        {
                            int length = stream.Read(buffer, 0, buffer.Length);
                            lock (HidLock)
                            {
                                _hidLength = Math.Min(length, HidReport.Length);
                                Array.Clear(HidReport, 0, HidReport.Length);
                                Array.Copy(buffer, HidReport, _hidLength);
                                _hidSequence++;
                            }
                        }
                        catch (TimeoutException)
                        {
                            continue;
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
            }

            Thread.Sleep(1000);
        }
    }

    private static bool TryRead(out int apiFound, out int userFound, out State found)
    {
        for (int api = 0; api < 3; api++)
        {
            for (uint user = 0; user < 4; user++)
            {
                uint result = api == 0
                    ? GetState14(user, out State state)
                    : api == 1
                        ? GetState13(user, out state)
                        : GetState910(user, out state);
                if (result == 0)
                {
                    apiFound = api;
                    userFound = (int)user;
                    found = state;
                    return true;
                }
            }
        }

        apiFound = -1;
        userFound = -1;
        found = default;
        return false;
    }
}
