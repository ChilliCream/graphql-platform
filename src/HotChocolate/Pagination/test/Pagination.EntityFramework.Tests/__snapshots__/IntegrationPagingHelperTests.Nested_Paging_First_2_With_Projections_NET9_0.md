# Nested_Paging_First_2_With_Projections

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
SELECT p1."BrandId", p3."Name", p3."BrandId", p3."Id"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    WHERE p."BrandId" = ANY (@__keys_0)
    GROUP BY p."BrandId"
) AS p1
LEFT JOIN (
    SELECT p2."Name", p2."BrandId", p2."Id"
    FROM (
        SELECT p0."Name", p0."BrandId", p0."Id", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Name", p0."Id") AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = ANY (@__keys_0)
    ) AS p2
    WHERE p2.row <= 3
) AS p3 ON p1."BrandId" = p3."BrandId"
ORDER BY p1."BrandId", p3."BrandId", p3."Name", p3."Id"
```

## Expression 1

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => value(HotChocolate.Data.IntegrationPagingHelperTests+ProductsByBrandDataLoader+<>c__DisplayClass2_0).keys.Contains(t.BrandId)).Select(root => new Product() {Name = root.Name, BrandId = root.BrandId, Id = root.Id}).GroupBy(t => t.BrandId).Select(g => new Group`2() {Key = g.Key, Items = g.OrderBy(t => t.Name).ThenBy(t => t.Id).Take(3).ToList()})
```

## Result

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

