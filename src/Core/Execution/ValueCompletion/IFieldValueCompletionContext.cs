using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal interface IFieldValueCompletionContext
    {
        ExecutionContext ExecutionContext { get; }

        IResolverContext ResolverContext { get; }

        ImmutableStack<object> Source { get; }

        FieldSelection Selection { get; }

        SelectionSetNode SelectionSet { get; }

        IType Type { get; }

        Path Path { get; }

        object Value { get; }

        bool IsNullable { get; }

        void AddErrors(IEnumerable<IQueryError> errors);

        void AddError(IQueryError error);

        void AddError(string message);

        void SetResult(object value);

        IFieldValueCompletionContext AsNonNullValueContext();

        IFieldValueCompletionContext AsElementValueContext(
            Path elementPath, IType elementType,
            object element, Action<object> addElementToList);
    }
}
