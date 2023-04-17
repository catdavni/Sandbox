using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SharpDxSandbox.Interop
{
    public static class User32
    {
        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public static ushort RegisterClass(in WndClass wndClass)
            => NativeMethods.RegisterClass(in wndClass)
                .ThrowWinErrorIfZero();

        public static ushort TryRegisterClass(in WndClass wndClass)
            => NativeMethods.RegisterClass(in wndClass);

        public static void UnregisterClass(string lpClassName, IntPtr hInstance)
            => NativeMethods.UnregisterClass(lpClassName, hInstance).ThrowWinErrorIfFalse();

        public static bool TryUnregisterClass(string lpClassName, IntPtr hInstance)
            => NativeMethods.UnregisterClass(lpClassName, hInstance);

        [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Native function name")]
        public static IntPtr CreateWindowEx(
            uint extendedStyle,
            string className,
            string windowName,
            uint style,
            int x,
            int y,
            int width,
            int height,
            IntPtr hwndParent,
            IntPtr handleMenu,
            IntPtr handleInstance,
            IntPtr param)
            => NativeMethods.CreateWindowEx(extendedStyle, className, windowName, style, x, y, width, height, hwndParent, handleMenu, handleInstance, param)
                .ThrowWinErrorIfNullPtr();

        [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Native function name")]
        public static IntPtr TryCreateWindowEx(
            uint extendedStyle,
            string className,
            string windowName,
            uint style,
            int x,
            int y,
            int width,
            int height,
            IntPtr hwndParent,
            IntPtr handleMenu,
            IntPtr handleInstance,
            IntPtr param)
            => NativeMethods.CreateWindowEx(extendedStyle, className, windowName, style, x, y, width, height, hwndParent, handleMenu, handleInstance, param);

        public static void DestroyWindow(IntPtr hwnd)
            => NativeMethods.DestroyWindow(hwnd)
                .ThrowWinErrorIfFalse();

        public static bool TryDestroyWindow(IntPtr hwnd)
            => NativeMethods.DestroyWindow(hwnd);

        [Flags]
        public enum WindowClassStyles
        {
            VRedraw = 0x0001,
            HRedraw = 0x0002
        }

        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "NativeMethods class")]
        [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not intended for comparison")]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WndClass
        {
            public uint Style;
            public WndProc WndProc;
            public int ClassExtra;
            public int WindowExtra;
            public IntPtr HandleInstance;
            public IntPtr HandleIcon;
            public IntPtr HandleCursor;
            public IntPtr HandleBrushBackground;
            public string MenuName;
            public string ClassName;
        }

        private static class NativeMethods
        {
            private const string User32Dll = "user32.dll";

            [DllImport(User32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern ushort RegisterClass([In] in WndClass wndClass);

            [DllImport(User32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

            [DllImport(User32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr CreateWindowEx(
                uint extendedStyle,
                string className,
                string windowName,
                uint style,
                int x,
                int y,
                int width,
                int height,
                IntPtr hwndParent,
                IntPtr handleMenu,
                IntPtr handleInstance,
                IntPtr param);

            [DllImport(User32Dll, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DestroyWindow(IntPtr hwnd);
        }
    }
}