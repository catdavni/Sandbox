namespace SharpDxSandbox.Utilities;

public static class DisposableExtensions
{
    public static INonDisposable<T> AsNonDisposable<T>(this T value) where T : IDisposable => new NonDisposable<T> { Value = value };

    public static T DisposeWith<T>(this T value, DisposableStack stack) where T : IDisposable
    {
        stack.Add(value);
        return value;
    }
}

file sealed class NonDisposable<T> : INonDisposable<T> where T : IDisposable
{
    public T Value { get; init; }
}