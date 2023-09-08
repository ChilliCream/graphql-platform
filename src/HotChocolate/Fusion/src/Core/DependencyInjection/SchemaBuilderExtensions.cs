using HotChocolate;
using HotChocolate.Fusion.Metadata;

namespace Microsoft.Extensions.DependencyInjection;

internal static class SchemaBuilderExtensions
{
    private const string _fusionGraphConfig = "HotChocolate.Fusion.FusionGraphConfig";

    public static bool ContainsFusionGraphConfig(
        this ISchemaBuilder builder)
        => builder.ContextData.ContainsKey(_fusionGraphConfig);

    public static FusionGraphConfiguration GetFusionGraphConfig(
        this ISchemaBuilder builder)
        => (FusionGraphConfiguration)builder.ContextData[_fusionGraphConfig]!;

    public static ISchemaBuilder SetFusionGraphConfig(
        this ISchemaBuilder builder,
        FusionGraphConfiguration config)
        => builder.SetContextData(_fusionGraphConfig, config);
}
