# Nested_Paging_First_2

## SQL

```sql
SELECT p."Id", p."AvailableStock", p."BrandId", p."Description", p."ImageFileName", p."MaxStockThreshold", p."Name", p."OnReorder", p."Price", p."RestockThreshold", p."TypeId"
FROM "Products" AS p
WHERE p."BrandId" IN (2, 1)
ORDER BY p."Name", p."Id"
```

## Result 2

```json
{
  "data": {
    "brands": {
      "edges": [
        {
          "cursor": "QnJhbmQwOjE="
        },
        {
          "cursor": "QnJhbmQxOjI="
        }
      ],
      "nodes": [
        {
          "products": {
            "nodes": [
              {
                "name": "Product 0-0"
              },
              {
                "name": "Product 0-1"
              }
            ],
            "pageInfo": {
              "hasNextPage": true,
              "hasPreviousPage": false,
              "startCursor": "UHJvZHVjdCAwLTA6MQ==",
              "endCursor": "UHJvZHVjdCAwLTE6Mg=="
            }
          }
        },
        {
          "products": {
            "nodes": [
              {
                "name": "Product 1-0"
              },
              {
                "name": "Product 1-1"
              }
            ],
            "pageInfo": {
              "hasNextPage": true,
              "hasPreviousPage": false,
              "startCursor": "UHJvZHVjdCAxLTA6MTAx",
              "endCursor": "UHJvZHVjdCAxLTE6MTAy"
            }
          }
        }
      ]
    }
  }
}
```

