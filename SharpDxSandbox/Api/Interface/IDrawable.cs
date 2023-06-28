using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDxSandbox.Api.Interface;

public interface IDrawable
{
    void RegisterWorldTransform(Func<Matrix> transform);

    DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device);
}

public readonly record struct DrawPipelineMetadata(
    int VertexBufferHash,
    int VertexShaderHash,
    int VertexShaderConstantBufferHash,
    int InputLayoutHash,
    int IndexBufferHash,
    int PixelShaderHash,
    int PixelShaderConstantBufferHash);