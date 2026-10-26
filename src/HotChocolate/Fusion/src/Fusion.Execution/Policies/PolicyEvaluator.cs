using System.Collections.Immutable;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Policies.Audit;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Policies;

/// <summary>
/// Evaluates the policies of an operation for one variable set.
/// </summary>
internal sealed class PolicyEvaluator
{
    private const string UnauthenticatedReason = "unauthenticated";

    private static readonly ImmutableDictionary<string, object?> s_noArguments =
#if NET10_0_OR_GREATER
        [];
#else
        ImmutableDictionary<string, object?>.Empty;
#endif

    private static readonly ImmutableDictionary<string, string> s_noArgumentValues =
#if NET10_0_OR_GREATER
        [];
#else
        ImmutableDictionary<string, string>.Empty;
#endif

    private readonly FusionAuthorizationOptions _options;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;

    /// <summary>
    /// Initializes a new instance of <see cref="PolicyEvaluator"/>.
    /// </summary>
    /// <param name="options">
    /// The authorization options.
    /// </param>
    /// <param name="diagnosticEvents">
    /// The diagnostic events that receive the failures that must not replace an original fault.
    /// </param>
    public PolicyEvaluator(
        FusionAuthorizationOptions options,
        IFusionExecutionDiagnosticEvents diagnosticEvents)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        _options = options;
        _diagnosticEvents = diagnosticEvents;
    }

    /// <summary>
    /// Evaluates every policy once with all of its occurrences that are part of the variable set.
    /// </summary>
    /// <param name="context">
    /// The request context.
    /// </param>
    /// <param name="user">
    /// The principal of the request.
    /// </param>
    /// <param name="plan">
    /// The plan of the operation.
    /// </param>
    /// <param name="policies">
    /// The policy plan of the operation plan.
    /// </param>
    /// <param name="variables">
    /// The coerced variable values of the variable set.
    /// </param>
    /// <param name="scope">
    /// The audit scope of the variable set that receives one entry per evaluated occurrence.
    /// </param>
    /// <param name="requestIndex">
    /// The index of the variable set within the request.
    /// </param>
    /// <param name="cancellationToken">
    /// The token that signals that the request was aborted.
    /// </param>
    public async ValueTask<PolicyEvaluation> EvaluateAsync(
        RequestContext context,
        ClaimsPrincipal user,
        OperationPlan plan,
        PolicyPlan policies,
        IVariableValueCollection variables,
        IAuditScope scope,
        int requestIndex,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(policies);
        ArgumentNullException.ThrowIfNull(variables);
        ArgumentNullException.ThrowIfNull(scope);

        var isAuthenticated = user.Identity?.IsAuthenticated is true;
        var occurrences = GetOccurrences(plan, policies.Descriptors, variables);
        var deniedDescriptors = new HashSet<PolicyDescriptor>();
        var groups = new List<PolicyGroup>();
        var groupsByPolicy = new Dictionary<IPolicy, PolicyGroup>();
        var answered = scope.IsRecording ? new HashSet<PolicyDescriptor>() : null;
        PolicyEvaluationContext? evaluating = null;

        try
        {
            foreach (var descriptor in occurrences)
            {
                // An anonymous principal never reaches the scope and named policies, except an
                // unresolved policy, which fails the request as a configuration error.
                if (!isAuthenticated
                    && descriptor.DirectiveName != DirectiveNames.Authenticated.Name
                    && !(policies.HasUnresolvedPolicies && descriptor.Policy is UnresolvedPolicy))
                {
                    deniedDescriptors.Add(descriptor);

                    if (answered is not null)
                    {
                        var entry = new PolicyEvaluationEntry(
                            descriptor,
                            CoerceArguments((Selection)descriptor.Selection, variables));

                        scope.Record(
                            CreateAuditEntry(
                                entry,
                                new PolicyVerdict(PolicyOutcome.Denied, UnauthenticatedReason, null)));
                        answered.Add(descriptor);
                    }

                    continue;
                }

                if (!groupsByPolicy.TryGetValue(descriptor.Policy, out var group))
                {
                    group = new PolicyGroup(descriptor.Policy);
                    groupsByPolicy.Add(descriptor.Policy, group);
                    groups.Add(group);
                }

                group.Add(descriptor, PolicyEvaluationOrder.Get(descriptor.DirectiveName));
            }

            foreach (var group in groups.OrderBy(static g => g.Order))
            {
                var entries = new PolicyEvaluationEntry[group.Descriptors.Count];

                for (var i = 0; i < entries.Length; i++)
                {
                    var descriptor = group.Descriptors[i];
                    entries[i] = new PolicyEvaluationEntry(
                        descriptor,
                        CoerceArguments((Selection)descriptor.Selection, variables));
                }

                var policyContext = new PolicyEvaluationContext(
                    user,
                    context.Features,
                    context.RequestServices,
                    plan,
                    requestIndex,
                    entries);

                evaluating = policyContext;

                try
                {
                    await group.Policy.EvaluateAsync(policyContext, cancellationToken);
                }
                catch (InvalidOperationException ex) when (group.Policy is UnresolvedPolicy)
                {
                    if (answered is not null)
                    {
                        foreach (var entry in policyContext.Entries)
                        {
                            scope.Record(
                                CreateAuditEntry(
                                    entry,
                                    new PolicyVerdict(PolicyOutcome.Unanswered, ex.Message, null)));
                            answered.Add(entry.Descriptor);
                        }

                        RecordUnanswered(scope, occurrences, groups, answered, variables);
                    }

                    return new PolicyEvaluation(null, ex);
                }

                if (answered is not null)
                {
                    Record(scope, policyContext, isAuthenticated, answered);
                }

                evaluating = null;
                CollectDenied(policyContext, deniedDescriptors);
            }

            return new PolicyEvaluation(
                CreateDecisions(occurrences, deniedDescriptors, isAuthenticated),
                null);
        }
        catch (Exception ex) when (answered is not null && IsFault(ex, cancellationToken))
        {
            RecordFault(context, scope, evaluating, isAuthenticated, occurrences, groups, answered, variables);
            throw;
        }
    }

    /// <summary>
    /// Determines whether an exception that ended the evaluation is a fault of the evaluation
    /// instead of the cancellation of the request.
    /// </summary>
    /// <param name="exception">
    /// The exception that ended the evaluation.
    /// </param>
    /// <param name="cancellationToken">
    /// The token that signals that the request was aborted.
    /// </param>
    public static bool IsFault(Exception exception, CancellationToken cancellationToken)
        => exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested;

    private PolicyDecisions? CreateDecisions(
        ImmutableArray<PolicyDescriptor> occurrences,
        HashSet<PolicyDescriptor> deniedDescriptors,
        bool isAuthenticated)
    {
        if (deniedDescriptors.Count == 0)
        {
            return null;
        }

        var kind = isAuthenticated
            ? AuthorizationDenialKind.Unauthorized
            : AuthorizationDenialKind.Unauthenticated;
        var selections = new List<Selection>();
        var descriptorsBySelection = new Dictionary<Selection, List<PolicyDescriptor>>();

        foreach (var descriptor in occurrences)
        {
            var selection = (Selection)descriptor.Selection;

            if (!descriptorsBySelection.TryGetValue(selection, out var descriptors))
            {
                descriptors = [];
                descriptorsBySelection.Add(selection, descriptors);
                selections.Add(selection);
            }

            descriptors.Add(descriptor);
        }

        var denials = ImmutableArray.CreateBuilder<SelectionDenial>();

        foreach (var selection in selections)
        {
            var denying = FindDenyingDescriptor(
                selection,
                descriptorsBySelection[selection],
                deniedDescriptors);

            if (denying is not null)
            {
                denials.Add(new SelectionDenial(selection, kind, denying));
            }
        }

        return denials.Count == 0
            ? null
            : new PolicyDecisions(
                denials.ToImmutable(),
                _options.DenyHandling,
                _options.EnableAttribution);
    }

    private static PolicyDescriptor? FindDenyingDescriptor(
        Selection selection,
        List<PolicyDescriptor> descriptors,
        HashSet<PolicyDescriptor> deniedDescriptors)
    {
        foreach (var descriptor in descriptors)
        {
            if (descriptor.DirectiveName != DirectiveNames.Policy.Name
                && deniedDescriptors.Contains(descriptor))
            {
                return descriptor;
            }
        }

        // The policy names of a selection are alternative groups. One group whose
        // policies all allowed satisfies the requirement.
        var groups = ((FusionOutputFieldDefinition)selection.Field).Authorization?.Policies ?? [];
        PolicyDescriptor? firstDenied = null;

        foreach (var group in groups)
        {
            var isSatisfied = true;

            foreach (var policyName in group)
            {
                foreach (var descriptor in descriptors)
                {
                    if (descriptor.DirectiveName == DirectiveNames.Policy.Name
                        && descriptor.PolicyName == policyName
                        && deniedDescriptors.Contains(descriptor))
                    {
                        isSatisfied = false;
                        firstDenied ??= descriptor;
                    }
                }
            }

            if (isSatisfied)
            {
                return null;
            }
        }

        return firstDenied;
    }

    private static void Record(
        IAuditScope scope,
        PolicyEvaluationContext policyContext,
        bool isAuthenticated,
        HashSet<PolicyDescriptor> answered)
    {
        var entries = policyContext.Entries;
        var verdicts = policyContext.Verdicts;

        for (var i = 0; i < entries.Length; i++)
        {
            if (answered.Contains(entries[i].Descriptor))
            {
                continue;
            }

            var verdict = verdicts[i];

            if (!isAuthenticated
                && entries[i].Descriptor.DirectiveName == DirectiveNames.Authenticated.Name
                && verdict is { Outcome: PolicyOutcome.Denied, Reason: null })
            {
                verdict = verdict with { Reason = UnauthenticatedReason };
            }

            scope.Record(CreateAuditEntry(entries[i], verdict));
            answered.Add(entries[i].Descriptor);
        }
    }

    private void RecordFault(
        RequestContext context,
        IAuditScope scope,
        PolicyEvaluationContext? evaluating,
        bool isAuthenticated,
        ImmutableArray<PolicyDescriptor> occurrences,
        List<PolicyGroup> groups,
        HashSet<PolicyDescriptor> answered,
        IVariableValueCollection variables)
    {
        try
        {
            if (evaluating is not null)
            {
                Record(scope, evaluating, isAuthenticated, answered);
            }

            RecordUnanswered(scope, occurrences, groups, answered, variables);
        }
        catch (Exception recordingFailure)
        {
            _diagnosticEvents.RequestError(context, recordingFailure);
        }
    }

    private static void RecordUnanswered(
        IAuditScope scope,
        ImmutableArray<PolicyDescriptor> occurrences,
        List<PolicyGroup> groups,
        HashSet<PolicyDescriptor> answered,
        IVariableValueCollection variables)
    {
        foreach (var group in groups.OrderBy(static g => g.Order))
        {
            foreach (var descriptor in group.Descriptors)
            {
                RecordUnanswered(scope, descriptor, answered, variables);
            }
        }

        foreach (var descriptor in occurrences)
        {
            RecordUnanswered(scope, descriptor, answered, variables);
        }
    }

    private static void RecordUnanswered(
        IAuditScope scope,
        PolicyDescriptor descriptor,
        HashSet<PolicyDescriptor> answered,
        IVariableValueCollection variables)
    {
        if (answered.Contains(descriptor))
        {
            return;
        }

        var entry = new PolicyEvaluationEntry(
            descriptor,
            CoerceArguments((Selection)descriptor.Selection, variables));

        scope.Record(CreateAuditEntry(entry, default));
        answered.Add(descriptor);
    }

    private static AuditLogEntry CreateAuditEntry(PolicyEvaluationEntry entry, PolicyVerdict verdict)
    {
        var descriptor = entry.Descriptor;

        return new AuditLogEntry(
            descriptor.Selection.Field.Coordinate,
            descriptor.DirectiveName,
            descriptor.PolicyName,
            descriptor.Scopes,
            CreateArgumentValues(entry.Arguments),
            verdict.Outcome,
            verdict.Reason,
            verdict.AuditData);
    }

    private static ImmutableDictionary<string, string> CreateArgumentValues(
        IReadOnlyDictionary<string, object?> arguments)
    {
        if (arguments.Count == 0)
        {
            return s_noArgumentValues;
        }

        var values = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);

        foreach (var (name, value) in arguments)
        {
            values.Add(
                name,
                value switch
                {
                    IValueNode node => node.Print(indented: false),
                    null => NullValueNode.Default.Print(indented: false),
                    _ => value.ToString() ?? string.Empty
                });
        }

        return values.ToImmutable();
    }

    private static void CollectDenied(
        PolicyEvaluationContext policyContext,
        HashSet<PolicyDescriptor> deniedDescriptors)
    {
        var entries = policyContext.Entries;
        var verdicts = policyContext.Verdicts;

        for (var i = 0; i < entries.Length; i++)
        {
            if (verdicts[i].Outcome is not PolicyOutcome.Allowed)
            {
                deniedDescriptors.Add(entries[i].Descriptor);
            }
        }
    }

    private static ImmutableArray<PolicyDescriptor> GetOccurrences(
        OperationPlan plan,
        ImmutableArray<PolicyDescriptor> descriptors,
        IVariableValueCollection variables)
    {
        var occurrences = ImmutableArray.CreateBuilder<PolicyDescriptor>(descriptors.Length);
        var flagsByOperation = new Dictionary<Operation, ConditionFlags>();
        var operationsThatDoNotRun = plan.IncrementalPlans.IsEmpty
            ? null
            : IncrementalPlan.GetOperationsThatDoNotRun(plan.IncrementalPlans, variables);

        foreach (var descriptor in descriptors)
        {
            if (operationsThatDoNotRun?.Contains(
                    ((Selection)descriptor.Selection).DeclaringSelectionSet.DeclaringOperation) is true)
            {
                continue;
            }

            if (IsReachable((Selection)descriptor.Selection, variables, flagsByOperation))
            {
                occurrences.Add(descriptor);
            }
        }

        return occurrences.ToImmutable();
    }

    private static bool IsReachable(
        Selection selection,
        IVariableValueCollection variables,
        Dictionary<Operation, ConditionFlags> flagsByOperation)
    {
        var operation = selection.DeclaringSelectionSet.DeclaringOperation;

        if (!flagsByOperation.TryGetValue(operation, out var flags))
        {
            flags = operation.CreateIncludeConditionFlags(variables);
            flagsByOperation.Add(operation, flags);
        }

        for (var current = (Selection?)selection;
            current is not null;
            current = current.DeclaringSelectionSet.DeclaringSelection)
        {
            if (!current.IsIncluded(flags))
            {
                return false;
            }
        }

        return true;
    }

    private static ImmutableDictionary<string, object?> CoerceArguments(
        Selection selection,
        IVariableValueCollection variables)
    {
        var definitions = selection.Field.Arguments;

        if (definitions.Count == 0)
        {
            return s_noArguments;
        }

        var syntaxNode = selection.SyntaxNodes[0].Node;
        var arguments = ImmutableDictionary.CreateBuilder<string, object?>(StringComparer.Ordinal);

        foreach (var definition in definitions)
        {
            IValueNode? value = null;

            foreach (var argument in syntaxNode.Arguments)
            {
                if (argument.Name.Value.Equals(definition.Name, StringComparison.Ordinal))
                {
                    value = argument.Value;
                    break;
                }
            }

            if (value is not null)
            {
                value = ResolveVariables(value, variables);
            }

            arguments.Add(definition.Name, value ?? definition.DefaultValue ?? NullValueNode.Default);
        }

        return arguments.ToImmutable();
    }

    private static IValueNode? ResolveVariables(IValueNode value, IVariableValueCollection variables)
    {
        switch (value)
        {
            case VariableNode variable:
                return variables.TryGetValue<IValueNode>(variable.Name.Value, out var resolved)
                    ? resolved
                    : null;

            case ObjectValueNode objectValue:
                List<ObjectFieldNode>? fields = null;

                for (var i = 0; i < objectValue.Fields.Count; i++)
                {
                    var field = objectValue.Fields[i];
                    var fieldValue = ResolveVariables(field.Value, variables);

                    if (fields is null && ReferenceEquals(fieldValue, field.Value))
                    {
                        continue;
                    }

                    if (fields is null)
                    {
                        fields = new List<ObjectFieldNode>(objectValue.Fields.Count);

                        for (var j = 0; j < i; j++)
                        {
                            fields.Add(objectValue.Fields[j]);
                        }
                    }

                    if (fieldValue is not null)
                    {
                        fields.Add(
                            ReferenceEquals(fieldValue, field.Value)
                                ? field
                                : new ObjectFieldNode(field.Location, field.Name, fieldValue));
                    }
                }

                return fields is null
                    ? objectValue
                    : new ObjectValueNode(objectValue.Location, fields);

            case ListValueNode listValue:
                List<IValueNode>? items = null;

                for (var i = 0; i < listValue.Items.Count; i++)
                {
                    var item = listValue.Items[i];
                    var itemValue = ResolveVariables(item, variables) ?? NullValueNode.Default;

                    if (items is null && ReferenceEquals(itemValue, item))
                    {
                        continue;
                    }

                    if (items is null)
                    {
                        items = new List<IValueNode>(listValue.Items.Count);

                        for (var j = 0; j < i; j++)
                        {
                            items.Add(listValue.Items[j]);
                        }
                    }

                    items.Add(itemValue);
                }

                return items is null
                    ? listValue
                    : new ListValueNode(listValue.Location, items);

            default:
                return value;
        }
    }

    private sealed class PolicyGroup(IPolicy policy)
    {
        public IPolicy Policy { get; } = policy;

        public int Order { get; private set; } = int.MaxValue;

        public ImmutableArray<PolicyDescriptor>.Builder Descriptors { get; } =
            ImmutableArray.CreateBuilder<PolicyDescriptor>();

        public void Add(PolicyDescriptor descriptor, int order)
        {
            Descriptors.Add(descriptor);
            Order = Math.Min(Order, order);
        }
    }
}
