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
        => MakeFromModel(device, resourceFactory, "cube.obj");

    public static FromModel LoadSphere(Device device, IResourceFactory resourceFactory)
        => MakeFromModel(device, resourceFactory, "sphere.obj");

    private static FromModel MakeFromModel(Device device, IResourceFactory resourceFactory, string modelName)
    {
        if (!SceneCache.TryGetValue(modelName, out var scene))
        {
            const PostProcessSteps flags = PostProcessSteps.JoinIdenticalVertices |
                                           PostProcessSteps.ImproveCacheLocality |
                                           PostProcessSteps.Triangulate |
                                           PostProcessSteps.PreTransformVertices |
                                           PostProcessSteps.OptimizeMeshes |
                                           PostProcessSteps.ValidateDataStructure;

            scene = GetContext(resourceFactory).ImportFile(modelName, flags);
            SceneCache.Add(modelName, scene);
        }

        var mesh = scene.Meshes[0];
        var indices = mesh.GetIndices();
        var vertices = mesh.Vertices.Select(v => new RawVector3(v.X, v.Y, v.Z)).ToArray();
        return new FromModel(device, resourceFactory, vertices, indices, modelName);
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