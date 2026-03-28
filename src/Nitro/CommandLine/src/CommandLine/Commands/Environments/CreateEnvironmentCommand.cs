using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Components;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Options;
using ChilliCream.Nitro.CommandLine.Configuration;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine.Commands.Environments;

internal sealed class CreateEnvironmentCommand : Command
{
    public CreateEnvironmentCommand() : base("create")
    {
        Description = "Creates a new environment";

        AddOption(Opt<EnvironmentNameOption>.Instance);
        AddOption(Opt<WorkspaceIdOption>.Instance);

        this.SetHandler(
            ExecuteAsync,
            Bind.FromServiceProvider<InvocationContext>(),
            Bind.FromServiceProvider<INitroConsole>(),
            Bind.FromServiceProvider<IEnvironmentsClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        INitroConsole console,
        IEnvironmentsClient client,
        CancellationToken cancellationToken)
    {
        var workspaceId = context.RequireWorkspaceId();

        console.WriteLine();
        console.WriteLine("Creating a environment");
        console.WriteLine();

        var name = await context.OptionOrAskAsync(
            "Name",
            Opt<EnvironmentNameOption>.Instance,
            cancellationToken);

        var environment = await client.CreateEnvironmentAsync(workspaceId, name, cancellationToken);
        console.PrintMutationErrorsAndExit(environment.Errors);

        var changeResult = environment.Changes?.SingleOrDefault();
        if (changeResult is null)
        {
            throw ThrowHelper.Exit("Could not create environment.");
        }

        if (changeResult.Error is IError error)
        {
            throw ThrowHelper.Exit(error.Message);
        }

        if (changeResult.Result is not IEnvironmentDetailPrompt_Environment detail)
        {
            throw ThrowHelper.Exit("Could not create environment.");
        }

        console.OkLine($"Environment {detail.Name.AsHighlight()} created");

        context.SetResult(EnvironmentDetailPrompt.From(detail).ToObject());

        return ExitCodes.Success;
    }
}
