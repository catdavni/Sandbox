using SharpDX;

namespace SharpDxSandbox.Graphics;

internal sealed class CameraView
{
    private const float MovementSpeed = 0.001f;
    private const float RotationSpeed = 0.002f;
    private const float YRotationDivider = 2f * (float)Math.PI;
    private const float XRotationDivider = (float)(Math.PI / 2 / 90) * 89;

    private float _rotationYAngle;
    private float _rotationXAngle;
    private float _goRight;
    private float _goForward;

    public (Vector3 Position, Vector3 Direction) GetCameraData()
    {
        var worldRotation = Matrix.RotationYawPitchRoll(_rotationYAngle, _rotationXAngle, 0);
        var cameraDirection = Vector3.TransformNormal(Vector3.UnitZ, worldRotation);
        var rightDirection = Vector3.TransformNormal(Vector3.UnitX, worldRotation);
        WorldPosition += cameraDirection * _goForward;
        WorldPosition += rightDirection * _goRight;

        _goForward = 0f;
        _goRight = 0f;

        return (WorldPosition, WorldPosition + cameraDirection);
    }

    public Vector3 WorldPosition { get; private set; } = new(0, 0, 0);

    public void Update(
        bool left = false,
        bool right = false,
        bool forward = false,
        bool backward = false,
        bool rotateRight = false,
        bool rotateLeft = false,
        bool rotateUp = false,
        bool rotateDown = false)
    {
        if (rotateRight)
        {
            _rotationYAngle = (_rotationYAngle + RotationSpeed) % YRotationDivider;
        }

        if (rotateLeft)
        {
            _rotationYAngle = (_rotationYAngle - RotationSpeed) % YRotationDivider;
        }

        if (rotateUp)
        {
            _rotationXAngle = Math.Max(_rotationXAngle - RotationSpeed, -XRotationDivider);
        }

        if (rotateDown)
        {
            _rotationXAngle = Math.Min(_rotationXAngle + RotationSpeed, XRotationDivider);
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