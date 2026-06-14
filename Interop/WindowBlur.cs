using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace Spur.Interop
{
    internal static class WindowBlur
    {
        public static void EnableBlur(Window window, uint tint = 0x99000000)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            EnableBlur(hwnd, tint);
        }

        public static void EnableBlur(IntPtr hwnd, uint tint = 0x99000000)
        {
            try
            {
                var accent = new AccentPolicy();
                // Try acrylic first
                accent.AccentState = (int)AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
                accent.GradientColor = (int)tint; // ARGB

                var accentStructSize = Marshal.SizeOf(accent);
                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };

                SetWindowCompositionAttribute(hwnd, ref data);

                Marshal.FreeHGlobal(accentPtr);
            }
            catch
            {
                // Intentional: best-effort — don't crash if unsupported
            }
        }

        public static void DisableBlur(IntPtr hwnd)
        {
            try
            {
                var accent = new AccentPolicy { AccentState = (int)AccentState.ACCENT_DISABLED };
                var accentStructSize = Marshal.SizeOf(accent);
                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };

                SetWindowCompositionAttribute(hwnd, ref data);
                Marshal.FreeHGlobal(accentPtr);
            }
            catch { /* intentional: best-effort blur disable, failure is non-critical */ }
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_ENABLE_HOSTBACKDROP = 5,
            ACCENT_INVALID_STATE = 6
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        private enum WindowCompositionAttribute
        {
            // ... other values omitted
            WCA_ACCENT_POLICY = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }
    }
}
