namespace SharpDxSandbox.Graphics;

internal interface IResourceFactory: IDisposable
{
    T EnsureCrated<T>(string key, Func<T> factory) where T : class, IDisposable;
}