namespace SharpDxSandbox.Infrastructure;

internal static class WindowExtensions
{
    public static async Task RunInWindow(this Window window, Func<Window, CancellationToken, Task> drawing)
    {
        using var leakGuard = new MemoryLeakGuard(MemoryLeakGuard.LeakBehavior.ThrowException);
        window.OnKeyPressed += (s, eventArgs) => Console.WriteLine(eventArgs.Input);

        using var restartOnResizeDrawing = new RestartDrawingOnResize(window, drawing);
        await restartOnResizeDrawing.Drawing;
        await window.Presentation;
    }
}

file class RestartDrawingOnResize : IDisposable
{
    private readonly Window _window;
    private readonly Func<Window, CancellationToken, Task> _drawingFactory;
    private CancellationTokenSource _drawingCancellation;
    private readonly TaskCompletionSource _drawTillWindowCloseTask;
    private Task _currentDrawing;

    public RestartDrawingOnResize(Window window, Func<Window, CancellationToken, Task> drawingFactory)
    {
        _window = window;
        _drawingFactory = drawingFactory;
        _drawTillWindowCloseTask = new TaskCompletionSource();

        RestartDrawing(this, EventArgs.Empty);
        window.OnWindowSizeChanged += RestartDrawing;
        window.OnWindowClosed += WindowCloseHandler;
    }

    private void RestartDrawing(object sender, EventArgs args)
    {
        if (_drawingCancellation == null) // first initialization
        {
            _drawingCancellation = new CancellationTokenSource();
        }
        else
        {
            _drawingCancellation.Cancel();
            _currentDrawing.Wait();
            _drawingCancellation.Dispose();
            _drawingCancellation = new CancellationTokenSource();
        }
        _currentDrawing = _drawingFactory(_window, _drawingCancellation.Token);
    }

    public Task Drawing => _drawTillWindowCloseTask.Task;

    public void Dispose()
    {
        _window.OnWindowSizeChanged -= RestartDrawing;
        _window.OnWindowClosed -= WindowCloseHandler;
        
        _drawingCancellation.Cancel();
        _drawingCancellation.Dispose();
    }

    private void WindowCloseHandler(object sender, EventArgs e)
    {
        _drawingCancellation.Cancel();
        _currentDrawing.Wait();
        _drawTillWindowCloseTask.SetResult();
    }
}