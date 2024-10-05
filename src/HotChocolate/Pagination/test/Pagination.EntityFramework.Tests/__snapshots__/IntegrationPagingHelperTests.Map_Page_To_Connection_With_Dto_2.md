# Map_Page_To_Connection_With_Dto_2

## SQL 0

```sql
-- @__p_0='3'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
ORDER BY b."Name", b."Id"
LIMIT @__p_0
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
          "cursor": "QnJhbmQwOjE=",
          "displayName": "BrandDisplay0",
          "node": {
            "id": 1,
            "name": "Brand0"
          }
        },
        {
          "cursor": "QnJhbmQxOjI=",
          "displayName": null,
          "node": {
            "id": 2,
            "name": "Brand1"
          }
        }
      ]
    }
  }
}
```

