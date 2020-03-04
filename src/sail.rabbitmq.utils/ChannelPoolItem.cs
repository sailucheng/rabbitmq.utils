using System;
using System.Threading;
using RabbitMQ.Client;

namespace sail.rabbitmq.utils
{
  public class ChannelPoolItem : IDisposable
  {
    public IModel Channel { get; }
    private volatile bool _isInUsed;
    public ChannelPoolItem(IModel channel)
    {
      Channel = channel;
    }
    public void Acquire()
    {
      lock (this)
      {
        while (_isInUsed == true)
        {
          Monitor.Wait(this);
        }
        _isInUsed = true;
      }
    }
    public void WaitIfInUsed(TimeSpan timeout)
    {
      lock (this)
      {
        if (_isInUsed == false) return;
        Monitor.Wait(this, timeout);
      }
    }
    public void Release()
    {
      lock (this)
      {
        _isInUsed = false;
        Monitor.PulseAll(this);
      }
    }
    public void Dispose()
    {
      if (Channel.IsOpen) Channel.Dispose();
    }
  }
}
