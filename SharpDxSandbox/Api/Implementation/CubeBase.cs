using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Api.Interface;
using SharpDxSandbox.Api.PrimitiveData;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Api.Implementation;

public abstract class CubeBase : IDrawable
{
    private readonly Buffer _vertexBuffer;
    private readonly VertexShader _vertexShader;
    private readonly InputLayout _inputLayout;
    private readonly PixelShader _pixelShader;
    private readonly Buffer _pixelShaderConstantBuffer;
    private Func<Matrix> _worldTransform;

    protected CubeBase(Device device, IResourceFactory resourceFactory)
    {
        _vertexBuffer = resourceFactory.EnsureCrated(Cube.VertexBufferKey,
            () =>
            {
                using var verticesByteCode = DataStream.Create(Cube.Vertices, true, false);
                return new Buffer(
                    device,
                    verticesByteCode,
                    Marshal.SizeOf<RawVector3>() * Cube.Vertices.Length,
                    ResourceUsage.Default,
                    BindFlags.VertexBuffer,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.None,
                    Marshal.SizeOf<RawVector3>());
            });

        using var vertexShaderBytes = ShaderBytecode.CompileFromFile("Resources/cube.hlsl", "VShader", "vs_4_0");
        _vertexShader = resourceFactory.EnsureCrated(Cube.VertexShaderKey, () => new VertexShader(device, vertexShaderBytes.Bytecode));

        var inputLayoutPositionElement = new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0);
        _inputLayout = resourceFactory.EnsureCrated(Cube.InputLayout, () => new InputLayout(device, vertexShaderBytes.Bytecode, new[] { inputLayoutPositionElement }));

        _pixelShader = resourceFactory.EnsureCrated(Cube.PixelShaderKey, () =>
        {
            using var psByteCode = ShaderBytecode.CompileFromFile("Resources/cube.hlsl", "PShader", "ps_4_0");
            return new PixelShader(device, psByteCode.Bytecode);
        });

        _pixelShaderConstantBuffer = resourceFactory.EnsureCrated(Cube.PixelShaderConstantBufferKey,
            () =>
            {
                using var dataStream = DataStream.Create(Cube.SideColors, true, false);
                return new Buffer(
                    device,
                    dataStream,
                    Marshal.SizeOf<RawVector4>() * Cube.SideColors.Length,
                    ResourceUsage.Default,
                    BindFlags.ConstantBuffer,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.None,
                    Marshal.SizeOf<RawVector4>());
            });
    }

    public void RegisterWorldTransform(Func<Matrix> transform) => _worldTransform = transform;

    public virtual DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device)
    {
        var currentMetadata = previous;

        if (previous.VertexBufferHash != _vertexBuffer.GetHashCode())
        {
            device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, Marshal.SizeOf<RawVector3>(), 0));
            currentMetadata = currentMetadata with { VertexBufferHash = _vertexBuffer.GetHashCode() };
        }

        if (previous.VertexShaderHash != _vertexShader.GetHashCode())
        {
            device.ImmediateContext.VertexShader.Set(_vertexShader);
            currentMetadata = currentMetadata with { VertexShaderHash = _vertexShader.GetHashCode() };
        }

        if (previous.InputLayoutHash != _inputLayout.GetHashCode())
        {
            device.ImmediateContext.InputAssembler.InputLayout = _inputLayout;
            currentMetadata = currentMetadata with { InputLayoutHash = _inputLayout.GetHashCode() };
        }

        if (previous.PixelShaderHash != _pixelShader.GetHashCode())
        {
            device.ImmediateContext.PixelShader.Set(_pixelShader);
            currentMetadata = currentMetadata with { PixelShaderHash = _pixelShader.GetHashCode() };
        }

        if (previous.PixelShaderConstantBufferHash != _pixelShaderConstantBuffer.GetHashCode())
        {
            device.ImmediateContext.PixelShader.SetConstantBuffer(0, _pixelShaderConstantBuffer);
            currentMetadata = currentMetadata with { PixelShaderConstantBufferHash = _pixelShaderConstantBuffer.GetHashCode() };
        }

        var worldTransform = _worldTransform();
        using var vsCbDataStream = DataStream.Create(worldTransform.ToArray(), true, true);
        using var transformationBuffer = new Buffer(
            device,
            vsCbDataStream,
            Marshal.SizeOf<Matrix>(),
            ResourceUsage.Dynamic,
            BindFlags.ConstantBuffer,
            CpuAccessFlags.Write,
            ResourceOptionFlags.None,
            Marshal.SizeOf<float>());
        device.ImmediateContext.VertexShader.SetConstantBuffer(0, transformationBuffer);

        return currentMetadata;
    }

    public void Dispose()
    {
    }
}