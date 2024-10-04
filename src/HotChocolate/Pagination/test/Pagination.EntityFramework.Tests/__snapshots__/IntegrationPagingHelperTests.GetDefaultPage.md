# GetDefaultPage

## SQL 0

```sql
-- @__p_0='11'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
ORDER BY b."Name", b."Id"
LIMIT @__p_0
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Take(11)
```

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "id": 1,
          "name": "Brand0"
        },
        {
          "id": 2,
          "name": "Brand1"
        },
        {
          "id": 11,
          "name": "Brand10"
        },
        {
          "id": 12,
          "name": "Brand11"
        },
        {
          "id": 13,
          "name": "Brand12"
        },
        {
          "id": 14,
          "name": "Brand13"
        },
        {
          "id": 15,
          "name": "Brand14"
        },
        {
          "id": 16,
          "name": "Brand15"
        },
        {
          "id": 17,
          "name": "Brand16"
        },
        {
          "id": 18,
          "name": "Brand17"
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": false,
        "startCursor": "QnJhbmQwOjE=",
        "endCursor": "QnJhbmQxNzoxOA=="
      }
    }
  }
}
```

