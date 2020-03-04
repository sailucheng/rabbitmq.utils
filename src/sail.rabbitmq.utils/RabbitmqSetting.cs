using System;
using System.Collections;
using System.Collections.Generic;

namespace sail.rabbitmq.utils.configuration
{
  public class RabbitmqSetting
  {
    public string Name { get; set; }
    public string Host { get; set; }
    public string Vhost { get; set; }
    public string Password { get; set; }
    public string UserName { get; set; }
    public int Port { get; set; }
  }

  public class RabbitmqSettings
  {
    public string DefaultConnectionName = "Default";
    private readonly Dictionary<string, RabbitmqSetting> _settings;

    public RabbitmqSettings()
    {
      _settings = new Dictionary<string, RabbitmqSetting>(StringComparer.OrdinalIgnoreCase);
    }

    public Dictionary<string, RabbitmqSetting> Values => _settings;
  }
}