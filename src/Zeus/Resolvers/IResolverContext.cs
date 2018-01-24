using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Zeus.Resolvers
{
    public interface IResolverContext
    {
        ISchema Schema { get; }

        IImmutableStack<object> Path { get; }

        T Parent<T>();

        T Argument<T>(string name);

        T Service<T>();

        IResolverContext Copy(object newParent);
        IResolverContext Copy(IDictionary<string, object> arguments, object newParent);
    }
}
