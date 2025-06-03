namespace HotChocolate.Execution.Serialization;

/// <summary>
/// Specifies when null values are ignored.
/// </summary>
public enum JsonNullIgnoreCondition
{
    /// <summary>
    /// No null values are ignore.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Fields that have a null value are ignored.
    /// </summary>
    Fields = 1,

    /// <summary>
    /// Null elements in lists are ignored.
    /// </summary>
    Lists = 2,

    /// <summary>
    /// Fields that have a null value and null elements in lists are ignored.
    /// </summary>
    All = 4
}
