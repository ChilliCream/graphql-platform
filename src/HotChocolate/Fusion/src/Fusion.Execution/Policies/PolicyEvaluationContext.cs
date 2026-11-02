using System.Collections.Immutable;
using System.Security.Claims;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Policies;

/// <summary>
/// The input and the answer sheet of one policy evaluation for one variable set.
/// </summary>
public sealed class PolicyEvaluationContext
{
    private readonly PolicyEvaluationEntry[] _entries;
    private readonly PolicyVerdict[] _verdicts;

    /// <summary>
    /// Initializes a new instance of <see cref="PolicyEvaluationContext"/>.
    /// </summary>
    /// <param name="user">
    /// The principal of the request. An unauthenticated principal represents an anonymous caller.
    /// </param>
    /// <param name="features">
    /// The features of the request.
    /// </param>
    /// <param name="requestServices">
    /// The request scoped service provider.
    /// </param>
    /// <param name="plan">
    /// The plan of the operation being evaluated.
    /// </param>
    /// <param name="requestIndex">
    /// The index of the variable set within the request.
    /// </param>
    /// <param name="entries">
    /// The occurrences of the policy that are evaluated.
    /// </param>
    public PolicyEvaluationContext(
        ClaimsPrincipal user,
        IFeatureCollection features,
        IServiceProvider requestServices,
        OperationPlan plan,
        int requestIndex,
        ReadOnlySpan<PolicyEvaluationEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(requestServices);
        ArgumentNullException.ThrowIfNull(plan);

        User = user;
        Features = features;
        RequestServices = requestServices;
        Plan = plan;
        RequestIndex = requestIndex;

        _entries = new PolicyEvaluationEntry[entries.Length];
        _verdicts = new PolicyVerdict[entries.Length];

        for (var i = 0; i < entries.Length; i++)
        {
            _entries[i] = entries[i] with { Slot = i + 1 };
        }
    }

    /// <summary>
    /// Gets the principal of the request.
    /// </summary>
    public ClaimsPrincipal User { get; }

    /// <summary>
    /// Gets the features of the request.
    /// </summary>
    public IFeatureCollection Features { get; }

    /// <summary>
    /// Gets the request scoped service provider.
    /// </summary>
    public IServiceProvider RequestServices { get; }

    /// <summary>
    /// Gets the plan of the operation being evaluated.
    /// </summary>
    public OperationPlan Plan { get; }

    /// <summary>
    /// Gets the index of the variable set within the request.
    /// </summary>
    public int RequestIndex { get; }

    /// <summary>
    /// Gets the occurrences of the policy that are evaluated.
    /// </summary>
    public ReadOnlySpan<PolicyEvaluationEntry> Entries => _entries;

    /// <summary>
    /// Gets the verdicts in the order of <see cref="Entries"/>.
    /// </summary>
    public ReadOnlySpan<PolicyVerdict> Verdicts => _verdicts;

    /// <summary>
    /// Allows the entry.
    /// </summary>
    /// <param name="entry">
    /// An entry of <see cref="Entries"/>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The entry was already answered.
    /// </exception>
    public void Allow(in PolicyEvaluationEntry entry)
        => Record(entry, PolicyOutcome.Allowed, null, null);

    /// <summary>
    /// Allows the entry.
    /// </summary>
    /// <param name="entry">
    /// An entry of <see cref="Entries"/>.
    /// </param>
    /// <param name="auditData">
    /// Key/value pairs that are only meant for auditing.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The entry was already answered.
    /// </exception>
    public void Allow(in PolicyEvaluationEntry entry, ImmutableDictionary<string, string> auditData)
    {
        ArgumentNullException.ThrowIfNull(auditData);
        Record(entry, PolicyOutcome.Allowed, null, auditData);
    }

    /// <summary>
    /// Denies the entry.
    /// </summary>
    /// <param name="entry">
    /// An entry of <see cref="Entries"/>.
    /// </param>
    /// <param name="reason">
    /// An optional reason that is only meant for auditing.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The entry was already answered.
    /// </exception>
    public void Deny(in PolicyEvaluationEntry entry, string? reason = null)
        => Record(entry, PolicyOutcome.Denied, reason, null);

    /// <summary>
    /// Denies the entry.
    /// </summary>
    /// <param name="entry">
    /// An entry of <see cref="Entries"/>.
    /// </param>
    /// <param name="reason">
    /// An optional reason that is only meant for auditing.
    /// </param>
    /// <param name="auditData">
    /// Key/value pairs that are only meant for auditing.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The entry was already answered.
    /// </exception>
    public void Deny(
        in PolicyEvaluationEntry entry,
        string? reason,
        ImmutableDictionary<string, string> auditData)
    {
        ArgumentNullException.ThrowIfNull(auditData);
        Record(entry, PolicyOutcome.Denied, reason, auditData);
    }

    /// <summary>
    /// Gets the verdict recorded for the entry.
    /// </summary>
    /// <param name="entry">
    /// An entry of <see cref="Entries"/>.
    /// </param>
    public PolicyVerdict GetVerdict(in PolicyEvaluationEntry entry)
        => _verdicts[GetIndex(entry)];

    private void Record(
        in PolicyEvaluationEntry entry,
        PolicyOutcome outcome,
        string? reason,
        ImmutableDictionary<string, string>? auditData)
    {
        var index = GetIndex(entry);

        if (_verdicts[index].Outcome is not PolicyOutcome.Unanswered)
        {
            var descriptor = entry.Descriptor;

            throw ThrowHelper.PolicyEntryAlreadyAnswered(
                descriptor.Selection.Field.Coordinate.ToString(),
                descriptor.PolicyName ?? descriptor.DirectiveName);
        }

        _verdicts[index] = new PolicyVerdict(outcome, reason, auditData);
    }

    private int GetIndex(in PolicyEvaluationEntry entry)
    {
        var index = entry.Slot - 1;

        if (index < 0
            || index >= _entries.Length
            || !ReferenceEquals(_entries[index].Descriptor, entry.Descriptor))
        {
            throw ThrowHelper.PolicyEntryNotPartOfContext();
        }

        return index;
    }
}
