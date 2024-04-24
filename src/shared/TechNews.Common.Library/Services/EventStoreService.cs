using EventStore.Client;

namespace TechNews.Common.Library.Services;

public class EventStoreService : IEventStoreService
{
    private readonly EventStoreClient _eventStoreClient;

    public EventStoreService(EventStoreClient eventStoreClient)
    {
        _eventStoreClient = eventStoreClient;
    }

    public async Task AppendToStreamAsync(
        string streamName,
        StreamState expectedState,
        IEnumerable<EventData> eventData,
        Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
        TimeSpan? deadline = null,
        UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        await _eventStoreClient.AppendToStreamAsync(
            streamName: streamName,
            expectedState: expectedState,
            eventData: eventData,
            configureOperationOptions: configureOperationOptions,
            deadline: deadline,
            userCredentials: userCredentials,
            cancellationToken: cancellationToken
        );
    }
}