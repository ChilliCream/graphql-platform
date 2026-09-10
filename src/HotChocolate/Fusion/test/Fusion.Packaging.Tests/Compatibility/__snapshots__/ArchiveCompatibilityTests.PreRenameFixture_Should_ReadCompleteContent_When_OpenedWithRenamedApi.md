# PreRenameFixture_Should_ReadCompleteContent_When_OpenedWithRenamedApi

```json
{
  "Metadata": {
    "FormatVersion": "1.0.0",
    "SupportedRouterFormats": [
      "1.0.0",
      "2.0.0",
      "2.1.0"
    ],
    "SourceSchemas": [
      "accounts",
      "catalog"
    ]
  },
  "Latest": "2.1.0",
  "Supported": [
    "2.1.0",
    "2.0.0",
    "1.0.0"
  ],
  "GatewayConfigurations": [
    {
      "ProbeMaxVersion": "1.0.0",
      "Found": true,
      "ResolvedVersion": "1.0.0",
      "Schema": "type Query {\n  hello: String\n}\n",
      "Settings": "{\n  \"maxOperationComplexity\": 100,\n  \"nodeResolution\": \"GATEWAY\"\n}"
    },
    {
      "ProbeMaxVersion": "2.0.0",
      "Found": true,
      "ResolvedVersion": "2.0.0",
      "Schema": "type Query {\n  hello: String @fusion__gateway_field(schema: SOURCE_SCHEMA)\n}\n",
      "Settings": "{\n  \"maxOperationComplexity\": 250,\n  \"nodeResolution\": \"GATEWAY\"\n}"
    },
    {
      "ProbeMaxVersion": "2.0.5",
      "Found": true,
      "ResolvedVersion": "2.0.0",
      "Schema": "type Query {\n  hello: String @fusion__gateway_field(schema: SOURCE_SCHEMA)\n}\n",
      "Settings": "{\n  \"maxOperationComplexity\": 250,\n  \"nodeResolution\": \"GATEWAY\"\n}"
    },
    {
      "ProbeMaxVersion": "2.1.0",
      "Found": true,
      "ResolvedVersion": "2.1.0",
      "Schema": "type Query {\n  hello: String @fusion__gateway_field(schema: SOURCE_SCHEMA)\n  world: String @fusion__gateway_field(schema: SOURCE_SCHEMA)\n}\n",
      "Settings": "{\n  \"maxOperationComplexity\": 500,\n  \"nodeResolution\": \"GATEWAY\"\n}"
    }
  ],
  "SourceSchemaConfigurations": [
    {
      "Name": "accounts",
      "Schema": "type Query {\n  account: String\n}\n",
      "Extensions": "extend type Query {\n  accountExtra: String\n}\n",
      "Settings": "{\n  \"schemaName\": \"accounts\"\n}"
    },
    {
      "Name": "catalog",
      "Schema": "type Query {\n  catalog: String\n}\n",
      "Extensions": null,
      "Settings": "{\n  \"schemaName\": \"catalog\"\n}"
    }
  ],
  "LegacyArchiveSha256": "319981fa1de14abd80b22ec5e53b58d88fcd1c60e29480425ec9e0bde682f304",
  "IsSigned": true,
  "SignatureVerificationResult": "Valid",
  "SignatureInfo": {
    "Algorithm": "SHA256",
    "IsValid": true,
    "HasSignerCertificate": true
  }
}
```
