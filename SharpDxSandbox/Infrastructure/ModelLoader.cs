using System.Diagnostics;
using Assimp;
using Assimp.Unmanaged;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Graphics;
using SharpDxSandbox.Graphics.Drawables;
using SharpDxSandbox.Infrastructure.Disposables;
using SharpDxSandbox.Resources;

namespace SharpDxSandbox.Infrastructure;

internal static class ModelLoader
{
    private static readonly string[] ModelsPath =
    {
        Path.Combine("Resources", "Models"),
        Path.Combine("Resources", "Models", "NanoSuit")
    };
    private static readonly Dictionary<string, Scene> SceneCache = new();

    public static FromModel LoadSimple(Device device, IResourceFactory resourceFactory, string modelName)
    {
        var (vertices, indices, _) = LoadModel(modelName, false);
        return new FromModel(device, resourceFactory, vertices, indices, modelName);
    }

    public static LightSource LoadLightSource(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "sphere.obj";
        var (vertices, indices, _) = LoadModel(modelName, false);
        return new LightSource(device, resourceFactory, vertices, indices, nameof(LightSource));
    }

    public static IDrawable LoadSkinnedCube(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "cube.obj";
        var scene = EnsureScene(modelName, false);
        var mesh = scene.Meshes[0];
        var indices = mesh.GetIndices();
        var texCoords = mesh.TextureCoordinateChannels[0];
        Debug.Assert(mesh.Vertices.Count == texCoords.Count, $"Mismatch indices and texCoord count for {modelName}");
        var vertices = mesh.Vertices.Zip(texCoords).Select(v =>
        {
            var (vertex, texCoord) = v;
            return new VertexWithTexCoord(new RawVector3(vertex.X, vertex.Y, vertex.Z), new RawVector2(texCoord.X, texCoord.Y));
        }).ToArray();
        return new SkinnedFromModel(device, resourceFactory, vertices, indices, modelName);
    }

    public static IDrawable LoadGouraudShaded(Device device, IResourceFactory resourceFactory, string modelName, bool smoothNormals)
    {
        var modelKey = modelName + "_gouraud" + (smoothNormals ? "_smooth" : "_notSmooth");
        var (vertices, indices, normals) = LoadModel(modelName, smoothNormals);
        return new GouraudShadedFromModel(device, resourceFactory, vertices, indices, normals, modelKey);
    }

    public static IDrawable LoadPhongShaded(Device device, IResourceFactory resourceFactory, string modelName, bool smoothNormals)
    {
        var modelKey = modelName + "_phong" + (smoothNormals ? "_smooth" : "_notSmooth");
        var (vertices, indices, normals) = LoadModel(modelName, smoothNormals);
        return new PhongShadedFromModel(device, resourceFactory, vertices, indices, normals, modelKey);
    }

    public static IDrawable LoadNanoSuit(Device device, IResourceFactory resourceFactory)
    {
        var scene = EnsureScene(Constants.Models.NanoSuit.ModelName, false);
        var parts = scene.Meshes.Select(m => new PhongShadedFromModel(
                device,
                resourceFactory,
                m.Vertices.Select(v => new RawVector3(v.X, v.Y, v.Z)).ToArray(),
                m.GetIndices(),
                m.Normals.Select(v => new RawVector3(v.X, v.Y, v.Z)).ToArray(),
                m.Name))
            .ToArray();

        return new NanoSuit(parts);
    }

    private static (RawVector3[] Vertices, int[] Indices, RawVector3[] Normals) LoadModel(string modelName, bool smoothNormals)
    {
        var scene = EnsureScene(modelName, smoothNormals);
        var mesh = scene.Meshes[0];
        var indices = mesh.GetIndices();
        var vertices = mesh.Vertices.Select(v => new RawVector3(v.X, v.Y, v.Z)).ToArray();
        var normals = mesh.Normals.Select(v => new RawVector3(v.X, v.Y, v.Z)).ToArray();

        return (vertices, indices, normals);
    }

    private static Scene EnsureScene(string modelName, bool smoothNormals)
    {
        var modelKey = modelName + smoothNormals;
        if (SceneCache.TryGetValue(modelKey, out var scene))
        {
            return scene;
        }

        var flags = PostProcessSteps.JoinIdenticalVertices |
                    PostProcessSteps.ImproveCacheLocality |
                    PostProcessSteps.Triangulate |
                    PostProcessSteps.PreTransformVertices |
                    PostProcessSteps.OptimizeMeshes |
                    PostProcessSteps.ValidateDataStructure;
        flags |= smoothNormals ? PostProcessSteps.GenerateSmoothNormals : PostProcessSteps.GenerateNormals;

        using var context = GetContext().Value;
        scene = context.ImportFile(modelName, flags);
        SceneCache.Add(modelKey, scene);
        return scene;
    }

    private static Disposable<AssimpContext> GetContext()
    {
        var disposables = new DisposableStack();

        var logStream = new ConsoleLogStream(nameof(ModelLoader)).DisposeWith(disposables);
        logStream.Attach();

        var fs = new FileIOSystem(ModelsPath).DisposeWith(disposables);

        var context = new AssimpContext().DisposeWith(disposables);
        AssimpLibrary.Instance.ThrowOnLoadFailure = false;
        context.SetIOSystem(fs);

        return new Disposable<AssimpContext>(context, disposables);
    }
}