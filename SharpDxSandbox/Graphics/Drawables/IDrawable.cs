using SharpDX;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Graphics.Drawables;

internal interface IDrawable
{
    void RegisterWorldTransform(Func<Transforms> transform);

    DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device);
}

public readonly record struct Transforms(Matrix Model, Matrix World, Matrix Camera, Matrix Projection)
{
    public Matrix Merged() => Model * World * Camera * Projection;
}