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
        IExecutionContext ExecutionContext { get; }

        IResolverContext ResolverContext { get; }

        ImmutableStack<object> Source { get; }

        FieldSelection Selection { get; }

        SelectionSetNode SelectionSet { get; }

        IType Type { get; }

        Path Path { get; }

        object Value { get; }

        bool IsNullable { get; }

        void ReportError(IEnumerable<IQueryError> errors);

        void ReportError(IQueryError error);

        void ReportError(string message);

        void IntegrateResult(object value);

        // enqueues the selectionset fields for processing in the next batch
        void EnqueueForProcessing(ObjectType objectType, OrderedDictionary objectResult);

        IFieldValueCompletionContext AsNonNullValueContext();

        IFieldValueCompletionContext AsElementValueContext(
            Path elementPath, IType elementType,
            object element, Action<object> addElementToList);
    }
}
