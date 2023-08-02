﻿using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Resources;
using SharpDxSandbox.Resources.Models;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Graphics.Drawables;

internal sealed class LightSource : IDrawable
{
    private readonly Device _device;
    private readonly IResourceFactory _resourceFactory;
    private readonly int[] _indices;
    private readonly Buffer _indexBuffer;
    private readonly VertexShader _vertexShader;
    private readonly InputLayout _inputLayout;
    private readonly Buffer _vertexBuffer;
    private readonly PixelShader _pixelShader;
    private Func<Buffer> _updateTransformMatrix;

    public LightSource(Device device, IResourceFactory resourceFactory, RawVector3[] vertices, int[] indices, string key)
    {
        _device = device;
        _resourceFactory = resourceFactory;
        _indices = indices;

        _vertexBuffer = resourceFactory.EnsureBuffer(device, MakeKey("Vertices"), vertices, BindFlags.VertexBuffer);
        _indexBuffer = resourceFactory.EnsureBuffer(device, MakeKey("Indices"), indices, BindFlags.IndexBuffer);

        var compiledVertexShader = resourceFactory.EnsureVertexShader(device, Constants.Shaders.LightSource, "VShader");
        _vertexShader = compiledVertexShader.Shader;
        _inputLayout = resourceFactory.EnsureInputLayout(device, compiledVertexShader.ByteCode, new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0));
        _pixelShader = resourceFactory.EnsurePixelShader(device, Constants.Shaders.LightSource, "PShader");

        string MakeKey(string purpose) => $"{nameof(LightSource)}_{key}_{purpose}";
    }

    public void RegisterWorldTransform(Func<Transforms> transform)
        => _updateTransformMatrix = _resourceFactory.EnsureUpdateBuffer(_device, Cube.TransformationMatrixKey, () => transform().Merged());

    public DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device)
    {
        var currentMetadata = previous;

        currentMetadata = currentMetadata.EnsureVertexBufferBinding<RawVector3>(device, _vertexBuffer);
        currentMetadata = currentMetadata.EnsureIndexBufferBinding(device, _indexBuffer);

        currentMetadata = currentMetadata.EnsureInputLayoutBinding(device, _inputLayout);
        currentMetadata = currentMetadata.EnsureVertexShaderBinding(device, _vertexShader);
        currentMetadata = currentMetadata.EnsurePixelShader(device, _pixelShader);

        device.ImmediateContext.VertexShader.SetConstantBuffer(0, _updateTransformMatrix());
        
        device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        device.ImmediateContext.DrawIndexed(_indices.Length, 0, 0);

        return currentMetadata;
    }
}