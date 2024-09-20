using HotChocolate.Language;
using StrawberryShake.Tools.Configuration;

namespace StrawberryShake.CodeGeneration.CSharp;

/// <summary>
/// The csharp generator settings.
/// </summary>
public class CSharpGeneratorSettings
{
    /// <summary>
    /// The name of the client class.
    /// </summary>
    public string ClientName { get; set; } = "GraphQLClient";

    /// <summary>
    /// The root namespace of the client.
    /// </summary>
    public string Namespace { get; set; } = "StrawberryShake.GraphQL";

    /// <summary>
    /// The access modifier of the client.
    /// </summary>
    public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;

    /// <summary>
    /// Defines if a schema needs to be fully valid.
    /// </summary>
    public bool StrictSchemaValidation { get; set; } = true;

    /// <summary>
    /// Generates the client without a store
    /// </summary>
    public bool NoStore { get; set; }

    /// <summary>
    /// Generates input types as records.
    /// </summary>
    public bool InputRecords { get; set; }

    /// <summary>
    /// Generates entity as records.
    /// </summary>
    public bool EntityRecords { get; set; }

    /// <summary>
    /// Generate razor components.
    /// </summary>
    public bool RazorComponents { get; set; }

    /// <summary>
    /// Generate a single CSharp code file.
    /// </summary>
    public bool SingleCodeFile { get; set; } = true;

    /// <summary>
    /// The default request strategy.
    /// </summary>
    public RequestStrategy RequestStrategy { get; set; } =
        RequestStrategy.Default;

    /// <summary>
    /// The <see cref="IDocumentHashProvider"/> that shall be used for persisted operations.
    /// </summary>
    public IDocumentHashProvider HashProvider { get; set; } =
        new Sha1DocumentHashProvider(HashFormat.Hex);

    /// <summary>
    /// The transport profiles that shall be generated.
    /// </summary>
    public List<TransportProfile> TransportProfiles { get; set; } =
    [
        new TransportProfile(
            TransportProfile.DefaultProfileName,
            TransportType.Http,
            subscription: TransportType.WebSocket),

    ];
}
