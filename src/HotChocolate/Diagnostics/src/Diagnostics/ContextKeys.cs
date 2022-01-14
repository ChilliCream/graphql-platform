namespace HotChocolate.Diagnostics;

internal static class ContextKeys
{
    public const string HttpRequestActivity = "HotChocolate.Diagnostics.HttpRequest";
    public const string ParseHttpRequestActivity = "HotChocolate.Diagnostics.ParseHttpRequest";
    public const string FormatHttpResponseActivity = "HotChocolate.Diagnostics.FormatHttpResponse";
    public const string WebSocketSessionActivity = "HotChocolate.Diagnostics.WebSocketSession";
    public const string RequestActivity = "HotChocolate.Diagnostics.Request";
    public const string ParserActivity = "HotChocolate.Diagnostics.Parser";
    public const string ValidateActivity = "HotChocolate.Diagnostics.Validate";
    public const string ComplexityActivity = "HotChocolate.Diagnostics.AnalyzeOperationComplexity";
    public const string ResolverActivity = "HotChocolate.Diagnostics.Resolver";
}
