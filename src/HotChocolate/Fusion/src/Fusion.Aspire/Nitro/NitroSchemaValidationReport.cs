using System.Security.Cryptography;
using System.Text;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal sealed record NitroSchemaValidationReport(
    NitroSchemaValidationStatus Status,
    string SchemaHash,
    string? RequestId,
    IReadOnlyList<NitroClientContractViolation> Clients,
    IReadOnlyList<NitroSchemaValidationFinding> Findings,
    string? UnavailableReason,
    DateTimeOffset CompletedAt)
{
    public bool HasMoreClientErrors { get; init; }

    public int ClientCount => Clients.Count;

    public int OperationCount => Clients.Sum(client => client.Operations.Count);

    public int FindingCount =>
        Findings.Count
        + Clients.Sum(client => client.Operations.Sum(operation => operation.Errors.Count));

    public string Fingerprint => ComputeFingerprint();

    public static NitroSchemaValidationReport Passed(
        string schemaHash,
        string requestId,
        DateTimeOffset completedAt)
        => new(
            NitroSchemaValidationStatus.Passed,
            schemaHash,
            requestId,
            [],
            [],
            null,
            completedAt);

    public static NitroSchemaValidationReport Unavailable(
        string schemaHash,
        string reason,
        DateTimeOffset completedAt,
        string? requestId = null)
        => new(
            NitroSchemaValidationStatus.Unavailable,
            schemaHash,
            requestId,
            [],
            [],
            reason,
            completedAt);

    private string ComputeFingerprint()
    {
        if (Status is not NitroSchemaValidationStatus.Violations)
        {
            return string.Empty;
        }

        var values = new List<string>(FindingCount);

        foreach (var client in Clients)
        {
            foreach (var operation in client.Operations)
            {
                foreach (var error in operation.Errors)
                {
                    values.Add(
                        $"client|{client.ClientId}|{operation.Hash}|{error.Code}|"
                        + $"{error.Path}|{error.Line}|{error.Column}");
                }
            }
        }

        foreach (var finding in Findings)
        {
            values.Add(
                $"other|{finding.Group}|{finding.Kind}|{finding.Code}|{finding.Coordinate}|"
                + $"{finding.Path}|{finding.Line}|{finding.Column}|{finding.Identity}");
        }

        values.Sort(StringComparer.Ordinal);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var value in values)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(value));
            hash.AppendData([0]);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }
}

internal sealed record NitroClientContractViolation(
    string ClientId,
    string ClientName,
    IReadOnlyList<NitroOperationContractViolation> Operations);

internal sealed record NitroOperationContractViolation(
    string Hash,
    IReadOnlyList<string> DeployedTags,
    IReadOnlyList<NitroSchemaValidationFinding> Errors);

internal sealed record NitroSchemaValidationFinding(
    string Group,
    string Kind,
    string Message,
    string? Code = null,
    string? Coordinate = null,
    string? Path = null,
    int? Line = null,
    int? Column = null,
    string? Identity = null);

internal enum NitroSchemaValidationStatus
{
    Passed,
    Violations,
    Unavailable
}
