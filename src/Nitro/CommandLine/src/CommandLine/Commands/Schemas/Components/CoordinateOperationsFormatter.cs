using ChilliCream.Nitro.CommandLine.Output;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;

/// <summary>
/// Renders the payload of <c>nitro schema operations</c> across all three output formats.
/// </summary>
internal sealed class CoordinateOperationsFormatter : IOutputFormatter<CoordinateOperationsResult>
{
    private readonly OutputFormat _format;

    public CoordinateOperationsFormatter(OutputFormat format)
    {
        _format = format;
    }

    public void Write(INitroConsole console, OutputEnvelope<CoordinateOperationsResult> envelope)
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
                    CoordinateOperationsJsonContext.Default.OutputEnvelopeCoordinateOperationsResult);
                break;
        }
    }

    public void WriteError(INitroConsole console, OutputEnvelope<CoordinateOperationsResult> envelope)
    {
        ErrorOutputWriter.Write(
            console,
            _format,
            envelope,
            CoordinateOperationsJsonContext.Default.OutputEnvelopeCoordinateOperationsResult);
    }

    private static string RenderMarkdown(OutputEnvelope<CoordinateOperationsResult> envelope)
    {
        var writer = new MarkdownWriter();
        var data = envelope.Data ?? throw new InvalidOperationException(
            "CoordinateOperationsFormatter cannot render markdown without a data payload.");

        writer.Frontmatter(
        [
            new KeyValuePair<string, string>("api", envelope.Api),
            new KeyValuePair<string, string>("stage", envelope.Stage),
            new KeyValuePair<string, string>("coordinate", data.Coordinate),
            new KeyValuePair<string, string>(
                "window",
                $"{MarkdownWriter.FormatDate(envelope.Window.From)} to {MarkdownWriter.FormatDate(envelope.Window.To)}")
        ]);

        writer.Heading($"Operations ({data.Operations.Count})");

        var rows = new List<IReadOnlyList<string>>();
        foreach (var entry in data.Operations)
        {
            rows.Add(
            [
                entry.OperationName,
                entry.Kind ?? "-",
                entry.ClientName,
                MarkdownWriter.FormatCount(entry.TotalCount),
                MarkdownWriter.FormatPercent(entry.ErrorRate)
            ]);
        }

        writer.Table(
            ["Operation", "Kind", "Client", "Requests", "Error rate"],
            rows);

        return writer.ToString();
    }

    private static void WriteTable(
        INitroConsole console,
        OutputEnvelope<CoordinateOperationsResult> envelope)
    {
        var data = envelope.Data ?? throw new InvalidOperationException(
            "CoordinateOperationsFormatter cannot render a table without a data payload.");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]{data.Coordinate.EscapeMarkup()}[/]")
            .AddColumn("Operation")
            .AddColumn("Kind")
            .AddColumn("Client")
            .AddColumn(new TableColumn("Requests").RightAligned())
            .AddColumn(new TableColumn("Error rate").RightAligned());

        foreach (var entry in data.Operations)
        {
            table.AddRow(
                entry.OperationName.EscapeMarkup(),
                (entry.Kind ?? "-").EscapeMarkup(),
                entry.ClientName.EscapeMarkup(),
                MarkdownWriter.FormatCount(entry.TotalCount),
                MarkdownWriter.FormatPercent(entry.ErrorRate));
        }

        console.Out.Write(table);
    }
}
