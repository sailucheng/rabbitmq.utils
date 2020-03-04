using System;

namespace sail.rabbitmq.utils
{
  internal class DisposableObject : IDisposable
  {
    private readonly Action _disposeAction;

    public DisposableObject(Action disposeAction)
    {
      _disposeAction = disposeAction;
    }

    public void Dispose()
    {
      _disposeAction?.Invoke();
    }

    public static IDisposable Empty { get; } = new DisposableObject(null);

    public static IDisposable Create(Action dispose)
    {
      return new DisposableObject(dispose);
    }
  }
}