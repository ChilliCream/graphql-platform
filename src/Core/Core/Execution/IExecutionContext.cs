using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    internal interface IExecutionContext
        : IDisposable
    {
        // schema
        ISchema Schema { get; }

        IServiceProvider Services { get; }

        IErrorHandler ErrorHandler { get; }

        // context
        object RootValue { get; }

        // query ast
        DocumentNode QueryDocument { get; }

        OperationDefinitionNode Operation { get; }

        ObjectType OperationType { get; }

        // query
        FragmentCollection Fragments { get; }

        VariableCollection Variables { get; }

        IDictionary<string, object> Custom { get; }

        CancellationToken RequestAborted { get; }

        void ReportError(IError error);

        IEnumerable<IError> GetErrors();

        IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType objectType,
            SelectionSetNode selectionSet);

        ExecuteMiddleware GetMiddleware(
            ObjectType objectType,
            FieldNode fieldSelection);

        T GetResolver<T>();
    }
}
