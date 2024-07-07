using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Security.Principal;

namespace IPC.NamedPipes;

public class Client
{
    public static async Task Start(string name, Action<string> messageReader, BlockingCollection<string> messagesToWrite,
        CancellationToken token)
    {
        await using var pipeClient =
            new NamedPipeClientStream(".", SimpleServer.Address, PipeDirection.InOut, PipeOptions.Asynchronous);

        Console.WriteLine($"Client started: {name}");
        await pipeClient.ConnectAsync(token);

        var reading = Task.Run(async () =>
        {
            using var reader = new StreamReader(pipeClient);
            while (!token.IsCancellationRequested)
            {
                var message = await reader.ReadLineAsync(token);
                if (message == null)
                {
                    Console.WriteLine("END OF PIPE STREAM HAS BEEN REACHED");
                    break;
                }
                messageReader(message);
            }
        }, token);

        var writing = Task.Run(() =>
        {
            using var writer = new StreamWriter(pipeClient);
            writer.AutoFlush = true;
            while (!token.IsCancellationRequested)
            {
                foreach (var message in messagesToWrite.GetConsumingEnumerable(token))
                {
                    writer.WriteLine(message);
                }
            }
        }, token);
        await Task.WhenAll(reading, writing);
    }
}