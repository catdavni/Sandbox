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

internal sealed class ColoredCube : IDrawable
{
    private static readonly RawVector4[] SideColors =
    {
        new(1, 0, 0, 1), // front
        new(0, 1, 0, 1), // left
        new(0, 0, 1, 1), // right
        new(1, 1, 0, 1), // top
        new(1, 0, 1, 1), // bottom
        new(0, 1, 1, 1) // back
    };

    private readonly Device _device;
    private readonly IResourceFactory _resourceFactory;
    private readonly Buffer _vertexBuffer;
    private readonly Buffer _indexBuffer;
    private readonly VertexShader _vertexShader;
    private readonly InputLayout _inputLayout;
    private readonly PixelShader _pixelShader;
    private readonly Buffer _pixelShaderConstantBuffer;
    private Func<Buffer> _updateTransformMatrix;

    public ColoredCube(Device device, IResourceFactory resourceFactory)
    {
        _device = device;
        _resourceFactory = resourceFactory;

        _vertexBuffer = resourceFactory.EnsureBuffer(device, Cube.Simple.Vertices.Key, Cube.Simple.Vertices.Data, BindFlags.VertexBuffer);
        _indexBuffer = resourceFactory.EnsureBuffer(device, Cube.Simple.TriangleIndices.Key, Cube.Simple.TriangleIndices.Data, BindFlags.IndexBuffer);

        var compiledVertexShader = resourceFactory.EnsureVertexShader(device, Constants.Shaders.WithColorsConstantBuffer, "VShader");
        _vertexShader = compiledVertexShader.Shader;
        _inputLayout = resourceFactory.EnsureInputLayout(device, compiledVertexShader.ByteCode, new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0));
        _pixelShader = resourceFactory.EnsurePixelShader(device, Constants.Shaders.WithColorsConstantBuffer, "PShader");
        _pixelShaderConstantBuffer = resourceFactory.EnsureBuffer(device, $"{nameof(ColoredCube)}_{nameof(SideColors)}", SideColors, BindFlags.ConstantBuffer);
    }

    public void RegisterWorldTransform(Func<TransformationData> transformationData)
        => _updateTransformMatrix = _resourceFactory.EnsureUpdateBuffer(_device, Cube.TransformationMatrixKey, () => transformationData().Merged());

    public DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device)
    {
        var currentMetadata = previous;

        currentMetadata = currentMetadata.EnsureVertexBufferBinding<RawVector3>(device, _vertexBuffer);
        currentMetadata = currentMetadata.EnsureIndexBufferBinding(device, _indexBuffer);

        currentMetadata = currentMetadata.EnsureInputLayoutBinding(device, _inputLayout);
        currentMetadata = currentMetadata.EnsureVertexShaderBinding(device, _vertexShader);
        currentMetadata = currentMetadata.EnsurePixelShader(device, _pixelShader);
        currentMetadata = currentMetadata.EnsurePixelShaderConstantBuffer(device, _pixelShaderConstantBuffer);

        var updatedConstantBuffer = _updateTransformMatrix();
        device.ImmediateContext.VertexShader.SetConstantBuffer(0, updatedConstantBuffer);

        device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

        device.ImmediateContext.DrawIndexed(Cube.Simple.TriangleIndices.Data.Length, 0, 0);

        return currentMetadata;
    }
}