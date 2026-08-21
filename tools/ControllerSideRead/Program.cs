using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Windows.Gaming.Input;

namespace NocturneModernController.ControllerProbe
{
    internal static class Program
    {
        private const int DefaultSeconds = 30;
        private const int DefaultIntervalMs = 250;

        private static int Main(string[] args)
        {
            Options options;
            try
            {
                options = Options.Parse(args);
            }
            catch (ArgumentException exception)
            {
                Console.Error.WriteLine(exception.Message);
                PrintUsage();
                return 2;
            }

            bool sdlInitialized = false;
            if (options.Provider == "sdl")
            {
                sdlInitialized = SdlNative.SDL_Init(SdlNative.InitGamepad);
                if (!sdlInitialized)
                {
                    Console.Error.WriteLine("SDL_Init failed: " + SdlNative.GetError());
                    return 3;
                }
            }

            using TextWriter writer = options.OutputPath == null
                ? Console.Out
                : new StreamWriter(options.OutputPath, append: false);

            DateTimeOffset started = DateTimeOffset.UtcNow;
            writer.WriteLine(JsonSerializer.Serialize(new
            {
                type = "session",
                provider = options.Provider == "sdl" ? "SDL3" : "Windows.Gaming.Input",
                condition = options.Condition,
                startedUtc = started,
                durationSeconds = options.Seconds,
                intervalMs = options.IntervalMs,
                processId = Environment.ProcessId,
                note = "Read-only probe; no virtual controller, hiding, suppression, or game modification."
            }));
            writer.Flush();

            DateTimeOffset deadline = started.AddSeconds(options.Seconds);
            int sample = 0;
            while (DateTimeOffset.UtcNow < deadline)
            {
                if (options.Provider == "sdl")
                {
                    WriteSdlSnapshot(writer, options.Condition, sample++);
                }
                else
                {
                    WriteWgiSnapshot(writer, options.Condition, sample++);
                }
                writer.Flush();
                Thread.Sleep(options.IntervalMs);
            }

            // SDL_Quit can block indefinitely in the target reWASD + Steam
            // environment. Process teardown safely releases this read-only
            // diagnostic singleton without delaying log completion.

            return 0;
        }

        private static void WriteWgiSnapshot(TextWriter writer, string condition, int sample)
        {
            IReadOnlyList<RawGameController> rawControllers = RawGameController.RawGameControllers;
            IReadOnlyList<Gamepad> gamepads = Gamepad.Gamepads;
            var candidates = new List<object>();
            var mappedGamepads = new HashSet<Gamepad>();

            for (int index = 0; index < rawControllers.Count; index++)
            {
                RawGameController raw = rawControllers[index];
                Gamepad? gamepad = Gamepad.FromGameController(raw);
                if (gamepad != null)
                {
                    mappedGamepads.Add(gamepad);
                }

                bool[] buttons = new bool[raw.ButtonCount];
                GameControllerSwitchPosition[] switches =
                    new GameControllerSwitchPosition[raw.SwitchCount];
                double[] axes = new double[raw.AxisCount];
                ulong timestamp = raw.GetCurrentReading(buttons, switches, axes);

                candidates.Add(new
                {
                    candidate = $"raw:{index}",
                    api = "WGI.RawGameController",
                    connected = true,
                    name = raw.DisplayName,
                    vid = $"0x{raw.HardwareVendorId:X4}",
                    pid = $"0x{raw.HardwareProductId:X4}",
                    raw.AxisCount,
                    raw.ButtonCount,
                    raw.SwitchCount,
                    timestamp,
                    axes,
                    pressedButtons = buttons
                        .Select((pressed, button) => new { pressed, button })
                        .Where(value => value.pressed)
                        .Select(value => value.button)
                        .ToArray(),
                    switches = switches.Select(value => value.ToString()).ToArray(),
                    gamepad = gamepad == null ? null : ReadGamepad(gamepad)
                });
            }

            for (int index = 0; index < gamepads.Count; index++)
            {
                Gamepad gamepad = gamepads[index];
                if (mappedGamepads.Contains(gamepad))
                {
                    continue;
                }

                candidates.Add(new
                {
                    candidate = $"gamepad-unmatched:{index}",
                    api = "WGI.Gamepad",
                    connected = true,
                    name = (string?)null,
                    vid = (string?)null,
                    pid = (string?)null,
                    gamepad = ReadGamepad(gamepad)
                });
            }

            writer.WriteLine(JsonSerializer.Serialize(new
            {
                type = "snapshot",
                provider = "Windows.Gaming.Input",
                condition,
                sample,
                timestampUtc = DateTimeOffset.UtcNow,
                rawControllerCount = rawControllers.Count,
                gamepadCount = gamepads.Count,
                candidates
            }));
        }

        private static void WriteSdlSnapshot(TextWriter writer, string condition, int sample)
        {
            SdlNative.SDL_UpdateJoysticks();
            IntPtr idsPointer = SdlNative.SDL_GetJoysticks(out int joystickCount);
            var candidates = new List<object>();
            try
            {
                for (int index = 0; index < joystickCount; index++)
                {
                    uint id = unchecked((uint)System.Runtime.InteropServices.Marshal.ReadInt32(
                        idsPointer,
                        index * sizeof(uint)));
                    IntPtr joystick = SdlNative.SDL_OpenJoystick(id);
                    if (joystick == IntPtr.Zero)
                    {
                        candidates.Add(new
                        {
                            candidate = $"sdl:{index}",
                            api = "SDL3",
                            connected = false,
                            instanceId = id,
                            error = SdlNative.GetError()
                        });
                        continue;
                    }

                    try
                    {
                        int axisCount = SdlNative.SDL_GetNumJoystickAxes(joystick);
                        int buttonCount = SdlNative.SDL_GetNumJoystickButtons(joystick);
                        short[] axes = Enumerable.Range(0, Math.Max(axisCount, 0))
                            .Select(axis => SdlNative.SDL_GetJoystickAxis(joystick, axis))
                            .ToArray();
                        int[] pressedButtons = Enumerable.Range(0, Math.Max(buttonCount, 0))
                            .Where(button => SdlNative.SDL_GetJoystickButton(joystick, button))
                            .ToArray();

                        bool isGamepad = SdlNative.SDL_IsGamepad(id);
                        IntPtr gamepad = isGamepad ? SdlNative.SDL_OpenGamepad(id) : IntPtr.Zero;
                        object? standardized = null;
                        if (gamepad != IntPtr.Zero)
                        {
                            try
                            {
                                standardized = new
                                {
                                    leftStickX = SdlNative.SDL_GetGamepadAxis(gamepad, 0),
                                    leftStickY = SdlNative.SDL_GetGamepadAxis(gamepad, 1),
                                    rightStickX = SdlNative.SDL_GetGamepadAxis(gamepad, 2),
                                    rightStickY = SdlNative.SDL_GetGamepadAxis(gamepad, 3),
                                    leftTrigger = SdlNative.SDL_GetGamepadAxis(gamepad, 4),
                                    rightTrigger = SdlNative.SDL_GetGamepadAxis(gamepad, 5),
                                    pressedButtons = Enumerable.Range(0, SdlNative.GamepadButtonCount)
                                        .Where(button => SdlNative.SDL_GetGamepadButton(gamepad, button))
                                        .ToArray()
                                };
                            }
                            finally
                            {
                                SdlNative.SDL_CloseGamepad(gamepad);
                            }
                        }

                        candidates.Add(new
                        {
                            candidate = $"sdl:{index}",
                            api = "SDL3",
                            connected = true,
                            instanceId = id,
                            name = SdlNative.GetUtf8(SdlNative.SDL_GetJoystickNameForID(id)),
                            path = SdlNative.GetUtf8(SdlNative.SDL_GetJoystickPathForID(id)),
                            vid = $"0x{SdlNative.SDL_GetJoystickVendorForID(id):X4}",
                            pid = $"0x{SdlNative.SDL_GetJoystickProductForID(id):X4}",
                            axisCount,
                            buttonCount,
                            axes,
                            pressedButtons,
                            isGamepad,
                            gamepad = standardized
                        });
                    }
                    finally
                    {
                        SdlNative.SDL_CloseJoystick(joystick);
                    }
                }
            }
            finally
            {
                if (idsPointer != IntPtr.Zero)
                {
                    SdlNative.SDL_free(idsPointer);
                }
            }

            writer.WriteLine(JsonSerializer.Serialize(new
            {
                type = "snapshot",
                provider = "SDL3",
                condition,
                sample,
                timestampUtc = DateTimeOffset.UtcNow,
                joystickCount,
                candidates
            }));
        }

        private static object ReadGamepad(Gamepad gamepad)
        {
            GamepadReading reading = gamepad.GetCurrentReading();
            return new
            {
                reading.Timestamp,
                leftStickX = reading.LeftThumbstickX,
                leftStickY = reading.LeftThumbstickY,
                rightStickX = reading.RightThumbstickX,
                rightStickY = reading.RightThumbstickY,
                leftTrigger = reading.LeftTrigger,
                rightTrigger = reading.RightTrigger,
                buttons = reading.Buttons.ToString()
            };
        }

        private static void PrintUsage()
        {
            Console.Error.WriteLine(
                "Usage: ControllerProbe --provider wgi|sdl --condition A|B|C|D " +
                "[--seconds 30] [--interval-ms 250] [--output path.jsonl]");
        }

        private sealed class Options
        {
            internal string Condition { get; private set; } = string.Empty;
            internal string Provider { get; private set; } = "wgi";
            internal int Seconds { get; private set; } = DefaultSeconds;
            internal int IntervalMs { get; private set; } = DefaultIntervalMs;
            internal string? OutputPath { get; private set; }

            internal static Options Parse(string[] args)
            {
                var result = new Options();
                for (int index = 0; index < args.Length; index++)
                {
                    string option = args[index];
                    string NextValue()
                    {
                        if (++index >= args.Length)
                        {
                            throw new ArgumentException("Missing value after " + option);
                        }
                        return args[index];
                    }

                    switch (option)
                    {
                        case "--provider":
                            result.Provider = NextValue().ToLowerInvariant();
                            break;
                        case "--condition":
                            result.Condition = NextValue().ToUpperInvariant();
                            break;
                        case "--seconds":
                            result.Seconds = ParsePositiveInt(NextValue(), option);
                            break;
                        case "--interval-ms":
                            result.IntervalMs = ParsePositiveInt(NextValue(), option);
                            break;
                        case "--output":
                            result.OutputPath = NextValue();
                            break;
                        default:
                            throw new ArgumentException("Unknown argument: " + option);
                    }
                }

                if (result.Condition != "A" && result.Condition != "B" &&
                    result.Condition != "C" && result.Condition != "D")
                {
                    throw new ArgumentException("--condition must be A, B, C, or D.");
                }
                if (result.Provider != "wgi" && result.Provider != "sdl")
                {
                    throw new ArgumentException("--provider must be wgi or sdl.");
                }
                return result;
            }

            private static int ParsePositiveInt(string text, string option)
            {
                if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value) ||
                    value <= 0)
                {
                    throw new ArgumentException(option + " must be a positive integer.");
                }
                return value;
            }
        }
    }
}
