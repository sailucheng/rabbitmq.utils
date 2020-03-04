using System.Collections.Generic;
using RabbitMQ.Client;

namespace sail.rabbitmq.utils
{
  public class QueueDefinition
  {
    public string QueueName { get; set; }
    public bool Durable { get; set; } = false;
    public bool Exclusive { get; set; } = false;
    public bool AutoDelete { get; set; } = false;
    public IDictionary<string, object> Arguments { get; set; }
  }
  public class ExchangeDefinition
  {
    public string ExchangeName { get; set; }
    public string Type { get; set; }
    public bool Durable { get; set; } = false;
    public bool AutoDelete { get; set; } = false;

    public IDictionary<string, object> Arguments { get; set; }
  }
}