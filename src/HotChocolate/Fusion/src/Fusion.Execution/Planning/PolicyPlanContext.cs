using System.Collections.Frozen;
using System.Collections.Immutable;
using HotChocolate.Fusion.Policies;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The policy state that is shared by all operations of one plan: the pinned policies,
/// the allocation of the synthetic variable names and the descriptors and variables that
/// the markers of the root operation and the deferred operations produce.
/// </summary>
internal sealed class PolicyPlanContext
{
    private const string VariableNamePrefix = "__fusion_policy_";

    private readonly IPolicyResolver _resolver;
    private readonly HashSet<string> _reservedNames = new(StringComparer.Ordinal);
    private readonly Dictionary<(string DirectiveName, string? PolicyName), IPolicy> _policies = [];
    private readonly List<PolicyMarker> _incrementalMarkers = [];
    private PolicyMarker? _rootMarker;
    private int _lastVariableId;

    public PolicyPlanContext(
        IPolicyResolver resolver,
        FrozenSet<string> protectedFieldNames,
        OperationDefinitionNode operationDefinition)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(protectedFieldNames);
        ArgumentNullException.ThrowIfNull(operationDefinition);

        _resolver = resolver;
        ProtectedFieldNames = protectedFieldNames;

        foreach (var variableDefinition in operationDefinition.VariableDefinitions)
        {
            _reservedNames.Add(variableDefinition.Variable.Name.Value);
        }
    }

    /// <summary>
    /// Gets the names of all fields of the schema that carry a policy requirement.
    /// </summary>
    public FrozenSet<string> ProtectedFieldNames { get; }

    public IPolicy GetPolicy(string directiveName, string? policyName)
    {
        if (!_policies.TryGetValue((directiveName, policyName), out var policy))
        {
            policy = _resolver.Resolve(policyName ?? string.Empty, directiveName)
                ?? new UnresolvedPolicy(directiveName, policyName);
            _policies.Add((directiveName, policyName), policy);
        }

        return policy;
    }

    public string NextVariableName()
    {
        string name;

        do
        {
            name = VariableNamePrefix + ++_lastVariableId;
        }
        while (_reservedNames.Contains(name));

        return name;
    }

    /// <summary>
    /// Registers the marker of the root operation.
    /// </summary>
    public void AddRoot(PolicyMarker marker)
    {
        ArgumentNullException.ThrowIfNull(marker);

        _rootMarker = marker;
    }

    /// <summary>
    /// Registers the marker of a deferred operation. Markers contribute to the plan in registration order.
    /// </summary>
    public void AddIncremental(PolicyMarker marker)
    {
        ArgumentNullException.ThrowIfNull(marker);

        _incrementalMarkers.Add(marker);
    }

    /// <summary>
    /// Creates the policy plan from the registered markers, root first, or returns <c>null</c>
    /// if no marker was registered.
    /// </summary>
    public PolicyPlan? CreatePlan()
    {
        if (_rootMarker is null && _incrementalMarkers.Count == 0)
        {
            return null;
        }

        var descriptors = ImmutableArray.CreateBuilder<PolicyDescriptor>();
        var variables = ImmutableArray.CreateBuilder<PolicyVariable>();

        if (_rootMarker is not null)
        {
            descriptors.AddRange(_rootMarker.Descriptors);
            variables.AddRange(_rootMarker.GetVariables());
        }

        foreach (var marker in _incrementalMarkers)
        {
            descriptors.AddRange(marker.Descriptors);
            variables.AddRange(marker.GetVariables());
        }

        return new PolicyPlan(descriptors.ToImmutable(), variables.ToImmutable());
    }
}
