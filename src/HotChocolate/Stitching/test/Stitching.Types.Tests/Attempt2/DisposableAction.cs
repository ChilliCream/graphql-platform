using System;

namespace HotChocolate.Stitching.Types.Attempt2;

public class DisposableAction : IDisposable
{
    private readonly Action _func;

    public DisposableAction(Action func)
    {
        _func = func;
    }

    public void Dispose()
    {
        _func.Invoke();
    }
}
