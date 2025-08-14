using System.CommandLine.Invocation;
using System.Text.Json;
using ChilliCream.Nitro.CLI.Client;
using ChilliCream.Nitro.CLI.Exceptions;
using ChilliCream.Nitro.CLI.Results;
using Spectre.Console.Json;
using StrawberryShake;

namespace ChilliCream.Nitro.CLI;

internal static class ConsoleHelpers
{
    public static void PrintHeader(this IAnsiConsole console)
    {
        console.Write(new FigletText("Nitro").Color(Color.Red).Centered());
        console.Write(new Rule("[white]By ChilliCream[/]"));
        console.WriteLine();
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

    public static void Json(this IAnsiConsole console, object obj)
    {
        // TODO enable for full AOT compliancy
        // console.Write(new JsonText(
        //     JsonSerializer.Serialize(obj,
        //         new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
        console.WriteLine("NOT SUPPORTED");
    }

    public static void ErrorLine(this IAnsiConsole console, string message)
    {
        console.MarkupLine(Glyphs.Cross.Space() + message);
    }

    public static void OkQuestion(this IAnsiConsole console, string question, string result)
    {
        console.MarkupLine(
            $"{Glyphs.QuestionMark.Space()}[bold]{question}[/]: [darkseagreen4]{result}[/]");
    }

    public static async Task<string> AskAsync(
        this IAnsiConsole console,
        string question,
        CancellationToken cancellationToken)
        => await new TextPrompt<string>(question.AsQuestion())
            .ShowAsync(console, cancellationToken);

    public static async Task<string> OptionOrAskAsync(
        this InvocationContext context,
        string question,
        Option<string> option,
        CancellationToken cancellationToken)
    {
        var value = context.ParseResult.GetValueForOption(option);

        if (value is not null)
        {
            return value;
        }

        var console = context.BindingContext.GetRequiredService<IAnsiConsole>();

        return await new TextPrompt<string>(question.AsQuestion())
            .ShowAsync(console, cancellationToken);
    }

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

    public static void Error(this IAnsiConsole console, string message)
    {
        console.MarkupLine($"[red bold]{message}[/]");
    }

    public static void EnsureNoErrors<T>(
        this IAnsiConsole console,
        IOperationResult<T> result) where T : class
    {
        if (result.Errors is { Count: > 0 })
        {
            console.PrintError(result.Errors[0]);
            throw new ExitException();
        }
    }

    public static T EnsureData<T>(this IAnsiConsole console, IOperationResult<T> result)
        where T : class
    {
        if (result.Data is null)
        {
            console.PrintError(Errors.BA00001Message, Errors.BA00001);

            throw new ExitException();
        }

        return result.Data;
    }

    public static void PrintError(this IAnsiConsole console, string message, string? code = null)
    {
        if (code is not null)
        {
            console.MarkupLineInterpolated(
                $"[red][bold]Error[/]: {message}[/][grey] ({code})[/]");
        }
        else
        {
            console.MarkupLineInterpolated($"[red][bold]Error[/]: {message}[/]");
        }
    }

    public static bool IsHumandReadable(this IAnsiConsole console)
    {
        return console is IExtendedConsole { IsInteractive: true };
    }

    public static IDisposable UseInteractive(this IAnsiConsole console)
    {
        return new InteractiveScope(console);
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

    private static void PrintError(this IAnsiConsole console, IClientError error)
    {
        console.PrintError(error.Message, error.Code);
    }

    private static void PrintError(this IAnsiConsole console, IError error)
    {
        console.PrintError(error.Message);
    }

    private static void PrintError(this IAnsiConsole console, IOperationsAreNotAllowedError error)
    {
        console.PrintError(error.Message);
    }

    private static void PrintError(this IAnsiConsole console, IConcurrentOperationError error)
    {
        console.PrintError(error.Message);
    }

    private static void PrintError(this IAnsiConsole console, IUnexpectedProcessingError error)
    {
        console.PrintError(error.Message);
    }

    private static void PrintError(this IAnsiConsole console, IProcessingTimeoutError error)
    {
        console.PrintError(error.Message);
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

    private static void PrintError(this IAnsiConsole console, ISchemaVersionSyntaxError error)
    {
        console.PrintError(error.Message);
    }

    private static void PrintError(
        this IAnsiConsole console,
        IStagesHavePublishedDependenciesError error)
    {
        console.PrintError(error.Message);
        console.WriteLine();
        foreach (var stage in error.Stages)
        {
            if (stage.PublishedSchema?.Version is { Tag: var tag })
            {
                console.ErrorLine(
                    $"The schema {tag.AsHighlight()} is still published to {stage.Name.AsHighlight()}");
            }

            foreach (var publishedClient in stage.PublishedClients)
            {
                var tags = string.Join(
                    ',',
                    publishedClient.PublishedVersions.Select(x => x.Version?.Tag));
                console.ErrorLine(
                    $"The client {publishedClient.Client.Name.AsHighlight()} in version {tags.AsHighlight()} is still published to {stage.Name.AsHighlight()}");
            }
        }
    }

    private static void PrintError(this IAnsiConsole console, IPersistedQueryValidationError error)
    {
        console.WarningLine(
            $"There were errors on client {error.Client?.Name.AsHighlight()} [dim](ID: {error.Client?.Id})[/]");
        console.PrintError(error.Message);
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

    private static void PrintError(
        this IAnsiConsole console,
        IInvalidGraphQLSchemaError error)
    {
        console.ErrorLine(
            "The schema you are trying to publish is invalid. Please fix the following errors:");

        console.PrintError(error.Message);

        var node = new Tree("");
        foreach (var query in error.Errors)
        {
            node.AddNode($"[red]{query.Message.EscapeMarkup()}[/] [grey]{query.Code}[/]");
        }

        console.Write(node);
    }

    private static void PrintMutationError(this IAnsiConsole ansiConsole, object error)
    {
        switch (error)
        {
            case IOperationsAreNotAllowedError err:
                ansiConsole.PrintError(err);
                break;

            case IConcurrentOperationError err:
                ansiConsole.PrintError(err);
                break;

            case IUnexpectedProcessingError err:
                ansiConsole.PrintError(err);
                break;

            case IProcessingTimeoutError err:
                ansiConsole.PrintError(err);
                break;

            case ISchemaVersionChangeViolationError err:
                ansiConsole.PrintError(err);
                break;

            case ISchemaVersionSyntaxError err:
                ansiConsole.PrintError(err);
                break;

            case IPersistedQueryValidationError err:
                ansiConsole.PrintError(err);
                break;

            case IStagesHavePublishedDependenciesError err:
                ansiConsole.PrintError(err);
                break;

            case IApiNotFoundError err:
                ansiConsole.PrintError(err);
                break;

            case IMockSchemaNonUniqueNameError err:
                ansiConsole.PrintError(err);
                break;

            case IMockSchemaNotFoundError err:
                ansiConsole.PrintError(err);
                break;

            case IStageNotFoundError err:
                ansiConsole.PrintError(err);
                break;

            case ISubgraphInvalidError err:
                ansiConsole.PrintError(err);
                break;

            case IInvalidGraphQLSchemaError err:
                ansiConsole.PrintError(err);
                break;

            case IError err:
                ansiConsole.PrintError(err);
                break;

            case ISchemaChangeViolationError err:
                ansiConsole.PrintError(err);
                break;

            default:
                ansiConsole.Error("Unexpected Error");
                break;
        }
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
