using System.Diagnostics;
using Assimp;
using Assimp.Unmanaged;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SharpDxSandbox.Graphics;
using SharpDxSandbox.Graphics.Drawables;
using SharpDxSandbox.Infrastructure.Disposables;
using SharpDxSandbox.Resources;
using Material = SharpDxSandbox.Graphics.Drawables.Material;

namespace SharpDxSandbox.Infrastructure;

internal static class ModelLoader
{
    private static readonly string ModelsPath = Path.Combine("Resources", "Models");
    private static readonly string NanoSuitPath = Path.Combine(ModelsPath, "NanoSuit");

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
        //var model = SharpGLTF.Schema2.ModelRoot.Load(Path.Combine(NanoSuitPath, Constants.Models.NanoSuit.ComplexModelName));

        var scene = EnsureScene(Constants.Models.NanoSuit.ModelName, false);
        var centroid = scene.CalculateCentroid();

        var parts = scene.Meshes.Select(
                mesh =>
                {
                    //var modelMaterial = model.LogicalMaterials.Single(material => material.Name == mesh.Name);

                    // var baseColorChannel = modelMaterial.FindChannel("BaseColor");
                    // var baseColor = baseColorChannel.Value.Parameter;
                    //
                    // var material = Material.RandomColor with { MaterialColor = new Vector4(baseColor.X, baseColor.Y, baseColor.Z, baseColor.W) };
                    // new Material(
                    // new Vector4(baseColor.X, baseColor.Y, baseColor.Z, baseColor.W),
                    // default,
                    // default);

                    var vertices = mesh.Vertices.Select(v => v.ApplyCentroid(centroid));
                    var normals = mesh.Normals.Select(v => new RawVector3(v.X, v.Y, v.Z));
                    var texCoords = mesh.TextureCoordinateChannels.Single(c => c.Count > 0).Select(t => new RawVector2(t.X, t.Y));

                    var verticesWithNormals = vertices.Zip(normals, texCoords).Select(v => new Vertex_Normal_TexCoord(v.First, v.Second, v.Third)).ToArray();

                    return new PhongTextureColored(
                        device,
                        resourceFactory,
                        verticesWithNormals,
                        mesh.GetIndices(),
                        mesh.Name,
                        scene.Materials[mesh.MaterialIndex]);
                })
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

        var fs = new FileIOSystem(ModelsPath, NanoSuitPath).DisposeWith(disposables);

        var context = new AssimpContext().DisposeWith(disposables);
        AssimpLibrary.Instance.ThrowOnLoadFailure = false;
        context.SetIOSystem(fs);

        return new Disposable<AssimpContext>(context, disposables);
    }
}

file static class PrimitiveExtensions
{
    public static Vector3D CalculateCentroid(this Scene scene)
    {
        var centroid = new Vector3D(0, 0, 0);
        var totalVertices = 0;

        foreach (var mesh in scene.Meshes)
        {
            totalVertices += mesh.VertexCount;
            centroid = mesh.Vertices.Aggregate(centroid, (current, vertex) => current + vertex);
        }
        centroid /= totalVertices;
        return centroid;
    }

    public static RawVector3 ApplyCentroid(this Vector3D source, Vector3D centroid)
        => new(source.X - centroid.X, source.Y - centroid.Y, source.Z - centroid.Z);
}