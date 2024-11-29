using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using IDirectivesProvider = HotChocolate.Skimmed.IDirectivesProvider;

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

        if (!context.FusionGraph.DirectiveDefinitions.TryGetDirective(
                WellKnownDirectives.Tag,
                out var tagDirectiveType))
        {
            tagDirectiveType = new DirectiveDefinition(WellKnownDirectives.Tag)
            {
                Locations = Types.DirectiveLocation.Object |
                    Types.DirectiveLocation.Interface |
                    Types.DirectiveLocation.Union |
                    Types.DirectiveLocation.InputObject |
                    Types.DirectiveLocation.Enum |
                    Types.DirectiveLocation.Scalar |
                    Types.DirectiveLocation.FieldDefinition |
                    Types.DirectiveLocation.InputFieldDefinition |
                    Types.DirectiveLocation.ArgumentDefinition |
                    Types.DirectiveLocation.EnumValue |
                    Types.DirectiveLocation.Schema,
                IsRepeatable = true,
                Arguments =
                {
                    new InputFieldDefinition(
                        WellKnownDirectives.Name,
                        new NonNullTypeDefinition(context.FusionGraph.Types["String"])),
                },
            };

            needsDirectiveType = true;
        }

        var tags = new HashSet<string>();
        Rewrite(context, tagDirectiveType, tags, makePublic);

        if (context.GetTagContext().HasTags && needsDirectiveType)
        {
            context.FusionGraph.DirectiveDefinitions.Add(tagDirectiveType);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        DirectiveDefinition tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
    {
        var tagContext = context.GetTagContext();

        ApplyDirectives(tagContext, context.FusionGraph, context.Subgraphs, tagDirectiveType, tags, makePublic);

        foreach (var type in context.FusionGraph.Types)
        {
            switch (type)
            {
                case ObjectTypeDefinition objectType:
                    Rewrite(context, tagContext, objectType, tagDirectiveType, tags, makePublic);
                    break;

                case InterfaceTypeDefinition interfaceType:
                    Rewrite(context, tagContext, interfaceType, tagDirectiveType, tags, makePublic);
                    break;

                case UnionTypeDefinition unionType:
                    Rewrite(context, tagContext, unionType, tagDirectiveType, tags, makePublic);
                    break;

                case InputObjectTypeDefinition inputObjectType:
                    Rewrite(context, tagContext, inputObjectType, tagDirectiveType, tags, makePublic);
                    break;

                case EnumTypeDefinition enumType:
                    Rewrite(context, tagContext, enumType, tagDirectiveType, tags, makePublic);
                    break;

                case ScalarTypeDefinition scalarType:
                    Rewrite(context, tagContext, scalarType, tagDirectiveType, tags, makePublic);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        foreach (var directiveType in context.FusionGraph.DirectiveDefinitions)
        {
            Rewrite(context, tagContext, directiveType, tagDirectiveType, tags, makePublic);
        }
    }

    private static void Rewrite(
        CompositionContext context,
        TagContext tagContext,
        ComplexTypeDefinition type,
        DirectiveDefinition tagDirectiveType,
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
        UnionTypeDefinition type,
        DirectiveDefinition tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
        => ApplyDirectives(context, tagContext, type, new(type.Name), tagDirectiveType, tags, makePublic);

    private static void Rewrite(
        CompositionContext context,
        TagContext tagContext,
        InputObjectTypeDefinition type,
        DirectiveDefinition tagDirectiveType,
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
        EnumTypeDefinition type,
        DirectiveDefinition tagDirectiveType,
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
        ScalarTypeDefinition type,
        DirectiveDefinition tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
        => ApplyDirectives(context, tagContext, type, new(type.Name), tagDirectiveType, tags, makePublic);

    private static void Rewrite(
        CompositionContext context,
        TagContext tagContext,
        DirectiveDefinition type,
        DirectiveDefinition tagDirectiveType,
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
        OutputFieldDefinition field,
        SchemaCoordinate parent,
        DirectiveDefinition tagDirectiveType,
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
        InputFieldDefinition field,
        SchemaCoordinate parent,
        DirectiveDefinition tagDirectiveType,
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
        DirectiveDefinition tagDirectiveType,
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
        DirectiveDefinition tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
        where T : ITypeSystemMemberDefinition, IDirectivesProvider
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
        DirectiveDefinition tagDirectiveType,
        HashSet<string> tags,
        bool makePublic)
        where T : ITypeSystemMemberDefinition, IDirectivesProvider
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
                            new ArgumentAssignment(WellKnownDirectives.Name, name)));
                    tagContext.HasTags = true;
                }
            }
        }
    }
}
