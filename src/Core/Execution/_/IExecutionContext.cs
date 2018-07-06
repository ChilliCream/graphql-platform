using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal interface IExecutionContext
    {
        // schema
        ISchema Schema { get; }

        // context
        object RootValue { get; }
        object UserContext { get; }

        // query ast
        DocumentNode QueryDocument { get; }
        OperationDefinitionNode Operation { get; }

        // query 
        FragmentCollection Fragments { get; }
        VariableCollection Variables { get; }

        // result
        OrderedDictionary Data { get; } // remove
        List<IQueryError> Errors { get; } // remove

        // processing
        List<FieldResolverTask> NextBatch { get; } // remove


        // field
        IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType objectType, SelectionSetNode selectionSet); // remove -> strategy base class
    }
}