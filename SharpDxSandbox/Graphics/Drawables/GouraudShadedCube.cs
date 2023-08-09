using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Resources;
using SharpDxSandbox.Resources.Models;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Graphics.Drawables;

internal sealed class GouraudShadedCube : IDrawable
{
    private readonly Device _device;
    private readonly IResourceFactory _resourceFactory;
    private readonly Buffer _vertexBuffer;
    private readonly Buffer _indexBuffer;
    private Func<Buffer> _updateWorldViewMatrix;
    private Func<Buffer> _updateLightSourcePosition;
    private readonly VertexShader _vertexShader;
    private readonly InputLayout _inputLayout;
    private readonly PixelShader _pixelShader;
    private readonly ShaderResourceView _pixelShaderTextureView;
    private readonly SamplerState _samplerState;
    private Func<Buffer> _updateCameraProjectionMatrix;

    public GouraudShadedCube(Device device, IResourceFactory resourceFactory)
    {
        _device = device;
        _resourceFactory = resourceFactory;

        _vertexBuffer = resourceFactory.EnsureBuffer(device, Cube.Shaded.Vertices.Key, Cube.Shaded.Vertices.Data, BindFlags.VertexBuffer);
        _indexBuffer = resourceFactory.EnsureBuffer(device, Cube.Shaded.TriangleIndices.Key, Cube.Shaded.TriangleIndices.Data, BindFlags.IndexBuffer);

        var compiledVertexShader = resourceFactory.EnsureVertexShader(device, Constants.Shaders.GouraudShading, "VShader");
        _vertexShader = compiledVertexShader.Shader;

        var positionLayout = new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0);
        var normalLayout = new InputElement("Normal", 0, Format.R32G32B32_Float, Marshal.SizeOf<RawVector3>(), 0);
        var texCoordLayout = new InputElement("TexCoord", 0, Format.R32G32_Float, Marshal.SizeOf<RawVector3>() * 2, 0);

        _inputLayout = resourceFactory.EnsureInputLayout(device, compiledVertexShader.ByteCode, positionLayout, normalLayout, texCoordLayout);
        _pixelShader = resourceFactory.EnsurePixelShader(device, Constants.Shaders.GouraudShading, "PShader");

        _pixelShaderTextureView = resourceFactory.EnsureTextureAsPixelShaderResourceView(device, Constants.Images.CatCuriousVertical);

        _samplerState = resourceFactory.EnsureCrated($"{nameof(GouraudShadedCube)}_SamplerState",
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

    public void RegisterWorldTransform(Func<TransformationData> transformationData)
    {
        _updateLightSourcePosition = _resourceFactory.EnsureUpdateBuffer(
            _device,
            $"{nameof(GouraudShadedFromModel)}_LightSourcePosition",
            () => transformationData().LightSourcePosition);
        
        _updateWorldViewMatrix = _resourceFactory.EnsureUpdateBuffer(
            _device,
            $"{nameof(GouraudShadedCube)}_WorldViewTransformMatrix",
            () =>
            {
                var transformMatrices = transformationData();
                return transformMatrices.Model * transformMatrices.World;
            });
        _updateCameraProjectionMatrix = _resourceFactory.EnsureUpdateBuffer(
            _device,
            $"{nameof(GouraudShadedCube)}_CameraProjectionViewTransformMatrix",
            () =>
            {
                var transformMatrices = transformationData();
                return transformMatrices.Camera * transformMatrices.Projection;
            });
    }

    public DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device)
    {
        var currentMetadata = previous;
        currentMetadata = currentMetadata.EnsureVertexBufferBinding<Vertex_Normal_TexCoord>(device, _vertexBuffer);
        currentMetadata = currentMetadata.EnsureIndexBufferBinding(device, _indexBuffer);

        currentMetadata = currentMetadata.EnsureInputLayoutBinding(device, _inputLayout);
        currentMetadata = currentMetadata.EnsureVertexShaderBinding(device, _vertexShader);
        currentMetadata = currentMetadata.EnsurePixelShader(device, _pixelShader);
        currentMetadata = currentMetadata.EnsureSamplerState(device, _samplerState);
        currentMetadata = currentMetadata.EnsurePixelShaderTextureView(device, _pixelShaderTextureView);

        var updateWorldViewMatrix = _updateWorldViewMatrix();
        device.ImmediateContext.VertexShader.SetConstantBuffer(0, updateWorldViewMatrix);

        var updateCameraProjectionView = _updateCameraProjectionMatrix();
        device.ImmediateContext.VertexShader.SetConstantBuffer(1, updateCameraProjectionView);

        var updateLightSourcePosition = _updateLightSourcePosition();
        device.ImmediateContext.VertexShader.SetConstantBuffer(2, updateLightSourcePosition);

        device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

        device.ImmediateContext.DrawIndexed(Cube.Shaded.TriangleIndices.Data.Length, 0, 0);

        return currentMetadata;
    }

}