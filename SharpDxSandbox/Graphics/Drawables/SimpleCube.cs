using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Resources.Models;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Graphics.Drawables;

internal sealed class SimpleCube : IDrawable
{
    private static readonly int[] LineIndices =
    {
        3, 0,
        0, 1,
        1, 2,
        2, 3,
        1, 5,
        5, 6,
        6, 2,
        3, 7,
        7, 6,
        7, 4,
        4, 0,
        5, 4
    };
    
    private readonly Buffer _indexBuffer;
    private readonly Device _device;
    private readonly IResourceFactory _resourceFactory;
    private readonly Buffer _vertexBuffer;
    private readonly VertexShader _vertexShader;
    private readonly InputLayout _inputLayout;
    private readonly PixelShader _pixelShader;
    private readonly Buffer _pixelShaderConstantBuffer;
    private Func<Buffer> _updateTransformMatrix;

    public SimpleCube(Device device, IResourceFactory resourceFactory)
    {
        _device = device;
        _resourceFactory = resourceFactory;
        
        _vertexBuffer = resourceFactory.EnsureBuffer(device, Cube.Vertices.Key, Cube.Vertices.Data, BindFlags.VertexBuffer);
        _indexBuffer = resourceFactory.EnsureBuffer(device, $"{nameof(SimpleCube)}LineIndexBuffer", LineIndices, BindFlags.IndexBuffer);
        
        var compiledVertexShader = resourceFactory.EnsureVertexShader(device, "cube.hlsl", "VShader");
        _vertexShader = compiledVertexShader.Shader;
        _inputLayout = resourceFactory.EnsureInputLayout(device, compiledVertexShader.ByteCode, new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0));
        _pixelShader = resourceFactory.EnsurePixelShader(device, "cube.hlsl", "PShader");
        _pixelShaderConstantBuffer = resourceFactory.EnsureBuffer(device, Cube.SideColors.Key, Cube.SideColors.Data, BindFlags.ConstantBuffer);
    }
    
    public void RegisterWorldTransform(Func<Matrix> transform) 
        => _updateTransformMatrix = _resourceFactory.EnsureUpdateTransformMatrix(_device, Cube.TransformationMatrixKey, transform);

    public DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device)
    {
        var currentMetadata = previous;
        
        currentMetadata = currentMetadata.EnsureVertexBufferBinding<RawVector3>(device, _vertexBuffer);
        currentMetadata = currentMetadata.EnsureInputLayoutBinding(device, _inputLayout);
        currentMetadata = currentMetadata.EnsureVertexShaderBinding(device, _vertexShader);
        currentMetadata = currentMetadata.EnsurePixelShader(device, _pixelShader);
        currentMetadata = currentMetadata.EnsurePixelShaderConstantBuffer(device, _pixelShaderConstantBuffer);

        var updatedConstantBuffer = _updateTransformMatrix();
        device.ImmediateContext.VertexShader.SetConstantBuffer(0, updatedConstantBuffer);

        currentMetadata = currentMetadata.EnsureIndexBufferBinding(device, _indexBuffer);

        device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;

        device.ImmediateContext.DrawIndexed(LineIndices.Length, 0, 0);

        return currentMetadata;
    }
}