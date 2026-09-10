# Register_Should_PreserveArgumentExceptions_When_UsingLegacyAndRouterEntryPoints

```json
[
  "FusionServerServiceCollectionExtensions.AddGraphQLRouter(services: null) -> ArgumentNullException(services)",
  "services.AddGraphQLRouter(maxAllowedRequestSize: -1) -> ArgumentOutOfRangeException(maxAllowedRequestSize)",
  "FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLRouter(host: null) -> NullReferenceException",
  "FusionServerServiceCollectionExtensions.AddGraphQLGatewayServer(services: null) -> ArgumentNullException(services)",
  "services.AddGraphQLGatewayServer(maxAllowedRequestSize: -1) -> ArgumentOutOfRangeException(maxAllowedRequestSize)",
  "FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLGateway(host: null) -> NullReferenceException"
]
```
