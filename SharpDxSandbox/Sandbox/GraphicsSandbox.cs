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

    private Matrix _projectionMatrix;
    private BoundingFrustum _frustum;

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
            RestoreModels(_graphics, _resourceFactory);

            window.OnKeyDown += UpdateCamera;
            _graphics.OnEndFrame += HandleGuiCalls;
            window.OnCharKeyPressed += MaybeAddModelHandler;

            await _graphics.Work(cancellation);

            window.OnKeyDown -= UpdateCamera;
            _graphics.OnEndFrame -= HandleGuiCalls;
            window.OnCharKeyPressed -= MaybeAddModelHandler;
        }
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

    private void MaybeAddModelHandler(object s, string key)
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

        if (!kindMap.TryGetValue(key, out var drawableKind))
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
        const float visibleNear = ZNear + modelRadius * 4;
        const float visibleFar = ZFar - modelRadius * 4;

        return _gui.CreateInRandomPosition ? MakeRandomPosition() : MakeCenterPosition();

        ModelState MakeCenterPosition() => new(kind, new Vector3(0, 0, visibleNear), 0, 0, 0);

        ModelState MakeRandomPosition()
        {
            var emptySphereCenter = visibleNear + (visibleFar - visibleNear) / 2;
            var emptySphereDiameter = modelRadius * 8;
            var emptySphereRadius = emptySphereDiameter / 2;

            var randomFarZ = (float)Random.Shared.NextDouble(emptySphereCenter + emptySphereRadius, visibleFar);
            Debug.Assert(emptySphereCenter + emptySphereRadius < visibleFar, "emptySphereCenter + emptySphereRadius < ZFar");
            var randomNearZ = (float)Random.Shared.NextDouble(visibleNear, emptySphereCenter - emptySphereRadius);
            Debug.Assert(visibleNear < emptySphereCenter - emptySphereRadius, "ZNear < emptySphereCenter - emptySphereRadius");

            var z = Random.Shared.Next(0, 1) == 0 ? randomNearZ : randomFarZ;

            var middleHeight = _frustum.GetHeightAtDepth(z) / 2 - modelRadius;
            var middleWidth = _frustum.GetWidthAtDepth(z) / 2 - modelRadius;

            var x = (float)Random.Shared.NextDouble(-middleWidth, middleWidth);

            var y = (float)Random.Shared.NextDouble(-middleHeight, middleHeight);
            var position1 = new Vector3(x, y, z);

            var rotation = (float)Random.Shared.NextDouble(0, Math.PI * 2);
            return new ModelState(kind, position1, rotation, rotation, rotation);
        }
    }

    private Matrix StandardTransformationMatrix(int modelHash)
    {
        var (_, position, rotX, rotY, rotZ) = _modelsState[modelHash];
        rotX += _gui.ModelRotation.X;
        rotY -= _gui.ModelRotation.Y;
        rotZ -= _gui.ModelRotation.Z;
        var transX = position.X + _gui.ModelTranslation.X;
        var transY = position.Y + _gui.ModelTranslation.Y;
        var transZ = position.Z + _gui.ModelTranslation.Z;


        var transformationMatrix = Matrix.Identity;
        transformationMatrix *= Matrix.RotationYawPitchRoll(rotY, rotX, rotZ);
        transformationMatrix *= Matrix.Translation(transX, transY, transZ);

        if (_gui.WithMovements)
        {
            RandomizeCoordinates(modelHash);
            transformationMatrix *= Matrix.Translation(0, 0, -transZ);
            transformationMatrix *= Matrix.RotationYawPitchRoll(rotY, rotX, 0);
            transformationMatrix *= Matrix.Translation(0, 0, transZ);
        }
        
        var (cameraPosition, cameraDirection) = _cameraView.GetCameraData();
        _gui.PrintInfo($"Camera position: {cameraPosition.ToString("F2")}");
        var cameraView = Matrix.LookAtLH(cameraPosition, cameraDirection, Vector3.UnitY);
        transformationMatrix *= cameraView;

        transformationMatrix *= _projectionMatrix;
        return transformationMatrix;
    }

    private void HandleGuiCalls(object sender, EventArgs e)
    {
        if (_gui.ClearElementsRequested)
        {
            _modelsState.Clear();
            _graphics.ClearDrawables();
        }

        if (_gui.GenerateManyElementsRequested)
        {
            GenerateMany();
        }
    }

    private void GenerateMany()
    {
        const int count = 3000;
        //const int count = 1;
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < count; i++)
        {
            MaybeAddModelHandler(this, (i % 8).ToString());
        }
        var elapsed = Stopwatch.GetElapsedTime(start);
        Trace.WriteLine($"{count} elements was loaded in {elapsed.TotalSeconds}s");
    }

    private void RandomizeCoordinates(int modelKey)
    {
        const float stepForward = 0.01f;
        var model = _modelsState[modelKey];
        const double divider = (2 * Math.PI);
        var rotY = (model.RotY + stepForward) % divider;
        _modelsState[modelKey] = model with { RotY = (float)rotY };
    }

    private sealed record ModelState(DrawableKind DrawableKind, Vector3 Position, float RotX, float RotY, float RotZ);

    private class CameraView
    {
        private const float MovementSpeed = 0.001f;
        private const float RotationSpeed = 0.00007f;
        private const float RotationDivider = 2f * (float)Math.PI;

        private Vector3 _cameraPosition = new(0, 0, 0);

        private float _rotationAngle;
        private float _goRight;
        private float _goForward;

        public (Vector3 Position, Vector3 Direction) GetCameraData()
        {
            var cameraDirection = Vector3.TransformNormal(Vector3.UnitZ, Matrix.RotationY(_rotationAngle));
            var rightDirection = Vector3.TransformNormal(Vector3.UnitX, Matrix.RotationY(_rotationAngle));
            _cameraPosition += cameraDirection * _goForward;
            _cameraPosition += rightDirection * _goRight;

            _goForward = 0f;
            _goRight = 0f;

            return (_cameraPosition, _cameraPosition + cameraDirection);
        }

        public void Update(bool left = false, bool right = false, bool forward = false, bool backward = false, bool rotateRight = false, bool rotateLeft = false)
        {
            if (rotateRight)
            {
                _rotationAngle = (_rotationAngle + RotationSpeed) % RotationDivider;
            }

            if (rotateLeft)
            {
                _rotationAngle = (_rotationAngle - RotationSpeed) % RotationDivider;
            }

            if (forward)
            {
                _goForward += MovementSpeed;
            }
            if (backward)
            {
                _goForward -= MovementSpeed;
            }

            if (right)
            {
                _goRight += MovementSpeed;
            }
            if (left)
            {
                _goRight -= MovementSpeed;
            }
        }
    }
}