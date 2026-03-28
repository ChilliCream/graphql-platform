namespace ChilliCream.Nitro.CommandLine;

internal interface INitroConsole
{
    bool IsInteractive { get; }

    void WriteLine(string? message = null);

    void WriteErrorLine(string? message = null);

    Task<string> PromptAsync(
        string question,
        string? defaultValue,
        CancellationToken cancellationToken);

    Task<T> PromptAsync<T>(
        string question,
        T[] items,
        CancellationToken cancellationToken)
        where T : notnull;

    Task<bool> ConfirmAsync(string question, CancellationToken cancellationToken);
}
