using System.Collections.Immutable;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Definitions;
using HotChocolate.Types.Mutable.Directives;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.DirectiveMergers;

internal class CacheControlDirectiveMerger(DirectiveMergeBehavior mergeBehavior)
    : DirectiveMergerBase(mergeBehavior)
{
    public override string DirectiveName { get; } =
        mergeBehavior is DirectiveMergeBehavior.IncludePrivate
            ? $"fusion__{DirectiveNames.CacheControl}"
            : DirectiveNames.CacheControl;

    public override MutableDirectiveDefinition GetCanonicalDirectiveDefinition(ISchemaDefinition schema)
    {
        return CacheControlMutableDirectiveDefinition.Create(schema);
    }

    public override void MergeDirectiveDefinition(
        MutableDirectiveDefinition directiveDefinition,
        MutableSchemaDefinition mergedSchema)
    {
        if (MergeBehavior is DirectiveMergeBehavior.IncludePrivate)
        {
            var scopeArgType = (MutableEnumTypeDefinition)directiveDefinition.Arguments["scope"].Type;
            scopeArgType.Name = $"fusion__{scopeArgType.Name}";
            mergedSchema.Types.Add(scopeArgType);
        }

        base.MergeDirectiveDefinition(directiveDefinition, mergedSchema);
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

        var cacheControlDirectives =
            memberDefinitions
                .SelectMany(
                    d => d.Directives.Where(dir => dir.Name == DirectiveNames.CacheControl))
                .Select(CacheControlDirective.From)
                .ToArray();

        if (cacheControlDirectives.Length != memberDefinitions.Length)
        {
            // Only merge if all member definitions have the @cacheControl directive.
            return;
        }

        // Null is the lowest value.
        var min = (int? acc, int? val) => (int?)(acc is null || val is null ? null : Math.Min(acc.Value, val.Value));
        var maxAge = cacheControlDirectives.Select(d => d.MaxAge).Aggregate(min);
        var sharedMaxAge = cacheControlDirectives.Select(d => d.SharedMaxAge).Aggregate(min);
        var inheritMaxAge = cacheControlDirectives.All(d => d.InheritMaxAge == true);
        var scope =
            cacheControlDirectives.Any(d => d.Scope is CacheControlScope.Private)
                ? CacheControlScope.Private
                : CacheControlScope.Public;
        var vary = cacheControlDirectives.Where(d => d.Vary.HasValue).SelectMany(d => d.Vary!.Value).ToHashSet();

        var argumentAssignments = new List<ArgumentAssignment>();

        if (maxAge is not null)
        {
            argumentAssignments.Add(new ArgumentAssignment(ArgumentNames.MaxAge, maxAge.Value));
        }

        if (sharedMaxAge is not null)
        {
            argumentAssignments.Add(new ArgumentAssignment(ArgumentNames.SharedMaxAge, sharedMaxAge.Value));
        }

        if (!cacheControlDirectives.All(d => d.InheritMaxAge is null))
        {
            argumentAssignments.Add(new ArgumentAssignment(ArgumentNames.InheritMaxAge, inheritMaxAge));
        }

        if (!cacheControlDirectives.All(d => d.Scope is null))
        {
            argumentAssignments.Add(
                new ArgumentAssignment(
                    ArgumentNames.Scope,
                    new EnumValueNode(Enum.GetName(scope)!.ToUpperInvariant())));
        }

        if (vary.Count != 0)
        {
            argumentAssignments.Add(
                new ArgumentAssignment(
                    ArgumentNames.Vary,
                    new ListValueNode(vary.Select(v => new StringValueNode(v)).ToList())));
        }

        var cacheControlDirective = new Directive(directiveDefinition, argumentAssignments);

        mergedMember.AddDirective(cacheControlDirective);
    }
}
