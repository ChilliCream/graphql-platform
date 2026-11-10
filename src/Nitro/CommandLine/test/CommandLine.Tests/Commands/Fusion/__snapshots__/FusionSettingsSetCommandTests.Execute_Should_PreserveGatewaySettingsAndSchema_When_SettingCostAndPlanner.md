# Execute_Should_PreserveGatewaySettingsAndSchema_When_SettingCostAndPlanner

```json
{
  "sourceSchemas": [
    {
      "name": "products"
    }
  ],
  "other": {
    "keep": true
  },
  "cost": {
    "maxFieldCost": 2000,
    "skipAnalyzer": true
  },
  "planner": {
    "mergePolicy": "Aggressive",
    "enableDefer": false
  }
}
```
