namespace SharpDxSandbox.Infrastructure;

internal static class WindowExtensions
{
    public static async Task RunInWindow(this Window window, Func<Window, CancellationToken, Task> drawing)
    {
        window.OnKeyPressed += (s, eventArgs) => Console.WriteLine(eventArgs.Input);

        using var drawingsCancellation = new CancellationTokenSource();
        window.OnWindowClosed += (_, _) => { drawingsCancellation.Cancel(); };

        using var leakGuard = new MemoryLeakGuard(MemoryLeakGuard.LeakBehavior.ThrowException);
        await drawing(window, drawingsCancellation.Token);
        await window.Presentation;
    }
}