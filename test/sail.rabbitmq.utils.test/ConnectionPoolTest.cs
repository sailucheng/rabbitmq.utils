using System;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace sail.rabbitmq.utils.test
{
  public class ConnectionPoolTest : IClassFixture<RabbitmqTestConfigurationFixture>
  {
    private readonly IServiceProvider _serviceProvider;

    public ConnectionPoolTest(RabbitmqTestConfigurationFixture fixture)
    {
      _serviceProvider = fixture.ServiceProvider;
    }

    [Fact]
    public void Not_Exists_In_Settings_Should_Throw_NotSupportException()
    {
      using var scope = _serviceProvider.CreateScope();
      var connectionPool = scope.ServiceProvider.GetRequiredService<IConnectionPool>();
      connectionPool.ShouldNotBeNull();
      Should.Throw<NotSupportedException>(() => connectionPool.GetConnection("default1"));
    }

    [Fact]
    public void In_Setting_Should_Create_Connection()
    {
      using var scope = _serviceProvider.CreateScope();
      var connectionPool = scope.ServiceProvider.GetRequiredService<IConnectionPool>();
      connectionPool.ShouldNotBeNull();
      var connection = connectionPool.GetConnection("default");
      connection.ShouldNotBeNull();
    }
  }
}