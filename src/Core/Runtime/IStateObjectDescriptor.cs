using System;

namespace HotChocolate.Runtime
{
    public interface IScopedStateDescriptor<TKey>
    {
        TKey Key { get; }

        Type Type { get; }

        Func<IServiceProvider, object> Factory { get; }

        ExecutionScope Scope { get; }
    }
}
