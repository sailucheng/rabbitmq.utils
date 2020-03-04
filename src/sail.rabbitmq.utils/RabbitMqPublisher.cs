using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using sail.rabbitmq.utils.serializable;

namespace sail.rabbitmq.utils
{
  public class RabbitMqPublisher : IRabbitMqPublisher
  {
    private readonly IChannelPool _channels;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private ExchangeDefinition _exchange;
    private QueueDefinition _queue;
    private string _connectionName;
    private string _routingKey;
    private readonly IRabbitmqMessageSerializer _messageSerializer;
    public RabbitMqPublisher(IChannelPool channels, IRabbitmqMessageSerializer messageSerializer, ILogger<RabbitMqPublisher> logger)
    {
      _channels = channels;
      _messageSerializer = messageSerializer;
      _logger = logger;
    }

    public void Initialize(QueueDefinition queue, ExchangeDefinition exchange, string routingKey, string connectionName = null)
    {
      _exchange = exchange;
      _queue = queue;
      _routingKey = routingKey;
      _connectionName = connectionName;
    }

    public void Publish<TEventData>(TEventData eventData) where TEventData : class
    {
      using (var channelAccessor = CreateChannelAndBindExchangeQueue())
      {
        var properties = channelAccessor.Channel.CreateBasicProperties();
        properties.Persistent = true;

        var body = _messageSerializer.Serialize(eventData);

        channelAccessor.Channel.BasicPublish(
          exchange: _exchange != null ? _exchange.ExchangeName : string.Empty,
          routingKey: _exchange != null ? _routingKey : _queue.QueueName,
          basicProperties: properties,
          body: body);
      }
    }

    private IChannelAccessor CreateChannelAndBindExchangeQueue()
    {
      var channelName = $"sail.channels.{_exchange?.ExchangeName ?? "_"}.{_queue.QueueName}";
      var channelAccessor = _channels.Acquire(channelName, _connectionName);
      BindExchangeQueue(channelAccessor);
      return channelAccessor;
    }

    private void BindExchangeQueue(IChannelAccessor channelAccessor)
    {
      channelAccessor.Channel.QueueDeclare(
       queue: _queue.QueueName,
       durable: _queue.Durable,
       exclusive: _queue.Exclusive,
       autoDelete: _queue.AutoDelete,
       arguments: _queue.Arguments);

      if (_exchange != null)
      {
        channelAccessor.Channel.ExchangeDeclare(
          exchange: _exchange.ExchangeName,
          type: _exchange.Type,
          durable: _exchange.Durable,
          autoDelete: _exchange.AutoDelete,
          arguments: _exchange.Arguments);

        channelAccessor.Channel.QueueBind(queue: _queue.QueueName, exchange: _exchange.ExchangeName, routingKey: _routingKey, arguments: null);
      }

      channelAccessor.Channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
    }
  }
}