# ToBatchPageAsync_Should_PreserveNestedOrdering_When_PredicateContainsOrderBy

## 1

```json
{
  "First": "e31Qcm9kdWN0IDAtMDox",
  "Last": "e31Qcm9kdWN0IDAtMToy",
  "Items": [
    {
      "Id": 1,
      "Name": "Product 0-0"
    },
    {
      "Id": 2,
      "Name": "Product 0-1"
    }
  ]
}
```

## 2

```json
{
  "First": "e31Qcm9kdWN0IDEtMDoxMDE=",
  "Last": "e31Qcm9kdWN0IDEtMToxMDI=",
  "Items": [
    {
      "Id": 101,
      "Name": "Product 1-0"
    },
    {
      "Id": 102,
      "Name": "Product 1-1"
    }
  ]
}
```

## 3

```json
{
  "First": "e31Qcm9kdWN0IDItMDoyMDE=",
  "Last": "e31Qcm9kdWN0IDItMToyMDI=",
  "Items": [
    {
      "Id": 201,
      "Name": "Product 2-0"
    },
    {
      "Id": 202,
      "Name": "Product 2-1"
    }
  ]
}
```

## SQL 0

```sql
SELECT t."BrandId", t0."Id", t0."AvailableStock", t0."BrandId", t0."Description", t0."ImageFileName", t0."MaxStockThreshold", t0."Name", t0."OnReorder", t0."Price", t0."RestockThreshold", t0."TypeId", t0."Id0"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    INNER JOIN "Brands" AS b ON p."BrandId" = b."Id"
    WHERE p."BrandId" IN (1, 2, 3) AND (
        SELECT p0."Id"
        FROM "Products" AS p0
        WHERE b."Id" = p0."BrandId"
        ORDER BY p0."Name" DESC
        LIMIT 1) > 0
    GROUP BY p."BrandId"
) AS t
LEFT JOIN (
    SELECT t1."Id", t1."AvailableStock", t1."BrandId", t1."Description", t1."ImageFileName", t1."MaxStockThreshold", t1."Name", t1."OnReorder", t1."Price", t1."RestockThreshold", t1."TypeId", t1."Id0"
    FROM (
        SELECT p1."Id", p1."AvailableStock", p1."BrandId", p1."Description", p1."ImageFileName", p1."MaxStockThreshold", p1."Name", p1."OnReorder", p1."Price", p1."RestockThreshold", p1."TypeId", b0."Id" AS "Id0", ROW_NUMBER() OVER(PARTITION BY p1."BrandId" ORDER BY p1."Name", p1."Id") AS row
        FROM "Products" AS p1
        INNER JOIN "Brands" AS b0 ON p1."BrandId" = b0."Id"
        WHERE (p1."BrandId" = 1 OR p1."BrandId" = 2 OR p1."BrandId" = 3) AND (
            SELECT p2."Id"
            FROM "Products" AS p2
            WHERE b0."Id" = p2."BrandId"
            ORDER BY p2."Name" DESC
            LIMIT 1) > 0
    ) AS t1
    WHERE t1.row <= 3
) AS t0 ON t."BrandId" = t0."BrandId"
ORDER BY t."BrandId", t0."BrandId", t0."Name", t0."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => ((((t.BrandId == 1) OrElse (t.BrandId == 2)) OrElse (t.BrandId == 3)) AndAlso (t.Brand.Products.AsQueryable().OrderByDescending(p => p.Name).First().Id > 0))).GroupBy(k => k.BrandId).Select(g => new Group`2() {Key = g.Key, Items = g.OrderBy(p => p.Name).ThenBy(p => p.Id).Take(3).ToList()})
```
