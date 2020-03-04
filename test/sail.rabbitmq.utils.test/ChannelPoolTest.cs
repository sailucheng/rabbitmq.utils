using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace sail.rabbitmq.utils.test
{
  public class ChannelPoolTest : IClassFixture<RabbitmqTestConfigurationFixture>
  {
    private readonly IServiceProvider _serviceProvider;

    public ChannelPoolTest(RabbitmqTestConfigurationFixture fixture)
    {
      _serviceProvider = fixture.ServiceProvider;
    }

    [Fact]
    public async Task Should_Get_Channel_With_Empty_Channel_Name()
    {
      using var scope = _serviceProvider.CreateScope();
      var channelPool = scope.ServiceProvider.GetRequiredService<IChannelPool>();
      var channelAccessor = channelPool.Acquire();
      channelAccessor.ChannelName.Length.ShouldBe(0);
      channelAccessor.Channel.ShouldNotBeNull();
      await Task.CompletedTask;
    }

    [Fact]
    public Task Should_Wait_If_Channel_In_Used()
    {
      using var scope = _serviceProvider.CreateScope();
      var channelPool = scope.ServiceProvider.GetRequiredService<IChannelPool>();
      var cts = new CancellationTokenSource();
      var t1 = Task.Run(() =>
      {
        var accessor = channelPool.Acquire();
        // cts.Token.WaitHandle.WaitOne(2000);
        Thread.Sleep(1000);
        accessor.Release();
      }, cts.Token);


      var sw = new Stopwatch();
      var t2 = Task.Run(() =>
      {
        sw.Start();
        var accessor = channelPool.Acquire();
        sw.Stop();
        accessor.Release();
      });

      Task.WaitAll(t1, t2);
      sw.ElapsedMilliseconds.ShouldBeGreaterThan(1000);

      return Task.CompletedTask;
    }

    [Fact]
    public void DisposeChannelAccessor_ChannelItem_Should_Remove_from_ChannelPool()
    {
      using (var scope = _serviceProvider.CreateScope())
      {
        var channelPool = scope.ServiceProvider.GetRequiredService<IChannelPool>();
        var channelAccessor = channelPool.Acquire();
        var channelsFieldInfo = channelPool.GetType().GetField("_channels", BindingFlags.Instance | BindingFlags.NonPublic);
        var channels = (IDictionary<string, ChannelPoolItem>)channelsFieldInfo.GetValue(channelPool);
        channels.Count.ShouldBe(1);
        channelAccessor.Dispose();
        channels.Count.ShouldBe(0);
      }
    }
  }
}