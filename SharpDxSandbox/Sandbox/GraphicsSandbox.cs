using System.Collections.Concurrent;
using System.Diagnostics;
using SharpDX;
using SharpDxSandbox.Graphics;
using SharpDxSandbox.Graphics.Drawables;
using SharpDxSandbox.Infrastructure;
using Vanara.PInvoke;

namespace SharpDxSandbox.Sandbox;

internal sealed class GraphicsSandbox
{
    private const float ZNear = 1;
    private const float ZFar = 200;

    private const float ModelRadius = 2;
    private const float VisibleNear = ZNear + ModelRadius * 4;
    private const float VisibleFar = ZFar - ModelRadius * 4;

    private float _movementZRotation;

    private Matrix _projectionMatrix;
    private BoundingFrustum _frustum;

    private Vector4 _lightSourcePosition;

    private ConcurrentDictionary<int, ModelState> _modelsState = new();
    private readonly CameraView _cameraView = new();

    private Graphics.Graphics _graphics;
    private ResourceFactory _resourceFactory;
    private GuiManager _gui;

    public Task Start() => new Window(2048, 1512).RunInWindow(Drawing);

    private async Task Drawing(Window window, CancellationToken cancellation)
    {
        _projectionMatrix = Matrix.PerspectiveLH(1, window.Height / (float)window.Width, ZNear, ZFar);
        _frustum = new BoundingFrustum(_projectionMatrix);

        using (_graphics = new Graphics.Graphics(window))
        using (_gui = new GuiManager(_graphics.Device, window))
        using (_resourceFactory = new ResourceFactory(_graphics.Logger))
        {
            _graphics.WithGui(_gui);
            RestoreModels();
            CreateLightSource();

            window.OnKeyDown += UpdateCamera;
            _graphics.OnEndFrame += HandleGuiCalls;
            _graphics.OnEndFrame += RotateIfRequested;

            await _graphics.Work(cancellation);

            window.OnKeyDown -= UpdateCamera;
            _graphics.OnEndFrame -= HandleGuiCalls;
            _graphics.OnEndFrame -= RotateIfRequested;
        }
    }

    private void CreateLightSource()
    {
        var model = DrawableFactory.Create(DrawableKind.LightSource, _graphics.Device, _resourceFactory);
        model.RegisterWorldTransform(() =>
        {
            _lightSourcePosition = new Vector4(_gui.LightSourcePosition.X, _gui.LightSourcePosition.Y, VisibleNear / 2 + _gui.LightSourcePosition.Z, 1);
            var model = Matrix.Scaling(0.1f);
            var world = Matrix.Translation(_lightSourcePosition.X, _lightSourcePosition.Y, _lightSourcePosition.Z);

            var (cameraPosition, cameraDirection) = _cameraView.GetCameraData();
            _gui.PrintInfo($"Camera position: {cameraPosition.ToString("F2")}");
            var camera = Matrix.LookAtLH(cameraPosition, cameraDirection, Vector3.UnitY);

            var projection = _projectionMatrix;
            return new TransformationData(model, world, camera, projection, _lightSourcePosition, _cameraView.WorldPosition);
        });
        _graphics.AddDrawable(model);
    }

    private void UpdateCamera(object sender, User32.VK e)
    {
        switch (e)
        {
            case User32.VK.VK_A:
                _cameraView.Update(left: true);
                break;
            case User32.VK.VK_D:
                _cameraView.Update(right: true);
                break;
            case User32.VK.VK_W:
                _cameraView.Update(forward: true);
                break;
            case User32.VK.VK_S:
                _cameraView.Update(backward: true);
                break;
            case User32.VK.VK_OEM_1:
                _cameraView.Update(rotateLeft: true);
                break;
            case User32.VK.VK_OEM_5:
                _cameraView.Update(rotateRight: true);
                break;
        }
    }

    private void RestoreModels()
    {
        ConcurrentDictionary<int, ModelState> restored = new();
        foreach (var modelState in _modelsState.Values)
        {
            CreateModel(modelState.DrawableKind, restored, modelState);
        }
        _modelsState = restored;
    }

    private void CreateModel(DrawableKind drawableKind, ConcurrentDictionary<int, ModelState> stateStorage = null, ModelState restoreState = null)
    {
        stateStorage ??= _modelsState;
        var model = DrawableFactory.Create(drawableKind, _graphics.Device, _resourceFactory);
        stateStorage[model.GetHashCode()] = restoreState ?? CreateWithPosition(drawableKind);
        model.RegisterWorldTransform(() => StandardTransformationMatrix(model.GetHashCode()));
        _graphics.AddDrawable(model);
    }

    private ModelState CreateWithPosition(DrawableKind kind)
    {
        return _gui.CreateInRandomPosition ? MakeRandomPosition() : MakeCenterPosition();

        ModelState MakeCenterPosition() => new(kind, new Vector3(-_gui.ModelTranslation.X, -_gui.ModelTranslation.Y, VisibleNear - _gui.ModelTranslation.Z), 0, 0, 0);

        ModelState MakeRandomPosition()
        {
            const float emptySphereDiameter = ModelRadius * 6;
            const float emptySphereRadius = emptySphereDiameter / 2;

            var z = (float)Random.Shared.NextDouble(VisibleNear, VisibleFar);

            var middleHeight = _frustum.GetHeightAtDepth(z) / 2 - ModelRadius;
            var middleWidth = _frustum.GetWidthAtDepth(z) / 2 - ModelRadius;
            var maxVisibleSphereRadius = Math.Min(middleHeight, middleWidth);

            var randomX = (float)Random.Shared.NextDouble(emptySphereRadius, maxVisibleSphereRadius);
            var randomY = (float)Random.Shared.NextDouble(emptySphereRadius, maxVisibleSphereRadius);
            var randomXY = new Vector3(randomX, randomY, z);

            var position1 = Vector3.TransformCoordinate(randomXY, Matrix.RotationZ((float)Random.Shared.NextDouble(0, 2 * Math.PI)));

            var rotation = (float)Random.Shared.NextDouble(0, Math.PI * 2);
            return new ModelState(kind, position1, rotation, rotation, rotation);
        }
    }

    private TransformationData StandardTransformationMatrix(int modelHash)
    {
        var (_, position, rotX, rotY, rotZ) = _modelsState[modelHash];
        rotX += _gui.ModelRotation.X;
        rotY -= _gui.ModelRotation.Y;
        rotZ -= _gui.ModelRotation.Z;
        var transX = position.X + _gui.ModelTranslation.X;
        var transY = position.Y + _gui.ModelTranslation.Y;
        var transZ = position.Z + _gui.ModelTranslation.Z;

        var model = Matrix.RotationYawPitchRoll(rotY + _movementZRotation, rotX + _movementZRotation, rotZ);
        var world = Matrix.Translation(transX, transY, transZ) * Matrix.RotationYawPitchRoll(0, 0, _movementZRotation);

        var (cameraPosition, cameraDirection) = _cameraView.GetCameraData();
        _gui.PrintInfo($"Camera position: {cameraPosition.ToString("F2")}");
        var camera = Matrix.LookAtLH(cameraPosition, cameraDirection, Vector3.UnitY);

        var projection = _projectionMatrix;
        return new TransformationData(model, world, camera, projection, _lightSourcePosition, _cameraView.WorldPosition);
    }

    private void RotateIfRequested(object sender, EventArgs e)
    {
        if (!_gui.WithMovements)
        {
            return;
        }
        const float stepForward = 0.002f;
        const float divider = (float)(2 * Math.PI);
        _movementZRotation = (_movementZRotation - stepForward) % divider;
    }

    private void HandleGuiCalls(object sender, EventArgs e)
    {
        if (_gui.ClearElementsRequested)
        {
            _modelsState.Clear();
            _graphics.ClearDrawables();
            CreateLightSource();
        }

        if (_gui.GenerateManyElementsRequested)
        {
            GenerateMany();
        }

        HandleObjectCreation();

        void HandleObjectCreation()
        {
            if (_gui.CreateSimpleObjectRequest.SimpleCube)
            {
                CreateModel(DrawableKind.SimpleCube);
            }
            if (_gui.CreateSimpleObjectRequest.ColoredCube)
            {
                CreateModel(DrawableKind.ColoredCube);
            }
            if (_gui.CreateSimpleObjectRequest.ColoredFromModelFile)
            {
                CreateModel(DrawableKind.ColoredFromModelCube);
            }
            if (_gui.CreateSimpleObjectRequest.ColoredSphere)
            {
                CreateModel(DrawableKind.ColoredSphere);
            }

            if (_gui.CreateSkinnedObjectRequest.Plane)
            {
                CreateModel(DrawableKind.Plane);
            }
            if (_gui.CreateSkinnedObjectRequest.SkinnedCube)
            {
                CreateModel(DrawableKind.SkinnedCube);
            }
            if (_gui.CreateSkinnedObjectRequest.SkinnedCubeFromModelFile)
            {
                CreateModel(DrawableKind.SkinnedFromModelCube);
            }
            if (_gui.CreateShadedObjectRequest.GouraudShadedSkinnedCube)
            {
                CreateModel(DrawableKind.GouraudShadedSkinnedCube);
            }
            if (_gui.CreateShadedObjectRequest.GouraudShadedSphere)
            {
                CreateModel(DrawableKind.GouraudShadedSphere);
            }
            if (_gui.CreateShadedObjectRequest.GouraudSmoothShadedSphere)
            {
                CreateModel(DrawableKind.GouraudSmoothShadedSphere);
            }
            if (_gui.CreateShadedObjectRequest.PhongShadedSphere)
            {
                CreateModel(DrawableKind.PhongShadedSphere);
            }
            if (_gui.CreateShadedObjectRequest.PhongShadedCube)
            {
                CreateModel(DrawableKind.PhongShadedCube);
            }
        }
    }

    private void GenerateMany()
    {
        const int count = 3000;
        //const int count = 1;
        var availableDrawables = Enum.GetValues<DrawableKind>();
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < count; i++)
        {
            //CreateModel(DrawableKind.ShadedSkinnedCube);
            CreateModel(availableDrawables[i % availableDrawables.Length]);
        }
        var elapsed = Stopwatch.GetElapsedTime(start);
        Trace.WriteLine($"{count} elements was loaded in {elapsed.TotalSeconds}s");
    }

    private sealed record ModelState(DrawableKind DrawableKind, Vector3 Position, float RotX, float RotY, float RotZ);
}