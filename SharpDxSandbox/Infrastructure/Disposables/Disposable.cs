namespace SharpDxSandbox.Infrastructure.Disposables;

public sealed class Disposable<T>: IDisposable
{
    private readonly IDisposable _disposable;

    public Disposable(T value, IDisposable disposable)
    {
        _disposable = disposable;
        Value = value;
    }

    public static Disposable<T> FromNonDisposable(T value) => new(value, null);

    public T Value { get; }

    public void Dispose() => _disposable?.Dispose();
}