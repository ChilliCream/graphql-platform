using System;

namespace HotChocolate.Language.Rewriters.Utilities;

public readonly struct DisposableAction : IDisposable
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
