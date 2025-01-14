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

    public void Publish<T>(T @event) where T : IntegrationEvent
    {
        using var connection = _connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();

        var exchangeName = "IntegrationEventExchange";
        channel.ExchangeDeclare(exchange: exchangeName, type: "fanout");

        var messageBody = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(messageBody);
        var properties = channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.DeliveryMode = 2; // Persistent delivery mode

        // Publish the message to the exchange directly
        channel.BasicPublish(
            exchange: exchangeName,
            routingKey: "", // routingKey is ignored in fanout exchanges
            basicProperties: properties,
            body: body
        );
    }

    public void Subscribe<T>(Func<T, Task> handler) where T : IntegrationEvent
    {
        var connection = _connectionFactory.CreateConnection();
        var channel = connection.CreateModel();

        var exchangeName = "IntegrationEventExchange";
        channel.ExchangeDeclare(exchange: exchangeName, type: "fanout");

        // Generate a unique queue name for each subscriber
        var queueName = $"{typeof(T).Name}_{Guid.NewGuid()}";
        channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: true, arguments: null);
        channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: "");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var @event = JsonSerializer.Deserialize<T>(message);
            await handler(@event);
        };

        channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
    }

}
