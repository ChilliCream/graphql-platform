using System.Diagnostics;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;
using IHasDirectives = HotChocolate.Skimmed.IHasDirectives;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class ApplyTagDirectiveMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        if (context.Features.MakeTagsPublic())
        {
            
            
        }

        await next(context);
    }

    private static void Rewrite(
        CompositionContext context)
    {
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
            context.FusionGraph.DirectiveTypes.Add(tagDirectiveType); 
        }
        
        var tags = new HashSet<string>();

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
                    break;

                case ScalarType scalarType:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }    
        }
        
    }

    private static void Rewrite(
        CompositionContext context,
        ComplexType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
    {
        var coordinate = new SchemaCoordinate(type.Name);

        ApplyDirectives(
            context.GetSubgraphMembers<ComplexType>(coordinate),
            type,
            tagDirectiveType,
            tags);

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
    {
        var coordinate = new SchemaCoordinate(type.Name);

        ApplyDirectives(
            context.GetSubgraphMembers<UnionType>(coordinate),
            type,
            tagDirectiveType,
            tags);
    }
    
    private static void Rewrite(
        CompositionContext context,
        InputObjectType type,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
    {
        var coordinate = new SchemaCoordinate(type.Name);

        ApplyDirectives(
            context.GetSubgraphMembers<ComplexType>(coordinate),
            type,
            tagDirectiveType,
            tags);

        foreach (var field in type.Fields)
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

        ApplyDirectives(
            context.GetSubgraphMembers<OutputField>(coordinate),
            field,
            tagDirectiveType,
            tags);

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

        ApplyDirectives(
            context.GetSubgraphMembers<InputField>(coordinate),
            field,
            tagDirectiveType,
            tags);
    }

    private static void ApplyDirectives(
        IEnumerable<IHasDirectives> parts,
        IHasDirectives merged,
        DirectiveType tagDirectiveType,
        HashSet<string> tags)
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
                }
            }
        }
    }
}