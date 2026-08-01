namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroSchemaValidationFormatterTests
{
    [Fact]
    public void Format_Should_BoundClientsAndFindings_When_ReportIsLarge()
    {
        // arrange
        var clients = Enumerable.Range(1, 21)
            .Select(clientIndex =>
                new NitroClientContractViolation(
                    $"client-{clientIndex}",
                    $"Client {clientIndex}",
                    [
                        new NitroOperationContractViolation(
                            $"operation-{clientIndex}",
                            ["production"],
                            [
                                .. Enumerable.Range(1, 5)
                                    .Select(findingIndex =>
                                        new NitroSchemaValidationFinding(
                                            "Client contract violations",
                                            "PersistedQueryValidationError",
                                            $"Finding {findingIndex}",
                                            $"HC{findingIndex:000}"))
                            ])
                    ]))
            .ToList();
        var report = new NitroSchemaValidationReport(
            NitroSchemaValidationStatus.Violations,
            "schema-hash",
            "request-id",
            clients,
            [],
            null,
            DateTimeOffset.UtcNow)
        {
            HasMoreClientErrors = true
        };

        // act
        var output = NitroSchemaValidationFormatter.Format(report);

        // assert
        var lines = output.Split(Environment.NewLine);
        $"""
        Clients written: {lines.Count(line => line.StartsWith("Client: ", StringComparison.Ordinal))}
        Findings written: {lines.Count(line => line.TrimStart().StartsWith("- ", StringComparison.Ordinal))}
        Local truncation stated: {output.Contains(
            "Output truncated: 20 clients shown, 21 total; 100 findings shown, 105 total.",
            StringComparison.Ordinal)}
        Remote truncation stated: {output.Contains(
            "Nitro truncated the client-contract findings returned by the validation request.",
            StringComparison.Ordinal)}
        """.MatchInlineSnapshot(
            """
            Clients written: 20
            Findings written: 100
            Local truncation stated: True
            Remote truncation stated: True
            """);
    }

    [Fact]
    public void Format_Should_PreserveHierarchyWithoutIncludingReportMetadata()
    {
        // arrange
        var report = new NitroSchemaValidationReport(
            NitroSchemaValidationStatus.Violations,
            "schema-secret",
            "request-token-secret",
            [
                new NitroClientContractViolation(
                    "client-id",
                    "Inventory UI",
                    [
                        new NitroOperationContractViolation(
                            "operation-hash",
                            ["production", "canary"],
                            [
                                new NitroSchemaValidationFinding(
                                    "Client contract violations",
                                    "PersistedQueryValidationError",
                                    "Field productName does not exist.",
                                    "HC001",
                                    Path: "query.productName",
                                    Line: 4,
                                    Column: 7)
                            ])
                    ])
            ],
            [
                new NitroSchemaValidationFinding(
                    "Schema change violations",
                    "FieldRemovedChange",
                    "Field 'name' was removed from type 'Product'.",
                    Coordinate: "Product.name",
                    Severity: "BREAKING")
            ],
            null,
            DateTimeOffset.UtcNow);

        // act
        var output = NitroSchemaValidationFormatter.Format(report);

        // assert
        output.MatchInlineSnapshot(
            """
            Nitro schema validation found 2 violations; 1 client and 1 operation are affected.
            Client: Inventory UI (client-id)
              Operation: operation-hash [tags: production, canary]
                - Field productName does not exist. [code: HC001] [path: query.productName] [line: 4, column: 7]
            Schema change violations:
              - Field 'name' was removed from type 'Product'. [severity: BREAKING] [coordinate: Product.name]
            """);
    }
}
