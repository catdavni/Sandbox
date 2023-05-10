using System.Diagnostics;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.DirextXApiHelpers;
using SharpDxSandbox.WicHelpers;
using SharpDxSandbox.Window;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;

namespace SharpDxSandbox.Sandbox;

public class Direct2DSandbox
{
    public static async Task DrawImage()
    {
        await FromDirect2D();
        await FromDirect3D11();
    }

    private static async Task FromDirect2D()
    {
        await new DirextXApiHelpers.Window(3840, 2160).RunInWindow(Drawing);

        Task Drawing(DirextXApiHelpers.Window window, WindowHandle windowHandle, CancellationToken cancellation)
            => Task.Run(() =>
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
                            Width = window.Width,
                            Height = window.Height,
                            Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm, //REQUIRED!!!
                            SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0), //REQUIRED!!!
                            Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                            BufferCount = 1,
                            SwapEffect = SharpDX.DXGI.SwapEffect.Discard
                        };
                        using var swapChain = new SharpDX.DXGI.SwapChain1(dxgiFactory, d3d11device, windowHandle.Value, ref swapChainDescription);

                        using var backBuffer = swapChain.GetBackBuffer<SharpDX.DXGI.Surface>(0);
                        using var d2dFactory = new SharpDX.Direct2D1.Factory(
                            SharpDX.Direct2D1.FactoryType.SingleThreaded,
                            SharpDX.Direct2D1.DebugLevel.Information);

                        using var renderTarget = new SharpDX.Direct2D1.RenderTarget(
                            d2dFactory,
                            backBuffer,
                            new SharpDX.Direct2D1.RenderTargetProperties(
                                new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Premultiplied)));

                        using var wicBitmap = ImageLoader.Load("Resources/4i.jpg");
                        var ellipse = new SharpDX.Direct2D1.Ellipse(
                            new RawVector2(window.Width / 2f, window.Height / 2f),
                            window.Width / 2f,
                            window.Height / 2f);
                        var step = 0d;
                        using var d2dBitmap = SharpDX.Direct2D1.Bitmap.FromWicBitmap(renderTarget, wicBitmap);

                        var boxWidth = (float)window.Width * 0.5f;
                        var boxHeight = (float)window.Height * 0.5f;
                        var imgWidth = d2dBitmap.Size.Width;
                        var imgHeight = d2dBitmap.Size.Height;

                        var boxX = 0;
                        var boxY = 0;
                        var boxW = 3840; //boxWidth - boxX;
                        var boxH = 2160; //boxHeight - boxY;
                        //var sourceRect = CalculateSourceRect(imgWidth, imgHeight, ScaleBehavior.Fit).ToMpRect(); //new RawRectangleF(0f, 0f, window.Width, window.Height);
                        //var destRect = CalculateDestRect(boxX, boxY, boxW, boxH, imgWidth, imgHeight, ScaleBehavior.Fit).ToMpRect();
                        var boxRect = new Rect2F(boxX, boxY, boxW, boxH).ToMpRect();
                        var (sourceRect, destRect) = CalculateImageRects(boxX, boxY, boxW, boxH, imgWidth, imgHeight, ScaleBehavior.None);

                        while (!cancellation.IsCancellationRequested)
                        {
                            renderTarget.BeginDraw();
                            using var brush = new SharpDX.Direct2D1.SolidColorBrush(renderTarget, new RawColor4(1f, 0f, (float)Math.Sin(step), 1f));

                            renderTarget.FillRectangle(boxRect, brush);
                            renderTarget.DrawBitmap(
                                d2dBitmap,
                                destRect.ToMpRect(),
                                1f,
                                SharpDX.Direct2D1.BitmapInterpolationMode.NearestNeighbor,
                                sourceRect.ToMpRect());
                            //renderTarget.FillEllipse(ellipse, brush);
                            renderTarget.EndDraw();

                            swapChain.Present(1, SharpDX.DXGI.PresentFlags.None);
                            step += 0.03;
                        }
                    }
                    catch (Exception)
                    {
                        logger.FlushMessages();
                    }
                },
                cancellation);
    }

    private static async Task FromDirect3D11()
    {
        await new DirextXApiHelpers.Window(600, 400).RunInWindow(Drawing);

        Task Drawing(DirextXApiHelpers.Window window, WindowHandle windowHandle, CancellationToken cancellation) =>
            Task.Run(
                () =>
                {
                    SharpDX.Direct3D11.Device.CreateWithSwapChain(
                        SharpDX.Direct3D.DriverType.Hardware,
                        SharpDX.Direct3D11.DeviceCreationFlags.Debug | SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport,
                        new SharpDX.DXGI.SwapChainDescription
                        {
                            BufferCount = 1,
                            IsWindowed = true,
                            ModeDescription = new SharpDX.DXGI.ModeDescription(window.Width, window.Height, SharpDX.DXGI.Rational.Empty, SharpDX.DXGI.Format.B8G8R8A8_UNorm),
                            OutputHandle = windowHandle.Value,
                            SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                            SwapEffect = SharpDX.DXGI.SwapEffect.Discard,
                            Usage = SharpDX.DXGI.Usage.RenderTargetOutput
                        },
                        out var outDevice,
                        out var outSwapChain);
                    using var logger = new DeviceLogger(outDevice);

                    try
                    {
                        using var device = outDevice;
                        using var swapChain = outSwapChain;
                        using var dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device>();

                        using var backBuffer = swapChain.GetBackBuffer<SharpDX.DXGI.Surface>(0);
                        var renderTargetProps = new SharpDX.Direct2D1.RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Premultiplied));
                        using var factory = new SharpDX.Direct2D1.Factory(SharpDX.Direct2D1.FactoryType.SingleThreaded, SharpDX.Direct2D1.DebugLevel.Information);
                        using var renderTarget = new SharpDX.Direct2D1.RenderTarget(factory, backBuffer, renderTargetProps);

                        var ellipse = new SharpDX.Direct2D1.Ellipse(
                            new RawVector2(window.Width / 2f, window.Height / 2f),
                            window.Width / 2f,
                            window.Height / 2f);
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
                    }
                    catch (Exception ex)
                    {
                        logger.FlushMessages();
                    }
                },
                cancellation);
    }

    private static (Rect2F SourceRect, Rect2F DestRect) CalculateImageRects(
        float boxX,
        float boxY,
        float boxWidth,
        float boxHeight,
        float mediaWidth,
        float mediaHeight,
        ScaleBehavior scaleBehavior)
    {
        switch (scaleBehavior)
        {
            case ScaleBehavior.None:
            {
                var imgX = mediaWidth > boxWidth ? (mediaWidth - boxWidth) / 2f : 0f;
                var imgY = mediaHeight > boxHeight ? (mediaHeight - boxHeight) / 2f : 0f;
                var imgW = mediaWidth > boxWidth ? boxWidth : mediaWidth;
                var imgH = mediaHeight > boxHeight ? boxHeight : mediaHeight;
                var sourceRect = new Rect2F(imgX, imgY, imgW, imgH);

                var destX = mediaWidth > boxWidth ? boxX : boxX + (boxWidth - mediaWidth) / 2f;
                var destY = mediaHeight > boxHeight ? boxY : boxY + (boxHeight - mediaHeight) / 2f;
                var destW = mediaWidth > boxWidth ? boxWidth : mediaWidth;
                var destH = mediaHeight > boxHeight ? boxHeight : mediaHeight;
                var destRect = new Rect2F(destX, destY, destW, destH);
                return (sourceRect, destRect);
            }
            case ScaleBehavior.Fit:
            {
                var sourceRect = new Rect2F(0f, 0f, mediaWidth, mediaHeight);

                var scaleX = boxWidth / mediaWidth;
                var scaleY = boxHeight / mediaHeight;
                var scale = scaleX < scaleY ? scaleX : scaleY;
                
                var rectHeight = mediaHeight * scale;
                var rectWidth = mediaWidth * scale;

                var rectX = boxX + (boxWidth - rectWidth) / 2f;
                var rectY = boxY + (boxHeight - rectHeight) / 2f;

                var destRect = new Rect2F(rectX, rectY, rectWidth, rectHeight);
                return (sourceRect, destRect);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(scaleBehavior), scaleBehavior, null);
        }
    }

    private static Rect2F CalculateSourceRect(float imgWidth, float imgHeight, ScaleBehavior scaleBehavior)
    {
        switch (scaleBehavior)
        {
            case ScaleBehavior.None:
                return default;
            case ScaleBehavior.Fit:
                return new Rect2F(0f, 0f, imgWidth, imgHeight);
            default:
                throw new ArgumentOutOfRangeException(nameof(scaleBehavior), scaleBehavior, null);
        }
    }

    private static Rect2F CalculateDestRect(
        float boxX,
        float boxY,
        float boxWidth,
        float boxHeight,
        float imageWidth,
        float imageHeight,
        ScaleBehavior scaleBehavior)
    {
        switch (scaleBehavior)
        {
            case ScaleBehavior.None:
                return default;
            case ScaleBehavior.Fit:

                var scaleX = boxWidth / imageWidth;
                var scaleY = boxHeight / imageHeight;
                var scale = scaleX < scaleY ? scaleX : scaleY;

                var rectHeight = imageHeight * scale;
                var rectWidth = imageWidth * scale;

                var rectX = boxX + (boxWidth - rectWidth) / 2f;
                var rectY = boxY + (boxHeight - rectHeight) / 2f;

                return new(rectX, rectY, rectX + rectWidth, rectY + boxHeight);
            default:
                throw new ArgumentOutOfRangeException(nameof(scaleBehavior), scaleBehavior, null);
        }
    }

    private enum ScaleBehavior
    {
        None,
        Fit
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

public readonly record struct Rect2F(Point2F Origin, Size2F Size)
{
    public Rect2F(Size2F size)
        : this(default, size)
    {
    }

    public Rect2F(float x, float y, float width, float height)
        : this(new Point2F(x, y), new Size2F(width, height))
    {
    }

    public float Left => Origin.X;

    public float Top => Origin.Y;

    public float Right => Origin.X + Size.Width;

    public float Bottom => Origin.Y + Size.Height;

    public float Width => Size.Width;

    public float Height => Size.Height;

    public bool IsEmpty => Size.IsEmpty;

    public static Rect2F FromLTRB(float left, float top, float right, float bottom) =>
        new(left, top, right - left, bottom - top);
}

public readonly record struct Point2F(float X, float Y)
{
    public static Point2F operator +(Point2F left, (float X, float Y) right) => new(left.X + right.X, left.Y + right.Y);

    public static Point2F operator -(Point2F left, (float X, float Y) right) => new(left.X - right.X, left.Y - right.Y);

    //public static Point2F operator +(Point2F left, Vector2F right) => new(left.X + right.X, left.Y + right.Y);

    //public static Point2F operator -(Point2F left, Vector2F right) => new(left.X - right.X, left.Y - right.Y);

    //public static Vector2F operator -(Point2F left, Point2F right) => new(left.X - right.X, left.Y - right.Y);
}

public readonly record struct Size2F(float Width, float Height)
{
    public bool IsEmpty => Width == 0 || Height == 0;

    public static implicit operator (float Width, float Height)(Size2F value) => (value.Width, value.Height);

    public static Size2F operator *(Size2F left, float right) => new(left.Width * right, left.Height * right);

    public static Size2F operator /(Size2F left, float right) => new(left.Width / right, left.Height / right);
}

public static class PrimitivesExtensions
{
//    public static Point2F ToMpPoint(this PointF point) => new(point.X, point.Y);

    public static RawRectangleF ToMpRect(this Rect2F rect) => new(rect.Left, rect.Top, rect.Right, rect.Bottom);

    //public static Color ToMpColor(this ColorF color) => Color.FromArgbNorm(color.A, color.R, color.G, color.B);
}