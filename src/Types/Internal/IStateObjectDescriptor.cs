using System;
using HotChocolate.Configuration;

namespace HotChocolate.Internal
{
    internal interface IStateObjectDescriptor<TKey>
    {
        TKey Key { get; }

        Type Type { get; }

        Func<IServiceProvider, object> Factory { get; }

        ExecutionScope Scope { get; }
    }
}
