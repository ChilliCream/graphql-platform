using ChilliCream.Nitro.CommandLine.Output;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;

/// <summary>
/// Renders the payload of <c>nitro schema unused</c> across all three output formats.
/// </summary>
internal sealed class UnusedCoordinatesFormatter : IOutputFormatter<UnusedCoordinatesResult>
{
    private readonly OutputFormat _format;

    public UnusedCoordinatesFormatter(OutputFormat format)
    {
        _format = format;
    }

    public void Write(INitroConsole console, OutputEnvelope<UnusedCoordinatesResult> envelope)
    {
        switch (_format)
        {
            case OutputFormat.Table:
                WriteTable(console, envelope);
                break;

            case OutputFormat.Markdown:
                console.Out.WriteLine(RenderMarkdown(envelope));
                break;

            case OutputFormat.Json:
                JsonOutputWriter.Write(
                    console,
                    envelope,
                    UnusedCoordinatesJsonContext.Default.OutputEnvelopeUnusedCoordinatesResult);
                break;
        }
    }

    public void WriteError(INitroConsole console, OutputEnvelope<UnusedCoordinatesResult> envelope)
    {
        ErrorOutputWriter.Write(
            console,
            _format,
            envelope,
            UnusedCoordinatesJsonContext.Default.OutputEnvelopeUnusedCoordinatesResult);
    }

    private static string RenderMarkdown(OutputEnvelope<UnusedCoordinatesResult> envelope)
    {
        var writer = new MarkdownWriter();
        var data = envelope.Data ?? throw new InvalidOperationException(
            "UnusedCoordinatesFormatter cannot render markdown without a data payload.");

        writer.Frontmatter(
        [
            new KeyValuePair<string, string>("api", envelope.Api),
            new KeyValuePair<string, string>("stage", envelope.Stage),
            new KeyValuePair<string, string>(
                "window",
                $"{MarkdownWriter.FormatDate(envelope.Window.From)} to {MarkdownWriter.FormatDate(envelope.Window.To)}"),
            new KeyValuePair<string, string>("limit", data.Limit.ToString())
        ]);

        writer.Heading($"Unused coordinates ({data.Coordinates.Count})");

        var rows = new List<IReadOnlyList<string>>();
        foreach (var entry in data.Coordinates)
        {
            rows.Add(
            [
                entry.Coordinate,
                entry.IsDeprecated ? "yes" : "no",
                MarkdownWriter.FormatDate(entry.LastSeen)
            ]);
        }

        writer.Table(["Coordinate", "Deprecated", "Last seen"], rows);

        return writer.ToString();
    }

    private static void WriteTable(
        INitroConsole console,
        OutputEnvelope<UnusedCoordinatesResult> envelope)
    {
        var data = envelope.Data ?? throw new InvalidOperationException(
            "UnusedCoordinatesFormatter cannot render a table without a data payload.");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]Unused coordinates ({data.Coordinates.Count})[/]")
            .AddColumn("Coordinate")
            .AddColumn("Deprecated")
            .AddColumn("Last seen");

        foreach (var entry in data.Coordinates)
        {
            table.AddRow(
                entry.Coordinate.EscapeMarkup(),
                entry.IsDeprecated ? "yes" : "no",
                MarkdownWriter.FormatDate(entry.LastSeen));
        }

        console.Out.Write(table);
    }
}
