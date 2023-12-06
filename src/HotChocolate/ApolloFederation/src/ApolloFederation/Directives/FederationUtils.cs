using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// Utility class to help calculate different Apollo Federation @link imports based on the supported version.
/// </summary>
internal sealed class FederationUtils
{
    private static string FEDERATION_SPEC_BASE_URL = "https://specs.apollo.dev/federation/v";

    private static List<string?> FEDERATION_IMPORTS_20 = new List<string?>
    {
        "@extends",
        "@external",
        "@key",
        "@inaccessible",
        "@override",
        "@provides",
        "@requires",
        "@shareable",
        "@tag",
        "FieldSet"
    };

    private static List<string?> FEDERATION_IMPORTS_21 = ConcatFederationImports(FEDERATION_IMPORTS_20, new List<string?> { "@composeDirective" });
    private static List<string?> FEDERATION_IMPORTS_22 = FEDERATION_IMPORTS_21;
    private static List<string?> FEDERATION_IMPORTS_23 = ConcatFederationImports(FEDERATION_IMPORTS_22, new List<string?> { "@interfaceObject" });

    private static List<string?> FEDERATION_IMPORTS_24 = FEDERATION_IMPORTS_23;

    // TODO add @authenticated and @requiresPolicy
    private static List<string?> FEDERATION_IMPORTS_25 = FEDERATION_IMPORTS_23;

    private static List<string?> ConcatFederationImports(List<string?> baseImports, List<string?> additionalImports)
    {
        var imports = new List<string?>(baseImports);
        imports.AddRange(additionalImports);
        return imports;
    }

    /// <summary>
    /// Retrieve Apollo Federation @link information corresponding to the specified version.
    /// </summary>
    /// <param name="federationVersion">
    /// Supported Apollo Federation version
    /// </param>
    /// <returns>
    /// Federation @link information corresponding to the specified version.
    /// </returns>
    internal static Link GetFederationLink(FederationVersion federationVersion)
    {
        switch (federationVersion)
        {
            case FederationVersion.FEDERATION_20:
            {
                return new Link(FEDERATION_SPEC_BASE_URL + "2.0", FEDERATION_IMPORTS_20);
            }
            case FederationVersion.FEDERATION_21:
            {
                return new Link(FEDERATION_SPEC_BASE_URL + "2.1", FEDERATION_IMPORTS_21);
            }
            case FederationVersion.FEDERATION_22:
            {
                return new Link(FEDERATION_SPEC_BASE_URL + "2.2", FEDERATION_IMPORTS_22);
            }
            case FederationVersion.FEDERATION_23:
            {
                return new Link(FEDERATION_SPEC_BASE_URL + "2.3", FEDERATION_IMPORTS_23);
            }
            case FederationVersion.FEDERATION_24:
            {
                return new Link(FEDERATION_SPEC_BASE_URL + "2.4", FEDERATION_IMPORTS_24);
            }
            case FederationVersion.FEDERATION_25:
            {
                return new Link(FEDERATION_SPEC_BASE_URL + "2.5", FEDERATION_IMPORTS_25);
            }
            default:
            {
                throw ThrowHelper.FederationVersion_Unknown(federationVersion);
            }
        }
    }
}
