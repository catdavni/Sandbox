using System.Runtime.InteropServices;
using SharpDX;
using Device = SharpDX.Direct3D11.Device;

namespace SharpDxSandbox.Graphics.Drawables;

internal interface IDrawable
{
    void RegisterTransforms(Func<TransformationData> transformationData);

    virtual void RegisterMaterialModifier(Func<Material, Material> materialModifier)
    {
    }

    DrawPipelineMetadata Draw(DrawPipelineMetadata previous, Device device);
}

public readonly record struct TransformationData(Matrix Model, Matrix World, Matrix Camera, Matrix Projection, Vector4 LightSourcePosition, Vector3 CameraPosition)
{
    public Matrix Merged() => Model * World * Camera * Projection;
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Material(Vector4 MaterialColor, LightTraits LightTraits, Attenuation Attenuation, float PLACEHOLDER_NOT_USED = 0f)
{
    public static Material RandomColor => new(
        new Vector4(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle(), 0),
        new LightTraits(Ambient: 0.3f, DiffuseIntensity: 0.7f, SpecularIntensity: 3.0f, SpecularPower: 31f),
        Attenuation.Create(1f, 0.001f, 0.00002f), 0f);
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct LightTraits(float Ambient, float DiffuseIntensity, float SpecularIntensity, float SpecularPower);

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Attenuation(float Constant, float Linear, float Quadric)
{
    public static Attenuation Create(float constant, float linear, float quadric)
        => new(constant, linear, quadric);
}