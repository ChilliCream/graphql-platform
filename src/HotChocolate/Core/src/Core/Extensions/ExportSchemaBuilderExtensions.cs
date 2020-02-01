using HotChocolate.Execution.Batching;

namespace HotChocolate
{
    public static class ExportSchemaBuilderExtensions
    {
        public static ISchemaBuilder AddExportDirectiveType(
            this ISchemaBuilder builder)
        {
            return builder.AddDirectiveType<ExportDirectiveType>();
        }
    }
}
