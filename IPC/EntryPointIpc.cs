using System.Collections.Concurrent;
using IPC.NamedPipes;

namespace IPC;

public sealed class EntryPointIpc
{
    public static void StartClient(string name)
    {
        using var cts = new CancellationTokenSource();
        using var messagesToServer = new BlockingCollection<string>();

        var clientTask = Client.Start(name, s => Console.WriteLine($"SERVER SAY: {s}"), messagesToServer, cts.Token);
        var stopTask = Task.Run(() =>
        {
            string message = null;
            while (message != "exit")
            {
                message = Console.ReadLine();
                messagesToServer.Add(message);
            }

            cts.Cancel();
        }, cts.Token);
        Task.WaitAll(clientTask, stopTask);
    }
}