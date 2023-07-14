using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;
using IHasDirectives = HotChocolate.Skimmed.IHasDirectives;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class ApplyTagDirectiveMiddleware : IMergeMiddleware
{
    private const string _hasTags = "ApplyTagDirective.HasTags";

    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        if (context.Features.MakeTagsPublic())
        {
            Rewrite(context);
        }

        await next(context);
    }

    private static void Rewrite(
        CompositionContext context)
    {
        var needsDirectiveType = false;
        
        if (!context.FusionGraph.DirectiveTypes.TryGetDirective(WellKnownDirectives.Tag, out var tagDirectiveType))
        {
            tagDirectiveType = new DirectiveType(WellKnownDirectives.Tag)
            {
                Locations = DirectiveLocation.Object |
                    DirectiveLocation.Interface |
                    DirectiveLocation.Union |
                    DirectiveLocation.InputObject |
                    DirectiveLocation.Enum |
                    DirectiveLocation.Scalar |
                    DirectiveLocation.FieldDefinition |
                    DirectiveLocation.InputFieldDefinition |
                    DirectiveLocation.ArgumentDefinition |
                    DirectiveLocation.EnumValue |
                    DirectiveLocation.Schema,
                IsRepeatable = true,
                Arguments =
                {
                    new InputField(
                        WellKnownDirectives.Name,
                        new NonNullType(context.FusionGraph.Types["String"]))
                }
            };

            needsDirectiveType = true;
        }

        var tags = new HashSet<string>();
        Rewrite(context, tagDirectiveType, tags);
        
        if(context.ContextData.ContainsKey(_hasTags) && needsDirectiveType)
        {
            context.FusionGraph.DirectiveTypes.Add(tagDirectiveType);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
    {
        ApplyDirectives(context, context.FusionGraph, context.Subgraphs, tagDirectiveType, tags);
        
        foreach (var type in context.FusionGraph.Types)
        {
            switch (type)
            {
                case ObjectType objectType:
                    Rewrite(context, objectType, tagDirectiveType, tags);
                    break;

                case InterfaceType interfaceType:
                    Rewrite(context, interfaceType, tagDirectiveType, tags);
                    break;

                case UnionType unionType:
                    Rewrite(context, unionType, tagDirectiveType, tags);
                    break;

                case InputObjectType inputObjectType:
                    Rewrite(context, inputObjectType, tagDirectiveType, tags);
                    break;

                case EnumType enumType:
                    Rewrite(context, enumType, tagDirectiveType, tags);
                    break;

                case ScalarType scalarType:
                    Rewrite(context, scalarType, tagDirectiveType, tags);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        foreach (var directiveType in context.FusionGraph.DirectiveTypes)
        {
            Rewrite(context, directiveType, tagDirectiveType, tags);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        ComplexType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
    {
        var coordinate = new SchemaCoordinate(type.Name);

        ApplyDirectives(context, type, coordinate, tagDirectiveType, tags);

        foreach (var field in type.Fields)
        {
            Rewrite(context, field, coordinate, tagDirectiveType, tags);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        UnionType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
        => ApplyDirectives(context, type, new SchemaCoordinate(type.Name), tagDirectiveType, tags);

    private static void Rewrite(
        CompositionContext context,
        InputObjectType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
    {
        var coordinate = new SchemaCoordinate(type.Name);

        ApplyDirectives(context, type, coordinate, tagDirectiveType, tags);

        foreach (var field in type.Fields)
        {
            Rewrite(context, field, coordinate, tagDirectiveType, tags);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        EnumType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
    {
        var coordinate = new SchemaCoordinate(type.Name);

        ApplyDirectives(context, type, coordinate, tagDirectiveType, tags);

        foreach (var field in type.Values)
        {
            Rewrite(context, field, coordinate, tagDirectiveType, tags);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        ScalarType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
        => ApplyDirectives(context, type, new SchemaCoordinate(type.Name), tagDirectiveType, tags);

    private static void Rewrite(
        CompositionContext context,
        DirectiveType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
    {
        var coordinate = new SchemaCoordinate(type.Name, ofDirective: true);

        foreach (var field in type.Arguments)
        {
            Rewrite(context, field, coordinate, tagDirectiveType, tags);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        OutputField field,
        SchemaCoordinate parent,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
    {
        var coordinate = new SchemaCoordinate(parent.Name, field.Name);

        ApplyDirectives(context, field, coordinate, tagDirectiveType, tags);

        foreach (var argument in field.Arguments)
        {
            Rewrite(context, argument, coordinate, tagDirectiveType, tags);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        InputField field,
        SchemaCoordinate parent,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
    {
        var coordinate = parent switch
        {
            { OfDirective: true } => new SchemaCoordinate(parent.Name, argumentName: field.Name, ofDirective: true),
            { MemberName: null } => new SchemaCoordinate(parent.Name, field.Name),
            { MemberName: not null } => new SchemaCoordinate(parent.Name, parent.MemberName, field.Name),
        };

        ApplyDirectives(context, field, coordinate, tagDirectiveType, tags);
    }

    private static void Rewrite(
        CompositionContext context,
        EnumValue value,
        SchemaCoordinate parent,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
        => ApplyDirectives(context, value, new SchemaCoordinate(parent.Name, value.Name), tagDirectiveType, tags);

    private static void ApplyDirectives<T>(
        CompositionContext context,
        T merged,
        SchemaCoordinate coordinate,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
        where T : ITypeSystemMember, IHasDirectives
    {
        var parts = context.GetSubgraphMembers<T>(coordinate);
        ApplyDirectives(context, merged, parts, tagDirectiveType, tags);
    }

    private static void ApplyDirectives<T>(
        CompositionContext context,
        T merged,
        IEnumerable<T> parts,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
        where T : ITypeSystemMember, IHasDirectives
    {
        tags.Clear();

        foreach (var tag in merged.Directives[WellKnownDirectives.Tag])
        {
            if (tag.Arguments.TryGetValue(WellKnownDirectives.Name, out var value) &&
                value is StringValueNode name)
            {
                tags.Add(name.Value);
            }
        }

        foreach (var part in parts)
        {
            foreach (var tag in part.Directives[WellKnownDirectives.Tag])
            {
                if (tag.Arguments.TryGetValue(WellKnownDirectives.Name, out var value) &&
                    value is StringValueNode name &&
                    tags.Add(name.Value))
                {
                    merged.Directives.Add(
                        new Directive(
                            tagDirectiveType,
                            new Argument(WellKnownDirectives.Name, name)));
                    context.ContextData.TryAdd(_hasTags, 1);
                }
            }
        }
    }
}