using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Text;
using MelonLoader;

namespace NocturneModernController
{
    internal static class ExternalInputBridge
    {
        private const string MapName = "NocturneModernController_XInput_v1";
        private const int Magic = 0x4E444331;
        private static Process? _helper;
        private static MemoryMappedFile? _map;
        private static MemoryMappedViewAccessor? _view;
        private static int _lastHidSequence;
        private static bool _loggedHidStatus;

        internal static void Start(MelonLogger.Instance logger)
        {
            string? directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(
                directory ?? string.Empty,
                "NocturneModernController.Helper",
                "NocturneModernController.InputHelper.exe");
            if (!File.Exists(path))
            {
                logger.Warning("Input helper is missing: " + path);
                return;
            }

            _helper = Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            logger.Msg("[NocturneModernController] External XInput helper started.");
        }

        internal static bool TryReadHidStatus(out int devices, out int opened, out int gamingInput)
        {
            devices = opened = gamingInput = 0;
            if (_loggedHidStatus)
            {
                return false;
            }
            try
            {
                _map ??= MemoryMappedFile.OpenExisting(MapName);
                _view ??= _map.CreateViewAccessor();
                devices = _view.ReadInt32(96);
                opened = _view.ReadInt32(100);
                gamingInput = _view.ReadInt32(104);
                _loggedHidStatus = true;
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        internal static bool TryReadGamingDetails(
            out int leftX, out int leftY, out int buttons, out int triggers)
        {
            leftX = leftY = buttons = triggers = 0;
            try
            {
                _map ??= MemoryMappedFile.OpenExisting(MapName);
                _view ??= _map.CreateViewAccessor();
                if (_view.ReadInt32(0) != Magic)
                {
                    return false;
                }

                leftX = _view.ReadInt32(108);
                leftY = _view.ReadInt32(112);
                buttons = _view.ReadInt32(116);
                triggers = _view.ReadInt32(120);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryReadChangedHidReport(out string report)
        {
            report = string.Empty;
            try
            {
                _map ??= MemoryMappedFile.OpenExisting(MapName);
                _view ??= _map.CreateViewAccessor();
                int length = _view.ReadInt32(24);
                int sequence = _view.ReadInt32(28);
                if (length <= 0 || sequence == _lastHidSequence)
                {
                    return false;
                }

                _lastHidSequence = sequence;
                length = Math.Min(length, 64);
                byte[] bytes = new byte[length];
                _view.ReadArray(32, bytes, 0, length);
                var text = new StringBuilder(length * 3);
                for (int index = 0; index < length; index++)
                {
                    if (index > 0) text.Append(' ');
                    text.Append(bytes[index].ToString("X2"));
                }
                report = text.ToString();
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        internal static bool TryRead(out int x, out int y, out int api, out int user)
        {
            x = y = 0;
            api = user = -1;
            try
            {
                _map ??= MemoryMappedFile.OpenExisting(MapName);
                _view ??= _map.CreateViewAccessor();
                if (_view.ReadInt32(0) != Magic || _view.ReadInt32(4) == 0)
                {
                    return false;
                }
                x = _view.ReadInt32(8);
                y = _view.ReadInt32(12);
                api = _view.ReadInt32(16);
                user = _view.ReadInt32(20);
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        internal static void Stop()
        {
            _view?.Dispose();
            _map?.Dispose();
            if (_helper != null && !_helper.HasExited)
            {
                _helper.Kill();
            }
            _helper?.Dispose();
        }
    }
}
