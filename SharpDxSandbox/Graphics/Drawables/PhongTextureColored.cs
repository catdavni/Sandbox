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

internal sealed class PhongTextureColored : IDrawable
{
    private readonly Device _device;
    private readonly IResourceFactory _resourceFactory;
    private readonly int[] _indices;
    private readonly Buffer _indexBuffer;
    private readonly VertexShader _vertexShader;
    private readonly InputLayout _inputLayout;
    private readonly Buffer _vertexBuffer;
    private readonly PixelShader _pixelShader;
    private readonly ShaderResourceView _pixelShaderDiffuseTextureView;
    private readonly ShaderResourceView _pixelShaderSpecularTextureView;
    private Func<Buffer> _updateLightSourcePosition;
    private Func<Buffer> _updateWorldViewMatrix;
    private Func<Buffer> _updateCameraProjectionMatrix;
    private Func<Buffer> _updateCameraPosition;

    private Material _material = Material.RandomColor;
    private Func<Buffer> _materialModifier;
    private readonly SamplerState _samplerDiffuseSamplerState;
    private readonly bool _hasSpecularTexture;

    public PhongTextureColored(
        Device device,
        IResourceFactory resourceFactory,
        Vertex_Normal_TexCoord[] vertexNormalTexCoords,
        int[] indices,
        string key,
        Assimp.Material material)
    {
        _device = device;
        _resourceFactory = resourceFactory;
        _indices = indices;

        _material = new Material(Vector4.One, new LightTraits(0.2f, DiffuseIntensity: 0.3f, SpecularIntensity: 6f, SpecularPower: 0f /* material.Shininess*/), Material.RandomColor.Attenuation);

        _materialModifier = _resourceFactory.EnsureUpdateBuffer(
            _device,
            $"{nameof(PhongTextureColored)}_MaterialModifier",
            () => _material);

        _vertexBuffer = resourceFactory.EnsureBuffer(device, MakeKey("Vertices"), vertexNormalTexCoords, BindFlags.VertexBuffer);
        _indexBuffer = resourceFactory.EnsureBuffer(device, MakeKey("Indices"), indices, BindFlags.IndexBuffer);

        var compiledVertexShader = resourceFactory.EnsureVertexShader(device, Constants.Shaders.PhongShadingTextureBased, "VShader");
        _vertexShader = compiledVertexShader.Shader;

        var positionLayout = new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0);
        var normalLayout = new InputElement("Normal", 0, Format.R32G32B32_Float, Marshal.SizeOf<RawVector3>(), 0);
        var texCoordLayout = new InputElement("TexCoord", 0, Format.R32G32_Float, Marshal.SizeOf<RawVector3>() * 2, 0);
        _inputLayout = resourceFactory.EnsureInputLayout(device, compiledVertexShader.ByteCode, positionLayout, normalLayout, texCoordLayout);
        _pixelShader = resourceFactory.EnsurePixelShader(device, Constants.Shaders.PhongShadingTextureBased, "PShader");

        _pixelShaderDiffuseTextureView = resourceFactory.EnsureTextureAsPixelShaderResourceView(device, Path.Combine(Constants.Models.NanoSuit.FolderName, material.TextureDiffuse.FilePath));

        _hasSpecularTexture = material.HasTextureSpecular;
        if (material.HasTextureSpecular)
        {
            _pixelShaderSpecularTextureView = resourceFactory.EnsureTextureAsPixelShaderResourceView(device, Path.Combine(Constants.Models.NanoSuit.FolderName, material.TextureSpecular.FilePath));
        }
        else
        {
            _pixelShaderSpecularTextureView = resourceFactory.EnsureBlackFrameAsPixelShaderResourceView(device);
        }

        _samplerDiffuseSamplerState = resourceFactory.EnsureCrated($"{nameof(PhongTextureColored)}_DiffuseSamplerState",
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

        string MakeKey(string purpose) => $"{nameof(PhongTextureColored)}_{key}_{purpose}";
    }

    public void RegisterMaterialModifier(Func<Material, Material> materialModifier)
        => _materialModifier = _resourceFactory.EnsureUpdateBuffer(
            _device,
            $"{nameof(PhongTextureColored)}_MaterialModifier",
            () => materialModifier(_material));

    public void RegisterTransforms(Func<TransformationData> transformationData)
    {
        _updateLightSourcePosition = _resourceFactory.EnsureUpdateBuffer(
            _device,
            $"{nameof(PhongTextureColored)}_LightSourcePosition",
            () => transformationData().LightSourcePosition);

        _updateWorldViewMatrix = _resourceFactory.EnsureUpdateBuffer(
            _device,
            $"{nameof(PhongTextureColored)}_WorldViewTransformMatrix",
            () =>
            {
                var data = transformationData();
                return data.Model * data.World;
            });
        _updateCameraProjectionMatrix = _resourceFactory.EnsureUpdateBuffer(
            _device,
            $"{nameof(PhongTextureColored)}_CameraProjectionViewTransformMatrix",
            () =>
            {
                var data = transformationData();
                return data.Camera * data.Projection;
            });
        _updateCameraPosition = _resourceFactory.EnsureUpdateBuffer(
            _device,
            $"{nameof(PhongTextureColored)}_cameraPosition",
            () =>
            {
                var data = transformationData();
                return new Vector4(data.CameraPosition, 1);
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
        //currentMetadata = currentMetadata.EnsureSamplerState(device, _samplerState);
        //currentMetadata = currentMetadata.EnsurePixelShaderTextureView(device, _pixelShaderTextureView);

        var updateWorldViewMatrix = _updateWorldViewMatrix();
        device.ImmediateContext.VertexShader.SetConstantBuffer(0, updateWorldViewMatrix);

        var updateCameraProjectionView = _updateCameraProjectionMatrix();
        device.ImmediateContext.VertexShader.SetConstantBuffer(1, updateCameraProjectionView);

        var updateLightSourcePosition = _updateLightSourcePosition();
        device.ImmediateContext.PixelShader.SetConstantBuffer(0, updateLightSourcePosition);

        var updateCameraPosition = _updateCameraPosition();
        device.ImmediateContext.PixelShader.SetConstantBuffer(1, updateCameraPosition);

        var updatedMaterial = _materialModifier();
        device.ImmediateContext.PixelShader.SetConstantBuffer(2, updatedMaterial);

        device.ImmediateContext.PixelShader.SetShaderResource(0, _pixelShaderDiffuseTextureView);
        device.ImmediateContext.PixelShader.SetShaderResource(1, _pixelShaderSpecularTextureView);
        device.ImmediateContext.PixelShader.SetSampler(0, _samplerDiffuseSamplerState);

        device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        device.ImmediateContext.DrawIndexed(_indices.Length, 0, 0);

        return currentMetadata;
    }
}