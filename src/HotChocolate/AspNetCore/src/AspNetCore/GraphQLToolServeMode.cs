using ChilliCream.Nitro.App;

namespace HotChocolate.AspNetCore;

/// <summary>
/// Represents the different modes of serving the Nitro GraphQL tool. This class enables
/// serving the tool in a variety of predefined ways:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="GraphQLToolServeMode.Latest"/>: Uses the latest version of the tool, served over the
/// cdn.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="GraphQLToolServeMode.Insider"/>: Uses the insider version of the tool, served over
/// the CDN.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="GraphQLToolServeMode.Embedded"/>: Uses the tool's embedded files in the package.
/// </description>
/// </item>
/// </list>
/// In addition, a specific version of the tool can be served over the CDN using the
/// <see cref="GraphQLToolServeMode.Version(string)"/> method.
/// <example>
/// <para>
/// The following example shows how to serve the embedded version of the tool:
/// </para>
/// <code>
/// endpoints
///   .MapGraphQL()
///   .WithOptions(new GraphQLServerOptions
///     {
///       Tool = { ServeMode = GraphQLToolServeMode.Embedded }
///     });
/// </code>
/// <para>
/// Or when you want to serve the insider version of the tool:
/// </para>
/// <code>
/// endpoints
///   .MapGraphQL()
///   .WithOptions(new GraphQLServerOptions
///     {
///       Tool = { ServeMode = GraphQLToolServeMode.Insider }
///     });
/// </code>
/// <para>
/// Or when you want to serve a specific version of the tool:
/// </para>
/// <code>
/// endpoints
///   .MapGraphQL()
///   .WithOptions(new GraphQLServerOptions
///     {
///       Tool = { ServeMode = GraphQLToolServeMode.Version("5.0.8") }
///     });
/// </code>
/// </example>
/// </summary>
public sealed class GraphQLToolServeMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLToolServeMode"/> class using the
    /// provided mode.
    /// </summary>
    /// <param name="mode">The mode to serve the GraphQL tool.</param>
    private GraphQLToolServeMode(string mode) { Mode = mode; }

    /// <summary>
    /// Gets the current mode of serving the GraphQL tool.
    /// </summary>
    internal string Mode { get; }

    /// <summary>
    /// Serves the GraphQL tool using the latest version available over the CDN.
    /// </summary>
    public static readonly GraphQLToolServeMode Latest = new(ServeMode.Constants.Latest);

    /// <summary>
    /// Serves the GraphQL tool using the insider version available over the CDN.
    /// </summary>
    public static readonly GraphQLToolServeMode Insider = new(ServeMode.Constants.Insider);

    /// <summary>
    /// Serves the GraphQL tool using the embedded files from the package.
    /// </summary>
    public static readonly GraphQLToolServeMode Embedded = new(ServeMode.Constants.Embedded);

    /// <summary>
    /// Serves the GraphQL tool from a specific version available over the CDN.
    /// </summary>
    /// <param name="version">The version of the tool to serve.</param>
    /// <returns>
    /// A new <see cref="GraphQLToolServeMode"/> object for serving the specific version.
    /// </returns>
    public static GraphQLToolServeMode Version(string version) => new(version);
}
