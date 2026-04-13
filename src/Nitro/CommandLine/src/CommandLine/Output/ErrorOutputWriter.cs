using System.Text.Json.Serialization.Metadata;

namespace ChilliCream.Nitro.CommandLine.Output;

/// <summary>
/// Shared helper that renders an <see cref="OutputEnvelope{T}"/> error payload in the
/// requested format. Keeps every analytical command's error rendering consistent.
/// </summary>
internal static class ErrorOutputWriter
{
    public static void Write<T>(
        INitroConsole console,
        OutputFormat format,
        OutputEnvelope<T> envelope,
        JsonTypeInfo<OutputEnvelope<T>> typeInfo)
    {
        var error = envelope.Error
            ?? throw new InvalidOperationException(
                "ErrorOutputWriter requires an envelope that carries an error payload.");

        switch (format)
        {
            case OutputFormat.Json:
                JsonOutputWriter.Write(console, envelope, typeInfo);
                break;

            case OutputFormat.Markdown:
                console.Out.WriteLine(RenderMarkdown(envelope, error));
                break;

            case OutputFormat.Table:
                console.Error.WriteErrorLine($"{error.Code}: {error.Message}");
                break;
        }
    }

    private static string RenderMarkdown<T>(OutputEnvelope<T> envelope, OutputEnvelopeError error)
    {
        var writer = new MarkdownWriter();

        writer.Frontmatter(
        [
            new KeyValuePair<string, string>("api", envelope.Api),
            new KeyValuePair<string, string>("stage", envelope.Stage),
            new KeyValuePair<string, string>(
                "window",
                $"{MarkdownWriter.FormatDate(envelope.Window.From)} to {MarkdownWriter.FormatDate(envelope.Window.To)}"),
            new KeyValuePair<string, string>("error", error.Code)
        ]);

        writer.Quote($"**Error:** {error.Message}");

        return writer.ToString();
    }
}
