using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using sail.rabbitmq.utils.configuration;

namespace sail.rabbitmq.utils
{
  public interface IConnectionPool : IDisposable
  {
    IConnection GetConnection(string connectionName = null);
  }

  public class ConnectionPool : IConnectionPool
  {
    private readonly ConcurrentDictionary<string, IConnection> _connections;
    private readonly RabbitmqSettings _settings;
    private volatile bool _isDisposed;

    public ConnectionPool(IOptions<RabbitmqSettings> settings)
    {
      _connections = new ConcurrentDictionary<string, IConnection>();
      _settings = settings.Value;
    }

    public IConnection GetConnection(string connectionName)
    {
      connectionName = connectionName ?? _settings.DefaultConnectionName;
      return _connections.GetOrAdd(connectionName, name =>
      {
        var connectionFac = new ConnectionFactory();
        if (_settings.Values.TryGetValue(connectionName, out var setting) == false)
          throw new NotSupportedException($"{connectionName} connection do not configure");
        connectionFac.UserName = setting.UserName;
        connectionFac.Password = setting.Password;
        connectionFac.Port = setting.Port;
        connectionFac.VirtualHost = setting.Vhost;
        connectionFac.HostName = setting.Host;
        return connectionFac.CreateConnection();
      });
    }

    public void Dispose()
    {
      if (_isDisposed) return;
      foreach (var connection in _connections.Values)
      {
        connection.Dispose();
      }

      _isDisposed = true;
    }
  }
}