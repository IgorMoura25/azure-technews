using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TechNews.Common.Library.Extensions;
using TechNews.Common.Library.Messages;
using TechNews.Common.Library.Messages.Events;

namespace TechNews.Common.Library.MessageBus.Brokers.RabbitMQ;

public class RabbitMQMessageBus : IMessageBus, IDisposable
{
    private readonly IModel _channel;
    private readonly IConnection _connection;

    private const string DEAD_LETTER_QUEUE_NAME_PATTERN = "{0}.dead-letter";
    private const string ERROR_QUEUE_NAME_PATTERN = "{0}.error";

    private HashSet<string> Exchanges { get; set; } = new();
    private HashSet<string> Queues { get; set; } = new();

    public RabbitMQMessageBus(RabbitMQMessageBusParameters parameters)
    {
        var factory = new ConnectionFactory
        {
            HostName = parameters.HostName,
            UserName = parameters.UserName,
            Password = parameters.Password,
            VirtualHost = parameters.VirtualHost
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public void Publish<T>(T message) where T : IEvent
    {
        var eventName = typeof(T).Name.ToLowerKebabCase();

        CreateExchangeIfNonExistent(exchangeName: eventName, type: ExchangeType.Fanout);
        CreateQueueIfNonExistent(queueName: string.Format(DEAD_LETTER_QUEUE_NAME_PATTERN, eventName), exchangeToBind: eventName);

        _channel.BasicPublish(
            exchange: eventName,
            routingKey: string.Empty,
            basicProperties: null,
            body: EncodeMessageToBytes(message)
        );
    }

    public void Consume<T>(string queueName, Action<T?> executeAfterConsumed) where T : IEvent
    {
        var eventName = typeof(T).Name.ToLowerKebabCase();
        queueName = queueName.ToLowerKebabCase();

        CreateExchangeIfNonExistent(exchangeName: eventName, type: ExchangeType.Fanout);
        CreateQueueIfNonExistent(queueName: queueName, exchangeToBind: eventName);

        _channel.QueueUnbind(
            queue: string.Format(DEAD_LETTER_QUEUE_NAME_PATTERN, eventName),
            exchange: eventName,
            routingKey: string.Empty,
            arguments: null);

        // TODO: Mover mensagens de deadletter para a fila principal depois do unbind

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (sender, eventArgs) =>
        {
            var encodedBody = eventArgs.Body.ToArray();
            var decodedBody = Encoding.UTF8.GetString(encodedBody);
            var message = JsonSerializer.Deserialize<T>(decodedBody);

            try
            {
                executeAfterConsumed(message);
            }
            catch (Exception ex)
            {
                CreateQueueIfNonExistent(queueName: string.Format(ERROR_QUEUE_NAME_PATTERN, queueName));

                _channel.BasicPublish(
                    exchange: string.Empty,
                    routingKey: string.Format(ERROR_QUEUE_NAME_PATTERN, queueName),
                    basicProperties: null,
                    body: EncodeMessageToBytes(new ErrorMessage
                    {
                        Description = ex.Message,
                        StackTrace = ex.StackTrace,
                        Message = decodedBody
                    })
                );
            }
        };

        _channel.BasicConsume(
            queue: queueName,
            autoAck: true, // automatically remove message from queue when processed
            consumer: consumer
        );
    }

    public void Dispose()
    {
        _connection.Dispose();
        _channel.Dispose();
    }


    private byte[] EncodeMessageToBytes<T>(T message)
    {
        var serializedMessage = JsonSerializer.Serialize(message);
        return Encoding.UTF8.GetBytes(serializedMessage);
    }

    private void CreateExchangeIfNonExistent(
        string exchangeName,
        string type,
        bool durable = true,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null)
    {
        if (Exchanges.Contains(exchangeName)) 
            return;

        _channel.ExchangeDeclare(
            exchange: exchangeName,
            type: type,
            durable: durable,
            autoDelete: autoDelete,
            arguments: arguments
        );

        Exchanges.Add(exchangeName);
    }

    private void CreateQueueIfNonExistent(
        string queueName,
        string? exchangeToBind = null,
        string routingKey = "",
        bool durable = true,
        bool exclusive = false,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null)
    {
        if (Queues.Contains(queueName)) 
            return;

        _channel.QueueDeclare(
            queue: queueName,
            durable: durable,
            exclusive: exclusive,
            autoDelete: autoDelete,
            arguments: arguments
        );

        Queues.Add(queueName);

        if (exchangeToBind == null) 
            return;

        _channel.QueueBind(
            queue: queueName,
            exchange: exchangeToBind,
            routingKey: routingKey,
            arguments: arguments
        );
    }
}