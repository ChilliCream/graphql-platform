using System;
using HotChocolate.Resolvers;

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
