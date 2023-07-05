using System.ComponentModel;
using System.Diagnostics;

namespace SharpDxSandbox.Infrastructure;

public sealed class MemoryLeakGuard : IDisposable
{
    private readonly LeakBehavior _leakBehavior;

    public enum LeakBehavior
    {
        ThrowException = 0,
        Trace
    }
    
    public MemoryLeakGuard(LeakBehavior leakBehavior)
    {
        _leakBehavior = leakBehavior;
        SharpDX.Configuration.EnableObjectTracking = true;
    }

    public void Dispose()
    {
        var aliveObjects = SharpDX.Diagnostics.ObjectTracker.FindActiveObjects();
        if (aliveObjects.Any())
        {
            var message = string.Join("Not disposed objects:" + Environment.NewLine, aliveObjects);
            Console.WriteLine(message);
            
            switch (_leakBehavior)
            {
                case LeakBehavior.ThrowException:
                    throw new WarningException(message);
                case LeakBehavior.Trace:
                    Trace.WriteLine(message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(_leakBehavior.ToString());
            }
        }
    }
}