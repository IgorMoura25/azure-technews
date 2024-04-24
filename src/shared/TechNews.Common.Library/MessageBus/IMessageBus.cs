using TechNews.Common.Library.Messages.Events;

namespace TechNews.Common.Library.MessageBus;

public interface IMessageBus
{
    public void Publish<T>(T message) where T : IEvent;
    public void Consume<T>(string queueName, Action<T?> executeAfterConsumed) where T : IEvent;
}