using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public interface IResolverContext
    {
        // schema context
        object Schema { get; }

        ObjectType ObjectType { get; }

        Field Field { get; }

        // query context
        object QueryDocument { get; }

        object OperationDefinition { get; }

        object FieldSelection { get; }

        // execution context
        IImmutableStack<object> Path { get; }

        T Parent<T>();

        T Argument<T>(string name);

        T Service<T>();

        void RegisterQuery(object query); // => redesign
    }
}