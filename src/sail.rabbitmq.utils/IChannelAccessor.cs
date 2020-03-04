using System;
using RabbitMQ.Client;

namespace sail.rabbitmq.utils
{
  public interface IChannelAccessor : IDisposable
  {
    string ChannelName { get; }
    IModel Channel { get; }
    void Release();
  }

  public class ChannelAccessor : IChannelAccessor
  {
    private readonly Action _disposeAction;
    private readonly Action _releaseAction;

    public ChannelAccessor(string channelName, IModel channel, Action releaseAction, Action disposeAction)
    {
      ChannelName = channelName;
      Channel = channel;
      _disposeAction = disposeAction;
      _releaseAction = releaseAction;
    }

    public string ChannelName { get; }
    public IModel Channel { get; }
    public void Release()
    {
      _releaseAction?.Invoke();
    }

    public void Dispose()
    {
      _disposeAction?.Invoke();
    }
  }
}