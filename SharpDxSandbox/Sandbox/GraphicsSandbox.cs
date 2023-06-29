using System.Collections.Concurrent;
using SharpDX;
using SharpDxSandbox.Api.Implementation;
using SharpDxSandbox.Window;

namespace SharpDxSandbox.Sandbox;

internal sealed class GraphicsSandbox
{
    private const int WindowWidth = 1024;
    private const int WindowHeight = 768;
    private const float ZNear = 1;
    private const float ZFar = 70;

    private static readonly Matrix ProjectionMatrix = Matrix.PerspectiveLH(1, (float)WindowHeight / WindowWidth, ZNear, ZFar);
    private static BoundingFrustum _frustum = new(ProjectionMatrix);

    private readonly ConcurrentDictionary<int, ModelState> _modelsState = new();

    public Task Start() => new DirextXApiHelpers.Window(WindowWidth, WindowHeight).RunInWindow(Drawing);

    private async Task Drawing(DirextXApiHelpers.Window window, WindowHandle windowHandle, CancellationToken cancellation)
    {
        using var graphics = new Graphics(window, windowHandle);
        using var resourceFactory = new ResourceFactory();

        window.KeyPressed += HandleRotations;
        window.KeyPressed += MaybeAddModelHandler;

        await graphics.Work(cancellation);

        window.KeyPressed -= HandleRotations;
        window.KeyPressed -= MaybeAddModelHandler;

        void MaybeAddModelHandler(object s, KeyPressedEventArgs e) => MaybeAddModel(e, resourceFactory, graphics);
    }

    private void MaybeAddModel(KeyPressedEventArgs keyPressedEventArgs, ResourceFactory resourceFactory, Graphics graphics)
    {
        switch (keyPressedEventArgs.Input)
        {
            case "1":
            {
                var simpleCube = new SimpleCube(graphics.Device, resourceFactory);
                _modelsState.TryAdd(simpleCube.GetHashCode(), CreateWithRandomPosition());
                simpleCube.RegisterWorldTransform(() => StandardTransformationMatrix(simpleCube.GetHashCode()));
                graphics.AddDrawable(simpleCube);
                break;
            }
            case "2":
            {
                var coloredCube = new ColoredCube(graphics.Device, resourceFactory);
                _modelsState.TryAdd(coloredCube.GetHashCode(), CreateWithRandomPosition());
                coloredCube.RegisterWorldTransform(() => StandardTransformationMatrix(coloredCube.GetHashCode()));
                graphics.AddDrawable(coloredCube);
                break;
            }
            case "3":
            {
                var model = ModelLoader.LoadCube(graphics.Device, resourceFactory);
                _modelsState.TryAdd(model.GetHashCode(), CreateWithRandomPosition());
                model.RegisterWorldTransform(() => StandardTransformationMatrix(model.GetHashCode()));
                graphics.AddDrawable(model);
                break;
            }
            case "4":
            {
                var model = ModelLoader.LoadSphere(graphics.Device, resourceFactory);
                _modelsState.TryAdd(model.GetHashCode(), CreateWithRandomPosition());
                model.RegisterWorldTransform(() => StandardTransformationMatrix(model.GetHashCode()));
                graphics.AddDrawable(model);
                break;
            }
        }
    }

    private static ModelState CreateWithRandomPosition()
    {
        const float modelRadius = 2;
        const float modelDepth = ZNear + (ZFar - ZNear) / 2;

        var middleHeight =_frustum.GetHeightAtDepth(modelDepth) / 2 - modelRadius;
        var middleWidth = _frustum.GetWidthAtDepth(modelDepth) / 2 - modelRadius;

        var x = (float)Random.Shared.NextDouble(middleWidth / 2, middleWidth);
        var y = (float)Random.Shared.NextDouble(-middleHeight, middleHeight);
        var position1 = new Vector3(x, y, modelDepth);
        
        var rotation = (float)Random.Shared.NextDouble(0, Math.PI * 2);
        return new ModelState(position1, rotation, rotation, rotation);
    }

    private Matrix StandardTransformationMatrix(int modelHash)
    {
        RandomizeCoordinates(modelHash);
        var (position, rotX, rotY, rotZ) = _modelsState[modelHash];

        var transformationMatrix = Matrix.Identity;
        transformationMatrix *= Matrix.RotationYawPitchRoll(rotY, rotX, rotZ);
        transformationMatrix *= Matrix.Translation(position.X, position.Y, 0);
        transformationMatrix *= Matrix.RotationYawPitchRoll(rotY, rotX, 0);
        transformationMatrix *= Matrix.Translation(0, 0, position.Z);
        transformationMatrix *= ProjectionMatrix;
        return transformationMatrix;
    }

    void RandomizeCoordinates(int modelKey)
    {
        const float stepForward = 0.01f;
        var model = _modelsState[modelKey];
        var divider = (2 * Math.PI);
        var rotY = (model.RotY + stepForward) % divider;
        _modelsState[modelKey] = model with {  RotY = (float)rotY };
    }

    private void HandleRotations(object sender, KeyPressedEventArgs e)
    {
        switch (e.Input.ToLower().First())
        {
            // Rotation X
            case 'w':
                foreach (var model in _modelsState.Keys)
                {
                    var value = _modelsState[model];
                    _modelsState[model] = value with { RotX = (value.RotX + 0.1f) % (float)(2f * Math.PI) };
                }
                break;
            case 's':
                foreach (var model in _modelsState.Keys)
                {
                    var value = _modelsState[model];
                    _modelsState[model] = value with { RotX = (value.RotX - 0.1f) % (float)(2f * Math.PI) };
                }
                break;
            // Rotation Y
            case 'd':
                foreach (var model in _modelsState.Keys)
                {
                    var value = _modelsState[model];
                    _modelsState[model] = value with { RotY = (value.RotY - 0.1f) % (float)(2f * Math.PI) };
                }
                break;
            case 'a':
                foreach (var model in _modelsState.Keys)
                {
                    var value = _modelsState[model];
                    _modelsState[model] = value with { RotY = (value.RotY + 0.1f) % (float)(2f * Math.PI) };
                }
                break;
            // Rotation Z
            case 'e':
                foreach (var model in _modelsState.Keys)
                {
                    var value = _modelsState[model];
                    _modelsState[model] = value with { RotZ = (value.RotZ + 0.1f) % (float)(2f * Math.PI) };
                }
                break;
            case 'q':
                foreach (var model in _modelsState.Keys)
                {
                    var value = _modelsState[model];
                    _modelsState[model] = value with { RotZ = (value.RotZ - 0.1f) % (float)(2f * Math.PI) };
                }
                break;
            // Translation Z
            case 'r':
                foreach (var model in _modelsState.Keys)
                {
                    var value = _modelsState[model];
                    value = value with { Position = Vector3.Add(value.Position, new Vector3(0, 0, 0.1f)) };
                    _modelsState[model] = value;
                }
                break;
            case 'f':
                foreach (var model in _modelsState.Keys)
                {
                    var value = _modelsState[model];
                    value = value with { Position = Vector3.Add(value.Position, new Vector3(0, 0, -0.1f)) };
                    _modelsState[model] = value;
                }
                break;
        }
    }

    private sealed record ModelState(Vector3 Position, float RotX, float RotY, float RotZ);
}