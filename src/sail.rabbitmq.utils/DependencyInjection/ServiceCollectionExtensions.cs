using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using sail.rabbitmq.utils.configuration;
using sail.rabbitmq.utils.serializable;

namespace sail.rabbitmq.utils.DependencyInjection
{
  public static class ServiceCollectionExtensions
  {
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, Action<RabbitmqSettings> configure)
    {
      AddComponentInternal(services);

      services.Configure<RabbitmqSettings>(settings =>
      {
        configure(settings);
      });

      return services;
    }

    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
      AddComponentInternal(services);
      services.Configure<RabbitmqSettings>(configuration);
      return services;
    }

    private static void AddComponentInternal(IServiceCollection services)
    {
      services.AddOptions();
      services.AddSingleton<IConnectionPool, ConnectionPool>();
      services.AddTransient<IRabbitMqConsumer, TimerRabbitMqConsumer>();
      services.AddTransient<IRabbitMqPublisher, RabbitMqPublisher>();
      services.AddSingleton<IChannelPool, ChannelPool>();
      services.AddTransient<IRabbitmqMessageSerializer, Utf8JsonMessageSerializer>();
    }
  }
}