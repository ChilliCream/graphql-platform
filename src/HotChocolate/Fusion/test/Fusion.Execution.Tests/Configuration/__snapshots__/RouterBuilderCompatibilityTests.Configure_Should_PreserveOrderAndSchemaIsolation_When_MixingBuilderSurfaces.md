# Configure_Should_PreserveOrderAndSchemaIsolation_When_MixingBuilderSurfaces

```json
{
  "SchemaNames": [
    "one",
    "two"
  ],
  "One": {
    "Name": "one",
    "CacheSize": 144,
    "Feature": "legacy",
    "ParserTokens": 100,
    "IncludeExceptionDetails": true,
    "Services": [
      "application",
      "legacy"
    ],
    "ApplicationService": true,
    "OptionsCallbacks": 4
  },
  "Two": {
    "Name": "two",
    "CacheSize": 32,
    "OptionsCallbacks": 1
  },
  "Callbacks": [
    "router",
    "legacy",
    "third-party",
    "legacy-continuation",
    "router",
    "legacy",
    "third-party",
    "legacy-continuation"
  ],
  "Warmups": [
    "one:router",
    "one:legacy",
    "two:legacy"
  ]
}
```
