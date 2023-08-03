﻿using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Resources;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Graphics.Drawables;

internal sealed class ShadedFromModel : INeedLightSourceDrawable
{
    private readonly Device _device;
    private readonly IResourceFactory _resourceFactory;
    private readonly int[] _indices;
    private readonly Buffer _indexBuffer;
    private readonly VertexShader _vertexShader;
    private readonly InputLayout _inputLayout;
    private readonly Buffer _vertexBuffer;
    private readonly PixelShader _pixelShader;
    // private readonly Buffer _pixelShaderConstantBuffer;
    // private Func<Buffer> _updateTransformMatrix;
    private Func<Buffer> _updateLightSourcePosition;
    private Func<Buffer> _updateWorldViewMatrix;
    private Func<Buffer> _updateCameraProjectionMatrix;
    private readonly ShaderResourceView _pixelShaderTextureView;
    private readonly SamplerState _samplerState;

    public ShadedFromModel(Device device, IResourceFactory resourceFactory, RawVector3[] vertices, int[] indices, RawVector3[] normals, string key)
    {
        _device = device;
        _resourceFactory = resourceFactory;
        _indices = indices;

        var verticesWithNormals = vertices.Zip(normals).Select(v => new Vertex_Normal_TexCoord(v.First, v.Second, Vector2.Zero)).ToArray();
        _vertexBuffer = resourceFactory.EnsureBuffer(device, MakeKey("Vertices"), verticesWithNormals, BindFlags.VertexBuffer);
        _indexBuffer = resourceFactory.EnsureBuffer(device, MakeKey("Indices"), indices, BindFlags.IndexBuffer);

        var compiledVertexShader = resourceFactory.EnsureVertexShader(device, Constants.Shaders.GouraudShading, "VShader");
        _vertexShader = compiledVertexShader.Shader;

        var positionLayout = new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0);
        var normalLayout = new InputElement("Normal", 0, Format.R32G32B32_Float, Marshal.SizeOf<RawVector3>(), 0);
        var texCoordLayout = new InputElement("TexCoord", 0, Format.R32G32_Float, Marshal.SizeOf<RawVector3>() * 2, 0);
        _inputLayout = resourceFactory.EnsureInputLayout(device, compiledVertexShader.ByteCode, positionLayout, normalLayout, texCoordLayout);
        _pixelShader = resourceFactory.EnsurePixelShader(device, Constants.Shaders.GouraudShading, "PShader");
        // _pixelShaderConstantBuffer = resourceFactory.EnsureBuffer(device, Cube.SideColors.Key, Cube.SideColors.Data, BindFlags.ConstantBuffer);

        _pixelShaderTextureView = resourceFactory.EnsureTextureAsPixelShaderResourceView(device, Constants.Images.CatCuriousVertical);

        _samplerState = resourceFactory.EnsureCrated($"{nameof(ShadedCube)}_SamplerState",
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

        string MakeKey(string purpose) => $"{nameof(FromModel)}_{key}_{purpose}";
    }

    public void RegisterLightSource(Func<Vector4> lightSourcePosition)
    {
        _updateLightSourcePosition = _resourceFactory.EnsureUpdateBuffer(
            _device,
            $"{nameof(ShadedFromModel)}_LightSourcePosition",
            lightSourcePosition);
    }

    public void RegisterWorldTransform(Func<Transforms> transform)
    {
        _updateWorldViewMatrix = _resourceFactory.EnsureUpdateBuffer(
            _device,
            $"{nameof(ShadedCube)}_WorldViewTransformMatrix",
            () =>
            {
                var transformMatrices = transform();
                return transformMatrices.Model * transformMatrices.World;
            });
        _updateCameraProjectionMatrix = _resourceFactory.EnsureUpdateBuffer(
            _device,
            $"{nameof(ShadedCube)}_CameraProjectionViewTransformMatrix",
            () =>
            {
                var transformMatrices = transform();
                return transformMatrices.Camera * transformMatrices.Projection;
            });
    }

    public DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device)
    {
        // var currentMetadata = previous;
        //
        // currentMetadata = currentMetadata.EnsureVertexBufferBinding<RawVector3>(device, _vertexBuffer);
        // currentMetadata = currentMetadata.EnsureIndexBufferBinding(device, _indexBuffer);
        //
        // currentMetadata = currentMetadata.EnsureInputLayoutBinding(device, _inputLayout);
        // currentMetadata = currentMetadata.EnsureVertexShaderBinding(device, _vertexShader);
        // currentMetadata = currentMetadata.EnsurePixelShader(device, _pixelShader);
        // currentMetadata = currentMetadata.EnsurePixelShaderConstantBuffer(device, _pixelShaderConstantBuffer);
        //
        // device.ImmediateContext.VertexShader.SetConstantBuffer(0, _updateTransformMatrix());

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
        device.ImmediateContext.DrawIndexed(_indices.Length, 0, 0);

        return currentMetadata;
    }
}