# AsSelector_Should_Project_Conditional_Child_When_Paging_Is_Used

## Result

```json
{
  "data": {
    "tenantsPaged": {
      "nodes": [
        {
          "id": 1
        }
      ]
    }
  }
}
```

## SQL

```text
SELECT "t"."Id"
FROM "Tenants" AS "t"
ORDER BY "t"."Id"
```
