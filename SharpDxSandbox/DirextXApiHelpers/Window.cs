using SharpDxSandbox.Window;

namespace SharpDxSandbox.DirextXApiHelpers
{
    internal sealed class Window
    {
        public Window(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        public int Width { get; }

        public int Height { get; }

        public async Task RunInWindow(Func<Window, WindowHandle, CancellationToken, Task> drawing)
        {
            using var window = new PresenterWindowLoop(Width, Height, WindowOptions.TopMost);
            window.KeyPressed += (s, eventArgs) =>
            {
                KeyPressed?.Invoke(s, eventArgs);
                Console.WriteLine(eventArgs.Input);
            };

            var windowHandle = window.GetWindowHandleAsync().Result;

            using var drawingsCancellation = new CancellationTokenSource();
            window.WindowClosed += (_, _) => { drawingsCancellation.Cancel(); };

            using var leakGuard = new MemoryLeakGuard(MemoryLeakGuard.LeakBehavior.ThrowException);
            await drawing(this, windowHandle, drawingsCancellation.Token);
        }
    }
}
