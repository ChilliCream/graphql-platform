namespace HotChocolate.Validation.Options;

/// <summary>
/// The validation options.
/// </summary>
public sealed class ValidationOptions
    : IMaxExecutionDepthOptionsAccessor
    , IErrorOptionsAccessor
    , IIntrospectionOptionsAccessor
{
    private int? _maxAllowedExecutionDepth;
    private int _maxErrors = 5;
    private ushort _maxAllowedOfTypeDepth = 16;
    private ushort _maxAllowedListRecursiveDepth = 1;

    /// <summary>
    /// Gets the maximum allowed depth of a query. The default value is
    /// <see langword="null"/>. The minimum allowed value is <c>1</c>.
    /// </summary>
    public int? MaxAllowedExecutionDepth
    {
        get => _maxAllowedExecutionDepth;
        set
        {
            _maxAllowedExecutionDepth = value < 1 ? 1 : value;
        }
    }

    /// <summary>
    /// Specifies that the max execution depth analysis
    /// shall skip introspection fields.
    /// </summary>
    public bool SkipIntrospectionFields { get; set; }

    /// <summary>
    /// Specifies how many errors are allowed before the validation is aborted.
    /// </summary>
    public int MaxAllowedErrors
    {
        get => _maxErrors;
        set
        {
            // if the value is lover than 1 we will set it to the default.
            if (value < 1)
            {
                value = 5;
            }

            _maxErrors = value;
        }
    }

    public bool DisableIntrospection { get; set; }

    public bool DisableDepthRule { get; set; }

    public ushort MaxAllowedOfTypeDepth
    {
        get => _maxAllowedOfTypeDepth;
        set => _maxAllowedOfTypeDepth = value > 0 ? value : (ushort)1;
    }

    public ushort MaxAllowedListRecursiveDepth
    {
        get => _maxAllowedListRecursiveDepth;
        set => _maxAllowedListRecursiveDepth = value > 0 ? value : (ushort)16;
    }
}
