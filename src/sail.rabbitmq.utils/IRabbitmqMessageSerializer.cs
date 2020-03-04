using System;
using System.Text;
using Newtonsoft.Json;

namespace sail.rabbitmq.utils.serializable
{
  public interface IRabbitmqMessageSerializer
  {
    byte[] Serialize(object obj);
    object Deserialize(Type type, byte[] buffer);
  }

  public class Utf8JsonMessageSerializer : IRabbitmqMessageSerializer
  {
    public object Deserialize(Type type, byte[] buffer)
    {
      return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(buffer), type);
    }

    public byte[] Serialize(object obj)
    {
      return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
    }
  }
}