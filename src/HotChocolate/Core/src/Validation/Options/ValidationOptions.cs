namespace HotChocolate.Validation.Options;

/// <summary>
/// The validation options.
/// </summary>
public sealed class ValidationOptions
    : IMaxExecutionDepthOptionsAccessor
    , IErrorOptionsAccessor
    , IIntrospectionOptionsAccessor
{
    /// <summary>
    /// Gets the maximum allowed depth of a query. The default value is
    /// <see langword="null"/>. The minimum allowed value is <c>1</c>.
    /// </summary>
    public int? MaxAllowedExecutionDepth
    {
        get;
        set
        {
            field = value < 1 ? 1 : value;
        }
    }

    /// <summary>
    /// Specifies that the max execution depth analysis
    /// shall skip introspection fields.
    /// </summary>
    public bool SkipIntrospectionFields { get; set; }

    /// <summary>
    /// Specifies how many errors are allowed before the validation is aborted.
    /// Defaults to <c>5</c>.
    /// </summary>
    public int MaxAllowedErrors
    {
        get;
        set
        {
            // if the value is lower than 1 we will set it to the default.
            if (value < 1)
            {
                value = 5;
            }

            field = value;
        }
    } = 5;

    /// <summary>
    /// Specifies the maximum number of locations added to a validation error.
    /// Defaults to <c>5</c>.
    /// </summary>
    public int MaxLocationsPerError
    {
        get;
        set
        {
            // if the value is lower than 1 we will set it to the default.
            if (value < 1)
            {
                value = 5;
            }

            field = value;
        }
    } = 5;

    public bool DisableIntrospection { get; set; }

    public bool DisableDepthRule { get; set; }

    public ushort MaxAllowedOfTypeDepth
    {
        get;
        set => field = value > 0 ? value : (ushort)1;
    } = 16;

    public ushort MaxAllowedListRecursiveDepth
    {
        get;
        set => field = value > 0 ? value : (ushort)16;
    } = 1;
}
