// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using IPC.NamedPipes;

using var cts = new CancellationTokenSource();
using var clientMessages = new BlockingCollection<(int, string)>();
using var server = new SimpleServer(clientMessages, cts.Token);

var clientMessagePrinter = Task.Run(async () =>
{
    await server.WaitForConnection();
    foreach (var message in clientMessages.GetConsumingEnumerable(cts.Token))
    {
        Console.WriteLine($"CLIENT SAY: {message.ToString()}");
    }
});

var stopTask = Task.Run(() =>
{
    string message = null;
    while (message != "exit")
    {
        message = Console.ReadLine();
        server.Send(message);
    }

    clientMessages.CompleteAdding();
    cts.Cancel();
});

Task.WaitAll(clientMessagePrinter, stopTask);

Console.WriteLine("Hello, World!");