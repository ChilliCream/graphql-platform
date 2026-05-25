namespace HotChocolate.Execution;

/// <summary>
/// The field names for a GraphQL result.
/// </summary>
public static class ResultFieldNames
{
    /// <summary>
    /// Gets the data field name.
    /// </summary>
    public static ReadOnlySpan<byte> Data => "data"u8;

    /// <summary>
    /// Gets the items field name.
    /// </summary>
    public static ReadOnlySpan<byte> Items => "items"u8;

    /// <summary>
    /// Gets the errors field name.
    /// </summary>
    public static ReadOnlySpan<byte> Errors => "errors"u8;

    /// <summary>
    /// Gets the extensions field name.
    /// </summary>
    public static ReadOnlySpan<byte> Extensions => "extensions"u8;

    /// <summary>
    /// Gets the message field name.
    /// </summary>
    public static ReadOnlySpan<byte> Message => "message"u8;

    /// <summary>
    /// Gets the locations field name.
    /// </summary>
    public static ReadOnlySpan<byte> Locations => "locations"u8;

    /// <summary>
    /// Gets the path field name.
    /// </summary>
    public static ReadOnlySpan<byte> Path => "path"u8;

    /// <summary>
    /// Gets the line field name.
    /// </summary>
    public static ReadOnlySpan<byte> Line => "line"u8;

    /// <summary>
    /// Gets the column field name.
    /// </summary>
    public static ReadOnlySpan<byte> Column => "column"u8;

    /// <summary>
    /// Gets the incremental field name.
    /// </summary>
    public static ReadOnlySpan<byte> Incremental => "incremental"u8;

    /// <summary>
    /// Gets the completed field name.
    /// </summary>
    public static ReadOnlySpan<byte> Completed => "completed"u8;

    /// <summary>
    /// Gets the pending field name.
    /// </summary>
    public static ReadOnlySpan<byte> Pending => "pending"u8;

    /// <summary>
    /// Gets the id field name.
    /// </summary>
    public static ReadOnlySpan<byte> Id => "id"u8;

    /// <summary>
    /// Gets the label field name.
    /// </summary>
    public static ReadOnlySpan<byte> Label => "label"u8;

    /// <summary>
    /// Gets the subPath field name.
    /// </summary>
    public static ReadOnlySpan<byte> SubPath => "subPath"u8;

    /// <summary>
    /// Gets the hasNext field name
    /// </summary>
    public static ReadOnlySpan<byte> HasNext => "hasNext"u8;

    /// <summary>
    /// Gets the request index field name.
    /// </summary>
    public static ReadOnlySpan<byte> RequestIndex => "requestIndex"u8;

    /// <summary>
    /// Gets the variable index field name.
    /// </summary>
    public static ReadOnlySpan<byte> VariableIndex => "variableIndex"u8;
}
