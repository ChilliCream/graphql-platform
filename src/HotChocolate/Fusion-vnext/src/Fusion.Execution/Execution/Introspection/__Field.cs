using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal sealed class __Field : ITypeResolverInterceptor
{
    public void OnApplyResolver(string fieldName, IFeatureCollection features)
    {
        switch (fieldName)
        {
            case "name":
                features.Set(new ResolveFieldValue(Name));
                break;

            case "description":
                features.Set(new ResolveFieldValue(Description));
                break;

            case "args":
                features.Set(new ResolveFieldValue(Arguments));
                break;

            case "type":
                features.Set(new ResolveFieldValue(Type));
                break;

            case "isDeprecated":
                features.Set(new ResolveFieldValue(IsDeprecated));
                break;

            case "deprecationReason":
                features.Set(new ResolveFieldValue(DeprecationReason));
                break;
        }
    }

    public static void Name(FieldContext context)
        => context.WriteValue(context.Parent<IOutputFieldDefinition>().Name);

    public static void Description(FieldContext context)
    {
        if (context.Parent<IOutputFieldDefinition>() is { Description: not null } fieldDef)
        {
            context.WriteValue(fieldDef.Description);
        }
    }

    public static void Arguments(FieldContext context)
    {
        var field = context.Parent<IOutputFieldDefinition>();
        var includeDeprecated = context.ArgumentValue<BooleanValueNode>("includeDeprecated").Value;
        var count = includeDeprecated
            ? field.Arguments.Count
            : field.Arguments.Count(t => !t.IsDeprecated);
        var list = context.FieldResult.SetListValue(count);

        var index = 0;
        foreach (var element in list.EnumerateArray())
        {
            var argument = field.Arguments[index++];

            if (!includeDeprecated && argument.IsDeprecated)
            {
                continue;
            }

            context.AddRuntimeResult(argument);
            element.SetObjectValue();
        }
    }

    public static void Type(FieldContext context)
    {
        var field = context.Parent<IOutputFieldDefinition>();
        context.AddRuntimeResult(field.Type);
        context.FieldResult.SetObjectValue();
    }

    public static void IsDeprecated(FieldContext context)
    {
        var field = context.Parent<IOutputFieldDefinition>();
        context.WriteValue(field.IsDeprecated);
    }

    public static void DeprecationReason(FieldContext context)
    {
        var field = context.Parent<IOutputFieldDefinition>();
        if (field.DeprecationReason is not null)
        {
            context.WriteValue(field.DeprecationReason);
        }
    }
}
