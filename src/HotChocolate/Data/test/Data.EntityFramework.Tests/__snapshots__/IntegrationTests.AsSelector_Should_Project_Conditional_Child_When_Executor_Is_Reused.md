# AsSelector_Should_Project_Conditional_Child_When_Executor_Is_Reused

## Result include=false

```json
{
  "data": {
    "tenants": [
      {
        "id": 1
      }
    ]
  }
}
```

## SQL include=false

```text
SELECT "t"."Id"
FROM "Tenants" AS "t"
```

## Result include=true

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

## SQL include=true

```text
SELECT "t"."Id", "w"."Id"
FROM "Tenants" AS "t"
LEFT JOIN "Workspaces" AS "w" ON "t"."Id" = "w"."ConditionalTenantId"
ORDER BY "t"."Id"
```
