using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal sealed class __InputValue : ITypeResolverInterceptor
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

            case "type":
                features.Set(new ResolveFieldValue(Type));
                break;

            case "isDeprecated":
                features.Set(new ResolveFieldValue(IsDeprecated));
                break;

            case "deprecationReason":
                features.Set(new ResolveFieldValue(DeprecationReason));
                break;

            case "defaultValue":
                features.Set(new ResolveFieldValue(DefaultValue));
                break;
        }
    }

    public static void Name(FieldContext context)
        => context.WriteValue(context.Parent<IInputValueDefinition>().Name);

    public static void Description(FieldContext context)
    {
        if (context.Parent<IInputValueDefinition>() is { Description: not null } fieldDef)
        {
            context.WriteValue(fieldDef.Description);
        }
    }

    public static void Type(FieldContext context)
    {
        var field = context.Parent<IInputValueDefinition>();
        context.AddRuntimeResult(field.Type);
        context.FieldResult.SetNextValue(context.RentInitializedObjectResult());
    }

    public static void IsDeprecated(FieldContext context)
    {
        var field = context.Parent<IInputValueDefinition>();
        context.WriteValue(field.IsDeprecated);
    }

    public static void DeprecationReason(FieldContext context)
    {
        var field = context.Parent<IInputValueDefinition>();
        if (field.DeprecationReason is not null)
        {
            context.WriteValue(field.DeprecationReason);
        }
    }

    public static void DefaultValue(FieldContext context)
    {
        var field = context.Parent<IInputValueDefinition>();
        if (field.DefaultValue is not null)
        {
            context.WriteValue(field.DefaultValue.Print());
        }
    }
}
