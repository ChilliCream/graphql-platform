namespace GreenDonut.Data;

public enum NullOrdering
{
    /// <summary>The null ordering is not specified.</summary>
    Unspecified = 0,

    /// <summary>
    /// The database orders null values <i>first</i> (i.e., null is considered <i>less than</i>
    /// non-null values).
    /// </summary>
    NativeNullsFirst = 1,

    /// <summary>
    /// The database orders null values <i>last</i> (i.e., null is considered <i>greater than</i>
    /// non-null values).
    /// </summary>
    NativeNullsLast = 2
}
