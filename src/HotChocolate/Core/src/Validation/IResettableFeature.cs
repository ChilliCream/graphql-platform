namespace HotChocolate.Validation;

/// <summary>
/// A validation feature can be used to extend the <see cref="DocumentValidatorContext"/>
/// with additional capabilities for a specific validation rule.
/// </summary>
public abstract class ValidatorFeature
{
    /// <summary>
    /// This method is called after the <see cref="DocumentValidatorContext"/> is initialized.
    /// </summary>
    /// <param name="context">
    /// The <see cref="DocumentValidatorContext"/>.
    /// </param>
    protected internal virtual void OnInitialize(DocumentValidatorContext context) { }

    /// <summary>
    /// This method is called when the <see cref="DocumentValidatorContext"/> is reset.
    /// </summary>
    protected internal virtual void Reset() { }
}
