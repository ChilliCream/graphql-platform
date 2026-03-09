using System.Collections.Immutable;

namespace HotChocolate.Validation;

public class DocumentValidatorResult
{
    private DocumentValidatorResult()
    {
        Errors = [];
        HasErrors = false;
    }

    public DocumentValidatorResult(IEnumerable<IError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        Errors = [.. errors];
        HasErrors = Errors.Count > 0;
    }

    public bool HasErrors { get; }

    public ImmutableList<IError> Errors { get; }

    public static DocumentValidatorResult OK { get; } = new();
}
