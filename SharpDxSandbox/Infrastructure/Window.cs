using System.Collections.Concurrent;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace SharpDxSandbox.Infrastructure;

internal sealed class Window : IDisposable
{
    private const int XPosition = 200;
    private const int YPosition = 200;
    private const string WindowTitle = "Piu";
    private const string WindowClassName = nameof(Window);
    private readonly CancellationTokenSource _windowMessagePumpCancellation;
    private readonly ConcurrentDictionary<string, WindowProcHandler> _windowProcedures;

    public Window(int width, int height)
    {
        Width = width;
        Height = height;
        _windowMessagePumpCancellation = new CancellationTokenSource();
        _windowProcedures = new();

        Presentation = CreateWindow();
        OnKeyPressed += (_, k) =>
        {
            if (k.Input == "")
            {
                CloseWindow();
            }
        };
    }

    public Task Presentation { get; }

    public IntPtr Handle { get; private set; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public event EventHandler<EventArgs> OnWindowSizeChanged;

    public event EventHandler<KeyPressedEventArgs> OnKeyPressed;

    public event EventHandler<EventArgs> OnWindowClosed;

    public void RegisterWindowProcHandler(string purpose, Action<HWND, uint, IntPtr, IntPtr> handler)
        => _windowProcedures[purpose] = WindowProcHandler.AsHandler(handler);

    public void RegisterWindowProcInterceptor(string purpose, Func<HWND, uint, IntPtr, IntPtr, IntPtr> hook)
        => _windowProcedures[purpose] = WindowProcHandler.AsInterceptor(hook);

    public void CloseWindow() => _windowMessagePumpCancellation.Cancel();

    private Task CreateWindow()
    {
        var waitWindowHandle = new TaskCompletionSource<IntPtr>();

        var windowMessageLoop = Task.Factory.StartNew(() =>
            {
                try
                {
                    var wndClass = new WNDCLASS
                    {
                        style = WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW,
                        lpfnWndProc = MainWindowProcHandler,
                        lpszClassName = WindowClassName,
                    };
                    Win32Error.ThrowLastErrorIfNull(Macros.MAKEINTATOM(RegisterClass(wndClass)));

                    const WindowStyles windowStyle = WindowStyles.WS_OVERLAPPED |
                                                     WindowStyles.WS_CAPTION |
                                                     WindowStyles.WS_SYSMENU |
                                                     WindowStyles.WS_THICKFRAME |
                                                     WindowStyles.WS_MAXIMIZEBOX |
                                                     WindowStyles.WS_VISIBLE;

                    var rect = new RECT(XPosition, YPosition, Width + XPosition, Height + YPosition);
                    Win32Error.ThrowLastErrorIfFalse(AdjustWindowRect(ref rect, windowStyle, false));

                    SetProcessDPIAware();

                    var handle = Win32Error.ThrowLastErrorIfInvalid(CreateWindowEx(
                        WindowStylesEx.WS_EX_OVERLAPPEDWINDOW,
                        WindowClassName,
                        WindowTitle,
                        windowStyle,
                        rect.X,
                        rect.Y,
                        rect.Width,
                        rect.Height));
                    waitWindowHandle.SetResult(handle.DangerousGetHandle());
                }
                catch (Exception ex)
                {
                    waitWindowHandle.SetException(ex);
                }

                while (!_windowMessagePumpCancellation.IsCancellationRequested && PumpWindowMessages())
                {
                }

                // request to close window by token and not by WM_Destroy
                if (_windowMessagePumpCancellation.IsCancellationRequested)
                {
                    Win32Error.ThrowLastErrorIfFalse(DestroyWindow(Handle));
                }
                Win32Error.ThrowLastErrorIfFalse(UnregisterClass(WindowClassName, HINSTANCE.NULL));
            },
            _windowMessagePumpCancellation.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        Handle = waitWindowHandle.Task.Result;
        return windowMessageLoop;
    }

    private IntPtr MainWindowProcHandler(HWND hwnd, uint umsg, IntPtr wparam, IntPtr lparam)
    {
        foreach (var windowProcedure in _windowProcedures)
        {
            var result = windowProcedure.Value.Handler(hwnd, umsg, wparam, lparam);
            if (windowProcedure.Value.IsInterceptor)
            {
                return result;
            }
        }

        var message = umsg.ToEnum<WindowMessage>();
        switch (message)
        {
            case WindowMessage.WM_CHAR:
            {
                OnKeyPressed?.Invoke(this, new(char.ConvertFromUtf32((char)wparam)));
                break;
            }
            case WindowMessage.WM_SIZE:
            {
                Width = Macros.LOWORD(lparam);
                Height = Macros.HIWORD(lparam);
                Console.WriteLine($"Size: {Width}:{Height}");
                OnWindowSizeChanged?.Invoke(this, EventArgs.Empty);
                break;
            }
            case WindowMessage.WM_DESTROY:
            {
                OnWindowClosed?.Invoke(this, EventArgs.Empty);
                PostQuitMessage();
                break;
            }
        }
        return DefWindowProc(hwnd, umsg, wparam, lparam);
    }

    private bool PumpWindowMessages()
    {
        while (PeekMessage(out var msg, IntPtr.Zero, 0, 0, PM.PM_REMOVE))
        {
            TranslateMessage(in msg);
            DispatchMessage(in msg);

            if (msg.message.ToEnum<WindowMessage>() == WindowMessage.WM_QUIT)
            {
                return false;
            }
        }
        return true;
    }

    public void Dispose()
    {
        _windowMessagePumpCancellation.Cancel();
        _windowMessagePumpCancellation.Dispose();
        Presentation.Wait();
    }

    private sealed record WindowProcHandler(Func<HWND, uint, IntPtr, IntPtr, IntPtr> Handler, bool IsInterceptor)
    {
        public static WindowProcHandler AsInterceptor(Func<HWND, uint, IntPtr, IntPtr, IntPtr> hook)
            => new(hook, true);

        public static WindowProcHandler AsHandler(Action<HWND, uint, IntPtr, IntPtr> handler)
            => new((h, m, w, l) =>
                {
                    handler(h, m, w, l);
                    return IntPtr.Zero;
                },
                false);
    }
}