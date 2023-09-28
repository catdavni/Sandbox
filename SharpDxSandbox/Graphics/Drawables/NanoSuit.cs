using SharpDX;
using SharpDX.Direct3D11;

namespace SharpDxSandbox.Graphics.Drawables;

internal sealed class NanoSuit : IDrawable
{
    private readonly IDrawable[] _parts;

    public NanoSuit(IDrawable[] parts)
    {
        _parts = parts;
    }

    public void RegisterTransforms(Func<TransformationData> transformationData)
    {
        var newTransformationData = () =>
        {
            var global = transformationData();
            return global with { Model = Matrix.Scaling(0.1f)  * global.Model };
        };
        foreach (var part in _parts)
        {
            part.RegisterTransforms(newTransformationData);
        }
    }

    public void RegisterMaterialModifier(Func<Material, Material> materialModifier)
    {
        foreach (var part in _parts)
        {
            part.RegisterMaterialModifier(materialModifier);
        }
    }

    public DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device)
    {
        foreach (var part in _parts)
        {
            previous = part.Draw(previous, device);
        }
        return previous;
    }
}