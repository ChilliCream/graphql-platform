using Mocha.Features;

namespace Mocha;

/// <summary>
/// A feature that exposes the per-exception policy configuration.
/// </summary>
public sealed class ExceptionPolicyFeature : ISealable
{
    private readonly List<ExceptionPolicyRule> _rules = [];

    /// <inheritdoc />
    public bool IsReadOnly { get; private set; }

    /// <summary>
    /// Gets the configured exception policy rules.
    /// </summary>
    public IReadOnlyList<ExceptionPolicyRule> Rules => _rules;

    /// <inheritdoc />
    public void Seal()
    {
        IsReadOnly = true;
    }

    /// <summary>
    /// Applies configuration to the exception policy options.
    /// </summary>
    /// <param name="configure">An action that modifies the exception policy options.</param>
    /// <exception cref="InvalidOperationException">Thrown if the feature has been sealed.</exception>
    public void Configure(Action<ExceptionPolicyOptions> configure)
    {
        if (IsReadOnly)
        {
            throw ThrowHelper.FeatureIsReadOnly();
        }

        configure(new ExceptionPolicyOptions(_rules));
    }
}
