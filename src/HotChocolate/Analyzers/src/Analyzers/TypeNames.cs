namespace HotChocolate.Data.Neo4J.Analyzers
{
    public static class TypeNames
    {
        public const string TypesNamespace = "HotChocolate.Types";
        public const string Neo4JNamespace = "HotChocolate.Data.Neo4J";
        public const string ExecutionNamespace = Neo4JNamespace + ".Execution";
        public const string Neo4JExecutable = ExecutionNamespace + "." + nameof(Neo4JExecutable);
        public const string UsePagingAttribute = TypesNamespace + "." + nameof(UsePagingAttribute);
        public const string UseOffsetPagingAttribute = 
            TypesNamespace + "." + nameof(UseOffsetPagingAttribute);

        public static string Global(string s)
        {
            return "global::" + s;
        }
    }
}
