namespace HotChocolate.Fusion.Types.Metadata;

/// <summary>
/// Provides information in which environment the schema is deployed.
/// </summary>
/// <param name="AppId">
/// The unique application identifier.
/// </param>
/// <param name="Name">
/// The application unique environment name.
/// </param>
internal sealed record SchemaEnvironment(string AppId, string Name);
