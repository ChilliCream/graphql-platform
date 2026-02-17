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
          "name": "Brand:0"
        },
        {
          "id": 2,
          "name": "Brand:1"
        },
        {
          "id": 11,
          "name": "Brand:10"
        },
        {
          "id": 12,
          "name": "Brand:11"
        },
        {
          "id": 13,
          "name": "Brand:12"
        },
        {
          "id": 14,
          "name": "Brand:13"
        },
        {
          "id": 15,
          "name": "Brand:14"
        },
        {
          "id": 16,
          "name": "Brand:15"
        },
        {
          "id": 17,
          "name": "Brand:16"
        },
        {
          "id": 18,
          "name": "Brand:17"
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": false,
        "startCursor": "e31CcmFuZFw6MDox",
        "endCursor": "e31CcmFuZFw6MTc6MTg="
      }
    }
  }
}
```

