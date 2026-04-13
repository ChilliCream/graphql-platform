using ChilliCream.Nitro.CommandLine.Output;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;

/// <summary>
/// Renders the payload of <c>nitro schema clients</c> across all three output formats.
/// </summary>
internal sealed class CoordinateClientsFormatter : IOutputFormatter<CoordinateClientsResult>
{
    private readonly OutputFormat _format;

    public CoordinateClientsFormatter(OutputFormat format)
    {
        _format = format;
    }

    public void Write(INitroConsole console, OutputEnvelope<CoordinateClientsResult> envelope)
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
                    CoordinateClientsJsonContext.Default.OutputEnvelopeCoordinateClientsResult);
                break;
        }
    }

    public void WriteError(INitroConsole console, OutputEnvelope<CoordinateClientsResult> envelope)
    {
        ErrorOutputWriter.Write(
            console,
            _format,
            envelope,
            CoordinateClientsJsonContext.Default.OutputEnvelopeCoordinateClientsResult);
    }

    private static string RenderMarkdown(OutputEnvelope<CoordinateClientsResult> envelope)
    {
        var writer = new MarkdownWriter();
        var data = envelope.Data ?? throw new InvalidOperationException(
            "CoordinateClientsFormatter cannot render markdown without a data payload.");

        writer.Frontmatter(
        [
            new KeyValuePair<string, string>("api", envelope.Api),
            new KeyValuePair<string, string>("stage", envelope.Stage),
            new KeyValuePair<string, string>("coordinate", data.Coordinate),
            new KeyValuePair<string, string>(
                "window",
                $"{MarkdownWriter.FormatDate(envelope.Window.From)} to {MarkdownWriter.FormatDate(envelope.Window.To)}")
        ]);

        writer.Heading($"Clients ({data.Clients.Count})");

        var rows = new List<IReadOnlyList<string>>();
        foreach (var entry in data.Clients)
        {
            rows.Add(
            [
                entry.Name,
                MarkdownWriter.FormatCount(entry.TotalVersions),
                MarkdownWriter.FormatCount(entry.TotalOperations),
                MarkdownWriter.FormatCount(entry.TotalRequests)
            ]);
        }

        writer.Table(["Client", "Versions", "Operations", "Requests"], rows);

        return writer.ToString();
    }

    private static void WriteTable(
        INitroConsole console,
        OutputEnvelope<CoordinateClientsResult> envelope)
    {
        var data = envelope.Data ?? throw new InvalidOperationException(
            "CoordinateClientsFormatter cannot render a table without a data payload.");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]{data.Coordinate.EscapeMarkup()}[/]")
            .AddColumn("Client")
            .AddColumn(new TableColumn("Versions").RightAligned())
            .AddColumn(new TableColumn("Operations").RightAligned())
            .AddColumn(new TableColumn("Requests").RightAligned());

        foreach (var entry in data.Clients)
        {
            table.AddRow(
                entry.Name.EscapeMarkup(),
                MarkdownWriter.FormatCount(entry.TotalVersions),
                MarkdownWriter.FormatCount(entry.TotalOperations),
                MarkdownWriter.FormatCount(entry.TotalRequests));
        }

        console.Out.Write(table);
    }
}
