# NativeFixtureSchema_Should_BeRejected_When_UsingPinnedGraphQLJsEditions

```json
{
  "HasDirectiveDefinition": true,
  "HasTagDirective": true,
  "HasRequiresOptInDirective": true,
  "HasDeprecatedDirective": true,
  "HasDirectiveDefinitionOnlyDirective": true,
  "Results": [
    {
      "Edition": "graphql-october-2021",
      "Schema": "nativeFixture",
      "SemanticNonNull": false,
      "Result": {
        "IsSuccess": false,
        "StandardOutput": "",
        "StandardError": "Syntax Error: Unexpected Name \"DIRECTIVE_DEFINITION\".\n"
      }
    },
    {
      "Edition": "graphql-september-2025",
      "Schema": "nativeFixture",
      "SemanticNonNull": false,
      "Result": {
        "IsSuccess": false,
        "StandardOutput": "",
        "StandardError": "Syntax Error: Unexpected Name \"DIRECTIVE_DEFINITION\".\n"
      }
    }
  ]
}
```
