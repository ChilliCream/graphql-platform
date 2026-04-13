namespace ChilliCream.Nitro.CommandLine.Output;

/// <summary>
/// The output format used by analytical commands.
/// </summary>
internal enum OutputFormat
{
    /// <summary>
    /// Spectre.Console table rendered for human consumption. Default when stdout is a TTY.
    /// </summary>
    Table,

    /// <summary>
    /// Versioned JSON envelope intended for scripting and tool-use protocols. Default when
    /// stdout is piped or redirected.
    /// </summary>
    Json,

    /// <summary>
    /// GitHub-flavoured Markdown with frontmatter intended for coding agents to paste back
    /// into their context window.
    /// </summary>
    Markdown
}
