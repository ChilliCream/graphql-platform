namespace HotChocolate.Execution.Options;

/// <summary>
/// 
/// </summary>
public sealed class RequestParserOptions
{
    public bool IncludeLocations { get; set; } = true;

    public int MaxAllowedNodes { get; set; } = int.MaxValue;

    public int MaxAllowedTokens { get; set; } = int.MaxValue;
}
