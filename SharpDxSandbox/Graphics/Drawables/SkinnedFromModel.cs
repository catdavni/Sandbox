﻿using System.Runtime.InteropServices;
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

internal sealed class SkinnedFromModel : IDrawable
{
    private readonly Device _device;
    private readonly IResourceFactory _resourceFactory;
    private readonly int[] _indices;
    private readonly Buffer _vertexBuffer;
    private readonly Buffer _indexBuffer;
    private readonly VertexShader _vertexShader;
    private readonly InputLayout _inputLayout;
    private readonly PixelShader _pixelShader;
    private readonly ShaderResourceView _pixelShaderTextureView;
    private readonly SamplerState _samplerState;
    private Func<Buffer> _updateTransformMatrix;

    public SkinnedFromModel(Device device, IResourceFactory resourceFactory, VertexWithTexCoord[] vertices, int[] indices, string key)
    {
        _device = device;
        _resourceFactory = resourceFactory;
        _indices = indices;

        _vertexBuffer = resourceFactory.EnsureBuffer(device, MakeKey("Vertices"), vertices, BindFlags.VertexBuffer);
        _indexBuffer = resourceFactory.EnsureBuffer(device,  MakeKey("Indices"), indices, BindFlags.IndexBuffer);

        var compiledVertexShader = resourceFactory.EnsureVertexShader(device, Constants.Shaders.WithTexCoordAndSampler, "VShader");
        _vertexShader = compiledVertexShader.Shader;

        var positionLayout = new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0);
        var texCoordLayout = new InputElement("TexCoord", 0, Format.R32G32_Float, Marshal.SizeOf<RawVector3>(), 0);
        _inputLayout = resourceFactory.EnsureInputLayout(device, compiledVertexShader.ByteCode, positionLayout, texCoordLayout);
        _pixelShader = resourceFactory.EnsurePixelShader(device, Constants.Shaders.WithTexCoordAndSampler, "PShader");

        _pixelShaderTextureView = resourceFactory.EnsureTextureAsPixelShaderResourceView(device, Constants.Images.CubeSides);

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
        string MakeKey(string purpose) => $"{nameof(SkinnedFromModel)}_{key}_{purpose}";
    }

    public void RegisterTransforms(Func<TransformationData> transformationData)
        => _updateTransformMatrix = _resourceFactory.EnsureUpdateBuffer(_device, $"{nameof(SkinnedFromModel)}_TransformMatrix", () => transformationData().Merged());

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

        device.ImmediateContext.DrawIndexed(_indices.Length, 0, 0);

        return currentMetadata;
    }
}