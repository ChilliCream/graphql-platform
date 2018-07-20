using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Runtime;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal interface IExecutionContext
    {
        // schema
        ISchema Schema { get; }
        IReadOnlySchemaOptions Options { get; }

        // context
        object RootValue { get; }
        IDataLoaderState DataLoaders { get; }

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
            ObjectType objectType, SelectionSetNode selectionSet);

        T GetState<T>();
    }
}
