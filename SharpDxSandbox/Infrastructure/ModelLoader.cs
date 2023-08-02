using System.Diagnostics;
using Assimp;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Graphics;
using SharpDxSandbox.Graphics.Drawables;
using SharpDxSandbox.Infrastructure.Disposables;

namespace SharpDxSandbox.Infrastructure;

internal static class ModelLoader
{
    private static readonly string ModelsPath = Path.Combine("Resources", "Models");
    private static readonly Dictionary<string, Scene> SceneCache = new();

    public static FromModel LoadCube(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "cube.obj";
        var (vertices, indices) = LoadModel(resourceFactory, modelName);
        return new FromModel(device, resourceFactory, vertices, indices, modelName);
    }

    public static FromModel LoadSphere(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "sphere.obj";
        var (vertices, indices) = LoadModel(resourceFactory, modelName);
        return new FromModel(device, resourceFactory, vertices, indices, modelName);
    }

    public static LightSource LoadLightSource(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "sphere.obj";
        var (vertices, indices) = LoadModel(resourceFactory, modelName);
        return new LightSource(device, resourceFactory, vertices, indices, nameof(LightSource));
    }

    public static IDrawable LoadSkinnedCube(Device device, IResourceFactory resourceFactory)
    {
        const string modelName = "cube.obj";
        var scene = EnsureScene(resourceFactory, modelName);
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

    private static (RawVector3[] Vertices, int[] Indices) LoadModel(IResourceFactory resourceFactory, string modelName)
    {
        var scene = EnsureScene(resourceFactory, modelName);
        var mesh = scene.Meshes[0];
        var indices = mesh.GetIndices();
        var vertices = mesh.Vertices.Select(v => new RawVector3(v.X, v.Y, v.Z)).ToArray();
        return (vertices, indices);
    }

    private static Scene EnsureScene(IResourceFactory resourceFactory, string modelName)
    {
        if (SceneCache.TryGetValue(modelName, out var scene))
        {
            return scene;
        }

        const PostProcessSteps flags = PostProcessSteps.JoinIdenticalVertices |
                                       PostProcessSteps.ImproveCacheLocality |
                                       PostProcessSteps.Triangulate |
                                       PostProcessSteps.PreTransformVertices |
                                       PostProcessSteps.OptimizeMeshes |
                                       PostProcessSteps.ValidateDataStructure;

        scene = GetContext(resourceFactory).ImportFile(modelName, flags);
        SceneCache.Add(modelName, scene);
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