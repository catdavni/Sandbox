using SharpDX.Direct3D11;
using SharpDxSandbox.Infrastructure;
using SharpDxSandbox.Resources;

namespace SharpDxSandbox.Graphics.Drawables;

internal static class DrawableFactory
{
    public static IDrawable Create(DrawableKind kind, Device device, IResourceFactory resourceFactory) =>
        kind switch
        {
            DrawableKind.SimpleCube => new SimpleCube(device, resourceFactory),
            DrawableKind.ColoredCube => new ColoredCube(device, resourceFactory),
            DrawableKind.ColoredFromModelCube => ModelLoader.LoadSimple(device, resourceFactory, Constants.Models.Cube),
            DrawableKind.ColoredSphere => ModelLoader.LoadSimple(device, resourceFactory, Constants.Models.Sphere),
            DrawableKind.Plane => new Plane(device, resourceFactory),
            DrawableKind.SkinnedCube => new SkinnedCube(device, resourceFactory),
            DrawableKind.SkinnedFromModelCube => ModelLoader.LoadSkinnedCube(device, resourceFactory),
            DrawableKind.GouraudShadedSkinnedCube => new GouraudShadedCube(device, resourceFactory),
            DrawableKind.LightSource => ModelLoader.LoadLightSource(device, resourceFactory),
            DrawableKind.GouraudShadedSphere => ModelLoader.LoadGouraudShaded(device, resourceFactory, Constants.Models.Sphere, false),
            DrawableKind.GouraudSmoothShadedSphere => ModelLoader.LoadGouraudShaded(device, resourceFactory, Constants.Models.Sphere, true),
            DrawableKind.GouraudShadedSuzanne => ModelLoader.LoadGouraudShaded(device, resourceFactory, Constants.Models.Suzanne, false),
            DrawableKind.PhongShadedSphere => ModelLoader.LoadPhongShaded(device, resourceFactory, Constants.Models.Sphere, true),
            DrawableKind.PhongShadedCube => ModelLoader.LoadPhongShaded(device, resourceFactory, Constants.Models.Cube, true),
            DrawableKind.PhongShadedSuzanne => ModelLoader.LoadPhongShaded(device, resourceFactory, Constants.Models.Suzanne, true),
            DrawableKind.NanoSuit => ModelLoader.LoadNanoSuit(device, resourceFactory),
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
    
    GouraudShadedSphere,
    GouraudSmoothShadedSphere,
    GouraudShadedSkinnedCube,
    GouraudShadedSuzanne,
    
    PhongShadedSphere,
    PhongShadedCube,
    PhongShadedSuzanne,
    
    NanoSuit
    
}