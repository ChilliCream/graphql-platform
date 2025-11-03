using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Cloud.Client;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;
using static ChilliCream.Nitro.CommandLine.Cloud.ThrowHelper;

namespace ChilliCream.Nitro.CommandLine.Cloud.Commands.Environment;

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
            Bind.FromServiceProvider<IAnsiConsole>(),
            Bind.FromServiceProvider<IApiClient>(),
            Bind.FromServiceProvider<CancellationToken>());
    }

    private static async Task<int> ExecuteAsync(
        InvocationContext context,
        IAnsiConsole console,
        IApiClient client,
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

        var result = await client.CreateEnvironmentCommandMutation
            .ExecuteAsync(workspaceId, name, cancellationToken);

        console.EnsureNoErrors(result);
        var data = console.EnsureData(result);
        console.PrintErrorsAndExit(data.PushWorkspaceChanges.Errors);

        var changeResult = data.PushWorkspaceChanges.Changes?.SingleOrDefault();
        if (changeResult is null)
        {
            throw Exit("Could not create environment.");
        }

        if (changeResult.Error is IError error)
        {
            throw Exit(error.Message);
        }

        if (changeResult.Result is not ICreateEnvironmentCommandMutation_Environment environment)
        {
            throw Exit("Could not create environment.");
        }

        console.OkLine($"Environment {environment.Name.AsHighlight()} created");

        if (changeResult.Result is IEnvironmentDetailPrompt_Environment detail)
        {
            context.SetResult(EnvironmentDetailPrompt.From(detail).ToObject());
        }

        return ExitCodes.Success;
    }
}
