using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal sealed class __Directive : ITypeResolverInterceptor
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
            case "isRepeatable":
                features.Set(new ResolveFieldValue(IsRepeatable));
                break;
            case "locations":
                features.Set(new ResolveFieldValue(Locations));
                break;
            case "arguments":
                features.Set(new ResolveFieldValue(Arguments));
                break;
        }
    }

    public static void Name(FieldContext context)
    {
        var directiveDef = context.Parent<IDirectiveDefinition>();
        context.WriteValue(directiveDef.Name);
    }

    public static void Description(FieldContext context)
    {
        var directiveDef = context.Parent<IDirectiveDefinition>();
        context.WriteValue(directiveDef.Description);
    }

    public static void IsRepeatable(FieldContext context)
    {
        var directiveDef = context.Parent<IDirectiveDefinition>();
        context.WriteValue(directiveDef.IsRepeatable);
    }

    public static void Locations(FieldContext context)
    {
        var directiveDef = context.Parent<IDirectiveDefinition>();
        switch (directiveDef.Locations)
        {
            case DirectiveLocation.Query:
                context.WriteValue(__DirectiveLocation.Query);
                break;
            case DirectiveLocation.Mutation:
                context.WriteValue(__DirectiveLocation.Mutation);
                break;
            case DirectiveLocation.Subscription:
                context.WriteValue(__DirectiveLocation.Subscription);
                break;
            case DirectiveLocation.Field:
                context.WriteValue(__DirectiveLocation.Field);
                break;
            case DirectiveLocation.FragmentDefinition:
                context.WriteValue(__DirectiveLocation.FragmentDefinition);
                break;
            case DirectiveLocation.FragmentSpread:
                context.WriteValue(__DirectiveLocation.FragmentSpread);
                break;
            case DirectiveLocation.InlineFragment:
                context.WriteValue(__DirectiveLocation.InlineFragment);
                break;
            case DirectiveLocation.VariableDefinition:
                context.WriteValue(__DirectiveLocation.VariableDefinition);
                break;
            case DirectiveLocation.Schema:
                context.WriteValue(__DirectiveLocation.Schema);
                break;
            case DirectiveLocation.Scalar:
                context.WriteValue(__DirectiveLocation.Scalar);
                break;
            case DirectiveLocation.Object:
                context.WriteValue(__DirectiveLocation.Object);
                break;
            case DirectiveLocation.FieldDefinition:
                context.WriteValue(__DirectiveLocation.FieldDefinition);
                break;
            case DirectiveLocation.ArgumentDefinition:
                context.WriteValue(__DirectiveLocation.ArgumentDefinition);
                break;
            case DirectiveLocation.Interface:
                context.WriteValue(__DirectiveLocation.Interface);
                break;
            case DirectiveLocation.Union:
                context.WriteValue(__DirectiveLocation.Union);
                break;
            case DirectiveLocation.Enum:
                context.WriteValue(__DirectiveLocation.Enum);
                break;
            case DirectiveLocation.EnumValue:
                context.WriteValue(__DirectiveLocation.EnumValue);
                break;
            case DirectiveLocation.InputObject:
                context.WriteValue(__DirectiveLocation.InputObject);
                break;
            case DirectiveLocation.InputFieldDefinition:
                context.WriteValue(__DirectiveLocation.InputFieldDefinition);
                break;
            default:
                throw new NotSupportedException($"Directive location {directiveDef.Locations} is not supported.");
        }
    }

    public static void Arguments(FieldContext context)
    {
        var directiveDef = context.Parent<IDirectiveDefinition>();
        var includeDeprecated = context.ArgumentValue<BooleanValueNode>("includeDeprecated").Value;
        var list = context.ResultPool.RentObjectListResult();
        context.FieldResult.SetNextValue(list);

        foreach (var argument in directiveDef.Arguments)
        {
            if (!includeDeprecated && argument.IsDeprecated)
            {
                continue;
            }

            context.AddRuntimeResult(argument);
            list.SetNextValue(context.RentInitializedObjectResult());
        }
    }
}
