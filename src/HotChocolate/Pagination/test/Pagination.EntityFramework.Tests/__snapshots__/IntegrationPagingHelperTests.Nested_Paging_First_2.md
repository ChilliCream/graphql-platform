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
SELECT t."BrandId", t0."Id", t0."AvailableStock", t0."BrandId", t0."Description", t0."ImageFileName", t0."MaxStockThreshold", t0."Name", t0."OnReorder", t0."Price", t0."RestockThreshold", t0."TypeId"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    WHERE p."BrandId" = ANY (@__keys_0)
    GROUP BY p."BrandId"
) AS t
LEFT JOIN (
    SELECT t1."Id", t1."AvailableStock", t1."BrandId", t1."Description", t1."ImageFileName", t1."MaxStockThreshold", t1."Name", t1."OnReorder", t1."Price", t1."RestockThreshold", t1."TypeId"
    FROM (
        SELECT p0."Id", p0."AvailableStock", p0."BrandId", p0."Description", p0."ImageFileName", p0."MaxStockThreshold", p0."Name", p0."OnReorder", p0."Price", p0."RestockThreshold", p0."TypeId", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Name", p0."Id") AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = ANY (@__keys_0)
    ) AS t1
    WHERE t1.row <= 3
) AS t0 ON t."BrandId" = t0."BrandId"
ORDER BY t."BrandId", t0."BrandId", t0."Name", t0."Id"
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

