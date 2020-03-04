using System;

namespace sail.rabbitmq.utils
{
  public interface IChannelPool : IDisposable
  {
    IChannelAccessor Acquire(string channelName = null, string connectionName = null);
  }
}