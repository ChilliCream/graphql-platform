using ChilliCream.Nitro.CommandLine.Output;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;

/// <summary>
/// Renders the payload of <c>nitro schema usage</c> across all three output formats.
/// </summary>
internal sealed class CoordinateUsageFormatter : IOutputFormatter<CoordinateUsageResultSet>
{
    private readonly OutputFormat _format;

    public CoordinateUsageFormatter(OutputFormat format)
    {
        _format = format;
    }

    public void Write(INitroConsole console, OutputEnvelope<CoordinateUsageResultSet> envelope)
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
                    CoordinateUsageJsonContext.Default.OutputEnvelopeCoordinateUsageResultSet);
                break;
        }
    }

    public void WriteError(INitroConsole console, OutputEnvelope<CoordinateUsageResultSet> envelope)
    {
        ErrorOutputWriter.Write(
            console,
            _format,
            envelope,
            CoordinateUsageJsonContext.Default.OutputEnvelopeCoordinateUsageResultSet);
    }

    private static string RenderMarkdown(OutputEnvelope<CoordinateUsageResultSet> envelope)
    {
        var writer = new MarkdownWriter();
        var data = envelope.Data ?? throw new InvalidOperationException(
            "CoordinateUsageFormatter cannot render markdown without a data payload.");

        var coordinates = data.Coordinates;
        var first = true;
        foreach (var (_, usage) in coordinates)
        {
            if (!first)
            {
                writer.BlankLine();
                writer.SectionBreak();
            }

            first = false;

            writer.Frontmatter(
            [
                new KeyValuePair<string, string>("api", envelope.Api),
                new KeyValuePair<string, string>("stage", envelope.Stage),
                new KeyValuePair<string, string>("coordinate", usage.Coordinate),
                new KeyValuePair<string, string>(
                    "window",
                    $"{MarkdownWriter.FormatDate(envelope.Window.From)} to {MarkdownWriter.FormatDate(envelope.Window.To)}")
            ]);

            writer.Table(
                ["Metric", "Value"],
                [
                    ["Total requests", MarkdownWriter.FormatCount(usage.TotalRequests)],
                    ["Clients", MarkdownWriter.FormatCount(usage.ClientCount)],
                    ["Operations", MarkdownWriter.FormatCount(usage.OperationCount)],
                    ["First seen", MarkdownWriter.FormatDate(usage.FirstSeen)],
                    ["Last seen", MarkdownWriter.FormatDate(usage.LastSeen)],
                    [
                        "Error rate",
                        usage.ErrorRate is { } errorRate
                            ? MarkdownWriter.FormatPercent(errorRate)
                            : "-"
                    ],
                    [
                        "Mean duration",
                        usage.MeanDuration is { } meanDuration
                            ? MarkdownWriter.FormatMilliseconds(meanDuration)
                            : "-"
                    ]
                ]);
        }

        return writer.ToString();
    }

    private static void WriteTable(
        INitroConsole console,
        OutputEnvelope<CoordinateUsageResultSet> envelope)
    {
        var data = envelope.Data ?? throw new InvalidOperationException(
            "CoordinateUsageFormatter cannot render a table without a data payload.");

        foreach (var (_, usage) in data.Coordinates)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title($"[bold]{usage.Coordinate.EscapeMarkup()}[/]")
                .AddColumn("Metric")
                .AddColumn(new TableColumn("Value").RightAligned())
                .AddRow("Total requests", MarkdownWriter.FormatCount(usage.TotalRequests))
                .AddRow("Clients", MarkdownWriter.FormatCount(usage.ClientCount))
                .AddRow("Operations", MarkdownWriter.FormatCount(usage.OperationCount))
                .AddRow("First seen", MarkdownWriter.FormatDate(usage.FirstSeen))
                .AddRow("Last seen", MarkdownWriter.FormatDate(usage.LastSeen))
                .AddRow(
                    "Error rate",
                    usage.ErrorRate is { } errorRate
                        ? MarkdownWriter.FormatPercent(errorRate)
                        : "-")
                .AddRow(
                    "Mean duration",
                    usage.MeanDuration is { } meanDuration
                        ? MarkdownWriter.FormatMilliseconds(meanDuration)
                        : "-");

            console.Out.Write(table);
        }
    }
}
