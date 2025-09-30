using System.Net.Mime;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;
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

        // TODO: shall we pool these as well?
        var list = new RawListFieldResult();
        context.FieldResult.SetNextValue(list);

        foreach (var location in directiveDef.Locations.AsEnumerable())
        {
            var locationSpan = MapLocationToSpan(location);

            var start = context.Memory.Length;
            var length = locationSpan.Length + 1;
            var span = context.Memory.GetSpan(length);
            span[0] = RawFieldValueType.String;

            locationSpan.CopyTo(span[1..]);

            context.Memory.Advance(length);
            var segment = context.Memory.GetWrittenMemorySegment(start, length);
            list.SetNextValue(segment);
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

    private static ReadOnlySpan<byte> MapLocationToSpan(DirectiveLocation location)
    {
        return location switch
        {
            DirectiveLocation.Query => __DirectiveLocation.Query,
            DirectiveLocation.Mutation => __DirectiveLocation.Mutation,
            DirectiveLocation.Subscription => __DirectiveLocation.Subscription,
            DirectiveLocation.Field => __DirectiveLocation.Field,
            DirectiveLocation.FragmentDefinition => __DirectiveLocation.FragmentDefinition,
            DirectiveLocation.FragmentSpread => __DirectiveLocation.FragmentSpread,
            DirectiveLocation.InlineFragment => __DirectiveLocation.InlineFragment,
            DirectiveLocation.VariableDefinition => __DirectiveLocation.VariableDefinition,
            DirectiveLocation.Schema => __DirectiveLocation.Schema,
            DirectiveLocation.Scalar => __DirectiveLocation.Scalar,
            DirectiveLocation.Object => __DirectiveLocation.Object,
            DirectiveLocation.FieldDefinition => __DirectiveLocation.FieldDefinition,
            DirectiveLocation.ArgumentDefinition => __DirectiveLocation.ArgumentDefinition,
            DirectiveLocation.Interface => __DirectiveLocation.Interface,
            DirectiveLocation.Union => __DirectiveLocation.Union,
            DirectiveLocation.Enum => __DirectiveLocation.Enum,
            DirectiveLocation.EnumValue => __DirectiveLocation.EnumValue,
            DirectiveLocation.InputObject => __DirectiveLocation.InputObject,
            DirectiveLocation.InputFieldDefinition => __DirectiveLocation.InputFieldDefinition,
            _ => throw new NotSupportedException($"Directive location {location} is not supported.")
        };
    }
}
