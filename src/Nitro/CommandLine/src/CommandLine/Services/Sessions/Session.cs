using System.Text.Json.Serialization.Metadata;
using ChilliCream.Nitro.CommandLine.Output;
using ChilliCream.Nitro.CommandLine.Services.Configuration;

namespace ChilliCream.Nitro.CommandLine.Services.Sessions;

internal class Session(
    string sessionId,
    string subjectId,
    string tenant,
    string identityServer,
    string apiUrl,
    string email,
    Tokens? tokens,
    Workspace? workspace) : IConfigurationFile
{
    public static string FileName => "session.json";

    public static object? Default { get; }

    public static JsonTypeInfo TypeInfo => NitroCLIJsonContext.Default.Session;

    public string SessionId { get; set; } = sessionId;

    public string SubjectId { get; set; } = subjectId;

    public string Tenant { get; set; } = tenant;

    public string IdentityServer { get; set; } = identityServer;

    public string ApiUrl { get; set; } = apiUrl;

    public string Email { get; set; } = email;

    public Tokens? Tokens { get; set; } = tokens;

    public Workspace? Workspace { get; set; } = workspace;

    /// <summary>
    /// The default API id used by analytical commands when no <c>--api-id</c> flag is
    /// supplied. Persisted via <c>nitro config set api</c>.
    /// </summary>
    public string? DefaultApiId { get; set; }

    /// <summary>
    /// The default stage name used by analytical commands when no <c>--stage</c> flag is
    /// supplied. Persisted via <c>nitro config set stage</c>.
    /// </summary>
    public string? DefaultStage { get; set; }

    /// <summary>
    /// The default output format used by analytical commands when no <c>--format</c> flag
    /// is supplied and stdout's TTY state should not be consulted. Persisted via
    /// <c>nitro config set format</c>.
    /// </summary>
    public OutputFormat? DefaultFormat { get; set; }
}
