using HotChocolate.Skimmed;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class PrepareFusionSchemaMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        foreach (var entity in context.Entities)
        {
            context.FusionGraph.Types.Add(new ObjectTypeDefinition(entity.Name));
        }

        foreach (var schema in context.Subgraphs)
        {
            foreach (var type in schema.Types)
            {
                if (type.Kind is not TypeKind.Object &&
                    !context.FusionGraph.Types.ContainsName(type.Name))
                {
                    switch (type.Kind)
                    {
                        case TypeKind.Interface:
                            context.FusionGraph.Types.Add(new InterfaceTypeDefinition(type.Name));
                            break;

                        case TypeKind.Union:
                            context.FusionGraph.Types.Add(new UnionTypeDefinition(type.Name));
                            break;

                        case TypeKind.InputObject:
                            context.FusionGraph.Types.Add(new InputObjectTypeDefinition(type.Name));
                            break;

                        case TypeKind.Enum:
                            context.FusionGraph.Types.Add(new EnumTypeDefinition(type.Name));
                            break;

                        case TypeKind.Scalar:
                            context.FusionGraph.Types.Add(new ScalarTypeDefinition(type.Name));
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        await next(context);
    }
}
