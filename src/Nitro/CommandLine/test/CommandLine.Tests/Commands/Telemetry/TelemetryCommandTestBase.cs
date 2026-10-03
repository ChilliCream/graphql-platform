using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry;

public abstract class TelemetryCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string WorkspaceId = "workspace-from-session";

    protected void SetupListAttributeKeys(bool hasNextPage = false, params AttributeKeyRow[] keys)
    {
        TelemetryClientMock
            .Setup(x =>
                x.ListAttributeKeysAsync(
                    WorkspaceId,
                    It.IsAny<OpenTelemetrySignalKind>(),
                    It.IsAny<IReadOnlyList<OpenTelemetryAttributeKind>?>(),
                    It.IsAny<string?>(),
                    It.IsAny<DateTimeOffset?>(),
                    It.IsAny<DateTimeOffset?>(),
                    50,
                    null,
                    It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new ConnectionPage<AttributeKeyRow>(keys, null, hasNextPage));
    }
}
