using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infra.Messaging.Rabbit;

public static class RabbitExtensions
{
    public static IServiceCollection AddRabbitMQEventBus(this IHostApplicationBuilder host)
    {
        host.AddRabbitMQClient("rabbitmq");

        host.Services.AddSingleton<IEventBus, RabbitMQEventBus>();

        return host.Services;
    }
}
