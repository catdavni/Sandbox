using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Api.Interface;

namespace SharpDxSandbox.Api.Implementation;

internal class ModelLoader
{
    public static FromModel LoadCube(Device device, IResourceFactory resourceFactory)
    {
        var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        using var fs = new FileIOSystem(Path.Combine(root, "Models"));

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
        var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        using var fs = new FileIOSystem(Path.Combine(root, "Models"));

        var context = new AssimpContext();
        context.SetIOSystem(fs);
        var model = context.ImportFile("sphere.obj", PostProcessSteps.Triangulate);
        
        var mesh = model.Meshes[0];
        
        var indices = mesh.GetIndices();
        var vertices = mesh.Vertices.Select(v => new RawVector3(v.X, v.Y, v.Z)).ToArray();
        return new FromModel(device, resourceFactory, vertices, indices, "sphere");
    }
}