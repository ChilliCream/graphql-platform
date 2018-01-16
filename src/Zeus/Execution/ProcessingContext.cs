using GraphQLParser.AST;

namespace Zeus.Execution
{
    internal class QueryContext
    {
        

        public string TypeName { get; }
        public GraphQLFieldSelection FieldSelection { get; }
        public IResolverContext ResolverContext { get; }
        public ResolverResult ResolverResult { get; }

    }
}