using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Resources;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Graphics.Drawables;

internal sealed class Plane : IDrawable
{
    private readonly Device _device;
    private readonly IResourceFactory _resourceFactory;
    private readonly Buffer _vertexBuffer;
    private readonly Buffer _indexBuffer;

    private Func<Buffer> _updateTransformMatrix;
    private readonly VertexShader _vertexShader;
    private readonly InputLayout _inputLayout;
    private readonly PixelShader _pixelShader;
    private readonly SamplerState _samplerState;
    private readonly ShaderResourceView _pixelShaderTextureView;

    public Plane(Device device, IResourceFactory resourceFactory)
    {
        _device = device;
        _resourceFactory = resourceFactory;

        _vertexBuffer = resourceFactory.EnsureBuffer(device, AuxiliaryData.Vertices.Key, AuxiliaryData.Vertices.Data, BindFlags.VertexBuffer);
        _indexBuffer = resourceFactory.EnsureBuffer(device, AuxiliaryData.TriangleIndices.Key, AuxiliaryData.TriangleIndices.Data, BindFlags.IndexBuffer);

        var compiledVertexShader = resourceFactory.EnsureVertexShader(device, Constants.Shaders.WithTexCoordAndSampler, "VShader");
        _vertexShader = compiledVertexShader.Shader;

        var positionLayout = new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0);
        var texCoordLayout = new InputElement("TexCoord", 0, Format.R32G32_Float, Marshal.SizeOf<RawVector3>(), 0);
        _inputLayout = resourceFactory.EnsureInputLayout(device, compiledVertexShader.ByteCode, positionLayout, texCoordLayout);
        _pixelShader = resourceFactory.EnsurePixelShader(device, Constants.Shaders.WithTexCoordAndSampler, "PShader");

        _pixelShaderTextureView = resourceFactory.EnsureTextureAsPixelShaderResourceView(device, "1i.jpg");

        _samplerState = resourceFactory.EnsureCrated($"{nameof(Plane)}_SamplerState",
            () =>
            {
                var desc = new SamplerStateDescription
                {
                    Filter = Filter.MinMagMipLinear,
                    AddressU = TextureAddressMode.Wrap,
                    AddressV = TextureAddressMode.Wrap,
                    AddressW = TextureAddressMode.Wrap,
                };
                return new SamplerState(device, desc);
            });
    }

    public void RegisterWorldTransform(Func<Transforms> transform)
        => _updateTransformMatrix = _resourceFactory.EnsureUpdateBuffer(_device, AuxiliaryData.TransformMatrixKey, () => transform().Merged());

    public DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device)
    {
        var currentMetadata = previous;

        currentMetadata = currentMetadata.EnsureVertexBufferBinding<VertexWithTexCoord>(device, _vertexBuffer);
        currentMetadata = currentMetadata.EnsureIndexBufferBinding(device, _indexBuffer);

        currentMetadata = currentMetadata.EnsureInputLayoutBinding(device, _inputLayout);
        currentMetadata = currentMetadata.EnsureVertexShaderBinding(device, _vertexShader);
        currentMetadata = currentMetadata.EnsurePixelShader(device, _pixelShader);
        currentMetadata = currentMetadata.EnsureSamplerState(device, _samplerState);
        currentMetadata = currentMetadata.EnsurePixelShaderTextureView(device, _pixelShaderTextureView);

        var updatedConstantBuffer = _updateTransformMatrix();
        device.ImmediateContext.VertexShader.SetConstantBuffer(0, updatedConstantBuffer);

        device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

        device.ImmediateContext.DrawIndexed(AuxiliaryData.TriangleIndices.Data.Length, 0, 0);

        return currentMetadata;
    }
}

file static class AuxiliaryData
{
    public const string TransformMatrixKey = $"{nameof(Plane)}_TransformMatrix";

    public static readonly (string Key, VertexWithTexCoord[] Data) Vertices =
        ($"{nameof(Plane)}_Vertices",
            new[]
            {
                new VertexWithTexCoord(new RawVector3(-1, -1, 0), new RawVector2(0, 1)),
                new VertexWithTexCoord(new RawVector3(-1, 1, 0), new RawVector2(0, 0)),
                new VertexWithTexCoord(new RawVector3(1, 1, 0), new RawVector2(1, 0)),
                new VertexWithTexCoord(new RawVector3(1, -1, 0), new RawVector2(1, 1))
            });

    public static readonly (string Key, int[] Data) TriangleIndices =
        ($"{nameof(Plane)}_Indices",
            new[]
            {
                0, 1, 2,
                2, 3, 0
            });
}