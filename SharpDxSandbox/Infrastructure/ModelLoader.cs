using System.Diagnostics;
using Assimp;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Graphics;
using SharpDxSandbox.Graphics.Drawables;
using SharpDxSandbox.Infrastructure.Disposables;
using SharpDxSandbox.Resources;

namespace SharpDxSandbox.Infrastructure;

internal static class ModelLoader
{
    private static readonly string ModelsPath = Path.Combine("Resources", "Models");
    private static readonly Dictionary<string, Scene> SceneCache = new();

    public static FromModel LoadCube(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "cube.obj";
        var (vertices, indices, _) = LoadModel(resourceFactory, modelName, false);
        return new FromModel(device, resourceFactory, vertices, indices, modelName);
    }

    public static FromModel LoadSphere(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "sphere.obj";
        var (vertices, indices, _) = LoadModel(resourceFactory, modelName, false);
        return new FromModel(device, resourceFactory, vertices, indices, modelName);
    }

    public static LightSource LoadLightSource(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "sphere.obj";
        var (vertices, indices, _) = LoadModel(resourceFactory, modelName, false);
        return new LightSource(device, resourceFactory, vertices, indices, nameof(LightSource));
    }

    public static IDrawable LoadSkinnedCube(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "cube.obj";
        var scene = EnsureScene(resourceFactory, modelName, false);
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

    public static IDrawable LoadGouraudShadedSphere(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "sphere.obj";
        var (vertices, indices, normals) = LoadModel(resourceFactory, modelName, false);
        return new GouraudShadedFromModel(device, resourceFactory, vertices, indices, normals, modelName);
    }

    public static IDrawable LoadGouraudSmoothShadedSphere(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "sphere.obj";
        const string modelKey = modelName + "GouraudSmooth";
        var (vertices, indices, normals) = LoadModel(resourceFactory, modelName, true);
        return new GouraudShadedFromModel(device, resourceFactory, vertices, indices, normals, modelKey);
    }
    
    public static IDrawable LoadPhongShadedSphere(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "sphere.obj";
        const string modelKey = modelName + "Phong";
        var (vertices, indices, normals) = LoadModel(resourceFactory, modelName, true);
        return new PhongShadedFromModel(device, resourceFactory, vertices, indices, normals, modelKey);
    }
    
    public static IDrawable LoadPhongShadedCube(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "cube.obj";
        const string modelKey = modelName + "Phong";
        var (vertices, indices, normals) = LoadModel(resourceFactory, modelName, false);
        return new PhongShadedFromModel(device, resourceFactory, vertices, indices, normals, modelKey);
    }

    private static (RawVector3[] Vertices, int[] Indices, RawVector3[] Normals) LoadModel(IResourceFactory resourceFactory, string modelName, bool smoothNormals)
    {
        var scene = EnsureScene(resourceFactory, modelName, smoothNormals);
        var mesh = scene.Meshes[0];
        var indices = mesh.GetIndices();
        var vertices = mesh.Vertices.Select(v => new RawVector3(v.X, v.Y, v.Z)).ToArray();
        var normals = mesh.Normals.Select(v => new RawVector3(v.X, v.Y, v.Z)).ToArray();

        return (vertices, indices, normals);
    }

    private static Scene EnsureScene(IResourceFactory resourceFactory, string modelName, bool smoothNormals)
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

        scene = GetContext(resourceFactory).ImportFile(modelName, flags);
        SceneCache.Add(modelKey, scene);
        return scene;
    }

    private static AssimpContext GetContext(IResourceFactory resourceFactory) =>
        resourceFactory.EnsureCrated(nameof(AssimpContext),
            () =>
            {
                var disposables = new DisposableStack();

                var logStream = new ConsoleLogStream(nameof(ModelLoader)).DisposeWith(disposables);
                logStream.Attach();

                var fs = new FileIOSystem(ModelsPath).DisposeWith(disposables);

                var context = new AssimpContext().DisposeWith(disposables);
                context.SetIOSystem(fs);

                return new Disposable<AssimpContext>(context, disposables);
            }).Value;
}