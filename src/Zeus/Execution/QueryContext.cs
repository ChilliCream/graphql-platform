using System.Collections.Generic;
using GraphQLParser.AST;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    internal class QueryContext
    {
        public QueryContext(string typeName, GraphQLFieldSelection fieldSelection,
           IResolverContext resolverContext)
           : this(typeName, fieldSelection, resolverContext, new Dictionary<string, object>())
        {
        }

        public QueryContext(string typeName, GraphQLFieldSelection fieldSelection,
            IResolverContext resolverContext, IDictionary<string, object> result)
        {
            TypeName = typeName;
            FieldSelection = fieldSelection;
            ResolverContext = resolverContext;
            Response = result;
        }

        public string TypeName { get; }
        public GraphQLFieldSelection FieldSelection { get; }

        public IResolverContext ResolverContext { get; }
        public ResolverResult ResolverResult { get; set; }

        public IDictionary<string, object> Response { get; }
    }
}