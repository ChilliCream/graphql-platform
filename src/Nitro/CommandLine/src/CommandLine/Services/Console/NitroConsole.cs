using ChilliCream.Nitro.CommandLine.Helpers;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsole(IAnsiConsole console) : INitroConsole
{
    public Profile Profile => console.Profile;

    public IExclusivityMode ExclusivityMode => console.ExclusivityMode;

    public IAnsiConsoleInput Input => console.Input;

    public RenderPipeline Pipeline => console.Pipeline;

    public IAnsiConsoleCursor Cursor => console.Cursor;

    public bool IsInteractive
        => console is not IExtendedConsole extendedConsole || extendedConsole.IsInteractive;

    public void Clear(bool home)
        => console.Clear(home);

    public void Write(IRenderable renderable)
        => console.Write(renderable);

    public void WriteLine(string message)
    {
        console.WriteLine(message);
    }

    // TODO: Should write to stderr
    public void WriteErrorLine(string message)
    {
        console.WriteLine(message);
    }

    public async Task<string> PromptAsync(
        string question,
        string? defaultValue,
        CancellationToken cancellationToken)
    {
        if (!IsInteractive)
        {
            throw new ExitException(
                "Attempted to prompt the user for input, but the console is running in non-interactive mode.");
        }

        var prompt = new TextPrompt<string>(question.AsQuestion());

        if (defaultValue is not null)
        {
            prompt = prompt.DefaultValue(defaultValue);
        }

        return await prompt.ShowAsync(console, cancellationToken);
    }

    public async Task<T> PromptAsync<T>(
        string question,
        T[] items,
        CancellationToken cancellationToken)
        where T : notnull
    {
        if (!IsInteractive)
        {
            throw new ExitException(
                "Attempted to prompt the user for a selection, but the console is running in non-interactive mode.");
        }

        var prompt = new SelectionPrompt<T>()
            .Title(question.AsQuestion())
            .AddChoices(items);

        return await prompt.ShowAsync(console, cancellationToken);
    }

    public async Task<bool> ConfirmAsync(string question, CancellationToken cancellationToken)
    {
        if (!IsInteractive)
        {
            throw new ExitException(
                "Attempted to prompt the user for confirmation, but the console is running in non-interactive mode.");
        }

        return await new ConfirmationPrompt(question.AsQuestion())
            .ShowAsync(console, cancellationToken);
    }
}
