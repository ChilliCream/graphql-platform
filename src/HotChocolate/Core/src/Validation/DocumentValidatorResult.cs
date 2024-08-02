namespace HotChocolate.Validation;

public class DocumentValidatorResult
{
    private DocumentValidatorResult()
    {
        Errors = Array.Empty<IError>();
        HasErrors = false;
    }

    public DocumentValidatorResult(IEnumerable<IError> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        Errors = errors.ToArray();
        HasErrors = Errors.Count > 0;
    }

    public bool HasErrors { get; }

    public IReadOnlyList<IError> Errors { get; }

    public static DocumentValidatorResult Ok { get; } = new();
}
