namespace HotChocolate.Types
{
    public enum TimeSpanFormat
    {
        /// <summary>
        /// https://www.w3.org/TR/xmlschema-2/#duration
        /// </summary>
        Iso8601,
        /// <summary>
        /// The Constant ("c") Format Specifier
        /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings#the-constant-c-format-specifier
        /// </summary>
        DotNet
    }
}
