using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Il2Cpp;
using MelonLoader;
using MelonLoader.NativeUtils;

namespace NocturneModernController
{
    /// <summary>
    /// Single native hook for the legacy logical pad check. It augments L1/R1
    /// only when the native caller stack is inside fldCamera.calcCamNormal().
    /// Right-stick sampling occurs in ModMain.OnUpdate(), outside this detour.
    /// </summary>
    internal static class NativePadCheckHook
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte PadCheckDelegate(
            Il2Cpplibsdf_H.SDF_PADMAP map,
            int padNumber,
            IntPtr methodInfo);

        private static NativeHook<PadCheckDelegate>? _hook;
        private static PadCheckDelegate? _detour;
        private static MelonLogger.Instance? _logger;

        internal static void Initialize(MelonLogger.Instance logger)
        {
            _logger = logger;

            IntPtr padCheckTarget = FindNativeMethod(
                typeof(dds3PadManager),
                "NativeMethodInfoPtr_DDS3_PADCHECK_PRESS_Public_Static_Boolean_SDF_PADMAP_Int32_0");
            _detour = PadCheckDetour;
            _hook = new NativeHook<PadCheckDelegate>(
                padCheckTarget,
                Marshal.GetFunctionPointerForDelegate(_detour));
            _hook.Attach();

            logger.Msg(
                "[NocturneModernController] Scoped pad hook attached: " +
                $"DDS3_PADCHECK_PRESS=0x{padCheckTarget.ToInt64():X}");
        }

        internal static void Shutdown()
        {
            if (_hook?.IsHooked == true)
            {
                _hook.Detach();
            }
        }

        private static byte PadCheckDetour(
            Il2Cpplibsdf_H.SDF_PADMAP map,
            int padNumber,
            IntPtr methodInfo)
        {
            byte original = _hook!.Trampoline(map, padNumber, methodInfo);
            if (original != 0)
            {
                return original;
            }

            if (map != Il2Cpplibsdf_H.SDF_PADMAP.SDF_PADMAP_L1 &&
                map != Il2Cpplibsdf_H.SDF_PADMAP.SDF_PADMAP_R1)
            {
                return original;
            }

            if (!FieldCameraMainProbe.Active)
            {
                return original;
            }

            return RightStickTurnInput.IsHeldForPadMap(map) ? (byte)1 : (byte)0;
        }

        private static IntPtr FindNativeMethod(Type type, string exactFieldName)
        {
            FieldInfo? field = type.GetField(
                exactFieldName,
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null)
            {
                throw new MissingFieldException(type.FullName, exactFieldName);
            }

            IntPtr methodInfo = (IntPtr)(field.GetValue(null) ?? IntPtr.Zero);
            if (methodInfo == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Native MethodInfo pointer is null: {type.FullName}.{exactFieldName}");
            }

            IntPtr function = Marshal.ReadIntPtr(methodInfo);
            if (function == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Native function pointer is null: {type.FullName}.{exactFieldName}");
            }

            return function;
        }
    }
}
