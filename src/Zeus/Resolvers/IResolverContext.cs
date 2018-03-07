using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    public interface IResolverContext
    {
        // schema context
        ISchema Schema { get; }

        ObjectTypeDefinition TypeDefinition { get; }

        FieldDefinition FieldDefinition { get; }

        // query context
        QueryDocument QueryDocument { get; }

        OperationDefinition OperationDefinition { get; }

        Field Field { get; }

        // execution context
        IImmutableStack<object> Path { get; }

        T Parent<T>();

        T Argument<T>(string name);

        T Service<T>();

        void RegisterQuery(IBatchedQuery query); // => redesign

        IResolverContext Create(SelectionContext selectionContext, object result);
    }
}
