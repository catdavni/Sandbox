using SharpDX;
using SharpDX.Direct3D11;
using SharpDxSandbox.Api.Interface;
using SharpDxSandbox.Utilities;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace SharpDxSandbox.Api.Implementation;

public sealed class SimpleCube : IDrawable
{
    public SimpleCube(
        INonDisposable<Buffer> asNonDisposable,
        INonDisposable<VertexShader> nonDisposable,
        INonDisposable<InputLayout> asNonDisposable1,
        INonDisposable<Buffer> nonDisposable1,
        INonDisposable<PixelShader> asNonDisposable2,
        INonDisposable<Buffer> nonDisposable2)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void RegisterWorldTransform(Func<Matrix> transform)
    {
        throw new NotImplementedException();
    }

    public DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device)
    {
        throw new NotImplementedException();
    }
}