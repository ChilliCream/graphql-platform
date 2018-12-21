using System;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal interface IExecutionContext
        : IDisposable
    {
        // schema
        ISchema Schema { get; }

        IReadOnlySchemaOptions Options { get; }

        IServiceProvider Services { get; }

        IErrorHandler ErrorHandler { get; }

        // context
        object RootValue { get; }

        IDataLoaderProvider DataLoaders { get; }

        ICustomContextProvider CustomContexts { get; }

        // query ast
        DocumentNode QueryDocument { get; }

        OperationDefinitionNode Operation { get; }

        ObjectType OperationType { get; }

        // query
        FragmentCollection Fragments { get; }

        VariableCollection Variables { get; }

        IReadOnlyDictionary<string, object> RequestProperties { get; }

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

        IExecutionContext Clone(
            IReadOnlyDictionary<string, object> requestProperties,
            CancellationToken requestAborted);
    }
}
