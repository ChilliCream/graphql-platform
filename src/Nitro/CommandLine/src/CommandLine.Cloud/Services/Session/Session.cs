using System.Text.Json.Serialization.Metadata;
using ChilliCream.Nitro.CLI.Auth;

namespace ChilliCream.Nitro.CLI;

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
}
