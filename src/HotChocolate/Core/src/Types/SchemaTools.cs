#nullable disable

using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate;

internal static class SchemaTools
{
    public static void AddSchemaConfiguration(
        this ISchemaBuilder builder,
        Action<ISchemaTypeDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Features.GetOrSet<TypeSystemFeature>().SchemaTypeOptions.Add(configure);
    }

    public static void AddSchemaConfiguration(
        this IDescriptorContext context,
        Action<ISchemaTypeDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configure);

        context.Features.GetOrSet<TypeSystemFeature>().SchemaTypeOptions.Add(configure);
    }

    public static void ApplySchemaConfigurations(
        this IDescriptorContext context,
        ISchemaTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!context.Features.TryGet(out TypeSystemFeature feature))
        {
            return;
        }

        foreach (var option in feature.SchemaTypeOptions)
        {
            option(descriptor);
        }
    }
}
