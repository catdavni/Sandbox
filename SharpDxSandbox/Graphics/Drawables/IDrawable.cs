using SharpDX;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Graphics.Drawables;

internal interface IDrawable
{
    void RegisterWorldTransform(Func<Matrix> transform);

    DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device);
}