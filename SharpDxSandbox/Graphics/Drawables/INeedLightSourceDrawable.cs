using SharpDX;

namespace SharpDxSandbox.Graphics.Drawables;

internal interface INeedLightSourceDrawable : IDrawable
{
    void RegisterLightSource(Func<Vector4> lightSourcePosition);
}