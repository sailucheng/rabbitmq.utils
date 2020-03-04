namespace sail.rabbitmq.utils
{
  public interface IRabbitMqPublisher
  {
    void Initialize(QueueDefinition queue, ExchangeDefinition exchange = null, string routingKey = null, string connectionName = null);
    void Publish<TEventData>(TEventData eventData) where TEventData : class;
  }
}