using System.Collections.Immutable;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Directives;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
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

    public override MutableDirectiveDefinition GetCanonicalDirectiveDefinition(MutableSchemaDefinition schema)
    {
        return CacheControlMutableDirectiveDefinition.Create(schema);
    }

    public override void MergeDirectiveDefinition(
        MutableDirectiveDefinition directiveDefinition,
        MutableSchemaDefinition mergedSchema)
    {
        var scopeArgType = (MutableEnumTypeDefinition)directiveDefinition.Arguments["scope"].Type;

        if (MergeBehavior is DirectiveMergeBehavior.IncludePrivate)
        {
            scopeArgType.Name = $"fusion__{scopeArgType.Name}";
            mergedSchema.Types.Add(scopeArgType);
        }
        else if (mergedSchema.Types.TryGetType<MutableEnumTypeDefinition>(
            WellKnownTypeNames.CacheControlScope, out var existingScopeType))
        {
            directiveDefinition.Arguments["scope"].Type = existingScopeType;
        }

        base.MergeDirectiveDefinition(directiveDefinition, mergedSchema);
    }

    public override void MergeDirectives(
        IDirectivesProvider mergedMember,
        ImmutableArray<DirectivesProviderInfo> memberDefinitions,
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
                    d => d.Member.Directives.Where(dir => dir.Name == DirectiveNames.CacheControl))
                .Select(CacheControlDirective.From)
                .ToArray();

        if (cacheControlDirectives.Length == 0)
        {
            return;
        }

        var allDefined = cacheControlDirectives.Length == memberDefinitions.Length;
        int? maxAge = null;
        int? sharedMaxAge = null;

        if (mergedMember is not MutableOutputFieldDefinition)
        {
            var lifetimes = cacheControlDirectives
                .Where(d => d.MaxAge is not null || d.SharedMaxAge is not null)
                .ToArray();

            if (lifetimes.All(d => d.SharedMaxAge is not null))
            {
                maxAge = lifetimes.Min(d => d.MaxAge);
                sharedMaxAge = lifetimes.Min(d => d.SharedMaxAge);
            }
            else
            {
                maxAge = lifetimes.Min(
                    d => Math.Min(d.MaxAge ?? int.MaxValue, d.SharedMaxAge ?? int.MaxValue));
            }
        }
        else if (allDefined)
        {
            // Null is the lowest value.
            static int? min(int? acc, int? val)
                => acc is null || val is null ? null : Math.Min(acc.Value, val.Value);
            maxAge = cacheControlDirectives.Select(d => d.MaxAge).Aggregate(min);
            sharedMaxAge = cacheControlDirectives.Select(d => d.SharedMaxAge).Aggregate(min);
        }

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

        if (allDefined && !cacheControlDirectives.All(d => d.InheritMaxAge is null))
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

        if (!allDefined && argumentAssignments.Count == 0)
        {
            return;
        }

        var cacheControlDirective = new Directive(directiveDefinition, argumentAssignments);

        mergedMember.AddDirective(cacheControlDirective);
    }
}
