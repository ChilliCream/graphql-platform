using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    internal class ExecutionContext
    {
        public Schema Schema { get; }
        public FragmentCollection Fragments { get; }
        public object RootValue { get; }
        public object UserContext { get; }
        public OperationDefinitionNode Operation { get; }
        public VariableCollection Variables { get; }
        public List<IQueryError> Errors { get; }

        // contextValue: mixed,
        // fieldResolver: GraphQLFieldResolver<any, any>,
    }
}
