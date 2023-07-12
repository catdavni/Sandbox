using System.Collections.Concurrent;
using System.Diagnostics;
using SharpDX;
using SharpDxSandbox.Graphics;
using SharpDxSandbox.Graphics.Drawables;
using SharpDxSandbox.Infrastructure;
using SharpDxSandbox.Window;
using Plane = SharpDxSandbox.Graphics.Drawables.Plane;

namespace SharpDxSandbox.Sandbox;

internal sealed class GraphicsSandbox
{
    private static readonly bool RandomizePositionAndMovements = true;
    
    private const int WindowWidth = 1024;
    private const int WindowHeight = 768;
    private const float ZNear = 1;
    private const float ZFar = 70;

    private static readonly Matrix ProjectionMatrix = Matrix.PerspectiveLH(1, (float)WindowHeight / WindowWidth, ZNear, ZFar);
    private static BoundingFrustum _frustum = new(ProjectionMatrix);

    private readonly ConcurrentDictionary<int, ModelState> _modelsState = new();

    public Task Start() => new Infrastructure.Window(WindowWidth, WindowHeight).RunInWindow(Drawing);

    private async Task Drawing(Infrastructure.Window window, WindowHandle windowHandle, CancellationToken cancellation)
    {
        using var graphics = new Graphics.Graphics(window, windowHandle);
        using var resourceFactory = new ResourceFactory(graphics.Logger);

        window.KeyPressed += HandleRotations;
        window.KeyPressed += MaybeAddModelHandler;

        //MakeTest();

        await graphics.Work(cancellation);

        window.KeyPressed -= HandleRotations;
        window.KeyPressed -= MaybeAddModelHandler;

        void MaybeAddModelHandler(object s, KeyPressedEventArgs e) => MaybeAddModel(e, resourceFactory, graphics);

        void MakeTest()
        {
            const int count = 3000;
            //const int count = 6;
            var start = Stopwatch.GetTimestamp();
            for (var i = 0; i < count; i++)
            {
                //MaybeAddModelHandler(this, new KeyPressedEventArgs("3"));
                //MaybeAddModelHandler(this, new KeyPressedEventArgs("5"));
                MaybeAddModelHandler(this, new KeyPressedEventArgs((i % 7).ToString()));
            }
            var elapsed = Stopwatch.GetElapsedTime(start);
            Trace.WriteLine($"{count} elements was loaded in {elapsed.TotalSeconds}s");
        }
    }

    private void MaybeAddModel(KeyPressedEventArgs keyPressedEventArgs, IResourceFactory resourceFactory, Graphics.Graphics graphics)
    {
        switch (keyPressedEventArgs.Input)
        {
            case "1":
            {
                var simpleCube = new SimpleCube(graphics.Device, resourceFactory);
                _modelsState.TryAdd(simpleCube.GetHashCode(), CreateWithPosition());
                simpleCube.RegisterWorldTransform(() => StandardTransformationMatrix(simpleCube.GetHashCode()));
                graphics.AddDrawable(simpleCube);
                break;
            }
            case "2":
            {
                var coloredCube = new ColoredCube(graphics.Device, resourceFactory);
                _modelsState.TryAdd(coloredCube.GetHashCode(), CreateWithPosition());
                coloredCube.RegisterWorldTransform(() => StandardTransformationMatrix(coloredCube.GetHashCode()));
                graphics.AddDrawable(coloredCube);
                break;
            }
            case "3":
            {
                var model = ModelLoader.LoadCube(graphics.Device, resourceFactory);
                _modelsState.TryAdd(model.GetHashCode(), CreateWithPosition());
                model.RegisterWorldTransform(() => StandardTransformationMatrix(model.GetHashCode()));
                graphics.AddDrawable(model);
                break;
            }
            case "4":
            {
                var model = ModelLoader.LoadSphere(graphics.Device, resourceFactory);
                _modelsState.TryAdd(model.GetHashCode(), CreateWithPosition());
                model.RegisterWorldTransform(() => StandardTransformationMatrix(model.GetHashCode()));
                graphics.AddDrawable(model);
                break;
            }
            case "5":
            {
                var plane = new Plane(graphics.Device, resourceFactory);
                _modelsState.TryAdd(plane.GetHashCode(), CreateWithPosition());
                plane.RegisterWorldTransform(() => StandardTransformationMatrix(plane.GetHashCode()));
                graphics.AddDrawable(plane);
                break;
            }
            case "6":
            {
                var plane = new SkinnedCube(graphics.Device, resourceFactory);
                _modelsState.TryAdd(plane.GetHashCode(), CreateWithPosition());
                plane.RegisterWorldTransform(() => StandardTransformationMatrix(plane.GetHashCode()));
                graphics.AddDrawable(plane);
                break;
            }
        }
    }

    private static ModelState CreateWithPosition()
    {
        const float modelRadius = 2;
        const float modelDepth = ZNear + (ZFar - ZNear) / 2;

        return RandomizePositionAndMovements ? MakeRandomPosition() : MakeCenterPosition();

        ModelState MakeCenterPosition() => new(new Vector3(0, 0, modelDepth / 4), 0, 0, 0);

        ModelState MakeRandomPosition()
        {
            var middleHeight = _frustum.GetHeightAtDepth(modelDepth) / 2 - modelRadius;
            var middleWidth = _frustum.GetWidthAtDepth(modelDepth) / 2 - modelRadius;

            var x = (float)Random.Shared.NextDouble(middleWidth / 2, middleWidth);
            var y = (float)Random.Shared.NextDouble(-middleHeight, middleHeight);
            var position1 = new Vector3(x, y, modelDepth);

            var rotation = (float)Random.Shared.NextDouble(0, Math.PI * 2);
            return new ModelState(position1, rotation, rotation, rotation);
        }
    }

    private Matrix StandardTransformationMatrix(int modelHash)
    {
        var (position, rotX, rotY, rotZ) = _modelsState[modelHash];
        if (RandomizePositionAndMovements)
        {
            RandomizeCoordinates(modelHash);
            var transformationMatrix = Matrix.Identity;
            transformationMatrix *= Matrix.RotationYawPitchRoll(rotY, rotX, rotZ);
            transformationMatrix *= Matrix.Translation(position.X, position.Y, 0);
            transformationMatrix *= Matrix.RotationYawPitchRoll(rotY, rotX, 0);
            transformationMatrix *= Matrix.Translation(0, 0, position.Z);
            transformationMatrix *= ProjectionMatrix;
            return transformationMatrix;
        }
        else
        {
            var transformationMatrix = Matrix.Identity;
            transformationMatrix *= Matrix.RotationYawPitchRoll(rotY, rotX, rotZ);
            transformationMatrix *= Matrix.Translation(position.X, position.Y, position.Z);
            transformationMatrix *= ProjectionMatrix;
            return transformationMatrix;
        }
    }

    private void RandomizeCoordinates(int modelKey)
    {
        const float stepForward = 0.01f;
        var model = _modelsState[modelKey];
        const double divider = (2 * Math.PI);
        var rotY = (model.RotY + stepForward) % divider;
        _modelsState[modelKey] = model with { RotY = (float)rotY };
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
            case 'f':
                foreach (var model in _modelsState.Keys)
                {
                    var value = _modelsState[model];
                    value = value with { Position = Vector3.Add(value.Position, new Vector3(0, 0, 0.3f)) };
                    _modelsState[model] = value;
                }
                break;
            case 'r':
                foreach (var model in _modelsState.Keys)
                {
                    var value = _modelsState[model];
                    value = value with { Position = Vector3.Add(value.Position, new Vector3(0, 0, -0.3f)) };
                    _modelsState[model] = value;
                }
                break;
        }
    }

    private sealed record ModelState(Vector3 Position, float RotX, float RotY, float RotZ);
}