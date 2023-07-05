using System.Reflection;
using Assimp;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Graphics;
using SharpDxSandbox.Graphics.Drawables;

namespace SharpDxSandbox.Infrastructure;

internal class ModelLoader
{
    private static readonly string ModelsPath = Path.Combine("Resources", "Models");
    
    public static FromModel LoadCube(Device device, IResourceFactory resourceFactory)
    {
        using var fs = new FileIOSystem(ModelsPath);

        var context = new AssimpContext();
        context.SetIOSystem(fs);
        var model = context.ImportFile("cube.obj");

        var mesh = model.Meshes[0];
        
        var indices = mesh.GetIndices();
        var vertices = mesh.Vertices.Select(v => new RawVector3(v.X, v.Y, v.Z)).ToArray();
        return new FromModel(device, resourceFactory, vertices, indices, "cube");
    }

    public static FromModel LoadSphere(Device device, IResourceFactory resourceFactory)
    {
        using var fs = new FileIOSystem(ModelsPath);

        var context = new AssimpContext();
        context.SetIOSystem(fs);
        var model = context.ImportFile("sphere.obj", PostProcessSteps.Triangulate);
        
        var mesh = model.Meshes[0];
        
        var indices = mesh.GetIndices();
        var vertices = mesh.Vertices.Select(v => new RawVector3(v.X, v.Y, v.Z)).ToArray();
        return new FromModel(device, resourceFactory, vertices, indices, "sphere");
    }
}