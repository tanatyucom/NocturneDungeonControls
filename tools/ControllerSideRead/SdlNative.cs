using System;
using System.Runtime.InteropServices;

namespace NocturneModernController.ControllerProbe
{
    internal static class SdlNative
    {
        internal const uint InitGamepad = 0x00002000;
        internal const int GamepadButtonCount = 26;

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SDL_Init(uint initFlags);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SDL_Quit();

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SDL_GetError();

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SDL_GetJoysticks(out int count);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SDL_free(IntPtr memory);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SDL_UpdateJoysticks();

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SDL_OpenJoystick(uint instanceId);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SDL_CloseJoystick(IntPtr joystick);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SDL_GetJoystickNameForID(uint instanceId);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SDL_GetJoystickPathForID(uint instanceId);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort SDL_GetJoystickVendorForID(uint instanceId);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern ushort SDL_GetJoystickProductForID(uint instanceId);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SDL_GetNumJoystickAxes(IntPtr joystick);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SDL_GetNumJoystickButtons(IntPtr joystick);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern short SDL_GetJoystickAxis(IntPtr joystick, int axis);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SDL_GetJoystickButton(IntPtr joystick, int button);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SDL_IsGamepad(uint instanceId);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SDL_OpenGamepad(uint instanceId);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SDL_CloseGamepad(IntPtr gamepad);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern short SDL_GetGamepadAxis(IntPtr gamepad, int axis);

        [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool SDL_GetGamepadButton(IntPtr gamepad, int button);

        internal static string GetError()
        {
            return GetUtf8(SDL_GetError()) ?? "unknown SDL error";
        }

        internal static string? GetUtf8(IntPtr value)
        {
            return value == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(value);
        }
    }
}
