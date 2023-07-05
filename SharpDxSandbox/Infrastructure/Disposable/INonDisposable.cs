namespace SharpDxSandbox.Infrastructure.Disposable;

public interface INonDisposable<out T> where T : IDisposable
{
    T Value { get; }
}