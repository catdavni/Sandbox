using System.Reactive.Linq;
using System.Reactive.Subjects;
using Linearstar.Windows.RawInput;
using SharpDxSandbox.Infrastructure.Disposables;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace SharpDxSandbox.Infrastructure;

internal sealed class InputOutput : IDisposable
{
    private readonly Window _window;
    private readonly DisposableStack _compositeDisposable;

    public InputOutput(Window window)
    {
        _window = window;

        _compositeDisposable = new DisposableStack();

        var cameraPositionChanges = new Subject<CameraMovements>().DisposeWith(_compositeDisposable);
        CameraPositionChanges = cameraPositionChanges;

        Observable.FromEventPattern<EventHandler<VK>, VK>(e => _window.OnKeyDown += e, e => _window.OnKeyDown -= e)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Where(e => e.EventArgs == VK.VK_CAPITAL)
            .Scan(false, (prev, _) => !prev)
            .Subscribe(capture =>
            {
                _window.ClipMouseToWindow = capture;
                _window.IsCursorVisible = !capture;
            })
            .DisposeWith(_compositeDisposable);

        Observable.FromEventPattern<EventHandler<VK>, VK>(e => _window.OnKeyDown += e, e => _window.OnKeyDown -= e)
            .Select(k => k.EventArgs switch
            {
                VK.VK_W => default(CameraMovements) with { W = true },
                VK.VK_A => default(CameraMovements) with { A = true },
                VK.VK_S => default(CameraMovements) with { S = true },
                VK.VK_D => default(CameraMovements) with { D = true },
                _ => default
            })
            .Subscribe(cameraPositionChanges)
            .DisposeWith(_compositeDisposable);

        _window.RegisterWindowProcHandler("RawMouseInput", HandleRawInput);

        RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse, RawInputDeviceFlags.None, _window.Handle);

        IntPtr HandleRawInput(HWND hwnd, uint msg, IntPtr wparam, IntPtr lparam)
        {
            var message = msg.ToEnum<WindowMessage>();
            switch (message)
            {
                case WindowMessage.WM_INPUT:
                    if (!_window.ClipMouseToWindow)
                    {
                        break;
                    }
                    var data = RawInputData.FromHandle(lparam) as RawInputMouseData;
                    var (x, y) = (data.Mouse.LastX, data.Mouse.LastY);

                    cameraPositionChanges.OnNext(default(CameraMovements)
                        with
                        {
                            RotateRight = x > 0,
                            RotateLeft = x < 0,
                            RotateUp = y < 0,
                            RotateDown = y > 0
                        });

                    break;
            }
            return IntPtr.Zero;
        }
    }

    public IObservable<CameraMovements> CameraPositionChanges { get; }

    public void Dispose()
    {
        RawInputDevice.UnregisterDevice(HidUsageAndPage.Mouse);
        _compositeDisposable.Dispose();
    }
}

public readonly record struct CameraMovements(
    bool W,
    bool A,
    bool S,
    bool D,
    bool RotateLeft,
    bool RotateRight,
    bool RotateUp,
    bool RotateDown);