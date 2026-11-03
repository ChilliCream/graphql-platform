namespace HotChocolate.Fusion;

public static class WellKnownVersions
{
    public static readonly Version LatestGatewayFormatVersion = new(2, 0, 0);

    /// <summary>
    /// Identifies the execution schema format that composition writes.
    /// Raise it only when the execution schema format changes, not for composition bug fixes.
    /// </summary>
    public static readonly Version ProducedExecutionSchemaVersion = new(2, 0, 0);

    /// <summary>
    /// Identifies the highest execution schema format that this gateway supports.
    /// Archives with a higher version must be loaded by an upgraded gateway.
    /// </summary>
    public static readonly Version MaxSupportedExecutionSchemaVersion = new(2, 0, 0);

    public static readonly Version LatestSourceSchemaVersion = new(2, 0, 0);

    public static readonly Version LegacyGatewayFormatVersion = new(1, 0, 0);
}
