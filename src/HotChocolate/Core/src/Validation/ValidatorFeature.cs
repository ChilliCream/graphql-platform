using HotChocolate.Features;

namespace HotChocolate.Validation;

/// <summary>
/// A validation feature can be used to extend the <see cref="DocumentValidatorContext"/>
/// with additional capabilities for a specific validation rule.
/// </summary>
public abstract class ValidatorFeature : IPooledFeature
{
    /// <summary>
    /// This method is called after the <see cref="DocumentValidatorContext"/> is initialized.
    /// </summary>
    /// <param name="context">
    /// The <see cref="DocumentValidatorContext"/>.
    /// </param>
    protected internal virtual void Initialize(DocumentValidatorContext context) { }

    /// <summary>
    /// This method is called after the <see cref="DocumentValidatorContext"/> is reset.
    /// </summary>
    protected internal virtual void Reset() { }

    void IPooledFeature.Initialize(object state)
    {
        if (state is not DocumentValidatorContext context)
        {
            throw new InvalidOperationException(
                $"The state of the {nameof(DocumentValidatorContext)} is not a {nameof(DocumentValidatorContext)}.");
        }

        Initialize(context);
    }

    void IPooledFeature.Reset() => Reset();
}
