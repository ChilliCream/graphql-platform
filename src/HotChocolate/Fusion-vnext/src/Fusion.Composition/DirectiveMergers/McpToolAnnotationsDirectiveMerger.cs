using System.Collections.Immutable;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Directives;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Options;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.DirectiveMergers;

internal class McpToolAnnotationsDirectiveMerger(DirectiveMergeBehavior mergeBehavior)
    : DirectiveMergerBase(mergeBehavior)
{
    public override string DirectiveName => DirectiveNames.McpToolAnnotations;

    public override MutableDirectiveDefinition GetCanonicalDirectiveDefinition(ISchemaDefinition schema)
    {
        return McpToolAnnotationsMutableDirectiveDefinition.Create(schema);
    }

    public override void MergeDirectives(
        IDirectivesProvider mergedMember,
        ImmutableArray<IDirectivesProvider> memberDefinitions,
        MutableSchemaDefinition mergedSchema)
    {
        if (MergeBehavior is DirectiveMergeBehavior.Ignore)
        {
            return;
        }

        if (!mergedSchema.DirectiveDefinitions.TryGetDirective(DirectiveName, out var directiveDefinition))
        {
            // Merged definition not found.
            return;
        }

        var mcpToolAnnotationsDirectives =
            memberDefinitions
                .SelectMany(d => d.Directives.Where(dir => dir.Name == DirectiveNames.McpToolAnnotations))
                .Select(McpToolAnnotationsDirective.From)
                .ToArray();

        if (mcpToolAnnotationsDirectives.Length == 0)
        {
            return;
        }

        var isMutationField = ((IFieldDefinition)memberDefinitions[0]).DeclaringMember is IObjectTypeDefinition
        {
            Name: WellKnownTypeNames.Mutation
        };

        var argumentAssignments = new List<ArgumentAssignment>();

        if (!mcpToolAnnotationsDirectives.All(d => d.DestructiveHint is null))
        {
            var destructiveHint =
                mcpToolAnnotationsDirectives.Any(
                    d => isMutationField
                        ? d.DestructiveHint is null or true
                        : d.DestructiveHint is true);

            argumentAssignments.Add(
                new ArgumentAssignment(ArgumentNames.DestructiveHint, destructiveHint));
        }

        if (!mcpToolAnnotationsDirectives.All(d => d.IdempotentHint is null))
        {
            var idempotentHint =
                mcpToolAnnotationsDirectives.All(
                    d => isMutationField
                        ? d.IdempotentHint is true
                        : d.IdempotentHint is null or true);

            argumentAssignments.Add(
                new ArgumentAssignment(ArgumentNames.IdempotentHint, idempotentHint));
        }

        if (!mcpToolAnnotationsDirectives.All(d => d.OpenWorldHint is null))
        {
            var openWorldHint =
                mcpToolAnnotationsDirectives.Any(d => d.OpenWorldHint is null or true);

            argumentAssignments.Add(
                new ArgumentAssignment(ArgumentNames.OpenWorldHint, openWorldHint));
        }

        var mcpToolAnnotationsDirective = new Directive(directiveDefinition, argumentAssignments);

        mergedMember.AddDirective(mcpToolAnnotationsDirective);
    }
}
