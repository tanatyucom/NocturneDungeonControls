using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using MelonLoader;

namespace NocturneModernController
{
    internal static class ExternalInputBridge
    {
        private const string MapName = "NocturneModernController_SDL_v2";
        private const int Magic = 0x4E4D4332;
        private const int StopRequested = 0x53544F50;

        private static MemoryMappedFile? _map;
        private static MemoryMappedViewAccessor? _view;
        private static int _lastSequence;
        private static int _lastX;
        private static int _lastY;
        private static bool _loggedConnected;

        internal static void Start(MelonLogger.Instance logger)
        {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            string path = Path.Combine(
                directory,
                "NocturneModernController.Helper",
                "NocturneModernController.InputHelper.exe");
            if (!File.Exists(path))
            {
                logger.Warning("[NocturneModernController] SDL input helper is missing: " + path);
                return;
            }

            // Explorer starts the helper outside the Steam game process tree. This is
            // Launch outside the game process so SDL can enumerate physical
            // controllers even when Steam Input exposes a virtual device.
            var startInfo = new ProcessStartInfo("explorer.exe", "\"" + path + "\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(startInfo)?.Dispose();
            logger.Msg("[NocturneModernController] External SDL input helper requested through Explorer.");
        }

        internal static bool TryRead(out int x, out int y)
        {
            x = y = 0;
            try
            {
                _map ??= MemoryMappedFile.OpenExisting(MapName);
                _view ??= _map.CreateViewAccessor();
                if (_view.ReadInt32(0) != Magic || _view.ReadInt32(4) == 0)
                {
                    return false;
                }

                int sequence = _view.ReadInt32(16);
                int age = unchecked(Environment.TickCount - _view.ReadInt32(20));
                if (age < 0 || age > 1000)
                {
                    return false;
                }

                if (sequence != _lastSequence)
                {
                    _lastSequence = sequence;
                    _lastX = _view.ReadInt32(8);
                    _lastY = _view.ReadInt32(12);
                }
                x = _lastX;
                y = _lastY;
                if (!_loggedConnected)
                {
                    _loggedConnected = true;
                    MelonLogger.Msg("[NocturneModernController] External SDL gamepad input connected.");
                }
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        internal static void UpdateGameContext(bool explorationActive)
        {
            try
            {
                _map ??= MemoryMappedFile.OpenExisting(MapName);
                _view ??= _map.CreateViewAccessor();
                _view.Write(28, explorationActive ? 1 : 0);
                _view.Write(32, Process.GetCurrentProcess().Id);
            }
            catch (FileNotFoundException)
            {
            }
        }

        internal static void Stop()
        {
            try
            {
                _map ??= MemoryMappedFile.OpenExisting(MapName);
                _view ??= _map.CreateViewAccessor();
                _view.Write(24, StopRequested);
                _view.Write(28, 0);
                _view.Flush();
            }
            catch (FileNotFoundException)
            {
            }
            _view?.Dispose();
            _map?.Dispose();
            _view = null;
            _map = null;
        }
    }
}
