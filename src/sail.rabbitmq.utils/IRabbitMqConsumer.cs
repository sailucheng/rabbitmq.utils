using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace sail.rabbitmq.utils
{
  public interface IRabbitMqConsumer : IDisposable
  {
    void Initialize(QueueDefinition queue, string connectionName = null);
    IDisposable Subscribe<TEventData>(Func<TEventData, Task> processor) where TEventData : class;
  }
}