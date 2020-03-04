using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace sail.rabbitmq.utils.test
{
  public class RabbitMqConsumerTest : IClassFixture<RabbitmqTestConfigurationFixture>
  {
    private readonly IServiceProvider _serviceProvider;

    public RabbitMqConsumerTest(RabbitmqTestConfigurationFixture fixture)
    {
      _serviceProvider = fixture.ServiceProvider;
    }

    [Fact]
    public void Consumer_Should_Auto_Get_Message_And_Invoke_Callback()
    {
      using (var scope = _serviceProvider.CreateScope())
      {
        var consumer = scope.ServiceProvider.GetRequiredService<IRabbitMqConsumer>();

        var connectionName = "default";

        var queueDefinition = new QueueDefinition
        {
          QueueName = null,
          AutoDelete = false,
          Durable = false,
          Exclusive = false
        };

        var disposableHandle = consumer.Subscribe<UserRegisterEventData>(UserRegisterProcessor);
        consumer.Initialize(queueDefinition, connectionName);


        Task UserRegisterProcessor(UserRegisterEventData user)
        {
          user.UserName.ShouldBe("sailucheng");
          user.RegisterTime.ShouldBeGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
          return Task.CompletedTask;
        }
      }
    }
  }

  public class UserRegisterEventData
  {
    public string UserName { get; set; }
    public long RegisterTime { get; set; }
  }
}