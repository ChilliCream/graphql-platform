using System.CommandLine.Invocation;
using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class ConsoleHelpers
{
    public static void PrintMutationErrorsAndExit<T>(this INitroConsole console, IReadOnlyList<T>? errors)
        where T : class
    {
        if (errors?.Count > 0)
        {
            // TODO: This needs to write to stderr
            console.WriteLine();

            foreach (var error in errors)
            {
                console.PrintMutationError(error);
            }

            throw new ExitException();
        }
    }

    public static void PrintMutationErrors<T>(this INitroConsole console, IReadOnlyList<T>? errors)
        where T : class
    {
        if (errors?.Count > 0)
        {
            // TODO: Write to stderr
            console.WriteLine();

            foreach (var error in errors)
            {
                console.PrintMutationError(error);
            }
        }
    }

    private static void PrintMutationError(
        this INitroConsole console,
        ISchemaVersionChangeViolationError error)
    {
        var tree = new Tree("");
        tree.AddSchemaChanges(error.Changes.OfType<ISchemaChange>());
        console.Write(tree);
    }

    private static void PrintMutationError(
        this INitroConsole console,
        ISchemaChangeViolationError error)
    {
        var tree = new Tree("");
        tree.AddSchemaChanges(error.Changes.OfType<ISchemaChange>());
        console.Write(tree);
    }

    private static void PrintMutationError(
        this INitroConsole console,
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

    private static void PrintMutationError(this INitroConsole console, IPersistedQueryValidationError error)
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

    private static void PrintMutationError(this INitroConsole console, IOpenApiCollectionValidationError error)
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
                    if (entityError is IOpenApiCollectionValidationDocumentError documentError)
                    {
                        var errorLocation = string.Empty;
                        if (documentError.Locations is { Count: > 0 } locations)
                        {
                            errorLocation = $"[grey]({locations[0].Line}:{locations[0].Column})[/]";
                        }

                        entityNode.AddNode($"{documentError.Message.EscapeMarkup()} {errorLocation}");
                    }
                    else if (entityError is IOpenApiCollectionValidationEntityValidationError entityValidationError)
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

        static string GetEntityNodeHeading(IOpenApiCollectionValidationEntity entity)
        {
            var heading = entity switch
            {
                IOpenApiCollectionValidationEndpoint endpoint => $"Endpoint '{endpoint.HttpMethod} {endpoint.Route}'",
                IOpenApiCollectionValidationModel model => $"Model '{model.Name}'",
                _ => "Unknown entity type"
            };

            return $"[red]{heading}[/]";
        }
    }

    private static void PrintMutationError(this INitroConsole console, IMcpFeatureCollectionValidationError error)
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
                    if (entityError is IMcpFeatureCollectionValidationDocumentError documentError)
                    {
                        var errorLocation = string.Empty;
                        if (documentError.Locations is { Count: > 0 } locations)
                        {
                            errorLocation = $"[grey]({locations[0].Line}:{locations[0].Column})[/]";
                        }

                        entityNode.AddNode($"{documentError.Message.EscapeMarkup()} {errorLocation}");
                    }
                    else if (entityError is IMcpFeatureCollectionValidationEntityValidationError entityValidationError)
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

        static string GetEntityNodeHeading(IMcpFeatureCollectionValidationEntity entity)
        {
            var heading = entity switch
            {
                IMcpFeatureCollectionValidationPrompt prompt => $"Prompt '{prompt.Name}'",
                IMcpFeatureCollectionValidationTool tool => $"Tool '{tool.Name}'",
                _ => "Unknown entity type"
            };

            return $"[red]{heading}[/]";
        }
    }

    private static void PrintMutationError(
        this INitroConsole console,
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

    private static void PrintInvalidOpenApiCollectionArchiveError(this INitroConsole console, string message)
    {
        console.WriteLine(
            "The server received an invalid archive. "
            + "This indicates a bug in the tooling. "
            + "Please notify ChilliCream."
            + "Error received: "
            + message);
    }

    private static void PrintInvalidMcpFeatureCollectionArchiveError(this INitroConsole console, string message)
    {
        console.WriteLine(
            "The server received an invalid archive. "
            + "This indicates a bug in the tooling. "
            + "Please notify ChilliCream."
            + "Error received: "
            + message);
    }

    private static void PrintMutationError(this INitroConsole console, object error)
    {
        switch (error)
        {
            case IOperationsAreNotAllowedError err:
                console.WriteLine(err.Message);
                break;

            case IConcurrentOperationError err:
                console.WriteLine(err.Message);
                break;

            case IUnexpectedProcessingError err:
                console.WriteLine(err.Message);
                break;

            case IProcessingTimeoutError err:
                console.WriteLine(err.Message);
                break;

            case ISchemaVersionChangeViolationError err:
                console.PrintMutationError(err);
                break;

            case ISchemaVersionSyntaxError err:
                console.WriteLine(err.Message);
                break;

            case IPersistedQueryValidationError err:
                console.PrintMutationError(err);
                break;

            case IStagesHavePublishedDependenciesError err:
                console.PrintMutationError(err);
                break;

            case IApiNotFoundError err:
                console.WriteLine(err.Message);
                break;

            case IMockSchemaNonUniqueNameError err:
                console.WriteLine(err.Message);
                break;

            case IMockSchemaNotFoundError err:
                console.WriteLine(err.Message);
                break;

            case IStageNotFoundError err:
                console.WriteLine(err.Message);
                break;

            case ISubgraphInvalidError err:
                console.WriteLine(err.Message);
                break;

            case IInvalidGraphQLSchemaError err:
                console.PrintMutationError(err);
                break;

            case ISchemaChangeViolationError err:
                console.PrintMutationError(err);
                break;

            case IInvalidFusionSourceSchemaArchiveError err:
                console.WriteLine(
                    "The server received an invalid archive. "
                    + "This indicates a bug in the tooling. "
                    + "Please notify ChilliCream."
                    + "Error received: "
                    + err.Message);
                break;

            case IOpenApiCollectionValidationError err:
                console.PrintMutationError(err);
                break;

            case IInvalidOpenApiCollectionArchiveError err:
                console.PrintInvalidOpenApiCollectionArchiveError(err.Message);
                break;

            case IOpenApiCollectionValidationArchiveError err:
                console.PrintInvalidOpenApiCollectionArchiveError(err.Message);
                break;

            case IMcpFeatureCollectionValidationError err:
                console.PrintMutationError(err);
                break;

            case IInvalidMcpFeatureCollectionArchiveError err:
                console.PrintInvalidMcpFeatureCollectionArchiveError(err.Message);
                break;

            case IMcpFeatureCollectionValidationArchiveError err:
                console.PrintInvalidMcpFeatureCollectionArchiveError(err.Message);
                break;

            case IError err:
                console.WriteLine("Unexpected mutation error: " + err.Message);
                break;

            default:
                console.WriteLine("Unexpected mutation error");
                break;
        }
    }

    public static void Log(this INitroConsole console, string str)
    {
        console.MarkupLine("[grey]LOG: [/]" + str);
    }

    public static Status DefaultStatus(this INitroConsole console)
    {
        return console.Status()
            .Spinner(Spinner.Known.BouncingBar)
            .SpinnerStyle(Style.Parse("green bold"));
    }

    public static void Title(this INitroConsole console, string str)
    {
        console.MarkupLineInterpolated($"[white bold]{str}:[/]");
        console.WriteLine();
    }

    public static void Success(this INitroConsole console, string message)
    {
        console.MarkupLine($"[green bold]{message}[/]");
    }

    public static void OkLine(this INitroConsole console, string message)
    {
        console.MarkupLine(Glyphs.Check.Space() + message);
    }

    public static void ErrorLine(this INitroConsole console, string message)
    {
        console.MarkupLine(Glyphs.Cross.Space() + message);
    }

    public static void ErrorLine(this TextWriter textWriter, string message)
    {
        textWriter.WriteLine("❌ " + message);
    }

    public static void OkQuestion(this INitroConsole console, string question, string result)
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

        var console = context.BindingContext.GetRequiredService<INitroConsole>();

        if (!console.IsInteractive)
        {
            throw new ExitException($"Missing required option '--{option.Name}'.");
        }

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
        this INitroConsole console,
        string question,
        string defaultValue,
        CancellationToken cancellationToken)
    {
        var questionText = $"{question}".AsQuestion();
        var prompt = new TextPrompt<string>(questionText).DefaultValue(defaultValue);
        return await prompt.ShowAsync(console, cancellationToken);
    }

    public static async Task<bool> ConfirmAsync(
        this INitroConsole console,
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

        var console = context.BindingContext.GetRequiredService<INitroConsole>();

        return await new ConfirmationPrompt(question.AsQuestion())
            .ShowAsync(console, cancellationToken);
    }

    public static void WarningLine(this INitroConsole console, string message)
    {
        console.MarkupLine(Glyphs.ExclamationMark.Space() + message);
    }
}
