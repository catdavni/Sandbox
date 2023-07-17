using System.Collections.Concurrent;
using System.Diagnostics;
using SharpDxSandbox.Infrastructure;

namespace SharpDxSandbox.Graphics;

internal sealed class ResourceFactory : IResourceFactory
{
    private readonly DeviceLogger _graphicsLogger;
    private readonly ConcurrentDictionary<string, Lazy<IDisposable>> _resources = new();

    public ResourceFactory(DeviceLogger graphicsLogger) => _graphicsLogger = graphicsLogger;

    public T EnsureCrated<T>(string key, Func<T> factory) where T : class, IDisposable
        => (T)_resources.GetOrAdd(key,
            new Lazy<IDisposable>(() =>
            {
                try
                {
                    return factory();
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Trace {nameof(ResourceFactory)} {e}");
                    _graphicsLogger.FlushMessages();
                    throw;
                }
            })
        ).Value;

    public void Dispose()
    {
        foreach (var resource in _resources.Values)
        {
            if (resource.IsValueCreated)
            {
                resource.Value.Dispose();
            }
        }
        _resources.Clear();
    }
}