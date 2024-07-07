using System.Collections.Concurrent;
using System.IO.Pipes;

namespace IPC.NamedPipes;

internal sealed class SimpleServer : IDisposable
{
    public const string Address = "CatPipe";

    private readonly BlockingCollection<( int, string )> _clientMessageAggregator;
    private readonly CancellationToken _communicationCancellation;
    private NamedPipeServerStream _pipeServer;
    private StreamReader _pipeReader;
    private StreamWriter _pipeWriter;
    private Task _readingTask;
    private bool _pipeInitialized;

    public SimpleServer(
        BlockingCollection<( int, string )> clientMessageAggregator,
        CancellationToken communicationCancellation)
    {
        this._clientMessageAggregator = clientMessageAggregator;
        _communicationCancellation = communicationCancellation;
    }

    public int AssociatedProcess { get; private set; } = -1;

    public async Task WaitForConnection()
    {
        _pipeServer = new NamedPipeServerStream(
            Address,
            PipeDirection.InOut,
            -1,
            transmissionMode: PipeTransmissionMode.Message,
            PipeOptions.Asynchronous
        );
        Console.WriteLine($"Server started by: {Address}");
        await _pipeServer.WaitForConnectionAsync(_communicationCancellation);
        Console.WriteLine($"Client connected to server by: {Address}");
        _pipeReader = new StreamReader(_pipeServer);
        _pipeWriter = new StreamWriter(_pipeServer);
        _pipeWriter.AutoFlush = true;
        _pipeInitialized = true;

        while (!Authorized())
        {
            Console.WriteLine("AUTHORIZATION FAILED");
        }

        _readingTask = Task.Factory.StartNew(
                () =>
                {
                    while (!_communicationCancellation.IsCancellationRequested)
                    {
                        var message = _pipeReader.ReadLine();
                        if (message == null)
                        {
                            Console.WriteLine("END OF PIPE STREAM HAS BEEN REACHED");
                            break;
                        }

                        _clientMessageAggregator.Add((AssociatedProcess, message), _communicationCancellation);
                    }
                },
                TaskCreationOptions.LongRunning)
            .ContinueWith(
                t =>
                    Console.WriteLine($"EXCEPTION {nameof(SimpleServer)}: {t.Exception}"),
                TaskContinuationOptions.OnlyOnFaulted);
    }

    public void Send(string message)
    {
        //ThrowIfNotInitialized().
        _pipeWriter?.WriteLine(message);
    }

    public void Dispose()
    {
        _readingTask?.Wait();
        _pipeReader?.Dispose();
        _pipeWriter?.Dispose();
        _pipeServer.Dispose();
    }

    private bool Authorized()
    {
        const string authTokenMarker = "auth_";

        var authToken = _pipeReader.ReadLine();
        if (!authToken.StartsWith(authTokenMarker, StringComparison.InvariantCulture))
        {
            // not authenticated
            Send("NOT AUTHORIZED");
            return false;
        }

        var token = authToken.Substring(authTokenMarker.Length);
        if (!int.TryParse(token, out var pid))
        {
            // not authenticated
            return false;
        }

        Send("AUTHORIZED");
        AssociatedProcess = pid;
        return true;
    }

    private SimpleServer ThrowIfNotInitialized()
    {
        if (!_pipeInitialized)
        {
            throw new InvalidOperationException("Server is not initialized");
        }

        return this;
    }
}