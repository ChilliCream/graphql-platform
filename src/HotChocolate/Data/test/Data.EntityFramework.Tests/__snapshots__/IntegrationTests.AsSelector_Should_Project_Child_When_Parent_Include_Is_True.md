# AsSelector_Should_Project_Child_When_Parent_Include_Is_True

## Result

```json
{
  "data": {
    "tenants": [
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
