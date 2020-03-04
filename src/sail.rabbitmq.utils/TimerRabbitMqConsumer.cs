using System.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using sail.rabbitmq.utils.serializable;

namespace sail.rabbitmq.utils
{
  public class TimerRabbitMqConsumer : IRabbitMqConsumer
  {
    private AbpTimer _timer;
    private readonly IConnectionPool _connections;
    private EventingBasicConsumer _consumer;
    private IObservable<BasicDeliverEventArgs> _observable;
    private readonly IRabbitmqMessageSerializer _serializer;
    private readonly ILogger<TimerRabbitMqConsumer> _logger;
    protected IModel Channel;


    protected readonly List<Func<IModel, BasicDeliverEventArgs, Task>> Callbacks;
    private readonly List<IDisposable> _observerDisposeHandles;

    private readonly object _syncObject = new object();
    private string _connectionName;
    public TimeSpan Period { get; set; } = TimeSpan.FromMilliseconds(2000);
    public QueueDefinition Queue { get; protected set; }

    public TimerRabbitMqConsumer(
      IRabbitmqMessageSerializer messageSerializer,
      IConnectionPool connectionPool, ILogger<TimerRabbitMqConsumer> logger)
    {
      _logger = logger;
      _connections = connectionPool;
      _serializer = messageSerializer;

      Callbacks = new List<Func<IModel, BasicDeliverEventArgs, Task>>();
      _observerDisposeHandles = new List<IDisposable>();
    }

    public void Initialize(QueueDefinition queue, string connectionName)
    {
      _timer = new AbpTimer { Period = (int)Period.TotalMilliseconds, RunOnStart = true };
      Queue = queue;
      _connectionName = connectionName;
      _timer.Elapsed += Timer_Elapsed;
      _timer.Start();
    }

    private void Timer_Elapsed(object sender, EventArgs e)
    {
      DisposeChannel();
      if (Callbacks.Any() == false) return;
      CreateChannel();
      DeclareQueue();
      CreateConsumer();
      SubscribeCallbacks();
    }
    private void CreateConsumer()
    {
      _consumer = new EventingBasicConsumer(Channel);

      _observable = Observable.FromEventPattern<BasicDeliverEventArgs>(
          handler => _consumer.Received += handler,
          handler => _consumer.Received -= handler)
        .Select(args => args.EventArgs);

      Channel.BasicConsume(queue: Queue.QueueName, autoAck: false, consumer: _consumer);
    }
    private void SubscribeCallbacks()
    {
      for (var i = 0; i < Callbacks.Count; i++)
      {
        var callback = Callbacks[i];
        var disposeHandle = _observable.Subscribe(async deliverEventArgs =>
        {
          try
          {
            await callback.Invoke(Channel, deliverEventArgs);
            Channel.BasicAck(deliverEventArgs.DeliveryTag, multiple: false);
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex, "rabbitmq timer consumer invoke callback error");
          }
        });
        _observerDisposeHandles[i] = disposeHandle;
      }
    }
    private void DeclareQueue()
    {
      var queueName = Channel.QueueDeclare(
        queue: Queue.QueueName,
        durable: Queue.Durable,
        exclusive: Queue.Exclusive,
        autoDelete: Queue.AutoDelete,
        arguments: Queue.Arguments);

      Queue.QueueName = queueName;
    }

    private void CreateChannel()
    {
      Channel = _connections.GetConnection(_connectionName).CreateModel();
    }

    private void DisposeChannel()
    {
      if (Channel != null)
      {
        try
        {
          lock (_syncObject)
          {
            Channel.Dispose();
          }
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "close channel error.");
        }
      }
    }

    public IDisposable Subscribe<TEventData>(Func<TEventData, Task> processor) where TEventData : class
    {
      lock (_syncObject)
      {
        // Add callback to queue
        Callbacks.Add(async (model, deliverEventArgs) =>
        {
          var data = (TEventData)_serializer.Deserialize(typeof(TEventData), deliverEventArgs.Body);
          await processor(data);
        });

        //create dispose action
        var disposeHandleIndex = Callbacks.Count - 1;
        _observerDisposeHandles.Insert(disposeHandleIndex, null);

        return Disposable.Create(() =>
        {
          _observerDisposeHandles[disposeHandleIndex]?.Dispose();
          _observerDisposeHandles.RemoveAt(disposeHandleIndex);
          Callbacks.RemoveAt(disposeHandleIndex);
        });
      }
    }

    public void Dispose()
    {
      if (Channel != null && Channel.IsClosed == false) Channel.Dispose();

      _timer?.Stop();
    }
  }
}