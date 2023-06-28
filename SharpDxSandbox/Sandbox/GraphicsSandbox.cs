using SharpDX;
using SharpDxSandbox.Api.Implementation;
using SharpDxSandbox.Window;

namespace SharpDxSandbox.Sandbox;

internal sealed class GraphicsSandbox
{
    private const int WindowWidth = 1024;
    private const int WindowHeight = 768;
    private const float ZNear = 1;
    private const float ZFar = 20;

    private readonly Dictionary<int, ModelState> _modelsState = new();

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
                var simpleCube = new SimpleCube(graphics.Device, resourceFactory);
                _modelsState.Add(simpleCube.GetHashCode(), ModelState.CreateCentered());

                simpleCube.RegisterWorldTransform(() =>
                {
                    var (x, y, z, rotX, rotY, rotZ) = _modelsState[simpleCube.GetHashCode()];
                    var transformationMatrix = Matrix.Identity;
                    transformationMatrix *= Matrix.RotationX(rotX);
                    transformationMatrix *= Matrix.RotationY(rotY);
                    transformationMatrix *= Matrix.RotationZ(rotZ);
                    transformationMatrix *= Matrix.Translation(x, y, z);
                    //transformationMatrix *= Matrix.RotationY(rotY);
                    transformationMatrix *= Matrix.PerspectiveLH(1, (float)WindowHeight / WindowWidth, ZNear, ZFar);
                    return transformationMatrix;
                });
                graphics.AddDrawable(simpleCube);
                break;
            case "2":
                var coloredCube = new ColoredCube(graphics.Device, resourceFactory);
                _modelsState.Add(coloredCube.GetHashCode(), ModelState.CreateCentered());
                coloredCube.RegisterWorldTransform(() =>
                {
                    var (x, y, z, rotX, rotY, rotZ) = _modelsState[coloredCube.GetHashCode()];

                    var transformationMatrix = Matrix.Identity;
                    transformationMatrix *= Matrix.RotationX(-rotX);
                    transformationMatrix *= Matrix.RotationY(-rotY);
                    transformationMatrix *= Matrix.RotationZ(rotZ);
                    transformationMatrix *= Matrix.Translation(x, y, z);
                    //transformationMatrix *= Matrix.RotationY(rotY);
                    transformationMatrix *= Matrix.PerspectiveLH(1, (float)WindowHeight / WindowWidth, ZNear, ZFar);
                    return transformationMatrix;
                });
                graphics.AddDrawable(coloredCube);
                break;
        }
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
                    _modelsState[model] = value with { Z = value.Z + 0.1f };
                }
                break;
            case 'f':
                foreach (var model in _modelsState.Keys)
                {
                    var value = _modelsState[model];
                    _modelsState[model] = value with { Z = value.Z - 0.1f };
                }
                break;
        }
    }

    private sealed record ModelState(float X, float Y, float Z, float RotX, float RotY, float RotZ)
    {
        public static ModelState CreateRandom() => new(
            (float)Random.Shared.NextDouble(),
            (float)Random.Shared.NextDouble(),
            (float)Random.Shared.NextDouble(ZNear, ZFar),
            Random.Shared.NextSingle(),
            Random.Shared.NextSingle(),
            Random.Shared.NextSingle());

        public static ModelState CreateCentered() => new(
            0,
            0, 
            ZNear + 4,
            Random.Shared.NextSingle(),
            Random.Shared.NextSingle(),
            Random.Shared.NextSingle());
    }
}