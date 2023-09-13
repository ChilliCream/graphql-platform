#nullable enable

namespace HotChocolate.Types;

public class InputParserOptions
{
    /// <summary>
    /// Specifies if additional input object fields should be ignored during parsing.
    /// 
    /// The default is <c>false</c>.
    /// </summary>
    public bool IgnoreAdditionalInputFields { get; set; }
}
