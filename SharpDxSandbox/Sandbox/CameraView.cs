using SharpDX;

namespace SharpDxSandbox.Sandbox;

internal sealed class CameraView
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