using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using sail.rabbitmq.utils.DependencyInjection;

namespace sail.rabbitmq.utils.test
{
  public class RabbitmqTestConfigurationFixture : RabbitmqTestConfigurationFixtureBase
  {
    protected override void Configure(ServiceCollection services)
    {
      var config = Configuration.GetSection("rabbitmq");

      services.AddLogging(builder => { builder.AddDebug().AddConsole(); });

      services.AddRabbitMq(config);
    }
  }
}