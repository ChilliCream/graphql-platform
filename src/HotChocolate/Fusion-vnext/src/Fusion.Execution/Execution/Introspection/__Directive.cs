using System.Diagnostics;
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
            case "args":
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
        var locations = directiveDef.Locations.AsEnumerable().ToArray();
        using var list = context.FieldResult.CreateListValue(locations.Length).EnumerateArray().GetEnumerator();

        foreach (var location in locations)
        {
            Debug.Assert(list.MoveNext());

            switch (location)
            {
                case DirectiveLocation.Query:
                    list.Current.SetStringValue(__DirectiveLocation.Query);
                    break;
                case DirectiveLocation.Mutation:
                    list.Current.SetStringValue(__DirectiveLocation.Mutation);
                    break;
                case DirectiveLocation.Subscription:
                    list.Current.SetStringValue(__DirectiveLocation.Subscription);
                    break;
                case DirectiveLocation.Field:
                    list.Current.SetStringValue(__DirectiveLocation.Field);
                    break;
                case DirectiveLocation.FragmentDefinition:
                    list.Current.SetStringValue(__DirectiveLocation.FragmentDefinition);
                    break;
                case DirectiveLocation.FragmentSpread:
                    list.Current.SetStringValue(__DirectiveLocation.FragmentSpread);
                    break;
                case DirectiveLocation.InlineFragment:
                    list.Current.SetStringValue(__DirectiveLocation.InlineFragment);
                    break;
                case DirectiveLocation.VariableDefinition:
                    list.Current.SetStringValue(__DirectiveLocation.VariableDefinition);
                    break;
                case DirectiveLocation.Schema:
                    list.Current.SetStringValue(__DirectiveLocation.Schema);
                    break;
                case DirectiveLocation.Scalar:
                    list.Current.SetStringValue(__DirectiveLocation.Scalar);
                    break;
                case DirectiveLocation.Object:
                    list.Current.SetStringValue(__DirectiveLocation.Object);
                    break;
                case DirectiveLocation.FieldDefinition:
                    list.Current.SetStringValue(__DirectiveLocation.FieldDefinition);
                    break;
                case DirectiveLocation.ArgumentDefinition:
                    list.Current.SetStringValue(__DirectiveLocation.ArgumentDefinition);
                    break;
                case DirectiveLocation.Interface:
                    list.Current.SetStringValue(__DirectiveLocation.Interface);
                    break;
                case DirectiveLocation.Union:
                    list.Current.SetStringValue(__DirectiveLocation.Union);
                    break;
                case DirectiveLocation.Enum:
                    list.Current.SetStringValue(__DirectiveLocation.Enum);
                    break;
                case DirectiveLocation.EnumValue:
                    list.Current.SetStringValue(__DirectiveLocation.EnumValue);
                    break;
                case DirectiveLocation.InputObject:
                    list.Current.SetStringValue(__DirectiveLocation.InputObject);
                    break;
                case DirectiveLocation.InputFieldDefinition:
                    list.Current.SetStringValue(__DirectiveLocation.InputFieldDefinition);
                    break;
                default:
                    throw new NotSupportedException($"Directive location {directiveDef.Locations} is not supported.");
            }
        }
    }

    public static void Arguments(FieldContext context)
    {
        var directiveDef = context.Parent<IDirectiveDefinition>();
        var includeDeprecated = context.ArgumentValue<BooleanValueNode>("includeDeprecated").Value;
        var count = includeDeprecated
            ? directiveDef.Arguments.Count
            : directiveDef.Arguments.Count(t => !t.IsDeprecated);
        var list = context.FieldResult.CreateListValue(count);

        var index = 0;
        foreach (var element in list.EnumerateArray())
        {
            var argument = directiveDef.Arguments[index++];

            if (!includeDeprecated && argument.IsDeprecated)
            {
                continue;
            }

            context.AddRuntimeResult(argument);
            element.CreateObjectValue(context.Selection, context.IncludeFlags);
        }
    }
}
