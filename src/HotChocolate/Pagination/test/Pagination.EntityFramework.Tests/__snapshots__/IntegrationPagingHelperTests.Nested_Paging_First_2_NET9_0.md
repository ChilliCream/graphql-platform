# Nested_Paging_First_2

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

## SQL 1

```sql
-- @__keys_0={ '2', '1' } (DbType = Object)
SELECT p1."BrandId", p3."Id", p3."AvailableStock", p3."BrandId", p3."Description", p3."ImageFileName", p3."MaxStockThreshold", p3."Name", p3."OnReorder", p3."Price", p3."RestockThreshold", p3."TypeId"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    WHERE p."BrandId" = ANY (@__keys_0)
    GROUP BY p."BrandId"
) AS p1
LEFT JOIN (
    SELECT p2."Id", p2."AvailableStock", p2."BrandId", p2."Description", p2."ImageFileName", p2."MaxStockThreshold", p2."Name", p2."OnReorder", p2."Price", p2."RestockThreshold", p2."TypeId"
    FROM (
        SELECT p0."Id", p0."AvailableStock", p0."BrandId", p0."Description", p0."ImageFileName", p0."MaxStockThreshold", p0."Name", p0."OnReorder", p0."Price", p0."RestockThreshold", p0."TypeId", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Name", p0."Id") AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = ANY (@__keys_0)
    ) AS p2
    WHERE p2.row <= 3
) AS p3 ON p1."BrandId" = p3."BrandId"
ORDER BY p1."BrandId", p3."BrandId", p3."Name", p3."Id"
```

## Expression 1

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => value(HotChocolate.Data.IntegrationPagingHelperTests+ProductsByBrandDataLoader+<>c__DisplayClass2_0).keys.Contains(t.BrandId)).GroupBy(t => t.BrandId).Select(g => new Group`2() {Key = g.Key, Items = g.OrderBy(t => t.Name).ThenBy(t => t.Id).Take(3).ToList()})
```

## Result 5

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

