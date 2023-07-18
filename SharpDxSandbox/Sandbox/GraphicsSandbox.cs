using System.Collections.Concurrent;
using System.Diagnostics;
using SharpDX;
using SharpDxSandbox.Graphics;
using SharpDxSandbox.Graphics.Drawables;
using SharpDxSandbox.Infrastructure;

namespace SharpDxSandbox.Sandbox;

internal sealed class GraphicsSandbox
{
    private const float ZNear = 1;
    private const float ZFar = 200;

    private Matrix _projectionMatrix;
    private BoundingFrustum _frustum;

    private ConcurrentDictionary<int, ModelState> _modelsState = new();

    private Graphics.Graphics _graphics;
    private ResourceFactory _resourceFactory;

    public Task Start() => new Window(2048, 1512).RunInWindow(Drawing);

    private async Task Drawing(Window window, CancellationToken cancellation)
    {
        _projectionMatrix = Matrix.PerspectiveLH(1, window.Height / (float)window.Width, ZNear, ZFar);
        _frustum = new BoundingFrustum(_projectionMatrix);

        using (_graphics = new Graphics.Graphics(window))
        using (_resourceFactory = new ResourceFactory(_graphics.Logger))
        {
            RestoreModels(_graphics, _resourceFactory);

            _graphics.Gui.ClearElementsRequested += ClearElements;
            _graphics.Gui.GenerateManyElementsRequested += GenerateMany;
            window.OnKeyPressed += HandleRotations;
            window.OnKeyPressed += MaybeAddModelHandler;

            await _graphics.Work(cancellation);

            _graphics.Gui.ClearElementsRequested -= ClearElements;
            _graphics.Gui.GenerateManyElementsRequested -= GenerateMany;
            window.OnKeyPressed -= HandleRotations;
            window.OnKeyPressed -= MaybeAddModelHandler;
        }
    }

    private void GenerateMany(object sender, EventArgs e)
    {
        const int count = 3000;
        //const int count = 1;
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < count; i++)
        {
            MaybeAddModelHandler(this, new KeyPressedEventArgs((i % 8).ToString()));
        }
        var elapsed = Stopwatch.GetElapsedTime(start);
        Trace.WriteLine($"{count} elements was loaded in {elapsed.TotalSeconds}s");
    }

    private void ClearElements(object sender, EventArgs e)
    {
        _modelsState.Clear();
        _graphics.ClearDrawables();
    }

    private void RestoreModels(Graphics.Graphics graphics, IResourceFactory resourceFactory)
    {
        ConcurrentDictionary<int, ModelState> restored = new();
        foreach (var modelState in _modelsState.Values)
        {
            var model = DrawableFactory.Create(modelState.DrawableKind, graphics.Device, resourceFactory);
            restored[model.GetHashCode()] = modelState;
            model.RegisterWorldTransform(() => StandardTransformationMatrix(model.GetHashCode()));
            graphics.AddDrawable(model);
        }
        _modelsState = restored;
    }

    private void MaybeAddModelHandler(object s, KeyPressedEventArgs keyPressedEventArgs)
    {
        IResourceFactory resourceFactory = _resourceFactory;
        var graphics = _graphics;

        var kindMap = new Dictionary<string, DrawableKind>
        {
            { "1", DrawableKind.SimpleCube },
            { "2", DrawableKind.ColoredCube },
            { "3", DrawableKind.ColoredFromModelCube },
            { "4", DrawableKind.ColoredSphere },
            { "5", DrawableKind.Plane },
            { "6", DrawableKind.SkinnedCube },
            { "7", DrawableKind.SkinnedFromModelCube },
        };

        if (!kindMap.TryGetValue(keyPressedEventArgs.Input, out var drawableKind))
        {
            return;
        }
        var model = DrawableFactory.Create(drawableKind, graphics.Device, resourceFactory);
        _modelsState[model.GetHashCode()] = CreateWithPosition(drawableKind);
        model.RegisterWorldTransform(() => StandardTransformationMatrix(model.GetHashCode()));
        graphics.AddDrawable(model);
    }

    private ModelState CreateWithPosition(DrawableKind kind)
    {
        const float modelRadius = 2;
        const float modelDepth = ZNear + (ZFar - ZNear) / 2;

        return _graphics.Gui.RandomizeMovements ? MakeRandomPosition() : MakeCenterPosition();

        ModelState MakeCenterPosition() => new(kind, new Vector3(0, 0, modelDepth / 4), 0, 0, 0);

        ModelState MakeRandomPosition()
        {
            var middleHeight = _frustum.GetHeightAtDepth(modelDepth) / 2 - modelRadius;
            var middleWidth = _frustum.GetWidthAtDepth(modelDepth) / 2 - modelRadius;

            var x = (float)Random.Shared.NextDouble(middleWidth / 2, middleWidth);
            var y = (float)Random.Shared.NextDouble(-middleHeight, middleHeight);
            var position1 = new Vector3(x, y, modelDepth);

            var rotation = (float)Random.Shared.NextDouble(0, Math.PI * 2);
            return new ModelState(kind, position1, rotation, rotation, rotation);
        }
    }

    private Matrix StandardTransformationMatrix(int modelHash)
    {
        var (_, position, rotX, rotY, rotZ) = _modelsState[modelHash];
        rotX += _graphics.Gui.XRotation;
        rotY -= _graphics.Gui.YRotation;
        rotZ -= _graphics.Gui.ZRotation;
        var transX = position.X + _graphics.Gui.XTranslation;
        var transY = position.Y + _graphics.Gui.YTranslation;
        var transZ = position.Z + _graphics.Gui.ZTranslation;

        _modelsState[modelHash] = _modelsState[modelHash] with
        {
            RotX = rotX, RotY = rotY, RotZ = rotZ
        };

        if (_graphics.Gui.RandomizeMovements)
        {
            RandomizeCoordinates(modelHash);
            var transformationMatrix = Matrix.Identity;
            transformationMatrix *= Matrix.RotationYawPitchRoll(rotY, rotX, rotZ);
            transformationMatrix *= Matrix.Translation(transX, transY, 0);
            transformationMatrix *= Matrix.RotationYawPitchRoll(rotY, rotX, 0);
            transformationMatrix *= Matrix.Translation(0, 0, transZ);
            transformationMatrix *= _projectionMatrix;

            return transformationMatrix;
        }
        else
        {
            var transformationMatrix = Matrix.Identity;
            transformationMatrix *= Matrix.RotationYawPitchRoll(rotY, rotX, rotZ);
            transformationMatrix *= Matrix.Translation(transX, transY, transZ);
            transformationMatrix *= _projectionMatrix;
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

    private sealed record ModelState(DrawableKind DrawableKind, Vector3 Position, float RotX, float RotY, float RotZ);
}