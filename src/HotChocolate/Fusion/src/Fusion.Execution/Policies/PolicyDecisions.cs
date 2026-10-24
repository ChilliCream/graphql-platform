using System.Collections.Frozen;
using System.Collections.Immutable;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Policies;

/// <summary>
/// The denied selections of one variable set of a request.
/// </summary>
internal sealed class PolicyDecisions
{
    private readonly FrozenDictionary<Selection, SelectionDenial> _denialsBySelection;
    private readonly DenyHandling _denyHandling;
    private readonly bool _isAttributed;

    /// <summary>
    /// Initializes a new instance of <see cref="PolicyDecisions"/>.
    /// </summary>
    /// <param name="denials">
    /// The denied selections in evaluation order. A selection appears at most once.
    /// </param>
    /// <param name="denyHandling">
    /// How a denied selection is reported.
    /// </param>
    /// <param name="isAttributed">
    /// Whether the errors of denied selections name the directive, policy and required scopes.
    /// </param>
    public PolicyDecisions(
        ImmutableArray<SelectionDenial> denials,
        DenyHandling denyHandling,
        bool isAttributed)
    {
        Denials = denials;
        _denialsBySelection = denials.ToFrozenDictionary(static d => d.Selection);
        _denyHandling = denyHandling;
        _isAttributed = isAttributed;

        RequiresFinalization = denyHandling is DenyHandling.Error || ContainsNonNullSelection(denials);
    }

    /// <summary>
    /// Gets the denied selections in evaluation order.
    /// </summary>
    public ImmutableArray<SelectionDenial> Denials { get; }

    /// <summary>
    /// Gets a value indicating whether the result of the request must be completed with the errors
    /// of the denied selections.
    /// </summary>
    public bool RequiresFinalization { get; }

    /// <summary>
    /// Gets a value indicating whether the selection is denied.
    /// </summary>
    /// <param name="selection">
    /// The selection to check.
    /// </param>
    public bool IsDenied(Selection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        return _denialsBySelection.ContainsKey(selection);
    }

    /// <summary>
    /// Creates the error for an occurrence of a denied selection, or <c>null</c> if the denial is
    /// reported as a silent <c>null</c>.
    /// </summary>
    /// <param name="selection">
    /// A denied selection.
    /// </param>
    /// <param name="path">
    /// The path of the occurrence.
    /// </param>
    public IError? CreateError(Selection selection, Path path)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(path);

        if (_denyHandling is DenyHandling.Error)
        {
            var denial = _denialsBySelection[selection];

            return ErrorHelper.DeniedField(
                denial.Kind,
                path,
                _isAttributed ? denial.Descriptor : null);
        }

        return selection.IsNonNull ? ErrorHelper.DeniedNonNullField(path) : null;
    }

    /// <summary>
    /// Gets the first denial that rejects the whole request for the given level.
    /// </summary>
    /// <param name="rejectRequestOn">
    /// The level of the denials that reject the request.
    /// </param>
    /// <param name="denial">
    /// The first denial that rejects the request.
    /// </param>
    public bool TryGetRejection(RejectRequestOn rejectRequestOn, out SelectionDenial denial)
    {
        foreach (var current in Denials)
        {
            var rejects = current.Kind is AuthorizationDenialKind.Unauthenticated
                ? rejectRequestOn >= RejectRequestOn.OnUnauthenticated
                : rejectRequestOn >= RejectRequestOn.OnUnauthorized;

            if (rejects)
            {
                denial = current;
                return true;
            }
        }

        denial = default;
        return false;
    }

    private static bool ContainsNonNullSelection(ImmutableArray<SelectionDenial> denials)
    {
        foreach (var denial in denials)
        {
            if (denial.Selection.IsNonNull)
            {
                return true;
            }
        }

        return false;
    }
}
