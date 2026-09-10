# RenamedWriter_Should_ProduceArchive_That_PinnedOldReaderReadsCompletely

```json
{
  "Metadata": {
    "FormatVersion": "1.0.0",
    "SupportedGatewayFormats": [
      "1.0.0",
      "2.0.0",
      "2.1.0"
    ],
    "SourceSchemas": [
      "billing",
      "inventory"
    ]
  },
  "LatestSupportedGatewayFormat": "2.1.0",
  "GatewayConfigurations": [
    {
      "ProbeMaxVersion": "1.0.0",
      "ResolvedVersion": "1.0.0",
      "Schema": "type Query {\n  ping: String\n}\n",
      "Settings": "{\"maxOperationComplexity\":120,\"nodeResolution\":\"GATEWAY\"}"
    },
    {
      "ProbeMaxVersion": "2.0.0",
      "ResolvedVersion": "2.0.0",
      "Schema": "type Query {\n  ping: String @fusion__gateway_field(schema: SOURCE_SCHEMA)\n}\n",
      "Settings": "{\"maxOperationComplexity\":260,\"nodeResolution\":\"GATEWAY\"}"
    },
    {
      "ProbeMaxVersion": "2.1.0",
      "ResolvedVersion": "2.1.0",
      "Schema": "type Query {\n  ping: String @fusion__gateway_field(schema: SOURCE_SCHEMA)\n  pong: String @fusion__gateway_field(schema: SOURCE_SCHEMA)\n}\n",
      "Settings": "{\"maxOperationComplexity\":520,\"nodeResolution\":\"GATEWAY\"}"
    }
  ],
  "SourceSchemaConfigurations": [
    {
      "Name": "billing",
      "Schema": "type Query {\n  billing: String\n}\n",
      "Extensions": null,
      "Settings": "{\"schemaName\":\"billing\"}"
    },
    {
      "Name": "inventory",
      "Schema": "type Query {\n  inventory: String\n}\n",
      "Extensions": "extend type Query {\n  inventoryExtra: String\n}\n",
      "Settings": "{\"schemaName\":\"inventory\"}"
    }
  ],
  "LegacyArchiveSha256": "c0bc973f5fbceb9a112fda2ea119affdbc184cda295745fb6d146d67dba5c4e9",
  "IsSigned": true,
  "SignatureVerificationResult": "Valid"
}
```
