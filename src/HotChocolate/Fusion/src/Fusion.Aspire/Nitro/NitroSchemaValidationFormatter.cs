using System.Text;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal static class NitroSchemaValidationFormatter
{
    private const int MaxClients = 20;
    private const int MaxFindings = 100;

    public static string Format(NitroSchemaValidationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        var clientLimit = Math.Min(report.ClientCount, MaxClients);
        var shownClients = 0;
        var shownFindings = 0;

        builder.Append("Nitro schema validation found ")
            .Append(report.FindingCount)
            .Append(report.FindingCount == 1 ? " violation; " : " violations; ")
            .Append(report.ClientCount)
            .Append(report.ClientCount == 1 ? " client and " : " clients and ")
            .Append(report.OperationCount)
            .Append(report.OperationCount == 1 ? " operation are affected." : " operations are affected.")
            .AppendLine();

        for (var clientIndex = 0;
            clientIndex < clientLimit && shownFindings < MaxFindings;
            clientIndex++)
        {
            var client = report.Clients[clientIndex];
            shownClients++;
            builder.Append("Client: ")
                .Append(client.ClientName)
                .Append(" (")
                .Append(client.ClientId)
                .AppendLine(")");

            foreach (var operation in client.Operations)
            {
                if (shownFindings >= MaxFindings)
                {
                    break;
                }

                builder.Append("  Operation: ")
                    .Append(operation.Hash)
                    .Append(" [tags: ")
                    .Append(operation.DeployedTags.Count == 0
                        ? "none"
                        : string.Join(", ", operation.DeployedTags))
                    .AppendLine("]");

                foreach (var error in operation.Errors)
                {
                    if (shownFindings >= MaxFindings)
                    {
                        break;
                    }

                    AppendFinding(builder, "    ", error);
                    shownFindings++;
                }
            }
        }

        if (report.Findings.Count > 0 && shownFindings < MaxFindings)
        {
            foreach (var group in report.Findings.GroupBy(finding => finding.Group))
            {
                builder.Append(group.Key).AppendLine(":");

                foreach (var finding in group)
                {
                    if (shownFindings >= MaxFindings)
                    {
                        break;
                    }

                    AppendFinding(builder, "  ", finding);
                    shownFindings++;
                }
            }
        }

        if (shownClients < report.ClientCount || shownFindings < report.FindingCount)
        {
            builder.Append("Output truncated: ")
                .Append(shownClients)
                .Append(" clients shown, ")
                .Append(report.ClientCount)
                .Append(" total; ")
                .Append(shownFindings)
                .Append(" findings shown, ")
                .Append(report.FindingCount)
                .AppendLine(" total.");
        }

        if (report.HasMoreClientErrors)
        {
            builder.AppendLine(
                "Nitro truncated the client-contract findings returned by the validation request.");
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendFinding(
        StringBuilder builder,
        string indent,
        NitroSchemaValidationFinding finding)
    {
        builder.Append(indent);

        for (var level = 0; level < finding.Depth; level++)
        {
            builder.Append("  ");
        }

        builder.Append("- ");

        if (finding.Severity is not null)
        {
            builder.Append(SeverityMarker(finding.Severity)).Append(' ');
        }

        builder.Append(finding.Message);

        if (finding.Code is not null)
        {
            builder.Append(" [code: ").Append(finding.Code).Append(']');
        }

        if (finding.Path is not null)
        {
            builder.Append(" [path: ").Append(finding.Path).Append(']');
        }

        if (finding.Line is not null || finding.Column is not null)
        {
            builder.Append(" [line: ")
                .Append(finding.Line?.ToString() ?? "?")
                .Append(", column: ")
                .Append(finding.Column?.ToString() ?? "?")
                .Append(']');
        }

        builder.AppendLine();
    }

    private static string SeverityMarker(string severity)
        => severity switch
        {
            "BREAKING" => "✕",
            "DANGEROUS" => "!",
            "SAFE" => "✓",
            _ => $"[{severity}]"
        };
}
