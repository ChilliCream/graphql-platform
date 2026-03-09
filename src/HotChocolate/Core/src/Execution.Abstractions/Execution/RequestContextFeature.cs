using HotChocolate.Features;

namespace HotChocolate.Execution;

/// <summary>
/// A feature that is pooled with a <see cref="RequestContext"/>.
/// </summary>
public abstract class RequestFeature : IPooledFeature
{
    /// <summary>
    /// Initializes the feature when the <see cref="RequestContext"/> is initialized.
    /// </summary>
    /// <param name="context">
    /// The <see cref="RequestContext"/> that is being initialized.
    /// </param>
    protected internal virtual void Initialize(RequestContext context)
    {
    }

    /// <summary>
    /// Resets the feature when the <see cref="RequestContext"/> is reset.
    /// </summary>
    protected internal virtual void Reset()
    {
    }

    void IPooledFeature.Initialize(object state)
    {
        if (state is not RequestContext context)
        {
            throw new InvalidOperationException(
                $"The state of the {nameof(RequestContext)} is not a {nameof(RequestContext)}.");
        }

        Initialize(context);
    }

    void IPooledFeature.Reset() => Reset();
}
