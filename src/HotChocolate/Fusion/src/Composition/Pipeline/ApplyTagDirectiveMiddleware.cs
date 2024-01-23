using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;
using IHasDirectives = HotChocolate.Skimmed.IHasDirectives;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class ApplyTagDirectiveMiddleware : IMergeMiddleware
{
    public ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        Rewrite(context, context.Features.MakeTagsPublic());
        return !context.Log.HasErrors
            ? next(context)
            : ValueTask.CompletedTask;
    }

    private static void Rewrite(
        CompositionContext context,
        bool makePublic)
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
        Rewrite(context, tagDirectiveType, tags, makePublic);

        if (context.GetTagContext().HasTags && needsDirectiveType)
        {
            context.FusionGraph.DirectiveTypes.Add(tagDirectiveType);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        DirectiveType tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
    {
        var tagContext = context.GetTagContext();

        ApplyDirectives(tagContext, context.FusionGraph, context.Subgraphs, tagDirectiveType, tags, makePublic);

        foreach (var type in context.FusionGraph.Types)
        {
            switch (type)
            {
                case ObjectType objectType:
                    Rewrite(context, tagContext, objectType, tagDirectiveType, tags, makePublic);
                    break;

                case InterfaceType interfaceType:
                    Rewrite(context, tagContext, interfaceType, tagDirectiveType, tags, makePublic);
                    break;

                case UnionType unionType:
                    Rewrite(context, tagContext, unionType, tagDirectiveType, tags, makePublic);
                    break;

                case InputObjectType inputObjectType:
                    Rewrite(context, tagContext, inputObjectType, tagDirectiveType, tags, makePublic);
                    break;

                case EnumType enumType:
                    Rewrite(context, tagContext, enumType, tagDirectiveType, tags, makePublic);
                    break;

                case ScalarType scalarType:
                    Rewrite(context, tagContext, scalarType, tagDirectiveType, tags, makePublic);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        foreach (var directiveType in context.FusionGraph.DirectiveTypes)
        {
            Rewrite(context, tagContext, directiveType, tagDirectiveType, tags, makePublic);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        TagContext tagContext,
        ComplexType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
    {
        var coordinate = new SchemaCoordinate(type.Name);

        ApplyDirectives(context, tagContext, type, coordinate, tagDirectiveType, tags, makePublic);

        foreach (var field in type.Fields)
        {
            Rewrite(context, tagContext, field, coordinate, tagDirectiveType, tags, makePublic);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        TagContext tagContext,
        UnionType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
        => ApplyDirectives(context, tagContext, type, new(type.Name), tagDirectiveType, tags, makePublic);

    private static void Rewrite(
        CompositionContext context,
        TagContext tagContext,
        InputObjectType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
    {
        var coordinate = new SchemaCoordinate(type.Name);

        ApplyDirectives(context, tagContext, type, coordinate, tagDirectiveType, tags, makePublic);

        foreach (var field in type.Fields)
        {
            Rewrite(context, tagContext, field, coordinate, tagDirectiveType, tags, makePublic);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        TagContext tagContext,
        EnumType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
    {
        var coordinate = new SchemaCoordinate(type.Name);

        ApplyDirectives(context, tagContext, type, coordinate, tagDirectiveType, tags, makePublic);

        foreach (var field in type.Values)
        {
            Rewrite(context, tagContext, field, coordinate, tagDirectiveType, tags, makePublic);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        TagContext tagContext,
        ScalarType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
        => ApplyDirectives(context, tagContext, type, new(type.Name), tagDirectiveType, tags, makePublic);

    private static void Rewrite(
        CompositionContext context,
        TagContext tagContext,
        DirectiveType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
    {
        var coordinate = new SchemaCoordinate(type.Name, ofDirective: true);

        foreach (var field in type.Arguments)
        {
            Rewrite(context, tagContext, field, coordinate, tagDirectiveType, tags, makePublic);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        TagContext tagContext,
        OutputField field,
        SchemaCoordinate parent,
        DirectiveType tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
    {
        var coordinate = new SchemaCoordinate(parent.Name, field.Name);

        ApplyDirectives(context, tagContext, field, coordinate, tagDirectiveType, tags, makePublic);

        foreach (var argument in field.Arguments)
        {
            Rewrite(context, tagContext, argument, coordinate, tagDirectiveType, tags, makePublic);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        TagContext tagContext,
        InputField field,
        SchemaCoordinate parent,
        DirectiveType tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
    {
        var coordinate = parent switch
        {
            { OfDirective: true, } => new SchemaCoordinate(parent.Name, argumentName: field.Name, ofDirective: true),
            { MemberName: null, } => new SchemaCoordinate(parent.Name, field.Name),
            { MemberName: not null, } => new SchemaCoordinate(parent.Name, parent.MemberName, field.Name),
        };

        ApplyDirectives(context, tagContext, field, coordinate, tagDirectiveType, tags, makePublic);
    }

    private static void Rewrite(
        CompositionContext context,
        TagContext tagContext,
        EnumValue value,
        SchemaCoordinate parent,
        DirectiveType tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
        => ApplyDirectives(
            context,
            tagContext,
            value,
            new SchemaCoordinate(parent.Name, value.Name),
            tagDirectiveType,
            tags,
            makePublic);

    private static void ApplyDirectives<T>(
        CompositionContext context,
        TagContext tagContext,
        T merged,
        SchemaCoordinate coordinate,
        DirectiveType tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
        where T : ITypeSystemMember, IHasDirectives
    {
        var parts = context.GetSubgraphMembers<T>(coordinate);
        ApplyDirectives(tagContext, merged, parts, tagDirectiveType, tags, makePublic);

        foreach (var tag in tags)
        {
            tagContext.RegisterTagCoordinate(tag, coordinate);
        }
    }

    private static void ApplyDirectives<T>(
        TagContext tagContext,
        T merged,
        IEnumerable<T> parts,
        DirectiveType tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
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
                    if (!makePublic)
                    {
                        continue;
                    }

                    merged.Directives.Add(
                        new Directive(
                            tagDirectiveType,
                            new Argument(WellKnownDirectives.Name, name)));
                    tagContext.HasTags = true;
                }
            }
        }
    }
}
