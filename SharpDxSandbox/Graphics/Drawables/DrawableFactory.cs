using SharpDX.Direct3D11;
using SharpDxSandbox.Infrastructure;

namespace SharpDxSandbox.Graphics.Drawables;

internal static class DrawableFactory
{
    public static IDrawable Create(DrawableKind kind, Device device, IResourceFactory resourceFactory) =>
        kind switch
        {
            DrawableKind.SimpleCube => new SimpleCube(device, resourceFactory),
            DrawableKind.ColoredCube => new ColoredCube(device, resourceFactory),
            DrawableKind.ColoredFromModelCube => ModelLoader.LoadCube(device, resourceFactory),
            DrawableKind.ColoredSphere => ModelLoader.LoadSphere(device, resourceFactory),
            DrawableKind.Plane => new Plane(device, resourceFactory),
            DrawableKind.SkinnedCube => new SkinnedCube(device, resourceFactory),
            DrawableKind.SkinnedFromModelCube => ModelLoader.LoadSkinnedCube(device, resourceFactory),
            DrawableKind.GouraudShadedSkinnedCube => new GouraudShadedCube(device, resourceFactory),
            DrawableKind.LightSource => ModelLoader.LoadLightSource(device, resourceFactory),
            DrawableKind.GouraudShadedSphere => ModelLoader.LoadGouraudShadedSphere(device, resourceFactory),
            DrawableKind.GouraudSmoothShadedSphere => ModelLoader.LoadGouraudSmoothShadedSphere(device, resourceFactory),
            DrawableKind.PhongShadedSphere => ModelLoader.LoadPhongShadedSphere(device, resourceFactory),
            DrawableKind.PhongShadedCube => ModelLoader.LoadPhongShadedCube(device, resourceFactory),
            _ => throw new InvalidOperationException($"Model kind {kind} not supported")
        };

}

internal enum DrawableKind
{
    SimpleCube,
    ColoredCube,
    ColoredFromModelCube,
    ColoredSphere,
    Plane,
    SkinnedCube,
    SkinnedFromModelCube,
    LightSource,
    // ShadedColoredCube,
    GouraudShadedSphere,
    GouraudSmoothShadedSphere,
    PhongShadedSphere,
    PhongShadedCube,
    // ShadedPlane,
    GouraudShadedSkinnedCube,
    //ShadedSkinnedFromModelCube,
}