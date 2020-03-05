# rabbitmq.utils
####  Rabbitmq 工具包，旨在简化对于`RabbitMQ.Client`的调用。封装了一些常用逻辑，意在精简代码调用。
---


### Configuration

链接配置类 `RabbitmqSettings`,默认连接名*Default*.
使用方法`Startup.cs`中进行组件注册.

> 从配置文件中读取
```cssharp
services.AddRabbitMq(configuration.GetSection("rabbitmq"));
```
***appsettings.json***

```
"rabbitmq": {
    "defaultConnectionName": "Default",
    "Values": {
      "Default": {
        "Vhost": "/",
        "UserName": "guest",
        "Password": "123123",
        "Host": "localhost",
        "Port": "5672",
        "Name": "Default"
      }
    }
}
```


> 手动设置
```
services.AddRabbitMq(settings=>{
  settings.DefaultConnnectionName = "anotherName";
  settings.Values.Add("anohterName" , new RabbitmqSetting{
    Host = "192.168.1.102"
    .....
  });
});
```
---
### ConnectionPool and ChannelPool
两种类型都注册为Singleton单例模式,获取到的Connnection或者Channel可以重用。

##### ConnectionPool
```
var connections = serviceProvider.GetRequiredService<IConnectionPool>();
var connection = connections.GetConnection("settingname"); //空则为Default节点
```
##### ChannelPool
```
var channelPool = serviceProvider.GetRequiredService<IChannelPool>();
var channelAccessor = channelPool.Acquire(channelName , connectionName);

//do Some thing

//释放资源
channelAccessor.Release();
//or 删除channel
channelAccessor.Dispose();
```
****注意：获取到`ChannelAccessor`之后，操作将对channel将进行独占，其他使用相同channelName获取到的channel资源，将会等待前一个操作释放,才会继续。****
- `channelAccessor.Release()`归还资源，用于之后重用
- `channelAccessor.Dispose()`将会从channelPool中彻底删除该channel.
---
### RabbitMq publisher and consumer
- `IRabbitMqPublisher` 用于message的发布,默认实现`RabbitMqPublisher`,*注册为Transient*。
- `IRabbitMqConsumer`用于接收message,默认实现`TimerRabbitMqConsumer`,*注册为Transient*。

#### 使用方法
*publisher*
```cssharp
//定义exchange
var exchange = new ExchangeDefinition
{
    ExchangeName = "logs",
    Type = ExchangeType.Topic
};
//定义queue
var queue = new QueueDefinition
{
    QueueName = "color.warning",
    AutoDelete = false,
    Exclusive = false,
    Durable = false
};
var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();
//initial publisher
publisher.Initialize(queue: queue, exchange: exchange, routingKey: "#.warning");
//send message
publisher.Publish(new UserRegisterEventData
{
    UserName = "sailucheng",
    RegisterTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
});
```
*Consumer*
```csshrap
var consumer = scope.ServiceProvider.GetRequiredService<IRabbitMqConsumer>();
consumer.Initialize(queue);

var disposableHandle = consumer.Subscribe<UserRegisterEventData>(usr =>
{
     Console.Write($"{usr.UserName}\t{DateTimeOffset.FromUnixTimeMilliseconds(usr.RegisterTime).ToString()}");
     Console.WriteLine();
     return Task.CompletedTask;
});

//unsubscribe 取消订阅,后续不会执行之前的订阅回调.
disposableHandle.Dispose();
```
*consumer默认实现`TimerRabbitMqConsumer`,机制为每隔一段时间，建立channel进行Message获取，然后遍历执行回调,轮询默认时间2 sec* 

*如果想手动设置轮询时间*.
```
var consumer = serviceProvider.GetRequriedService<IRabbitMqConsumer>();
((TimerRabbitMqConsumer)consumer).Period = timespan;
consumer.Initialize();

```
`Initialize()`*之后修改时间无效*.

> 该utils包，很大程度借鉴了[Volo.Abp.RabbitMQ](https://github.com/sailucheng/abp/tree/dev/framework/src/Volo.Abp.RabbitMQ)
另外abp中有自己的基于rabbitmq的eventbus实现。该包对很多部分进行了简化处理，避免了对于abp包的依赖。