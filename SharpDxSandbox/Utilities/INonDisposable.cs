namespace SharpDxSandbox.Utilities;

public interface INonDisposable<out T> where T : IDisposable
{
    T Value { get; }
}