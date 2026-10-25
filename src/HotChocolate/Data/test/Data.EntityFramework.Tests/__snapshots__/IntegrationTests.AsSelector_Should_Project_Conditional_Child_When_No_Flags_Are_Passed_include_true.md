# AsSelector_Should_Project_Conditional_Child_When_No_Flags_Are_Passed

## Result

```json
{
  "data": {
    "tenantsWithDefaultSelector": [
      {
        "id": 1,
        "workspaces": [
          {
            "id": 1
          },
          {
            "id": 2
          }
        ]
      }
    ]
  }
}
```

## SQL

```text
SELECT "t"."Id", "w"."Id"
FROM "Tenants" AS "t"
LEFT JOIN "Workspaces" AS "w" ON "t"."Id" = "w"."ConditionalTenantId"
ORDER BY "t"."Id"
```
