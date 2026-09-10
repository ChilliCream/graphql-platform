# Compile_Should_KeepLegacySignaturesWithGuidance_When_ConsumerUsesStaticCalls

```json
[
  "Warning CS0618: 'IFusionGatewayBuilder' is obsolete: 'Use IFusionRouterBuilder instead.'",
  "Warning CS0618: 'IFusionGatewayBuilder' is obsolete: 'Use IFusionRouterBuilder instead.'",
  "Warning CS0618: 'AspNetCoreFusionGatewayBuilderExtensions' is obsolete: 'Use AspNetCoreFusionRouterBuilderExtensions instead.'",
  "Warning CS0618: 'FusionServerServiceCollectionExtensions.AddGraphQLGatewayServer(IServiceCollection, string?, int, bool)' is obsolete: 'Use AddGraphQLRouter() instead.'",
  "Warning CS0618: 'AspNetCoreFusionGatewayBuilderExtensions.ModifyServerOptions(IFusionGatewayBuilder, Action<GraphQLServerOptions>)' is obsolete: 'Use ModifyServerOptions on IFusionRouterBuilder instead.'",
  "Warning CS0618: 'FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLGateway(IHostApplicationBuilder, string?, int, bool)' is obsolete: 'Use AddGraphQLRouter() instead.'"
]
```
