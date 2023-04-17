using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.DirextXApiHelpers;
using SharpDxSandbox.Window;
using Buffer = SharpDX.Direct3D11.Buffer;
using InfoQueueFilter = SharpDX.Direct3D11.InfoQueueFilter;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace SharpDxSandbox.Sandbox;

public class DirectXSandbox
{
    public static async Task StartTriangle()
    {
        var windowWidth = 200;
        var windowHeight = 200;

        using (var window = new PresenterWindowLoop(windowWidth, windowHeight, WindowOptions.TopMost))
        {
            window.KeyPressed += (sender, eventArgs) => Console.WriteLine(eventArgs.Input);

            using var resetEvent = new AutoResetEvent(false);
            var windowHandle = window.GetWindowHandleAsync().Result;
            ///////////////////////////////////////////

            SharpDX.Direct3D11.Device.CreateWithSwapChain(
                DriverType.Hardware,
                DeviceCreationFlags.Debug,
                new SwapChainDescription
                {
                    BufferCount = 2,
                    IsWindowed = true,
                    ModeDescription = new ModeDescription(windowWidth, windowHeight, Rational.Empty, Format.R8G8B8A8_UNorm),
                    OutputHandle = windowHandle.Value,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.FlipDiscard,
                    Usage = Usage.RenderTargetOutput,
                },
                out var outDevice,
                out var outSwapChain);
            using var device = outDevice;
            using var oldSwapChain = outSwapChain;
            using var swapChain = oldSwapChain.QueryInterface<SwapChain3>();
            using var logger = new DeviceLogger(device);
            
            Vertex[] vertices =
            {
                new(0.0f, 0.5f, 0.0f, new Color4(1.0f, 0.0f, 0.0f, 1.0f)),
                new(0.45f, -0.5f, 0.0f, new Color4(1.0f, 1.0f, 0.0f, 1.0f)),
                new(-0.45f, -0.5f, 0.0f, new Color4(1.0f, 0.0f, 1.0f, 1.0f))
            };

            using var vertexDataStream = DataStream.Create(vertices, true, false);
            using var vertexBuffer = new Buffer(
                device,
                vertexDataStream,
                new BufferDescription
                {
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    SizeInBytes = Marshal.SizeOf<Vertex>() * vertices.Length
                });

            // creating shaders
            using var vertexShaderByteCode = ShaderBytecode.CompileFromFile("Resources/shaders.shader", "VShader", "vs_4_0");
            using var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Resources/shaders.shader", "PShader", "ps_4_0");
            using var vertexShader = new VertexShader(device, vertexShaderByteCode);
            using var pixelShader = new PixelShader(device, pixelShaderByteCode);
            device.ImmediateContext.VertexShader.Set(vertexShader);
            device.ImmediateContext.PixelShader.Set(pixelShader);
            ///////////////////////////////////////

            // setting input layout
            var positionInputElement = new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0);
            var colorOffset = FormatHelper.SizeOfInBytes(Format.R32G32B32_Float);
            var colorInputElement = new InputElement("COLOR", 0, Format.R32G32B32A32_Float, colorOffset, 0);
            using var inputLayout = new InputLayout(device, vertexShaderByteCode, new[] { positionInputElement, colorInputElement });
            device.ImmediateContext.InputAssembler.InputLayout = inputLayout;

            using var backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
            using var renderTargetView = new RenderTargetView(device, backBuffer);
            device.ImmediateContext.OutputMerger.SetRenderTargets(renderTargetView);

            //--------------------
            device.ImmediateContext.Rasterizer.SetViewport(0f,0f, windowWidth, windowHeight);
            //--------------------

            using var drawingsCancellation = new CancellationTokenSource();
            var drawings = Task.Run(() =>
                {
                    var colorShift = 0f;
                    var stride = Marshal.SizeOf<Vertex>();
                    device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                    device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, stride, 0));
                    

                    try
                    {
                        while (!drawingsCancellation.IsCancellationRequested)
                        {
                            device.ImmediateContext.OutputMerger.SetRenderTargets(renderTargetView);
                            device.ImmediateContext.ClearRenderTargetView(renderTargetView, new RawColor4(0, (float)Math.Sin(colorShift), 0, 0f));
                            device.ImmediateContext.Draw(3, 0);
                            swapChain.Present(1, PresentFlags.None);

                            colorShift += 0.05f;
                        }
                    }
                    catch (SEHException e)
                    {
                        logger.FlushMessages();
                    }
                },
                drawingsCancellation.Token);

            ////////////////////////////////////////////

            window.WindowClosed += (sender, eventArgs) =>
            {
                drawingsCancellation.Cancel();
                resetEvent.Set();
            };
            resetEvent.WaitOne();
            await drawings;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex
    {
        public float X;
        public float Y;
        public float Z;
        private Color4 Color;

        public Vertex(float x, float y, float z, Color4 color)
        {
            X = x;
            Y = y;
            Z = z;
            Color = color;
        }
    }
}