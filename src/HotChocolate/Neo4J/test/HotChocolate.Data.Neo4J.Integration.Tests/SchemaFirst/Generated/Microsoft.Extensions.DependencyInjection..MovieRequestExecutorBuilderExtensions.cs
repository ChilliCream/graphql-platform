namespace Microsoft.Extensions.DependencyInjection
{
    [global::System.CodeDom.Compiler.GeneratedCode("HotChocolate", "1.0.0.0")]
    public static partial class MovieRequestExecutorBuilderExtensions
    {
        public static global::HotChocolate.Execution.Configuration.IRequestExecutorBuilder AddMovieTypes(this global::HotChocolate.Execution.Configuration.IRequestExecutorBuilder builder)
        {
            global::Microsoft.Extensions.DependencyInjection.SchemaRequestExecutorBuilderExtensions.AddTypeExtension<global::HotChocolate.Data.Neo4J.Integration.SchemaFirst.Query>(builder);
            global::HotChocolate.Data.Neo4J.Neo4JDataRequestBuilderExtensions.AddNeo4JFiltering(builder);
            global::HotChocolate.Data.Neo4J.Neo4JDataRequestBuilderExtensions.AddNeo4JSorting(builder);
            global::HotChocolate.Data.Neo4J.Neo4JDataRequestBuilderExtensions.AddNeo4JProjections(builder);
            return builder;
        }
    }
}