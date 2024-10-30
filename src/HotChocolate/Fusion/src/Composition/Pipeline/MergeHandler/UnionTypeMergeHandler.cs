using HotChocolate.Skimmed;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// Defines a type handler that is responsible for merging a group of union types into a
/// single distributed union type on the fusion graph.
/// </summary>
internal sealed class UnionTypeMergeHandler : ITypeMergeHandler
{
    /// <inheritdoc />
    public ValueTask<MergeStatus> MergeAsync(
        CompositionContext context,
        TypeGroup typeGroup,
        CancellationToken cancellationToken)
    {
        // If any type in the group is not a union type, skip merging
        if (typeGroup.Parts.Any(t => t.Type.Kind is not TypeKind.Union))
        {
            return new(MergeStatus.Skipped);
        }

        var target = (UnionTypeDefinition)context.FusionGraph.Types[typeGroup.Name];

        foreach (var part in typeGroup.Parts)
        {
            var source = (UnionTypeDefinition)part.Type;
            MergeType(context, source, part.Schema, target, context.FusionGraph);
        }

        return new(MergeStatus.Completed);
    }

    private static void MergeType(
        CompositionContext context,
        UnionTypeDefinition source,
        SchemaDefinition sourceSchema,
        UnionTypeDefinition target,
        SchemaDefinition targetSchema)
    {
        context.TryApplySource(source, sourceSchema, target);

        target.MergeDirectivesWith(source, context);

        target.MergeDescriptionWith(source);

        foreach (var sourceType in source.Types)
        {
            // Retrieve the target member type from the schema.
            var targetMemberType = (ObjectTypeDefinition)targetSchema.Types[sourceType.Name];

            // If the target union type does not contain the target member type, add it.
            if (!target.Types.Contains(targetMemberType))
            {
                target.Types.Add(targetMemberType);
            }
        }
    }
}
