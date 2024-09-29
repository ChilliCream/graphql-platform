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

        List<Action<ISchemaTypeDescriptor>> options;

        if (!builder.ContextData.TryGetValue(WellKnownContextData.InternalSchemaOptions, out var value))
        {
            options = [];
            builder.ContextData.Add(WellKnownContextData.InternalSchemaOptions, options);
            value = options;
        }

        options = (List<Action<ISchemaTypeDescriptor>>)value!;
        options.Add(configure);
    }

    public static void AddSchemaConfiguration(
        this IDescriptorContext context,
        Action<ISchemaTypeDescriptor> configure)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configure);

        List<Action<ISchemaTypeDescriptor>> options;

        if (!context.ContextData.TryGetValue(WellKnownContextData.InternalSchemaOptions, out var value))
        {
            options = [];
            context.ContextData.Add(WellKnownContextData.InternalSchemaOptions, options);
            value = options;
        }

        options = (List<Action<ISchemaTypeDescriptor>>)value!;
        options.Add(configure);
    }

    public static void ApplySchemaConfigurations(
        this IDescriptorContext context,
        ISchemaTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(descriptor);

        if (context.ContextData.TryGetValue(WellKnownContextData.InternalSchemaOptions, out var value) &&
            value is List<Action<ISchemaTypeDescriptor>> options)
        {
            foreach (var option in options)
            {
                option(descriptor);
            }
        }
    }
}
