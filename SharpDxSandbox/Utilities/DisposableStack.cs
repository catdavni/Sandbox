namespace SharpDxSandbox.Utilities;

public sealed class DisposableStack : IDisposable
{
    private readonly Stack<IDisposable> _disposables = new();

    public void Add(IDisposable value) => _disposables.Push(value);

    public void Dispose()
    {
        while (_disposables.TryPop(out var disposable))
        {
            disposable.Dispose(); 
        }
    }
}