namespace SharpDxSandbox.Window;

public readonly struct WindowHandle : IEquatable<WindowHandle>
{
    public WindowHandle(IntPtr value) => Value = value;

    public IntPtr Value { get; }

    public static bool operator ==(WindowHandle left, WindowHandle right) => left.Equals(right);

    public static bool operator !=(WindowHandle left, WindowHandle right) => !(left == right);

    public bool Equals(WindowHandle other) => Value.Equals(other.Value);

    public override bool Equals(object obj) => obj is WindowHandle other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();
}