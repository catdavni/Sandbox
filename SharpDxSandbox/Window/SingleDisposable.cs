namespace SharpDxSandbox.Window;

public sealed class SingleDisposable : IDisposable
{
    private readonly Type _ownerType;

    public SingleDisposable(Type ownerType) => _ownerType = ownerType;

    public bool IsDisposed { get; private set; }

    public void ThrowIfDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(_ownerType.Name);
        }
    }

    public void Dispose()
    {
        ThrowIfDisposed();
        IsDisposed = true;
    }
}