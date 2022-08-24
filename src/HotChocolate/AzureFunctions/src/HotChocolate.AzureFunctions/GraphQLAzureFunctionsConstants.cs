using System;

namespace HotChocolate.AzureFunctions;

public static class GraphQLAzureFunctionsConstants
{
    public const string DefaultAzFuncHttpTriggerRoute = "graphql/{**slug}";
    public const string DefaultGraphQLRoute = "/api/graphql";
    public const string DefaultJsonContentType = "application/json";
    public const int DefaultMaxRequests = 20 * 1000 * 1000;
}
