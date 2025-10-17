using ChilliCream.Nitro.CommandLine.Cloud.Client;
using StrawberryShake;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal static class ConsoleHelpers
{
    public static void EnsureNoErrors<T>(
        this IAnsiConsole console,
        IOperationResult<T> result) where T : class
    {
        if (result.Errors is { Count: > 0 })
        {
            var firstError = result.Errors[0];
            console.Error.WriteLine($"{firstError.Message} ({firstError.Code})");

            throw new ExitException();
        }
    }

    public static T EnsureData<T>(this IAnsiConsole console, IOperationResult<T> result)
        where T : class
    {
        if (result.Data is null)
        {
            console.Error.WriteLine($"{Errors.BA00001Message} ({Errors.BA00001})");

            throw new ExitException();
        }

        return result.Data;
    }

    public static void PrintErrorsAndExit<T>(this IAnsiConsole console, IReadOnlyList<T>? errors)
        where T : class
    {
        if (errors?.Count > 0)
        {
            // we need to enable interactive mode to print errors
            using var _ = console.UseInteractive();

            console.WriteLine();

            foreach (var error in errors)
            {
                console.PrintMutationError(error);
            }

            throw new ExitException();
        }
    }

    public static void PrintErrors<T>(this IAnsiConsole console, IReadOnlyList<T>? errors)
        where T : class
    {
        if (errors?.Count > 0)
        {
            // we need to enable interactive mode to print errors
            using var _ = console.UseInteractive();

            console.WriteLine();

            foreach (var error in errors)
            {
                console.PrintMutationError(error);
            }
        }
    }

    private static void PrintError(
        this IAnsiConsole console,
        ISchemaVersionChangeViolationError error)
    {
        var tree = new Tree("");
        tree.AddSchemaChanges(error.Changes.OfType<ISchemaChange>());
        // TODO: This needs to write to stderr
        console.Write(tree);
    }

    private static void PrintError(
        this IAnsiConsole console,
        ISchemaChangeViolationError error)
    {
        var tree = new Tree("");
        tree.AddSchemaChanges(error.Changes.OfType<ISchemaChange>());
        // TODO: This needs to write to stderr
        console.Write(tree);
    }

    private static void PrintError(
        this IAnsiConsole console,
        IStagesHavePublishedDependenciesError error)
    {
        console.Error.WriteLine(error.Message);
        console.Error.WriteLine();

        foreach (var stage in error.Stages)
        {
            if (stage.PublishedSchema?.Version is { Tag: var tag })
            {
                // TODO: This needs to write to stderr
                console.ErrorLine(
                    $"The schema {tag.AsHighlight()} is still published to {stage.Name.AsHighlight()}");
            }

            foreach (var publishedClient in stage.PublishedClients)
            {
                var tags = string.Join(
                    ',',
                    publishedClient.PublishedVersions.Select(x => x.Version?.Tag));
                // TODO: This needs to write to stderr
                console.ErrorLine(
                    $"The client {publishedClient.Client.Name.AsHighlight()} in version {tags.AsHighlight()} is still published to {stage.Name.AsHighlight()}");
            }
        }
    }

    private static void PrintError(this IAnsiConsole console, IPersistedQueryValidationError error)
    {
        // TODO: This needs to write to stderr
        console.WarningLine(
            $"There were errors on client {error.Client?.Name.AsHighlight()} [dim](ID: {error.Client?.Id})[/]");

        console.Error.WriteLine(error.Message);

        var node = new Tree("");
        foreach (var query in error.Queries)
        {
            var publishingInfo = query.DeployedTags.Count > 0
                ? $" [dim](Deployed tags: {string.Join(",", query.DeployedTags)})[/]"
                : "";

            var queryNode = node.AddNode(
                $"[red]{query.Message.EscapeMarkup().Replace(query.Hash, $"[bold]{query.Hash}[/]{publishingInfo}")}[/]");

            foreach (var err in query.Errors)
            {
                var errorLocation = string.Empty;
                if (err.Locations is { Count: > 0 } locations)
                {
                    errorLocation = $"[grey]({locations[0].Line}:{locations[0].Column})[/]";
                }

                queryNode.AddNode($"{err.Message.EscapeMarkup()} {errorLocation}");
            }
        }

        // TODO: This needs to write to stderr
        console.Write(node);
    }

    private static void PrintError(
        this IAnsiConsole console,
        IInvalidGraphQLSchemaError error)
    {
        console.Error.ErrorLine(
            "The schema you are trying to publish is invalid. Please fix the following errors:");

        console.Error.WriteLine(error.Message);

        var node = new Tree("");
        foreach (var query in error.Errors)
        {
            node.AddNode($"[red]{query.Message.EscapeMarkup()}[/] [grey]{query.Code}[/]");
        }

        // TODO: This needs to write to stderr
        console.Write(node);
    }

    private static void PrintMutationError(this IAnsiConsole ansiConsole, object error)
    {
        switch (error)
        {
            case IOperationsAreNotAllowedError err:
                ansiConsole.Error.WriteLine(err.Message);
                break;

            case IConcurrentOperationError err:
                ansiConsole.Error.WriteLine(err.Message);
                break;

            case IUnexpectedProcessingError err:
                ansiConsole.Error.WriteLine(err.Message);
                break;

            case IProcessingTimeoutError err:
                ansiConsole.Error.WriteLine(err.Message);
                break;

            case ISchemaVersionChangeViolationError err:
                ansiConsole.PrintError(err);
                break;

            case ISchemaVersionSyntaxError err:
                ansiConsole.Error.WriteLine(err.Message);
                break;

            case IPersistedQueryValidationError err:
                ansiConsole.PrintError(err);
                break;

            case IStagesHavePublishedDependenciesError err:
                ansiConsole.PrintError(err);
                break;

            case IApiNotFoundError err:
                ansiConsole.Error.WriteLine(err.Message);
                break;

            case IMockSchemaNonUniqueNameError err:
                ansiConsole.Error.WriteLine(err.Message);
                break;

            case IMockSchemaNotFoundError err:
                ansiConsole.Error.WriteLine(err.Message);
                break;

            case IStageNotFoundError err:
                ansiConsole.Error.WriteLine(err.Message);
                break;

            case ISubgraphInvalidError err:
                ansiConsole.Error.WriteLine(err.Message);
                break;

            case IInvalidGraphQLSchemaError err:
                ansiConsole.PrintError(err);
                break;

            case IError err:
                ansiConsole.Error.WriteLine(err.Message);
                break;

            case ISchemaChangeViolationError err:
                ansiConsole.PrintError(err);
                break;

            default:
                ansiConsole.Error.WriteLine("Unexpected Error");
                break;
        }
    }
}
