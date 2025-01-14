using Infra.Messaging.Models;

namespace Infra.Messaging;

public interface IEventBus
{
    void Publish<T>(T @event) where T : IntegrationEvent;
    void Subscribe<T>(Func<T, Task> handler) where T : IntegrationEvent;
}
