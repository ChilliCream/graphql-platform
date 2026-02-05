using System.CommandLine.Invocation;
using ChilliCream.Nitro.CommandLine.Client;
using StrawberryShake;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class ConsoleHelpers
{
    public static void EnsureNoErrors<T>(
        this IAnsiConsole console,
        IOperationResult<T> result) where T : class
    {
        if (result.Errors is { Count: > 0 })
        {
            var firstError = result.Errors[0];
            console.WriteLine($"{firstError.Message} ({firstError.Code})");

            throw new ExitException();
        }
    }

    public static T EnsureData<T>(this IAnsiConsole console, IOperationResult<T> result)
        where T : class
    {
        if (result.Data is null)
        {
            console.WriteLine($"{Errors.BA00001Message} ({Errors.BA00001})");

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
        console.Write(tree);
    }

    private static void PrintError(
        this IAnsiConsole console,
        ISchemaChangeViolationError error)
    {
        var tree = new Tree("");
        tree.AddSchemaChanges(error.Changes.OfType<ISchemaChange>());
        console.Write(tree);
    }

    private static void PrintError(
        this IAnsiConsole console,
        IStagesHavePublishedDependenciesError error)
    {
        console.WriteLine(error.Message);
        console.WriteLine();

        foreach (var stage in error.Stages)
        {
            if (stage.PublishedSchema?.Version is { Tag: var tag })
            {
                console.WriteLine(
                    $"The schema {tag.AsHighlight()} is still published to {stage.Name.AsHighlight()}");
            }

            foreach (var publishedClient in stage.PublishedClients)
            {
                var tags = string.Join(
                    ',',
                    publishedClient.PublishedVersions.Select(x => x.Version?.Tag));
                console.WriteLine(
                    $"The client {publishedClient.Client.Name.AsHighlight()} in version {tags.AsHighlight()} is still published to {stage.Name.AsHighlight()}");
            }
        }
    }

    private static void PrintError(this IAnsiConsole console, IPersistedQueryValidationError error)
    {
        console.WarningLine(
            $"There were errors on client {error.Client?.Name.AsHighlight()} [dim](ID: {error.Client?.Id})[/]");

        console.WriteLine(error.Message);

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

        console.Write(node);
    }

    private static void PrintError(this IAnsiConsole console, IOpenApiCollectionValidationError error)
    {
        foreach (var collectionError in error.Collections)
        {
            var openApiCollection = collectionError.OpenApiCollection;

            console.WarningLine(
                $"There were errors in the OpenAPI collection '{openApiCollection?.Name.AsHighlight()}' [dim](ID: {openApiCollection?.Id})[/]");

            var node = new Tree("");
            foreach (var entity in collectionError.Entities)
            {
                var entityNode = node.AddNode(GetEntityNodeHeading(entity));

                foreach (var entityError in entity.Errors)
                {
                    if (entityError is
                        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_OpenApiCollectionValidationDocumentError
                        documentError)
                    {
                        var errorLocation = string.Empty;
                        if (documentError.Locations is { Count: > 0 } locations)
                        {
                            errorLocation = $"[grey]({locations[0].Line}:{locations[0].Column})[/]";
                        }

                        entityNode.AddNode($"{documentError.Message.EscapeMarkup()} {errorLocation}");
                    }
                    else if (entityError is
                        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_OpenApiCollectionValidationEntityValidationError
                        entityValidationError)
                    {
                        entityNode.AddNode(entityValidationError.Message.EscapeMarkup());
                    }
                    else
                    {
                        entityNode.AddNode("Unknown error type");
                    }
                }
            }

            console.Write(node);
        }

        static string GetEntityNodeHeading(
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities
                entity)
        {
            var heading = entity switch
            {
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_OpenApiCollectionValidationEndpoint endpoint
                    => $"Endpoint '{endpoint.HttpMethod} {endpoint.Route}'",
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_OpenApiCollectionValidationModel model
                    => $"Model '{model.Name}'",
                _ => "Unknown entity type"
            };

            return $"[red]{heading}[/]";
        }
    }

    private static void PrintError(this IAnsiConsole console, IMcpFeatureCollectionValidationError error)
    {
        foreach (var collectionError in error.Collections)
        {
            var mcpFeatureCollection = collectionError.McpFeatureCollection;

            console.WarningLine(
                $"There were errors in the MCP Feature Collection '{mcpFeatureCollection?.Name.AsHighlight()}' [dim](ID: {mcpFeatureCollection?.Id})[/]");

            var node = new Tree("");
            foreach (var entity in collectionError.Entities)
            {
                var entityNode = node.AddNode(GetEntityNodeHeading(entity));

                foreach (var entityError in entity.Errors)
                {
                    if (entityError is
                        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_McpFeatureCollectionValidationDocumentError
                        documentError)
                    {
                        var errorLocation = string.Empty;
                        if (documentError.Locations is { Count: > 0 } locations)
                        {
                            errorLocation = $"[grey]({locations[0].Line}:{locations[0].Column})[/]";
                        }

                        entityNode.AddNode($"{documentError.Message.EscapeMarkup()} {errorLocation}");
                    }
                    else if (entityError is
                        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_McpFeatureCollectionValidationEntityValidationError
                        entityValidationError)
                    {
                        entityNode.AddNode(entityValidationError.Message.EscapeMarkup());
                    }
                    else
                    {
                        entityNode.AddNode("Unknown error type");
                    }
                }
            }

            console.Write(node);
        }

        static string GetEntityNodeHeading(
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_1
                entity)
        {
            var heading = entity switch
            {
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_McpFeatureCollectionValidationPrompt prompt
                    => $"Prompt '{prompt.Name}'",
                IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_McpFeatureCollectionValidationTool tool
                    => $"Tool '{tool.Name}'",
                _ => "Unknown entity type"
            };

            return $"[red]{heading}[/]";
        }
    }

    private static void PrintError(
        this IAnsiConsole console,
        IInvalidGraphQLSchemaError error)
    {
        console.WriteLine(
            "The schema you are trying to publish is invalid. Please fix the following errors:");

        console.WriteLine(error.Message);

        var node = new Tree("");
        foreach (var query in error.Errors)
        {
            node.AddNode($"[red]{query.Message.EscapeMarkup()}[/] [grey]{query.Code}[/]");
        }

        console.Write(node);
    }

    private static void PrintInvalidOpenApiCollectionArchiveError(this IAnsiConsole console, string message)
    {
        console.WriteLine(
            "The server received an invalid archive. "
            + "This indicates a bug in the tooling. "
            + "Please notify ChilliCream."
            + "Error received: " + message);
    }

    private static void PrintInvalidMcpFeatureCollectionArchiveError(this IAnsiConsole console, string message)
    {
        console.WriteLine(
            "The server received an invalid archive. "
            + "This indicates a bug in the tooling. "
            + "Please notify ChilliCream."
            + "Error received: " + message);
    }

    private static void PrintMutationError(this IAnsiConsole ansiConsole, object error)
    {
        switch (error)
        {
            case IOperationsAreNotAllowedError err:
                ansiConsole.WriteLine(err.Message);
                break;

            case IConcurrentOperationError err:
                ansiConsole.WriteLine(err.Message);
                break;

            case IUnexpectedProcessingError err:
                ansiConsole.WriteLine(err.Message);
                break;

            case IProcessingTimeoutError err:
                ansiConsole.WriteLine(err.Message);
                break;

            case ISchemaVersionChangeViolationError err:
                ansiConsole.PrintError(err);
                break;

            case ISchemaVersionSyntaxError err:
                ansiConsole.WriteLine(err.Message);
                break;

            case IPersistedQueryValidationError err:
                ansiConsole.PrintError(err);
                break;

            case IStagesHavePublishedDependenciesError err:
                ansiConsole.PrintError(err);
                break;

            case IApiNotFoundError err:
                ansiConsole.WriteLine(err.Message);
                break;

            case IMockSchemaNonUniqueNameError err:
                ansiConsole.WriteLine(err.Message);
                break;

            case IMockSchemaNotFoundError err:
                ansiConsole.WriteLine(err.Message);
                break;

            case IStageNotFoundError err:
                ansiConsole.WriteLine(err.Message);
                break;

            case ISubgraphInvalidError err:
                ansiConsole.WriteLine(err.Message);
                break;

            case IInvalidGraphQLSchemaError err:
                ansiConsole.PrintError(err);
                break;

            case ISchemaChangeViolationError err:
                ansiConsole.PrintError(err);
                break;

            case IInvalidFusionSourceSchemaArchiveError err:
                ansiConsole.WriteLine(
                    "The server received an invalid archive. "
                    + "This indicates a bug in the tooling. "
                    + "Please notify ChilliCream."
                    + "Error received: " + err.Message);
                break;

            case IOpenApiCollectionValidationError err:
                ansiConsole.PrintError(err);
                break;

            case IInvalidOpenApiCollectionArchiveError err:
                ansiConsole.PrintInvalidOpenApiCollectionArchiveError(err.Message);
                break;

            case IOpenApiCollectionValidationArchiveError err:
                ansiConsole.PrintInvalidOpenApiCollectionArchiveError(err.Message);
                break;

            case IMcpFeatureCollectionValidationError err:
                ansiConsole.PrintError(err);
                break;

            case IInvalidMcpFeatureCollectionArchiveError err:
                ansiConsole.PrintInvalidMcpFeatureCollectionArchiveError(err.Message);
                break;

            case IMcpFeatureCollectionValidationArchiveError err:
                ansiConsole.PrintInvalidMcpFeatureCollectionArchiveError(err.Message);
                break;

            case IError err:
                ansiConsole.WriteLine(err.Message);
                break;

            default:
                ansiConsole.WriteLine("Unexpected Error");
                break;
        }
    }

    public static void Log(this IAnsiConsole console, string str)
    {
        console.MarkupLine("[grey]LOG: [/]" + str);
    }

    public static Status DefaultStatus(this IAnsiConsole console)
    {
        return console.Status()
            .Spinner(Spinner.Known.BouncingBar)
            .SpinnerStyle(Style.Parse("green bold"));
    }

    public static void Title(this IAnsiConsole console, string str)
    {
        console.MarkupLineInterpolated($"[white bold]{str}:[/]");
        console.WriteLine();
    }

    public static void Success(this IAnsiConsole console, string message)
    {
        console.MarkupLine($"[green bold]{message}[/]");
    }

    public static void OkLine(this IAnsiConsole console, string message)
    {
        console.MarkupLine(Glyphs.Check.Space() + message);
    }

    public static void ErrorLine(this IAnsiConsole console, string message)
    {
        console.MarkupLine(Glyphs.Cross.Space() + message);
    }

    public static void ErrorLine(this TextWriter textWriter, string message)
    {
        textWriter.WriteLine("❌ " + message);
    }

    public static void OkQuestion(this IAnsiConsole console, string question, string result)
    {
        console.MarkupLine(
            $"{Glyphs.QuestionMark.Space()}[bold]{question}[/]: [darkseagreen4]{result}[/]");
    }

    public static async Task<string> OptionOrAskAsync(
        this InvocationContext context,
        string question,
        Option<string> option,
        string? defaultValue,
        CancellationToken cancellationToken)
    {
        var value = context.ParseResult.GetValueForOption(option);

        if (value is not null)
        {
            return value;
        }

        var console = context.BindingContext.GetRequiredService<IAnsiConsole>();

        var prompt = new TextPrompt<string>(question.AsQuestion());

        if (defaultValue is not null)
        {
            prompt = prompt.DefaultValue(defaultValue);
        }

        return await prompt.ShowAsync(console, cancellationToken);
    }

    public static Task<string> OptionOrAskAsync(
        this InvocationContext context,
        string question,
        Option<string> option,
        CancellationToken cancellationToken)
        => OptionOrAskAsync(context, question, option, defaultValue: null, cancellationToken);

    public static async Task<string> AskAsync(
        this IAnsiConsole console,
        string question,
        string defaultValue,
        CancellationToken cancellationToken)
    {
        var questionText = $"{question}".AsQuestion();
        var prompt = new TextPrompt<string>(questionText).DefaultValue(defaultValue);
        return await prompt.ShowAsync(console, cancellationToken);
    }

    public static async Task<bool> ConfirmAsync(
        this IAnsiConsole console,
        string question,
        CancellationToken cancellationToken)
        => await new ConfirmationPrompt(question.AsQuestion())
            .ShowAsync(console, cancellationToken);

    public static async Task<bool> OptionOrConfirmAsync(
        this InvocationContext context,
        string question,
        Option<bool?> option,
        CancellationToken cancellationToken)
    {
        var value = context.ParseResult.GetValueForOption(option);

        if (value is not null)
        {
            return value.Value;
        }

        var console = context.BindingContext.GetRequiredService<IAnsiConsole>();

        return await new ConfirmationPrompt(question.AsQuestion())
            .ShowAsync(console, cancellationToken);
    }

    public static void WarningLine(this IAnsiConsole console, string message)
    {
        console.MarkupLine(Glyphs.ExclamationMark.Space() + message);
    }

    public static bool IsHumanReadable(this IAnsiConsole console)
    {
        return console is IExtendedConsole { IsInteractive: true };
    }

    public static IDisposable UseInteractive(this IAnsiConsole console)
    {
        return new InteractiveScope(console);
    }

    private sealed class InteractiveScope : IDisposable
    {
        private readonly IAnsiConsole _console;
        private readonly bool _originalValue;

        public InteractiveScope(IAnsiConsole console)
        {
            _console = console;

            if (_console is IExtendedConsole customConsole)
            {
                _originalValue = customConsole.IsInteractive;
                customConsole.IsInteractive = true;
            }
        }

        public void Dispose()
        {
            if (_console is IExtendedConsole customConsole)
            {
                customConsole.IsInteractive = _originalValue;
            }
        }
    }
}
