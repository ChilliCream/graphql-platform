using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry.Models;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry;

public abstract class TelemetryCommandTestBase(NitroCommandFixture fixture) : CommandTestBase(fixture)
{
    protected const string WorkspaceId = "workspace-from-session";
    protected const string ServiceName = "products";

    protected void SetupListServices(
        bool hasNextPage = false,
        params ServiceRow[] services)
    {
        TelemetryClientMock.Setup(x => x.ListServicesAsync(
                WorkspaceId,
                It.IsAny<string?>(),
                null,
                It.IsAny<IReadOnlyList<string>?>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                50,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<ServiceRow>(services, null, hasNextPage));
    }

    protected void SetupGetService(ServiceRow? service)
    {
        TelemetryClientMock.Setup(x => x.GetServiceAsync(
                WorkspaceId,
                ServiceName,
                It.IsAny<IReadOnlyList<string>?>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
    }

    protected void SetupListAttributeKeys(
        bool hasNextPage = false,
        params AttributeKeyRow[] keys)
    {
        TelemetryClientMock.Setup(x => x.ListAttributeKeysAsync(
                WorkspaceId,
                It.IsAny<OpenTelemetrySignalKind>(),
                It.IsAny<IReadOnlyList<OpenTelemetryAttributeKind>?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                50,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<AttributeKeyRow>(keys, null, hasNextPage));
    }

    protected void SetupListAttributeValues(
        bool hasNextPage = false,
        params AttributeValue[] values)
    {
        TelemetryClientMock.Setup(x => x.ListAttributeValuesAsync(
                WorkspaceId,
                It.IsAny<OpenTelemetrySignalKind>(),
                "service.name",
                null,
                It.IsAny<string?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                50,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<AttributeValue>(values, null, hasNextPage));
    }

    protected static ServiceRow CreateService(
        string name = ServiceName,
        string[]? environments = null,
        ServiceVersionMarker[]? versionMarkers = null)
        => new(
            name,
            environments ?? ["production"],
            versionMarkers
            ??
            [
                new ServiceVersionMarker(
                    new DateTimeOffset(2025, 12, 31, 22, 0, 0, TimeSpan.Zero),
                    "1.0.0"),
                new ServiceVersionMarker(
                    new DateTimeOffset(2025, 12, 31, 23, 0, 0, TimeSpan.Zero),
                    "1.1.0")
            ]);
}
