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
SELECT s."BrandId", s1."Id", s1."AvailableStock", s1."BrandId", s1."Description", s1."ImageFileName", s1."MaxStockThreshold", s1."Name", s1."OnReorder", s1."Price", s1."RestockThreshold", s1."TypeId"
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
) AS s
LEFT JOIN (
    SELECT s0."Id", s0."AvailableStock", s0."BrandId", s0."Description", s0."ImageFileName", s0."MaxStockThreshold", s0."Name", s0."OnReorder", s0."Price", s0."RestockThreshold", s0."TypeId"
    FROM (
        SELECT p1."Id", p1."AvailableStock", p1."BrandId", p1."Description", p1."ImageFileName", p1."MaxStockThreshold", p1."Name", p1."OnReorder", p1."Price", p1."RestockThreshold", p1."TypeId", ROW_NUMBER() OVER(PARTITION BY p1."BrandId" ORDER BY p1."Name", p1."Id") AS row
        FROM "Products" AS p1
        INNER JOIN "Brands" AS b0 ON p1."BrandId" = b0."Id"
        WHERE (p1."BrandId" = 1 OR p1."BrandId" = 2 OR p1."BrandId" = 3) AND (
            SELECT p2."Id"
            FROM "Products" AS p2
            WHERE b0."Id" = p2."BrandId"
            ORDER BY p2."Name" DESC
            LIMIT 1) > 0
    ) AS s0
    WHERE s0.row <= 3
) AS s1 ON s."BrandId" = s1."BrandId"
ORDER BY s."BrandId", s1."BrandId", s1."Name", s1."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => ((((t.BrandId == 1) OrElse (t.BrandId == 2)) OrElse (t.BrandId == 3)) AndAlso (t.Brand.Products.AsQueryable().OrderByDescending(p => p.Name).First().Id > 0))).GroupBy(k => k.BrandId).Select(g => new Group`2() {Key = g.Key, Items = g.OrderBy(p => p.Name).ThenBy(p => p.Id).Take(3).ToList()})
```
