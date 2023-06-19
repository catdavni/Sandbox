namespace SharpDxSandbox.Api.Interface;

public interface IResourceFactory: IDisposable
{
    T EnsureCrated<T>(string key, Func<T> factory) where T : class, IDisposable;
}