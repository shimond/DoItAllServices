using Infra.Messaging.Models;
using Infra.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

public class RabbitMQEventBus : IEventBus
{
    private readonly IConnectionFactory _connectionFactory;

    public RabbitMQEventBus(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task PublishAsync<T>(T @event) where T : IntegrationEvent
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        var exchangeName = "IntegrationEventExchange";
        await channel.ExchangeDeclareAsync(exchange: exchangeName, type: "fanout");

        var messageBody = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(messageBody);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent // Persistent delivery mode
        };

        // Publish the message to the exchange directly
        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: "", // routingKey is ignored in fanout exchanges
            mandatory: false,
            basicProperties: properties,
            body: body
        );
    }

    public async Task SubscribeAsync<T>(Func<T, Task> handler) where T : IntegrationEvent
    {
        var connection = await _connectionFactory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        var exchangeName = "IntegrationEventExchange";
        await channel.ExchangeDeclareAsync(exchange: exchangeName, type: "fanout");

        // Generate a unique queue name for each subscriber
        var queueName = $"{typeof(T).Name}_{Guid.NewGuid()}";
        await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: true, arguments: null);
        await channel.QueueBindAsync(queue: queueName, exchange: exchangeName, routingKey: "");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var @event = JsonSerializer.Deserialize<T>(message);
            if (@event != null)
            {
                await handler(@event);
            }
        };

        await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);
    }
}
