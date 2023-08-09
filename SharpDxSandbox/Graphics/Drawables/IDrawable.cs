using SharpDX;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Graphics.Drawables;

internal interface IDrawable
{
    void RegisterWorldTransform(Func<TransformationData> transformationData);

    DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device);
}

public readonly record struct TransformationData(Matrix Model, Matrix World, Matrix Camera, Matrix Projection, Vector4 LightSourcePosition, Vector3 CameraPosition)
{
    public Matrix Merged() => Model * World * Camera * Projection;
}