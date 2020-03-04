using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace sail.rabbitmq.utils
{
  public class ChannelPool : IChannelPool
  {
    private readonly ConcurrentDictionary<string, ChannelPoolItem> _channels;
    private bool _isDisposed;
    private readonly ILogger<ChannelPool> _logger;
    private readonly IConnectionPool _connections;
    public TimeSpan TotalDisposeRemainsSeconds = TimeSpan.FromSeconds(5);
    public ChannelPool(IConnectionPool connections, ILogger<ChannelPool> logger)
    {
      _channels = new ConcurrentDictionary<string, ChannelPoolItem>();
      _logger = logger;
      _connections = connections;
    }

    public IChannelAccessor Acquire(string channelName = null, string connectionName = null)
    {
      channelName = channelName ?? string.Empty;
      var channelItem = _channels.GetOrAdd(channelName, name => CreateChannelItem(name, connectionName));
      channelItem.Acquire();
      return new ChannelAccessor(channelName, channelItem.Channel, channelItem.Release, () =>
      {
        _channels.TryRemove(channelName, out var _);
        channelItem.Dispose();
      });
    }
    protected virtual ChannelPoolItem CreateChannelItem(string channelName, string connectionName)
    {
      var connection = _connections.GetConnection(connectionName);
      var channel = connection.CreateModel();
      return new ChannelPoolItem(channel);
    }
    public void Dispose()
    {
      if (_isDisposed) return;

      var sw = Stopwatch.StartNew();
      _logger.LogInformation($"begin disposing {_channels.Count} channel");
      var remainDisposeSeconds = TotalDisposeRemainsSeconds;
      foreach (var channel in _channels.Values)
      {
        var itemSw = Stopwatch.StartNew();
        try
        {
          channel.WaitIfInUsed(remainDisposeSeconds);
          channel.Dispose();
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "disposing channel error");
        }
        finally
        {
          remainDisposeSeconds = remainDisposeSeconds.Subtract(itemSw.Elapsed);
        }
      }
      sw.Stop();
      var totalUseSeconds = TotalDisposeRemainsSeconds - remainDisposeSeconds;
      _logger.LogInformation($"dispose {_channels.Count} channel , use {totalUseSeconds:0.00} ms.");
      _isDisposed = true;
    }
  }
}