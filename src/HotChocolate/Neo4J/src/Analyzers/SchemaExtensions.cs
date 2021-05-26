using HotChocolate.Data.Neo4J.Analyzers.Types;

namespace HotChocolate.Data.Neo4J.Analyzers
{
    public static class SchemaExtensions
    {
        public static DataGeneratorContext CreateGeneratorContext(this ISchema schema)
        {
            return DataGeneratorContext.FromSchema(schema);
        }
    }
}
