using System.Collections.Concurrent;
using SharpDxSandbox.Api.Interface;

namespace SharpDxSandbox.Api.Implementation;

public sealed class ResourceFactory : IResourceFactory
{
    private readonly ConcurrentDictionary<string, Lazy<IDisposable>> _resources = new();

    public T EnsureCrated<T>(string key, Func<T> factory) where T : class, IDisposable 
        => (T)_resources.GetOrAdd(key, new Lazy<IDisposable>(factory)).Value;

    public void Dispose()
    {
        foreach (var resource in _resources.Values)
        {
            resource.Value.Dispose();
        }
    }

}