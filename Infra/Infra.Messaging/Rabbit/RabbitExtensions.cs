using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace Infra.Messaging.Rabbit;

public static class RabbitExtensions
{
    public static IServiceCollection AddRabbitMQEventBus(this IHostApplicationBuilder host)
    {
        //services.AddSingleton<IConnectionFactory, ConnectionFactory>(sp =>
        //{
        //    var configuration = sp.GetRequiredService<IConfiguration>();

        //    return new ConnectionFactory
        //    {
        //        HostName = configuration["RabbitMQ:HostName"],
        //        UserName = configuration["RabbitMQ:UserName"],
        //        Password = configuration["RabbitMQ:Password"]
        //    };
        //});
        host.AddRabbitMQClient("rabbitmq");

        host.Services.AddSingleton<IEventBus, RabbitMQEventBus>();

        return host.Services;
    }
}
