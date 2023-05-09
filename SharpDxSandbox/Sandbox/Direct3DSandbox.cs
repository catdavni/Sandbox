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
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Sandbox;

public static class Direct3DSandbox
{
    public static async Task StartTest()
    {
        await new DirextXApiHelpers.Window(600, 400).RunInWindow(Drawing);

        Task Drawing(DirextXApiHelpers.Window window, WindowHandle windowHandle, CancellationToken cancellation)
        {
            return Task.Run(() =>
                {
                    Device.CreateWithSwapChain(
                        DriverType.Hardware,
                        DeviceCreationFlags.Debug,
                        new SwapChainDescription
                        {
                            BufferCount = 2,
                            IsWindowed = true,
                            ModeDescription = new ModeDescription(window.Width, window.Height, Rational.Empty, Format.R8G8B8A8_UNorm),
                            OutputHandle = windowHandle.Value,
                            SampleDescription = new SampleDescription(1, 0),
                            SwapEffect = SwapEffect.FlipSequential,
                            Usage = Usage.RenderTargetOutput
                        },
                        out var outDevice,
                        out var outSwapChain);
                    using var device = outDevice;
                    using var oldSwapChain = outSwapChain;
                    using var swapChain = oldSwapChain.QueryInterface<SwapChain3>();
                    using var logger = new DeviceLogger(device);

                    try
                    {
                        ColoredVertex[] vertices =
                        {
                            new(new(0f, 1f, 0f), Color.Blue),
                            new(new(-0.5f, -0.3f, 0f), Color.Red),
                            new(new(-0.3f, 0.7f, 0f), Color.Green),
                            new(new(0.3f, 0.7f, 0f), Color.Green),
                            new(new(0.5f, -0.3f, 0f), Color.Blue),
                            new(new(0f, -0.7f, 0f), Color.Green),
                        };

                        // vertex buffer
                        using var vertexDataStream = DataStream.Create(vertices, true, false);
                        using var vertexBuffer = new Buffer(
                            device,
                            vertexDataStream,
                            new BufferDescription
                            {
                                //Usage = ResourceUsage.Default,
                                BindFlags = BindFlags.VertexBuffer,
                                //CpuAccessFlags = CpuAccessFlags.None,
                                SizeInBytes = Marshal.SizeOf<ColoredVertex>() * vertices.Length
                            });
 
                        // index buffer
                        var indices = new uint[]
                        {
                            0, 1, 2,
                            0, 3, 4,
                            0, 4, 1,
                            4, 5, 1
                        };
                        using var indexDataStream = DataStream.Create(indices, true, false);
                        using var indexBuffer = new Buffer(
                            device,
                            indexDataStream,
                            new BufferDescription
                            {
                                StructureByteStride = Marshal.SizeOf<int>(),
                                BindFlags = BindFlags.IndexBuffer,
                                SizeInBytes = Marshal.SizeOf<int>() * indices.Length
                            });
                        device.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);

                        // creating shaders
                        using var vertexShaderByteCode =
                            ShaderBytecode.CompileFromFile("Resources/test.shader", "VShader", "vs_4_0");
                        using var pixelShaderByteCode =
                            ShaderBytecode.CompileFromFile("Resources/test.shader", "PShader", "ps_4_0");
                        using var vertexShader = new VertexShader(device, vertexShaderByteCode);
                        using var pixelShader = new PixelShader(device, pixelShaderByteCode);
                        device.ImmediateContext.VertexShader.Set(vertexShader);
                        device.ImmediateContext.PixelShader.Set(pixelShader);
                        ///////////////////////////////////////

                        // setting input layout
                        var positionInputElement = new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0);
                        var colorOffset = Format.R32G32B32_Float.SizeOfInBytes();
                        var colorInputElement = new InputElement("COLOR", 0, Format.R32G32B32_Float, colorOffset, 0);
                        using var inputLayout = new InputLayout(device, vertexShaderByteCode, new[] { positionInputElement, colorInputElement });
                        device.ImmediateContext.InputAssembler.InputLayout = inputLayout;

                        using var backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
                        using var renderTargetView = new RenderTargetView(device, backBuffer);

                        //--------------------
                        device.ImmediateContext.Rasterizer.SetViewport(0f, 0f, window.Width, window.Height);
                        //--------------------

                        var iterationShift = 0f;
                        var stride = Marshal.SizeOf<ColoredVertex>();
                        device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                        device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, stride, 0));

                        while (!cancellation.IsCancellationRequested)
                        {
                            var vertexConstantBufferMatrix = SharpDX.Matrix.RotationZ(iterationShift)
                                                             * SharpDX.Matrix.Scaling((float)window.Height / window.Width, 1f, 1f);
                            // GPU store array as column based but CPU as row based
                            vertexConstantBufferMatrix.Transpose();
                            var vertexConstantBufferData = vertexConstantBufferMatrix.ToArray();
                           
                            using var vertexConstantBufferDataStream = DataStream.Create(vertexConstantBufferData.ToArray(), true, false);
                            using var rotationConstantBuffer = new Buffer(device,
                                vertexConstantBufferDataStream,
                                new BufferDescription()
                                {
                                    BindFlags = BindFlags.ConstantBuffer,
                                    Usage = ResourceUsage.Default,
                                    //CpuAccessFlags = CpuAccessFlags.Write,
                                    SizeInBytes = Marshal.SizeOf<float>() * vertexConstantBufferData.Length
                                });
                            device.ImmediateContext.VertexShader.SetConstantBuffer(0, rotationConstantBuffer);
                            
                            device.ImmediateContext.OutputMerger.SetRenderTargets(renderTargetView);
                            var color = new RawColor4(0.5f, (float)Math.Sin(iterationShift), 0, 1f);
                            device.ImmediateContext.ClearRenderTargetView(renderTargetView, new RawColor4(1f, 1f, 1f, 1f));

                            device.ImmediateContext.DrawIndexed(indices.Length, 0, 0);

                            var presentResult = swapChain.Present(1, PresentFlags.None);
                            if (presentResult.Failure)
                            {
                                logger.FlushMessages();
                            }

                            iterationShift += 0.03f;
                            iterationShift %= (float)(2f*Math.PI);
                        }
                    }
                    catch (SEHException)
                    {
                        logger.FlushMessages();
                    }
                },
                cancellation);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private record struct ColoredVertex(Vertex Vertex, Color Color);

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Vertex(float X, float Y, float Z);

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Color(float R, float G, float B)
    {
        public static Color Red => new(1f, 0f, 0f);

        public static Color Green => new(0f, 1f, 0f);

        public static Color Blue => new(0f, 0f, 1f);
    }
}