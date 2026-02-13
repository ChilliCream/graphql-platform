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
-- @__keys_0={ '1', '2' } (DbType = Object)
SELECT t."BrandId", t0."Name", t0."BrandId", t0."Id"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    WHERE p."BrandId" = ANY (@__keys_0)
    GROUP BY p."BrandId"
) AS t
LEFT JOIN (
    SELECT t1."Name", t1."BrandId", t1."Id"
    FROM (
        SELECT p0."Name", p0."BrandId", p0."Id", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Name", p0."Id") AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = ANY (@__keys_0)
    ) AS t1
    WHERE t1.row <= 3
) AS t0 ON t."BrandId" = t0."BrandId"
ORDER BY t."BrandId", t0."BrandId", t0."Name", t0."Id"
```

## Expression 1

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => value(HotChocolate.Data.PagingHelperIntegrationTests+ProductsByBrandDataLoader+<>c__DisplayClass2_0).keys.Contains(t.BrandId)).GroupBy(t => t.BrandId).Select(g => new Group`2() {Key = g.Key, Items = g.OrderBy(t => t.Name).ThenBy(t => t.Id).Select(root => new Product() {Name = root.Name, BrandId = root.BrandId, Id = root.Id}).Take(3).ToList()})
```

## Result

```json
{
  "data": {
    "brands": {
      "edges": [
        {
          "cursor": "e31CcmFuZFw6MDox"
        },
        {
          "cursor": "e31CcmFuZFw6MToy"
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
              "startCursor": "e31Qcm9kdWN0IDAtMDox",
              "endCursor": "e31Qcm9kdWN0IDAtMToy"
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
              "startCursor": "e31Qcm9kdWN0IDEtMDoxMDE=",
              "endCursor": "e31Qcm9kdWN0IDEtMToxMDI="
            }
          }
        }
      ]
    }
  }
}
```

