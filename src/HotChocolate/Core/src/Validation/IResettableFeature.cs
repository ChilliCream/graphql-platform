namespace HotChocolate.Validation;

public abstract class ValidatorFeature
{
    public virtual void OnInitialize(DocumentValidatorContext context) { }

    public virtual void Reset() { }
}
