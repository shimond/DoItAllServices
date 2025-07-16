using Infra.Messaging.Models;

namespace Infra.Messaging;

public interface IEventBus
{
    Task PublishAsync<T>(T @event) where T : IntegrationEvent;
    Task SubscribeAsync<T>(Func<T, Task> handler) where T : IntegrationEvent;
}
