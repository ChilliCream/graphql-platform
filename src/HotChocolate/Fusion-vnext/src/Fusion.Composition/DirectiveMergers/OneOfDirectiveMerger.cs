using System.Collections.Immutable;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Options;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.DirectiveMergers;

internal class OneOfDirectiveMerger(DirectiveMergeBehavior mergeBehavior)
    : DirectiveMergerBase(mergeBehavior)
{
    public override string DirectiveName => DirectiveNames.OneOf;

    public override MutableDirectiveDefinition GetCanonicalDirectiveDefinition(ISchemaDefinition schema)
    {
        return OneOfMutableDirectiveDefinition.Create(schema);
    }

    public override void MergeDirectives(
        IDirectivesProvider mergedMember,
        ImmutableArray<IDirectivesProvider> memberDefinitions,
        MutableSchemaDefinition mergedSchema)
    {
        var oneOfDirective = memberDefinitions[0].Directives.FirstOrDefault(DirectiveNames.OneOf);

        if (oneOfDirective is null)
        {
            // No @oneOf directive to merge.
            return;
        }

        if (!mergedSchema.DirectiveDefinitions.TryGetDirective(DirectiveNames.OneOf, out var directiveDefinition))
        {
            // Merged definition not found.
            return;
        }

        if (mergedMember is not MutableInputObjectTypeDefinition inputObjectType)
        {
            // @oneOf can only be applied to input object types.
            return;
        }

        inputObjectType.IsOneOf = true;
        inputObjectType.AddDirective(new Directive(directiveDefinition));
    }
}
