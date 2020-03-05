using System;
using System.Threading;
using RabbitMQ.Client;

namespace sail.rabbitmq.utils
{
  public class ChannelPoolItem : IDisposable
  {
    public IModel Channel { get; }
    private volatile bool _isInUsed;
    private volatile bool _isDisposed;
    public ChannelPoolItem(IModel channel)
    {
      Channel = channel;
    }
    public void Acquire()
    {
      lock (this)
      {
        if (_isDisposed == true) throw new ObjectDisposedException(nameof(Channel));

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
        if (_isInUsed == false || _isDisposed == true) return;
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
      lock (this)
      {
        if (Channel.IsOpen)
        {
          Channel.Dispose();
        }
        _isDisposed = true;
        Monitor.PulseAll(this);
      }
    }
  }
}
