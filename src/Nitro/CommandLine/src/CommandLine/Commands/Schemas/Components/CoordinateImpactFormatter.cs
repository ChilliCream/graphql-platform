using ChilliCream.Nitro.CommandLine.Output;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;

/// <summary>
/// Renders the payload of <c>nitro schema impact</c> across all three output formats. The
/// multi-section Markdown output is the money shot for agents deciding whether a coordinate
/// is safe to remove.
/// </summary>
internal sealed class CoordinateImpactFormatter : IOutputFormatter<CoordinateImpactResult>
{
    private readonly OutputFormat _format;

    public CoordinateImpactFormatter(OutputFormat format)
    {
        _format = format;
    }

    public void Write(INitroConsole console, OutputEnvelope<CoordinateImpactResult> envelope)
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
                    CoordinateImpactJsonContext.Default.OutputEnvelopeCoordinateImpactResult);
                break;
        }
    }

    public void WriteError(INitroConsole console, OutputEnvelope<CoordinateImpactResult> envelope)
    {
        ErrorOutputWriter.Write(
            console,
            _format,
            envelope,
            CoordinateImpactJsonContext.Default.OutputEnvelopeCoordinateImpactResult);
    }

    private static string RenderMarkdown(OutputEnvelope<CoordinateImpactResult> envelope)
    {
        var writer = new MarkdownWriter();
        var data = envelope.Data ?? throw new InvalidOperationException(
            "CoordinateImpactFormatter cannot render markdown without a data payload.");

        writer.Frontmatter(
        [
            new KeyValuePair<string, string>("api", envelope.Api),
            new KeyValuePair<string, string>("stage", envelope.Stage),
            new KeyValuePair<string, string>("coordinate", data.Coordinate),
            new KeyValuePair<string, string>(
                "window",
                $"{MarkdownWriter.FormatDate(envelope.Window.From)} to {MarkdownWriter.FormatDate(envelope.Window.To)}"),
            new KeyValuePair<string, string>("verdict", data.Verdict)
        ]);

        writer.Heading("Usage");
        writer.Table(
            ["Metric", "Value"],
            [
                ["Total requests", MarkdownWriter.FormatCount(data.Usage.TotalRequests)],
                ["Clients", MarkdownWriter.FormatCount(data.Usage.ClientCount)],
                ["Operations", MarkdownWriter.FormatCount(data.Usage.OperationCount)],
                ["First seen", MarkdownWriter.FormatDate(data.Usage.FirstSeen)],
                ["Last seen", MarkdownWriter.FormatDate(data.Usage.LastSeen)],
                [
                    "Error rate",
                    data.Usage.ErrorRate is { } errorRate
                        ? MarkdownWriter.FormatPercent(errorRate)
                        : "-"
                ]
            ]);

        writer.Heading($"Clients ({data.Clients.Count})");
        var clientRows = new List<IReadOnlyList<string>>();
        foreach (var entry in data.Clients)
        {
            clientRows.Add(
            [
                entry.Name,
                MarkdownWriter.FormatCount(entry.TotalVersions),
                MarkdownWriter.FormatCount(entry.TotalOperations),
                MarkdownWriter.FormatCount(entry.TotalRequests)
            ]);
        }

        writer.Table(["Client", "Versions", "Operations", "Requests"], clientRows);

        writer.Heading($"Operations ({data.Operations.Count})");
        var operationRows = new List<IReadOnlyList<string>>();
        foreach (var entry in data.Operations)
        {
            operationRows.Add(
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
            operationRows);

        return writer.ToString();
    }

    private static void WriteTable(
        INitroConsole console,
        OutputEnvelope<CoordinateImpactResult> envelope)
    {
        var data = envelope.Data ?? throw new InvalidOperationException(
            "CoordinateImpactFormatter cannot render a table without a data payload.");

        console.Out.MarkupLine(
            $"[bold]{data.Coordinate.EscapeMarkup()}[/] — verdict: [yellow]{data.Verdict.EscapeMarkup()}[/]");

        var summary = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Usage[/]")
            .AddColumn("Metric")
            .AddColumn(new TableColumn("Value").RightAligned())
            .AddRow("Total requests", MarkdownWriter.FormatCount(data.Usage.TotalRequests))
            .AddRow("Clients", MarkdownWriter.FormatCount(data.Usage.ClientCount))
            .AddRow("Operations", MarkdownWriter.FormatCount(data.Usage.OperationCount));
        console.Out.Write(summary);

        var clients = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]Clients ({data.Clients.Count})[/]")
            .AddColumn("Client")
            .AddColumn(new TableColumn("Versions").RightAligned())
            .AddColumn(new TableColumn("Operations").RightAligned())
            .AddColumn(new TableColumn("Requests").RightAligned());
        foreach (var entry in data.Clients)
        {
            clients.AddRow(
                entry.Name.EscapeMarkup(),
                MarkdownWriter.FormatCount(entry.TotalVersions),
                MarkdownWriter.FormatCount(entry.TotalOperations),
                MarkdownWriter.FormatCount(entry.TotalRequests));
        }
        console.Out.Write(clients);

        var operations = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold]Operations ({data.Operations.Count})[/]")
            .AddColumn("Operation")
            .AddColumn("Kind")
            .AddColumn("Client")
            .AddColumn(new TableColumn("Requests").RightAligned())
            .AddColumn(new TableColumn("Error rate").RightAligned());
        foreach (var entry in data.Operations)
        {
            operations.AddRow(
                entry.OperationName.EscapeMarkup(),
                (entry.Kind ?? "-").EscapeMarkup(),
                entry.ClientName.EscapeMarkup(),
                MarkdownWriter.FormatCount(entry.TotalCount),
                MarkdownWriter.FormatPercent(entry.ErrorRate));
        }
        console.Out.Write(operations);
    }
}
