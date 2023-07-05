namespace SharpDxSandbox.Infrastructure.Disposables;

public interface INonDisposable<out T> where T : IDisposable
{
    T Value { get; }
}