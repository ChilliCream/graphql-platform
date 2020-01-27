using HotChocolate.Types;

#nullable enable

namespace HotChocolate
{
    public static class DirectiveTypeExtensions
    {
        public static ISchemaBuilder AddCostDirectiveType(
            this ISchemaBuilder builder) =>
            builder.AddDirectiveType<CostDirectiveType>();
    }
}
