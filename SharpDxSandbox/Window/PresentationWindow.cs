using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using SharpDxSandbox.Interop;

namespace SharpDxSandbox.Window;

internal sealed class PresentationWindow : IDisposable
{
    private const string WindowName = nameof(PresentationWindow);
    private const string WindowClassName = nameof(SharedWindowClass);

    private static readonly SharedWindowClass Klass = new();
    private readonly SingleDisposable _singleDisposable = new(typeof(PresentationWindow));

    private readonly IDisposable _klassLifetime;
    [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Justification = "Disposal pattern implemented")]
    private readonly nint _handle;

    public PresentationWindow(int width, int height, WindowOptions windowOptions)
    {
        _klassLifetime = Klass.Acquire();

        GCHandle windowGcHandle = default;
        try
        {
            windowGcHandle = GCHandle.Alloc(this);
            nint thisPtr = (nint)windowGcHandle;

            uint extendedStyle = NativeMethods.WsExAppWindow;
            uint style = NativeMethods.WsOverlapped | NativeMethods.WsVisible | NativeMethods.WsMaximizeBox | NativeMethods.WsSysMenu;

            if (windowOptions.HasFlag(WindowOptions.TopMost))
            {
                extendedStyle |= NativeMethods.WsExTopMost;
            }
            else
            {
                //style |= NativeMethods.WsMaximize;
            }

            _handle = User32.CreateWindowEx(
                extendedStyle,
                SharedWindowClass.Name,
                WindowName,
                style,
                200,
                200,
                width,
                height,
                nint.Zero,
                nint.Zero,
                nint.Zero,
                thisPtr);

            if (windowOptions.HasFlag(WindowOptions.HideCursor))
            {
                NativeMethods.SetCursor(nint.Zero);
            }
            else
            {
                NativeMethods.SetCursor(NativeMethods.LoadCursor(nint.Zero, NativeMethods.IdcArrow));
            }
        }
        finally
        {
            windowGcHandle.Free();
        }
    }

    public WindowHandle Handle => new(_handle);

    public event Action<string> KeyPressed;

    public event Action WindowClosed;

    ~PresentationWindow() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool ProcessMessages()
    {
        while (NativeMethods.PeekMessage(out var msg, _handle, 0, 0, NativeMethods.PmRemove))
        {
            NativeMethods.TranslateMessage(ref msg);
            NativeMethods.DispatchMessage(ref msg);

            if (msg.message == NativeMethods.WmQuit)
            {
                return false;
            }
        }

        return true;
    }

    private void Dispose(bool isDisposing)
    {
        User32.TryDestroyWindow(_handle);

        if (isDisposing)
        {
            _singleDisposable.Dispose();
            _klassLifetime.Dispose();
        }
    }

    private void HandleKeyPress(int charCode) => KeyPressed?.Invoke(char.ConvertFromUtf32(charCode));

    private void HandleWindowClosed() => WindowClosed?.Invoke();

    private sealed class SharedWindowClass
    {
        public const string Name = WindowClassName;

        private static readonly ConcurrentDictionary<nint, PresentationWindow> _windowLookup = new();

        private readonly object _lock = new();
        private User32.WndClass? _class;
        private int _refCount;

        public IDisposable Acquire()
        {
            lock (_lock)
            {
                if (!_class.HasValue)
                {
                    Debug.Assert(_refCount == 0, "References should not exist at this point.");
                    _class = RegisterWindowClass(Name, WindowProcedure);
                    ++_refCount;
                }
                return new Lease(this);
            }
        }

        private void Release()
        {
            lock (_lock)
            {
                if (--_refCount == 0)
                {
                    User32.UnregisterClass(Name, nint.Zero);
                    _class = null;
                }
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification = "ShowCursor returns not HRESULT, but the display counter")]
        private static nint WindowProcedure(nint hwnd, uint msg, nint wordParam, nint longParam)
        {
            // if (_windowLookup.ContainsKey(hwnd))
            // {
            //     Console.WriteLine($"0x{msg:X}");
            // }
            
            switch (msg)
            {
                case NativeMethods.WmCreate:
                {
                    var createStruct = (NativeMethods.CREATESTRUCT)Marshal.PtrToStructure(longParam, typeof(NativeMethods.CREATESTRUCT));
                    var handle = GCHandle.FromIntPtr(createStruct.lpCreateParams);
                    var pw = (PresentationWindow)handle.Target;
                    Verify.IsTrue(_windowLookup.TryAdd(hwnd, pw))?
                        .OrThrow("Window has been already registered.");
                    break;
                }

                case NativeMethods.WmNcActivate:
                    // This prevents window from loosing the focus.
                    if (wordParam == nint.Zero)
                    {
                        return nint.Zero;
                    }

                    break;

                case NativeMethods.WmChar:
                {
                    if (_windowLookup.TryGetValue(hwnd, out var pw))
                    {
                        pw.HandleKeyPress(checked((int)wordParam));
                    }
                    break;
                }

                case NativeMethods.WmDestroy:
                {
                    Verify.IsTrue(_windowLookup.TryRemove(hwnd, out var pw))?
                        .OrThrow("Window was not registered.");
                    pw!.HandleWindowClosed();
                    NativeMethods.PostQuitMessage(0);
                    break;
                }
            }

            return NativeMethods.DefWindowProc(hwnd, msg, wordParam, longParam);
        }

        private static User32.WndClass RegisterWindowClass(string className, User32.WndProc wndProcImpl)
        {
            var wndClass = new User32.WndClass
            {
                Style = (uint)(User32.WindowClassStyles.VRedraw | User32.WindowClassStyles.HRedraw),
                WndProc = wndProcImpl,
                ClassName = className,
                HandleCursor = nint.Zero, // NativeMethods.LoadCursor(IntPtr.Zero, idcArrow),
                HandleBrushBackground = NativeMethods.GetStockObject(NativeMethods.StockObjects.BLACK_BRUSH)
            };

            User32.RegisterClass(in wndClass);
            return wndClass;
        }

        private sealed class Lease : IDisposable
        {
            private readonly SingleDisposable _guard = new(typeof(Lease));
            private readonly SharedWindowClass _owner;

            public Lease(SharedWindowClass owner) => _owner = owner;

            public void Dispose()
            {
                _guard.Dispose();
                _owner.Release();
            }
        }
    }

    // TODO: move to Tobii.Interop.Windows

    // The following items match Windows headers naming.
#pragma warning disable SA1305
#pragma warning disable SA1307
    private static class NativeMethods
    {
        public const uint WsChild = 0x40000000;
        public const uint WsPopUp = 0x80000000;
        public const uint WsBorder = 0x00800000;
        public const uint WsDlgFrame = 0x00400000;
        public const uint WsCaption = WsBorder | WsDlgFrame;
        public const uint WsSysMenu = 0x00080000;
        public const uint WsMaximize = 0x01000000;
        public const uint WsThickFrame = 0x00040000;
        public const uint WsMaximizeBox = 0x00010000;
        public const uint WsMinimizeBpx = 0x00020000;
        public const uint WsPopUpWindow = WsPopUp | WsBorder | WsSysMenu;
        public const uint WsVisible = 0x10000000;
        public const uint WsExTopMost = 0x00000008;
        public const uint WsExToolWindow = 0x00000080;
        public const uint WsExAppWindow = 0x00040000;
        public const uint WsOverlapped = 0x00000000;
        public const uint WmCreate = 0x0001;
        public const uint WmClose = 0x0010;
        public const uint WmDestroy = 0x0002;
        public const uint WmQuit = 0x0012;
        public const uint WmChar = 0x0102;
        public const uint WmNcActivate = 0x0086;
        public const uint PmNoRemove = 0x0;
        public const uint PmRemove = 0x0001;

        public const int IdcArrow = 32512;

        public enum StockObjects
        {
            WHITE_BRUSH = 0,
            LTGRAY_BRUSH = 1,
            GRAY_BRUSH = 2,
            DKGRAY_BRUSH = 3,
            BLACK_BRUSH = 4,
            NULL_BRUSH = 5,
            HOLLOW_BRUSH = NULL_BRUSH,
            WHITE_PEN = 6,
            BLACK_PEN = 7,
            NULL_PEN = 8,
            OEM_FIXED_FONT = 10,
            ANSI_FIXED_FONT = 11,
            ANSI_VAR_FONT = 12,
            SYSTEM_FONT = 13,
            DEVICE_DEFAULT_FONT = 14,
            DEFAULT_PALETTE = 15,
            SYSTEM_FIXED_FONT = 16,
            DEFAULT_GUI_FONT = 17,
            DC_BRUSH = 18,
            DC_PEN = 19,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct MSG
        {
            public nint hwnd;
            public uint message;
            public nuint wParam;
            public nuint lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CREATESTRUCT
        {
            public nint lpCreateParams;
            public nint hInstance;
            public nint hMenu;
            public nint hwndParent;
            public int cy;
            public int cx;
            public int y;
            public int x;
            public int style;
            public nint lpszName;
            public nint lpszClass;
            public int dwExStyle;
        }

        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int nExitCode);

        [DllImport("gdi32.dll")]
        public static extern nint GetStockObject(StockObjects fnObject);

        [DllImport("user32.dll")]
        public static extern nint LoadCursor(nint hInstance, int lpCursorName);

        [DllImport("user32.dll")]
        public static extern nint SetCursor(nint hCursor);

        [DllImport("user32.dll")]
        public static extern nint DefWindowProc(nint hwnd, uint msg, nint wordParam, nint longParam);

        [DllImport("user32.dll")]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        public static extern nint DispatchMessage([In] ref MSG lpmsg);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PeekMessage(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);
    }
#pragma warning restore SA1307
#pragma warning restore SA1305
}