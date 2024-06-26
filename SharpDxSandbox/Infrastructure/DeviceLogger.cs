﻿using System.Diagnostics;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using InfoQueue = SharpDX.Direct3D11.InfoQueue;

namespace SharpDxSandbox.Infrastructure;

internal sealed class DeviceLogger: IDisposable
{
    private readonly InfoQueue _infoQueue;

    public DeviceLogger(Device device)
    {
        _infoQueue = device.QueryInterface<InfoQueue>();
        _infoQueue.SetBreakOnSeverity(MessageSeverity.Warning, true);
        _infoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
        _infoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
    }

    public void  FlushMessages()
    {
        for (var i = 0; i < _infoQueue.NumStoredMessages; i++)
        {
            var message = _infoQueue.GetMessage(i);
            Trace.WriteLine($"Trace {message.Severity}: {message.Description}");
            Console.WriteLine($"{message.Severity}: {message.Description}");
        }
        _infoQueue.ClearStoredMessages();
    }


    public void Dispose()
    {
        FlushMessages();
        _infoQueue.Dispose();
    }
}