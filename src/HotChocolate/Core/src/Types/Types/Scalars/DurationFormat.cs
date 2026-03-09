namespace HotChocolate.Types;

public enum DurationFormat
{
    /// <summary>
    /// Duration ISO 8601 Format
    /// https://tools.ietf.org/html/rfc3339
    /// </summary>
    Iso8601,
    /// <summary>
    /// Duration .NET Constant ("c") Format
    /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings#the-constant-c-format-specifier
    /// </summary>
    DotNet
}
