using System.Diagnostics;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.DirextXApiHelpers;
using SharpDxSandbox.Window;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;

namespace SharpDxSandbox.Sandbox;

public class Direct2DSandbox
{
    private static readonly int windowWidth = 600;
    private static readonly int windowHeight = 400;

    public static async Task DrawImage()
    {
        await FromDirect2D();
        await FromDirect3D11();
    }

    private static async Task FromDirect2D()
    {
        await RunInWindow(Drawing);

        async Task Drawing(WindowHandle windowHandle, CancellationToken cancellation)
        {
            using var d3d11device = new SharpDX.Direct3D11.Device(
                SharpDX.Direct3D.DriverType.Hardware,
                SharpDX.Direct3D11.DeviceCreationFlags.Debug | SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport);

            using var logger = new DeviceLogger(d3d11device);
            try
            {
                using var dxgiDevice = d3d11device.QueryInterface<SharpDX.DXGI.Device>();
                using var adapter = dxgiDevice.Adapter;
                using var dxgiFactory = adapter.GetParent<SharpDX.DXGI.Factory2>();
                var swapChainDescription = new SharpDX.DXGI.SwapChainDescription1
                {
                    Width = windowWidth,
                    Height = windowHeight,
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm, //REQUIRED!!!
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0), //REQUIRED!!!
                    Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                    BufferCount = 1,
                    SwapEffect = SharpDX.DXGI.SwapEffect.Discard
                };
                using var swapChain = new SharpDX.DXGI.SwapChain1(dxgiFactory, d3d11device, windowHandle.Value, ref swapChainDescription);
                using var backBuffer = swapChain.GetBackBuffer<SharpDX.DXGI.Surface>(0);
                using var d2dFactory = new SharpDX.Direct2D1.Factory(SharpDX.Direct2D1.FactoryType.SingleThreaded, SharpDX.Direct2D1.DebugLevel.Information); //dxgiFactory.QueryInterface<SharpDX.Direct2D1.Factory>();
                using var renderTarget = new SharpDX.Direct2D1.RenderTarget(
                    d2dFactory,
                    backBuffer,
                    new SharpDX.Direct2D1.RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Premultiplied)));

                await Task.Run(() =>
                    {
                        var ellipse = new SharpDX.Direct2D1.Ellipse(new RawVector2(windowWidth / 2f, windowHeight / 2f), windowWidth / 2f, windowHeight / 2f);
                        var step = 0d;

                        while (!cancellation.IsCancellationRequested)
                        {
                            renderTarget.BeginDraw();
                            using var brush = new SharpDX.Direct2D1.SolidColorBrush(renderTarget, new RawColor4(1f, 0f, (float)Math.Sin(step), 1f));
                            renderTarget.FillEllipse(ellipse, brush);
                            renderTarget.EndDraw();

                            swapChain.Present(1, SharpDX.DXGI.PresentFlags.None);
                            step += 0.03;
                        }
                    },
                    cancellation);
            }
            catch (Exception)
            {
                logger.FlushMessages();
            }
        }
    }

    private static async Task FromDirect3D11()
    {
        await RunInWindow(Drawing);

        async Task Drawing(WindowHandle windowHandle, CancellationToken cancellation)
        {
            SharpDX.Direct3D11.Device.CreateWithSwapChain(
                SharpDX.Direct3D.DriverType.Hardware,
                SharpDX.Direct3D11.DeviceCreationFlags.Debug | SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport,
                new SharpDX.DXGI.SwapChainDescription
                {
                    BufferCount = 1,
                    IsWindowed = true,
                    ModeDescription = new SharpDX.DXGI.ModeDescription(windowWidth, windowHeight, SharpDX.DXGI.Rational.Empty, SharpDX.DXGI.Format.B8G8R8A8_UNorm),
                    OutputHandle = windowHandle.Value,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    SwapEffect = SharpDX.DXGI.SwapEffect.Discard,
                    Usage = SharpDX.DXGI.Usage.RenderTargetOutput
                },
                out var outDevice,
                out var outSwapChain);
            using var logger = new DeviceLogger(outDevice);
            using var device = outDevice;
            using var swapChain = outSwapChain;
            using var dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device>();

            try
            {
                using var backBuffer = swapChain.GetBackBuffer<SharpDX.DXGI.Surface>(0);
                var renderTargetProps = new SharpDX.Direct2D1.RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Premultiplied));
                using var factory = new SharpDX.Direct2D1.Factory(SharpDX.Direct2D1.FactoryType.SingleThreaded, SharpDX.Direct2D1.DebugLevel.Information);
                using var renderTarget = new SharpDX.Direct2D1.RenderTarget(factory, backBuffer, renderTargetProps);

                await Task.Run(
                    () =>
                    {
                        var ellipse = new SharpDX.Direct2D1.Ellipse(new RawVector2(windowWidth / 2f, windowHeight / 2f), windowWidth / 2f, windowHeight / 2f);
                        var sw = Stopwatch.StartNew();
                        while (!cancellation.IsCancellationRequested)
                        {
                            var red = Math.Abs(Math.Sin(sw.ElapsedMilliseconds));
                            //Console.WriteLine($"{red} - {(float)red}");
                            using var brush = new SharpDX.Direct2D1.SolidColorBrush(renderTarget, new RawColor4(1f, 0f, 0f, 1f));

                            renderTarget.BeginDraw();
                            renderTarget.FillEllipse(ellipse, brush);
                            renderTarget.EndDraw();

                            swapChain.Present(1, SharpDX.DXGI.PresentFlags.None);
                        }
                    },
                    cancellation);
            }
            catch (Exception ex)
            {
                logger.FlushMessages();
            }
        }
    }

    private static async Task RunInWindow(Func<WindowHandle, CancellationToken, Task> drawing)
    {
        using var window = new PresenterWindowLoop(windowWidth, windowHeight, WindowOptions.TopMost);
        window.KeyPressed += (_, eventArgs) => Console.WriteLine(eventArgs.Input);

        var windowHandle = window.GetWindowHandleAsync().Result;

        using var drawingsCancellation = new CancellationTokenSource();
        window.WindowClosed += (_, _) => { drawingsCancellation.Cancel(); };

        using var leakGuard = new MemoryLeakGuard(MemoryLeakGuard.LeakBehavior.ThrowException);
        await drawing(windowHandle, drawingsCancellation.Token);
    }

    #region Sandbox

    //private static async Task FromDirect2D()
    //{
    //    using (var window = new PresenterWindowLoop(windowWidth, windowHeight, WindowOptions.TopMost))
    //    {
    //        window.KeyPressed += (sender, eventArgs) => Console.WriteLine(eventArgs.Input);

    //        using var resetEvent = new AutoResetEvent(false);
    //        var windowHandle = window.GetWindowHandleAsync().Result;

    //        var defaultDevice = new Device(DriverType.Hardware,
    //            DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport);

    //        // Query the default device for the supported device and context interfaces.
    //        using var device = defaultDevice.QueryInterface<Device1>();
    //        //using var d3dContext = device.ImmediateContext.QueryInterface<SharpDX.Direct3D11.DeviceContext1>();

    //        using var dxgiDevice2 = device.QueryInterface<Device2>();
    //        using var dxgiAdapter = dxgiDevice2.Adapter;
    //        using var dxgiFactory2 = dxgiAdapter.GetParent<Factory2>();

    //        var description = new SwapChainDescription1
    //        {
    //            // 0 means to use automatic buffer sizing.
    //            Width = 0,
    //            Height = 0,
    //            // 32 bit RGBA color.
    //            Format = Format.B8G8R8A8_UNorm,
    //            // No stereo (3D) display.
    //            Stereo = false,
    //            // No multisampling.
    //            SampleDescription = new SampleDescription(1, 0),
    //            // Use the swap chain as a render target.
    //            Usage = Usage.RenderTargetOutput,
    //            // Enable double buffering to prevent flickering.
    //            BufferCount = 2,
    //            // No scaling.
    //            Scaling = Scaling.None,
    //            // Flip between both buffers.
    //            SwapEffect = SwapEffect.FlipSequential
    //        };

    //        // Generate a swap chain for our window based on the specified description.
    //        using var
    //            swapChain = new SwapChain1(dxgiFactory2, device, windowHandle.Value,
    //                ref description); //dxgiFactory2.CreateSwapChainForCoreWindow(device, new ComObject(window), ref description, null);

    //        using var backBuffer = swapChain.GetBackBuffer<Surface>(0);

    //        var renderTargetProps = new RenderTargetProperties(
    //            new PixelFormat(Format.Unknown, AlphaMode.Premultiplied));
    //        using var factory = new Factory(
    //            FactoryType.SingleThreaded, DebugLevel.Information);
    //        var renderTarget = new RenderTarget(
    //            factory,
    //            backBuffer,
    //            renderTargetProps);

    //        renderTarget.BeginDraw();
    //        renderTarget.DrawEllipse(
    //            new Ellipse(new RawVector2(windowWidth / 2f, windowHeight / 2f), windowWidth / 2f, windowHeight / 2f),
    //            new SolidColorBrush(renderTarget, new RawColor4(1f, 0f, 0f, 1f)));
    //        renderTarget.EndDraw();

    //        using var drawingsCancellation = new CancellationTokenSource();
    //        var drawings = Task.Run(() =>
    //        {
    //            try
    //            {
    //                while (!drawingsCancellation.IsCancellationRequested)
    //                    swapChain.Present(1, PresentFlags.None);
    //            }
    //            catch (SEHException e)
    //            {
    //                //logger.FlushMessages();
    //            }
    //        },
    //            drawingsCancellation.Token);
    //        window.WindowClosed += (sender, eventArgs) =>
    //        {
    //            drawingsCancellation.Cancel();
    //            resetEvent.Set();
    //        };
    //        resetEvent.WaitOne();
    //        await drawings;
    //        // }
    //        // catch (Exception ex)
    //        // {
    //        //     //logger.FlushMessages();
    //        // }
    //    }
    //}

    #endregion

}