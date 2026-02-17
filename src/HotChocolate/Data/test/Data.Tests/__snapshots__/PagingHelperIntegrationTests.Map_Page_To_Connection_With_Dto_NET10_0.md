# Map_Page_To_Connection_With_Dto

## SQL 0

```sql
-- @p='3'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
ORDER BY b."Name", b."Id"
LIMIT @p
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Take(3)
```

## Result 3

```json
{
  "data": {
    "brands": {
      "edges": [
        {
          "cursor": "e31CcmFuZFw6MDox",
          "displayName": "BrandDisplay0",
          "node": {
            "id": 1,
            "name": "Brand:0"
          }
        },
        {
          "cursor": "e31CcmFuZFw6MToy",
          "displayName": null,
          "node": {
            "id": 2,
            "name": "Brand:1"
          }
        }
      ]
    }
  }
}
```

