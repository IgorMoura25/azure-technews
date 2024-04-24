using EventStore.Client;

namespace TechNews.Common.Library.Services;

public interface IEventStoreService
{
    public Task AppendToStreamAsync(
        string streamName,
        StreamState expectedState,
        IEnumerable<EventData> eventData,
        Action<EventStoreClientOperationOptions>? configureOperationOptions = null,
        TimeSpan? deadline = null,
        UserCredentials? userCredentials = null,
        CancellationToken cancellationToken = default(CancellationToken)
    );
}