using System.Collections.Concurrent;
using System.Diagnostics;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Graphics.Drawables;
using SharpDxSandbox.Infrastructure;
using SharpDxSandbox.Infrastructure.Disposables;
using SharpDxSandbox.Window;

namespace SharpDxSandbox.Graphics;

internal sealed class Graphics : IDisposable
{
    private readonly DisposableStack _disposable;
    private readonly ConcurrentQueue<IDrawable> _drawables;

    // cleaned up with disposable stack
    private readonly DepthStencilView _depthStencilView;
    private readonly RenderTargetView _renderTargetView;
    private readonly SwapChain _swapChain;

    public Graphics(Infrastructure.Window window, WindowHandle windowHandle)
    {
        _drawables = new();
        _disposable = new DisposableStack();

        try
        {
            Device = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.Debug).DisposeWith(_disposable);
            Debug.Assert(Device != null, "Device is null!");
            
            Logger = new DeviceLogger(Device).DisposeWith(_disposable);

            var dxgiDevice = Device.QueryInterface<SharpDX.DXGI.Device>().DisposeWith(_disposable);
            var adapter = dxgiDevice.Adapter.DisposeWith(_disposable);
            var factory = adapter.GetParent<Factory>().DisposeWith(_disposable);
            _swapChain = new SwapChain(
                factory,
                Device,
                new SwapChainDescription
                {
                    BufferCount = 2,
                    IsWindowed = true,
                    ModeDescription = new ModeDescription
                        { Width = window.Width, Height = window.Height, Format = Format.R8G8B8A8_UNorm },
                    OutputHandle = windowHandle.Value,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.FlipSequential,
                    Usage = Usage.RenderTargetOutput,
                }).DisposeWith(_disposable);

            var backBuffer = _swapChain.GetBackBuffer<Texture2D>(0).DisposeWith(_disposable);
            _renderTargetView = new RenderTargetView(Device, backBuffer).DisposeWith(_disposable);

            Device.ImmediateContext.Rasterizer.SetViewport(0, 0, window.Width, window.Height);

            var depthStencilState = new DepthStencilState(
                    Device,
                    new DepthStencilStateDescription
                    {
                        IsDepthEnabled = true,
                        DepthWriteMask = DepthWriteMask.All,
                        DepthComparison = Comparison.Less,
                    })
                .DisposeWith(_disposable);

            Device.ImmediateContext.OutputMerger.SetDepthStencilState(depthStencilState);

            var depthStencilTexture = new Texture2D(Device,
                    new Texture2DDescription
                    {
                        Width = window.Width,
                        Height = window.Height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = Format.D32_Float,
                        SampleDescription = _swapChain.Description.SampleDescription,
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.DepthStencil
                    })
                .DisposeWith(_disposable);

            _depthStencilView = new DepthStencilView(Device, depthStencilTexture).DisposeWith(_disposable);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            _disposable.Dispose();
        }
    }

    public DeviceLogger Logger { get; }

    public SharpDX.Direct3D11.Device Device { get; }

    public void AddDrawable(IDrawable drawable) => _drawables.Enqueue(drawable);

    public Task Work(CancellationToken token)
    {
        return Task.Run(() =>
            {
                DrawPipelineMetadata drawPipelineMetadata = default;
                while (!token.IsCancellationRequested)
                {
                    Device.ImmediateContext.OutputMerger.SetRenderTargets(_depthStencilView, _renderTargetView);
                    Device.ImmediateContext.ClearRenderTargetView(_renderTargetView, new RawColor4(0f, 0.5f, 0f, 1f));
                    Device.ImmediateContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth, 1f, 0);

                    foreach (var drawable in _drawables)
                    {
                        drawPipelineMetadata = drawable.Draw(drawPipelineMetadata, Device);
                    }

                    if (_swapChain.Present(1, PresentFlags.None).Failure)
                    {
                        Logger.FlushMessages();
                    }
                }
            },
            token);
        //.ContinueWith(t=> _logger.FlushMessages(), TaskContinuationOptions.ExecuteSynchronously);
    }

    public void Dispose() => _disposable.Dispose();
}