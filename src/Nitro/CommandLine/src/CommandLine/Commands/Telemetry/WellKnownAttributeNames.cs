namespace ChilliCream.Nitro.CommandLine.Commands.Telemetry;

/// <summary>
/// The OpenTelemetry attribute names the telemetry commands read from spans, events and logs.
/// </summary>
internal static class WellKnownAttributeNames
{
    public const string ServiceName = "service.name";

    public const string CodeFunction = "code.function";

    public const string CodeFilePath = "code.filepath";

    public const string CodeLineNumber = "code.lineno";

    public const string ExceptionType = "exception.type";

    public const string ExceptionMessage = "exception.message";

    public const string ExceptionStackTrace = "exception.stacktrace";

    public const string HttpRequestMethod = "http.request.method";

    public const string HttpMethod = "http.method";

    public const string HttpRoute = "http.route";

    public const string DbSystem = "db.system";

    public const string DbOperation = "db.operation";

    public const string DbOperationName = "db.operation.name";

    public const string GraphQLOperationName = "graphql.operation.name";
}
