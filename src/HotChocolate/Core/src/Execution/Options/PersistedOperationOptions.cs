namespace HotChocolate.Execution.Options;

/// <summary>
/// Represents the persisted operation options.
/// </summary>
public sealed class PersistedOperationOptions
{
    private IError _operationNotAllowedError = ErrorHelper.OnlyPersistedOperationsAreAllowed();

    /// <summary>
    /// Specifies if only persisted operation documents are allowed.
    /// </summary>
    public bool OnlyAllowPersistedDocuments { get; set; }

    /// <summary>
    /// Specifies that if <see cref="OnlyAllowPersistedDocuments"/> is switched on
    /// whether a standard GraphQL request with document body is allowed as long as
    /// it matches a persisted document.
    /// </summary>
    public bool AllowDocumentBody { get; set; }

    /// <summary>
    /// Specifies if persisted operation documents
    /// need to be validated.
    /// </summary>
    public bool SkipPersistedDocumentValidation { get; set; }

    /// <summary>
    /// The error that will be thrown when only persisted
    /// operations are allowed and a normal operation is issued.
    /// </summary>
    public IError OperationNotAllowedError
    {
        get => _operationNotAllowedError;
        set
        {
            _operationNotAllowedError = value
                ?? throw new ArgumentNullException(nameof(OperationNotAllowedError));
        }
    }
}
