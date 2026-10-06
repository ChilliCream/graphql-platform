using System.Collections.Immutable;
using HotChocolate.Fusion.Directives;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Info;
using HotChocolate.Types;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// Warns when source schemas disagree on <c>@authenticated</c> for a shared type or field, and
/// when the merged scopes or policies requirement of a member exceeds the group count threshold.
/// </summary>
internal sealed class AuthorizationDirectiveRule
    : IEventHandler<TypeGroupEvent>
    , IEventHandler<OutputFieldGroupEvent>
{
    /// <summary>
    /// The number of reduced groups above which composition warns about a scopes or policies
    /// requirement.
    /// </summary>
    private const int GroupCountThreshold = 64;

    public void Handle(TypeGroupEvent @event, CompositionContext context)
    {
        var (typeName, typeGroup) = @event;

        if (typeGroup[0].Type.Kind
            is not (TypeKind.Object or TypeKind.Interface or TypeKind.Enum or TypeKind.Scalar))
        {
            return;
        }

        Validate(
            new SchemaCoordinate(typeName),
            [.. typeGroup.Select(static g => new DirectivesProviderInfo(g.Type, g.Schema))],
            context);
    }

    public void Handle(OutputFieldGroupEvent @event, CompositionContext context)
    {
        var (fieldName, fieldGroup, typeName) = @event;

        Validate(
            new SchemaCoordinate(typeName, fieldName, ofDirective: false),
            [.. fieldGroup.Select(static g => new DirectivesProviderInfo(g.Field, g.Schema))],
            context);
    }

    private static void Validate(
        SchemaCoordinate coordinate,
        ImmutableArray<DirectivesProviderInfo> memberDefinitions,
        CompositionContext context)
    {
        var marked = memberDefinitions
            .Where(static m => m.Member.Directives.Any(static d => d.Name == DirectiveNames.Authenticated))
            .ToImmutableArray();

        if (marked.Length > 0 && marked.Length < memberDefinitions.Length)
        {
            context.Log.Write(
                AuthenticatedMismatch(
                    coordinate,
                    marked[0].Schema,
                    marked.Select(static m => m.Schema.Name),
                    memberDefinitions.Except(marked).Select(static m => m.Schema.Name)));
        }

        var merged = AuthorizationGroups.Merge(memberDefinitions);
        var schema = memberDefinitions[0].Schema;

        if (merged.Scopes.Length > GroupCountThreshold)
        {
            context.Log.Write(
                AuthorizationGroupCountExceeded(
                    "scopes",
                    coordinate,
                    schema,
                    merged.Scopes.Length,
                    GroupCountThreshold));
        }

        if (merged.Policies.Length > GroupCountThreshold)
        {
            context.Log.Write(
                AuthorizationGroupCountExceeded(
                    "policies",
                    coordinate,
                    schema,
                    merged.Policies.Length,
                    GroupCountThreshold));
        }
    }
}
