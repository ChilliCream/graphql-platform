namespace Mocha;

/// <summary>
/// Default implementation of <see cref="IReceiveTypeBindDescriptor"/> that collects per-type
/// auto-binding and explicit binding intents using an explicit-wins ordering rule.
/// </summary>
public sealed class ReceiveTypeBindDescriptor : IReceiveTypeBindDescriptor
{
    private bool? _explicitAutoBind;
    private bool _hasBindFrom;
    private List<BindFromIntent>? _bindFroms;

    /// <summary>
    /// Gets the resolved auto-binding decision for this type.
    /// Returns the explicit value if <see cref="AutoBind(bool)"/> was called, <c>false</c> when
    /// at least one <see cref="BindFrom(Uri, string?)"/> was recorded and no explicit value was
    /// set, or <c>null</c> when neither was configured (inherit from queue or transport scope).
    /// </summary>
    public bool? ResolvedAutoBind
    {
        get
        {
            if (_explicitAutoBind.HasValue)
            {
                return _explicitAutoBind.Value;
            }

            if (_hasBindFrom)
            {
                return false;
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the list of explicit binding intents recorded via <see cref="BindFrom(Uri, string?)"/>,
    /// or an empty list when none were added.
    /// </summary>
    public IReadOnlyList<BindFromIntent> BindFroms =>
        _bindFroms is not null ? _bindFroms : [];

    /// <inheritdoc />
    public IReceiveTypeBindDescriptor AutoBind(bool enabled)
    {
        _explicitAutoBind = enabled;
        return this;
    }

    /// <inheritdoc />
    public IReceiveTypeBindDescriptor BindFrom(Uri source, string? routingKey = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        _hasBindFrom = true;
        _bindFroms ??= [];
        _bindFroms.Add(new BindFromIntent(source, routingKey));
        return this;
    }
}
