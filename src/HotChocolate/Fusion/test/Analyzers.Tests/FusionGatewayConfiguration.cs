using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Skimmed.Serialization;

namespace Projects;

public static class FusionGatewayConfigurationFiles
{
    public static readonly string[] SubgraphProjects =
    [
        """/Users/michael/local/webshop-workshop/src/Basket.API/eShop.Basket.API.csproj""",
    ];

    public const string GatewayProject =
        """/Users/michael/local/webshop-workshop/src/Gateway/eShop.Gateway.csproj""";
}