namespace HotChocolate.Analyzers
{
    public static class TypeNames
    {
        public const string Driver = "Neo4j.Driver";
        public const string Types = "HotChocolate." + nameof(Types);
        public const string Data = "HotChocolate." + nameof(Data);
        public const string Configuration = "HotChocolate.Execution.Configuration";
        public const string Neo4J = Data + "." + nameof(Neo4J);
        public const string Execution = Neo4J + "." + nameof(Execution);
        public const string DependencyInjection = "Microsoft.Extensions.DependencyInjection";
        public const string SystemCollections = "System.Collections.Generic";
        public const string Neo4JExecutable = Execution + "." + nameof(Neo4JExecutable);
        public const string UsePagingAttribute = Types + "." + nameof(UsePagingAttribute);
        public const string UseOffsetPagingAttribute = Types + "." + 
            nameof(UseOffsetPagingAttribute);
        public const string UseFilteringAttribute = Data + "." + nameof(UseFilteringAttribute);
        public const string UseSortingAttribute = Data + "." + nameof(UseSortingAttribute);
        public const string UseProjectionAttribute = Data + "." + nameof(UseProjectionAttribute);
        public const string List = SystemCollections + "." + nameof(List);
        public const string IAsyncSession = Driver + "." + nameof(IAsyncSession);
        public const string UseNeo4JDatabaseAttribute = Neo4J + "." + 
            nameof(UseNeo4JDatabaseAttribute);
        public const string Neo4JRelationshipAttribute = Neo4J + "." + 
            nameof(Neo4JRelationshipAttribute);
        public const string Neo4JRelationshipDirection = Neo4J + "." + "RelationshipDirection";
        public const string Neo4JDataRequestBuilderExtensions = Neo4J + "." + 
            nameof(Neo4JDataRequestBuilderExtensions);
        public const string IRequestExecutorBuilder = Configuration + "." + 
            nameof(IRequestExecutorBuilder); 
        public const string SchemaRequestExecutorBuilderExtensions = DependencyInjection + "." +
            nameof(SchemaRequestExecutorBuilderExtensions);

        public static string Global(string s)
        {
            return "global::" + s;
        }

        public static string Generics(string s, params string[] args)
        {
            return $"{s}<{string.Join(", ", args)}>";
        }

        public static string Nullable(string s)
        {
            return $"{s}?";
        }
    }
}