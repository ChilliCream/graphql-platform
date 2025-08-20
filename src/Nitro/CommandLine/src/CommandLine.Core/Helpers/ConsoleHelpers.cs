using System.CommandLine.Invocation;

namespace ChilliCream.Nitro.CommandLine;

public static class ConsoleHelpers
{
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

    public static void OkQuestion(this IAnsiConsole console, string question, string result)
    {
        console.MarkupLine(
            $"{Glyphs.QuestionMark.Space()}[bold]{question}[/]: [darkseagreen4]{result}[/]");
    }

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
