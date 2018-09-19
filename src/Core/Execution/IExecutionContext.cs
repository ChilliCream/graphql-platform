using System;
using System.Collections.Generic;
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

        void ReportError(IQueryError error);

        IEnumerable<IQueryError> GetErrors();

        IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType objectType,
            SelectionSetNode selectionSet);

        IReadOnlyCollection<IDirective> CollectDirectives(
            ObjectType objectType,
            FieldSelection fieldSelection,
            DirectiveScope scope);

        T GetResolver<T>();
    }
}
