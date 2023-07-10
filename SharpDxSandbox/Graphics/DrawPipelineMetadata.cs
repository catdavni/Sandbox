namespace SharpDxSandbox.Graphics;

public readonly record struct DrawPipelineMetadata(
    int VertexBufferHash,
    int VertexShaderHash,
    int InputLayoutHash,
    int IndexBufferHash,
    int PixelShaderHash,
    int PixelShaderTextureView,
    int SamplerHash,
    int PixelShaderConstantBufferHash);