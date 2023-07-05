using SharpDX.Direct3D11;

namespace SharpDxSandbox.Graphics;

internal sealed record CompiledVertexShader(VertexShader Shader, byte[] ByteCode) : IDisposable
{
    public void Dispose()
    {
        Shader.Dispose();
    }
}