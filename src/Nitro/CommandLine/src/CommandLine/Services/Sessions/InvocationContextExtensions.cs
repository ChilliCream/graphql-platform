using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;

namespace ChilliCream.Nitro.CommandLine.Services.Sessions;

internal static class InvocationContextExtensions
{
    public static string RequireWorkspaceId(this InvocationContext context)
    {
        var service = context.BindingContext.GetRequiredService<ISessionService>();

        return service.Session?.Workspace?.Id
            ?? context.ParseResult.GetValueForOption(Opt<WorkspaceIdOption>.Instance)
            ?? throw ThrowHelper.NoDefaultWorkspace();
    }

    public static async Task<bool> ConfirmWhenNotForced(
        this InvocationContext context,
        string message,
        CancellationToken cancellationToken)
    {
        var forceOption = context.ParseResult.FindResultFor(Opt<ForceOption>.Instance);
        if (forceOption is not null)
        {
            return true;
        }

        var console = context.BindingContext.GetRequiredService<IAnsiConsole>();

        return await console.ConfirmAsync(message, cancellationToken);
    }
}
