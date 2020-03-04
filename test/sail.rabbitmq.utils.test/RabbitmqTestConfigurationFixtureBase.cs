using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace sail.rabbitmq.utils.test
{
  public abstract class RabbitmqTestConfigurationFixtureBase : IDisposable
  {
    public IServiceProvider ServiceProvider { get; protected set; }
    public string ConfigureFile { get; set; } = "appsettings.dev.json";
    public IConfiguration Configuration { get; set; }

    public RabbitmqTestConfigurationFixtureBase()
    {
      var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile(ConfigureFile)
        .Build();

      Configuration = configuration;
      var serviceCollection = new ServiceCollection();

      serviceCollection.AddSingleton(configuration);

      Configure(serviceCollection);

      ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    protected virtual void Configure(ServiceCollection services)
    {
    }

    public virtual void Dispose()
    {
    }
  }
}