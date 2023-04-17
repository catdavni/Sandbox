namespace SharpDxSandbox.Window;

public sealed class PresenterWindowLoop : IDisposable
{
    private readonly TaskCompletionSource<WindowHandle> _windowHandleSource;
    private readonly SeparateThreadJob _windowPump;

    public PresenterWindowLoop(int width, int height, WindowOptions options)
    {
        _windowHandleSource = new TaskCompletionSource<WindowHandle>();
        _windowPump = new SeparateThreadJob(nameof(_windowPump),
            token =>
            {
                using var window = new PresentationWindow(width, height, options);

                window.KeyPressed += RaiseKeyPressed;
                window.WindowClosed += RaiseWindowClosed;

                _windowHandleSource.SetResult(window.Handle);
                try
                {
                    while (!token.IsCancellationRequested && window.ProcessMessages())
                    {
                    }
                }
                finally
                {
                    window.WindowClosed -= RaiseWindowClosed;
                    window.KeyPressed -= RaiseKeyPressed;
                }
            });
    }

    public event EventHandler<KeyPressedEventArgs> KeyPressed;

    public event EventHandler<EventArgs> WindowClosed;

    public Task<WindowHandle> GetWindowHandleAsync() => _windowHandleSource.Task;

    public void Dispose() => _windowPump.Dispose();

    private void RaiseKeyPressed(string c) => KeyPressed?.Invoke(this, new KeyPressedEventArgs(c == " " ? "Space" : c));

    private void RaiseWindowClosed() => WindowClosed?.Invoke(this, EventArgs.Empty);
}