#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Launch;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class LaunchCommand : Command
{
    public LaunchCommand() : base("launch")
    {
        Description = "Launch Nitro in your default browser.";

        this.AddExamples("launch");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var sessionService = services.GetRequiredService<ISessionService>();
        var browser = services.GetRequiredService<IBrowserLauncher>();

        var url = ResolveUrl(sessionService.Session);

        browser.Open(url);
        console.OkLine($"[link={url}]Nitro[/] is launched!");

        return Task.FromResult(ExitCodes.Success);
    }

    private static string ResolveUrl(Session? session)
    {
        if (session is null)
        {
            return Constants.NitroWebUrl;
        }

        var defaultApiUrl = Constants.ApiUrl["https://".Length..];

        if (session.ApiUrl == defaultApiUrl || session.ApiUrl == Constants.ApiUrl)
        {
            return Constants.NitroWebUrl;
        }

        var baseUrl = session.ApiUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || session.ApiUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? session.ApiUrl
            : $"https://{session.ApiUrl}";

        return $"{baseUrl.TrimEnd('/')}/ui";
    }
}
