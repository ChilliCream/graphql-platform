# Configure_Should_UseOneNamedOptionsPipeline_When_RegisteringThroughBothEntryPoints

```json
{
  "Name": "_Default",
  "LegacyName": "_Default",
  "SchemaNames": [
    "_Default"
  ],
  "ManagerCount": 1,
  "OptionsCallbacks": 2,
  "CacheSize": 128,
  "Pipeline": [
    "InstrumentationMiddleware",
    "ExceptionMiddleware",
    "TimeoutMiddleware",
    "DocumentCacheMiddleware",
    "DocumentParserMiddleware",
    "DocumentValidationMiddleware",
    "OperationPlanCacheMiddleware",
    "OperationPlanMiddleware",
    "SkipWarmupExecutionMiddleware",
    "OperationVariableCoercionMiddleware",
    "ConcurrencyGateMiddleware",
    "OperationExecutionMiddleware"
  ]
}
```
