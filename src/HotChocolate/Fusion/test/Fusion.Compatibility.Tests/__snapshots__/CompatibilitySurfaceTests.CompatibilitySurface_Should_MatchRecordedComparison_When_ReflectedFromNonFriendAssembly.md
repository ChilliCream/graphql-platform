# CompatibilitySurface_Should_MatchRecordedComparison_When_ReflectedFromNonFriendAssembly

```json
{
  "Covered": {
    "Families": [
      {
        "Name": "Core",
        "LegacyMethodCount": 66,
        "RouterMethodCount": 66,
        "AllLegacySignaturesHaveRouterTwin": true,
        "AllLegacyObsoleteOnGatewayBuilder": true,
        "AllRouterCleanOnRouterBuilder": true,
        "Signatures": [
          "AddApplicationService<1>(): hasRouterTwin=True",
          "AddConfigurationProvider<0>(Func`2): hasRouterTwin=True",
          "AddDiagnosticEventListener<1>(): hasRouterTwin=True",
          "AddDiagnosticEventListener<1>(Func`2): hasRouterTwin=True",
          "AddErrorFilter<0>(Func`2): hasRouterTwin=True",
          "AddErrorFilter<1>(): hasRouterTwin=True",
          "AddErrorFilter<1>(Func`2): hasRouterTwin=True",
          "AddFileSystemConfiguration<0>(String): hasRouterTwin=True",
          "AddHttpClientConfiguration<0>(String, Uri, SupportedOperationType, SourceSchemaClientCapabilities, Nullable`1, Nullable`1, Nullable`1, Nullable`1, Action`3, Action`3, Action`3): hasRouterTwin=True",
          "AddHttpClientConfiguration<0>(String, String, Uri, SupportedOperationType, SourceSchemaClientCapabilities, Nullable`1, Nullable`1, Nullable`1, Nullable`1, Action`3, Action`3, Action`3): hasRouterTwin=True",
          "AddHttpClientConfiguration<0>(HttpSourceSchemaClientConfiguration): hasRouterTwin=True",
          "AddHttpClientConfiguration<0>(Func`2): hasRouterTwin=True",
          "AddInMemoryConfiguration<0>(DocumentNode, JsonDocumentOwner): hasRouterTwin=True",
          "AddMD5DocumentHashProvider<0>(HashFormat): hasRouterTwin=True",
          "AddMaxAllowedFieldCycleDepthRule<0>(Nullable`1, ValueTuple`2[], Func`3): hasRouterTwin=True",
          "AddMaxExecutionDepthRule<0>(Int32, Boolean, Boolean, Func`3): hasRouterTwin=True",
          "AddNodeIdParser<1>(): hasRouterTwin=True",
          "AddOperationPlannerInterceptor<0>(Func`2): hasRouterTwin=True",
          "AddSha1DocumentHashProvider<0>(HashFormat): hasRouterTwin=True",
          "AddSha256DocumentHashProvider<0>(HashFormat): hasRouterTwin=True",
          "AddValidationRule<1>(): hasRouterTwin=True",
          "AddValidationRule<1>(Func`3): hasRouterTwin=True",
          "AddValidationVisitor<1>(Boolean): hasRouterTwin=True",
          "AddValidationVisitor<1>(Func`3, Boolean): hasRouterTwin=True",
          "AddWarmupTask<0>(Func`3, Func`2): hasRouterTwin=True",
          "AddWarmupTask<0>(IRequestExecutorWarmupTask, Func`2): hasRouterTwin=True",
          "AddWarmupTask<1>(Func`2): hasRouterTwin=True",
          "AddWarmupTask<1>(Func`2, Func`2): hasRouterTwin=True",
          "BuildRequestExecutorAsync<0>(String, CancellationToken): hasRouterTwin=True",
          "ConfigureSchemaFeatures<0>(Action`2): hasRouterTwin=True",
          "ConfigureSchemaServices<0>(Action`2): hasRouterTwin=True",
          "ConfigureValidation<0>(Action`2): hasRouterTwin=True",
          "DisableIntrospection<0>(Boolean): hasRouterTwin=True",
          "DisableIntrospection<0>(Func`3): hasRouterTwin=True",
          "ModifyOptions<0>(Action`1): hasRouterTwin=True",
          "ModifyParserOptions<0>(Action`1): hasRouterTwin=True",
          "ModifyPlannerOptions<0>(Action`1): hasRouterTwin=True",
          "ModifyRequestOptions<0>(Action`1): hasRouterTwin=True",
          "RemoveMaxAllowedFieldCycleDepthRule<0>(): hasRouterTwin=True",
          "SetIntrospectionAllowedDepth<0>(UInt16, UInt16): hasRouterTwin=True",
          "SetMaxAllowedFieldMergeComparisons<0>(Int32): hasRouterTwin=True",
          "SetMaxAllowedLocationsPerValidationError<0>(Int32): hasRouterTwin=True",
          "SetMaxAllowedValidationErrors<0>(Int32): hasRouterTwin=True",
          "UseAutomaticPersistedOperationNotFound<0>(String, String): hasRouterTwin=True",
          "UseAutomaticPersistedOperationPipeline<0>(): hasRouterTwin=True",
          "UseConcurrencyGate<0>(): hasRouterTwin=True",
          "UseDefaultPipeline<0>(): hasRouterTwin=True",
          "UseDocumentCache<0>(): hasRouterTwin=True",
          "UseDocumentParser<0>(): hasRouterTwin=True",
          "UseDocumentValidation<0>(): hasRouterTwin=True",
          "UseExceptions<0>(): hasRouterTwin=True",
          "UseInstrumentation<0>(): hasRouterTwin=True",
          "UseOnlyPersistedOperationAllowed<0>(String, String): hasRouterTwin=True",
          "UseOperationExecution<0>(): hasRouterTwin=True",
          "UseOperationPlan<0>(): hasRouterTwin=True",
          "UseOperationPlanCache<0>(): hasRouterTwin=True",
          "UseOperationVariableCoercion<0>(): hasRouterTwin=True",
          "UsePersistedOperationNotFound<0>(String, String): hasRouterTwin=True",
          "UsePersistedOperationPipeline<0>(): hasRouterTwin=True",
          "UseReadPersistedOperation<0>(String, String): hasRouterTwin=True",
          "UseRequest<0>(Func`2, String, String, String, Boolean): hasRouterTwin=True",
          "UseRequest<0>(RequestMiddleware, String, String, String, Boolean): hasRouterTwin=True",
          "UseRequest<0>(RequestMiddlewareConfiguration, String, String, Boolean): hasRouterTwin=True",
          "UseSkipWarmupExecution<0>(): hasRouterTwin=True",
          "UseTimeout<0>(): hasRouterTwin=True",
          "UseWritePersistedOperation<0>(String, String): hasRouterTwin=True"
        ]
      },
      {
        "Name": "Caching",
        "LegacyMethodCount": 3,
        "RouterMethodCount": 3,
        "AllLegacySignaturesHaveRouterTwin": true,
        "AllLegacyObsoleteOnGatewayBuilder": true,
        "AllRouterCleanOnRouterBuilder": true,
        "Signatures": [
          "AddCacheControl<0>(): hasRouterTwin=True",
          "ModifyCacheControlOptions<0>(Action`1): hasRouterTwin=True",
          "UseQueryCache<0>(String): hasRouterTwin=True"
        ]
      },
      {
        "Name": "Diagnostics",
        "LegacyMethodCount": 2,
        "RouterMethodCount": 2,
        "AllLegacySignaturesHaveRouterTwin": true,
        "AllLegacyObsoleteOnGatewayBuilder": true,
        "AllRouterCleanOnRouterBuilder": true,
        "Signatures": [
          "AddInstrumentation<0>(Action`1): hasRouterTwin=True",
          "AddInstrumentation<0>(Action`2): hasRouterTwin=True"
        ]
      },
      {
        "Name": "InMemory",
        "LegacyMethodCount": 3,
        "RouterMethodCount": 3,
        "AllLegacySignaturesHaveRouterTwin": true,
        "AllLegacyObsoleteOnGatewayBuilder": true,
        "AllRouterCleanOnRouterBuilder": true,
        "Signatures": [
          "AddInMemorySchema<0>(IRequestExecutorBuilder): hasRouterTwin=True",
          "AddInMemorySchema<0>(String): hasRouterTwin=True",
          "ModifyInMemoryCompositionOptions<0>(Action`1): hasRouterTwin=True"
        ]
      },
      {
        "Name": "AspNetCore",
        "LegacyMethodCount": 9,
        "RouterMethodCount": 9,
        "AllLegacySignaturesHaveRouterTwin": true,
        "AllLegacyObsoleteOnGatewayBuilder": true,
        "AllRouterCleanOnRouterBuilder": true,
        "Signatures": [
          "AddHttpRequestInterceptor<1>(): hasRouterTwin=True",
          "AddHttpRequestInterceptor<0>(Func`2): hasRouterTwin=True",
          "AddHttpResponseFormatter<0>(Boolean, IncrementalDeliveryFormat): hasRouterTwin=True",
          "AddHttpResponseFormatter<0>(HttpResponseFormatterOptions, IncrementalDeliveryFormat): hasRouterTwin=True",
          "AddHttpResponseFormatter<1>(): hasRouterTwin=True",
          "AddHttpResponseFormatter<1>(Func`2): hasRouterTwin=True",
          "AddSocketSessionInterceptor<1>(): hasRouterTwin=True",
          "AddSocketSessionInterceptor<1>(Func`2): hasRouterTwin=True",
          "ModifyServerOptions<0>(Action`1): hasRouterTwin=True"
        ]
      },
      {
        "Name": "NATS",
        "LegacyMethodCount": 2,
        "RouterMethodCount": 2,
        "AllLegacySignaturesHaveRouterTwin": true,
        "AllLegacyObsoleteOnGatewayBuilder": true,
        "AllRouterCleanOnRouterBuilder": true,
        "Signatures": [
          "AddNatsEventStreamBroker<0>(Action`1): hasRouterTwin=True",
          "AddNatsEventStreamBroker<0>(String, Action`1): hasRouterTwin=True"
        ]
      },
      {
        "Name": "Kafka",
        "LegacyMethodCount": 2,
        "RouterMethodCount": 2,
        "AllLegacySignaturesHaveRouterTwin": true,
        "AllLegacyObsoleteOnGatewayBuilder": true,
        "AllRouterCleanOnRouterBuilder": true,
        "Signatures": [
          "AddKafkaEventStreamBroker<0>(Action`1): hasRouterTwin=True",
          "AddKafkaEventStreamBroker<0>(String, Action`1): hasRouterTwin=True"
        ]
      },
      {
        "Name": "Redis",
        "LegacyMethodCount": 2,
        "RouterMethodCount": 2,
        "AllLegacySignaturesHaveRouterTwin": true,
        "AllLegacyObsoleteOnGatewayBuilder": true,
        "AllRouterCleanOnRouterBuilder": true,
        "Signatures": [
          "AddRedisEventStreamBroker<0>(Action`1): hasRouterTwin=True",
          "AddRedisEventStreamBroker<0>(String, Action`1): hasRouterTwin=True"
        ]
      },
      {
        "Name": "AmazonSqs",
        "LegacyMethodCount": 2,
        "RouterMethodCount": 2,
        "AllLegacySignaturesHaveRouterTwin": true,
        "AllLegacyObsoleteOnGatewayBuilder": true,
        "AllRouterCleanOnRouterBuilder": true,
        "Signatures": [
          "AddAmazonSqsEventStreamBroker<0>(Action`1): hasRouterTwin=True",
          "AddAmazonSqsEventStreamBroker<0>(String, Action`1): hasRouterTwin=True"
        ]
      },
      {
        "Name": "AzureEventHubs",
        "LegacyMethodCount": 2,
        "RouterMethodCount": 2,
        "AllLegacySignaturesHaveRouterTwin": true,
        "AllLegacyObsoleteOnGatewayBuilder": true,
        "AllRouterCleanOnRouterBuilder": true,
        "Signatures": [
          "AddAzureEventHubsEventStreamBroker<0>(Action`1): hasRouterTwin=True",
          "AddAzureEventHubsEventStreamBroker<0>(String, Action`1): hasRouterTwin=True"
        ]
      },
      {
        "Name": "Mcp",
        "LegacyMethodCount": 5,
        "RouterMethodCount": 5,
        "AllLegacySignaturesHaveRouterTwin": true,
        "AllLegacyObsoleteOnGatewayBuilder": true,
        "AllRouterCleanOnRouterBuilder": true,
        "Signatures": [
          "AddMcp<0>(Action`1, Action`1, Func`2): hasRouterTwin=True",
          "AddMcpStorage<0>(IMcpStorage): hasRouterTwin=True",
          "AddMcpStorage<1>(): hasRouterTwin=True",
          "AddMcpStorage<0>(Func`2): hasRouterTwin=True",
          "ModifyMcpToolOptions<0>(Action`1): hasRouterTwin=True"
        ]
      },
      {
        "Name": "OpenApi",
        "LegacyMethodCount": 4,
        "RouterMethodCount": 4,
        "AllLegacySignaturesHaveRouterTwin": true,
        "AllLegacyObsoleteOnGatewayBuilder": true,
        "AllRouterCleanOnRouterBuilder": true,
        "Signatures": [
          "AddOpenApi<0>(Func`2): hasRouterTwin=True",
          "AddOpenApiDefinitionStorage<0>(IOpenApiDefinitionStorage): hasRouterTwin=True",
          "AddOpenApiDefinitionStorage<1>(): hasRouterTwin=True",
          "AddOpenApiDefinitionStorage<0>(Func`2): hasRouterTwin=True"
        ]
      }
    ],
    "EntryPoints": [
      "HotChocolateFusionServiceCollectionExtensions.AddGraphQLGateway(IServiceCollection): Obsolete=\"Use AddGraphQLRouterCore() instead.\"",
      "FusionServerServiceCollectionExtensions.AddGraphQLGatewayServer(IServiceCollection): Obsolete=\"Use AddGraphQLRouter() instead.\"",
      "FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLGateway(IHostApplicationBuilder): Obsolete=\"Use AddGraphQLRouter() instead.\""
    ]
  },
  "Excluded": {
    "Packaging": {
      "GatewayConfigurationTypeRemoved": true,
      "TryGetRouterConfigurationAsyncExists": true,
      "TryGetGatewayConfigurationAsyncRemoved": true,
      "GetSupportedRouterFormatsAsyncExists": true,
      "GetSupportedGatewayFormatsAsyncRemoved": true,
      "SupportedGatewayFormatsRemoved": true
    },
    "Setup": {
      "FusionGatewaySetupTypeRemoved": true
    },
    "ExecutionTypes": {
      "IsRouterFieldExists": true,
      "IsGatewayFieldRemoved": true
    },
    "BuildHooks": [
      {
        "Name": "BuildGatewayAsync",
        "IsAssembly": true,
        "IsPublic": false,
        "Obsolete": "Use BuildRouterAsync() instead."
      },
      {
        "Name": "BuildRouterAsync",
        "IsAssembly": true,
        "IsPublic": false,
        "Obsolete": null
      }
    ]
  }
}
```
