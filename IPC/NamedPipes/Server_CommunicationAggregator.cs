using System.Collections.Concurrent;

namespace IPC.NamedPipes;

public class ServerCommunicationAggregator : IDisposable
{
    private readonly CancellationTokenSource _communicationCancellation = new();
    private readonly ConcurrentBag<SimpleServer> _communicationChannels = new();
    private readonly BlockingCollection<(int Id, string Message)> _messages = new();
    private readonly List<Action> _onConnectionActions = new();

    private Task _pipeClientsConnection;

    public void Start(string address)
    {
        _pipeClientsConnection = Task.Factory.StartNew(
                () =>
                {
                    while (!_communicationCancellation.Token.IsCancellationRequested)
                    {
                        var communicationChannel = new SimpleServer(
                            _messages,
                            _communicationCancellation.Token);
                        //TODO: bad 
                        communicationChannel.WaitForConnection()
                            .Wait(_communicationCancellation.Token);
                        _communicationChannels.Add(communicationChannel);

                        // send Initialization message to each new client
                        foreach (var onConnectionAction in _onConnectionActions)
                        {
                            onConnectionAction();
                        }
                    }
                }, _communicationCancellation.Token)
            .ContinueWith(
                t => Console.WriteLine($"EXCEPTION: {t.Exception}"),
                TaskContinuationOptions.OnlyOnFaulted);
    }

    public void RegisterOnConnectionAction(Action onConnectionAction) => _onConnectionActions.Add(onConnectionAction);

    public string Read()
    {
        var message = _messages.Take(_communicationCancellation.Token);
        return message.Message;
    }

    public void Write(string data)
    {
        foreach (var communicationChannel in _communicationChannels)
        {
            communicationChannel.Send(data);
        }
    }

    public void Dispose()
    {
        _messages.CompleteAdding();
        _communicationCancellation.Cancel();
        _pipeClientsConnection.Wait();
        foreach (var communicationChannel in _communicationChannels)
        {
            communicationChannel.Dispose();
        }

        _communicationCancellation.Dispose();
    }
}