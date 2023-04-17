namespace SharpDxSandbox.Window;

public sealed class SeparateThreadJob : IDisposable
{
    private readonly string _name;
    private readonly CancellationTokenSource _cancellation;
    private readonly Task _worker;

    public SeparateThreadJob(string name, Action<CancellationToken> work)
    {
        _name = name;
        _cancellation = new CancellationTokenSource();
        _worker = Task.Factory.StartNew(
            () => work(_cancellation.Token),
            _cancellation.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _worker.Wait();
        _cancellation.Dispose();
    }
}