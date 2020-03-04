using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using sail.rabbitmq.utils;
using sail.rabbitmq.utils.DependencyInjection;

namespace rabbitmq.test.console
{
  class Program
  {
    static void Main(string[] args)
    {
      //configuration
      var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.dev.json").Build();
      var services = new ServiceCollection();
      services.AddSingleton(configuration);
      //logging
      services.AddLogging(cfg => cfg.AddConsole());
      //rabbitmq
      services.AddRabbitMq(configuration.GetSection("rabbitmq"));

      var serviceProvider = services.BuildServiceProvider();
      using (var scope = serviceProvider.CreateScope())
      {

        var exchange = new ExchangeDefinition
        {
          ExchangeName = "logs",
          Type = ExchangeType.Topic
        };
        var queue = new QueueDefinition
        {
          QueueName = "color.warning",
          AutoDelete = false,
          Exclusive = false,
          Durable = false
        };



        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();
        //publisher
        publisher.Initialize(queue: queue, exchange: exchange, routingKey: "#.warning");

        publisher.Publish(new UserRegisterEventData
        {
          UserName = "sailucheng",
          RegisterTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        var consumer = scope.ServiceProvider.GetRequiredService<IRabbitMqConsumer>();
        consumer.Initialize(queue);

        var disposableHandle = consumer.Subscribe<UserRegisterEventData>(usr =>
        {
          Console.Write($"{usr.UserName}\t{DateTimeOffset.FromUnixTimeMilliseconds(usr.RegisterTime).ToString()}");
          Console.WriteLine();
          return Task.CompletedTask;
        });

        Console.WriteLine("Press any key to exit..");
        Console.ReadLine();
        //unsubscribe
        disposableHandle.Dispose();
        Console.WriteLine("exit..");
      }
    }

    public class UserRegisterEventData
    {
      public string UserName { get; set; }
      public long RegisterTime { get; set; }
    }
  }
}