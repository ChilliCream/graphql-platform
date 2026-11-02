# ToPageAsync_Should_FetchSecondPage_When_OrderKeyContainsNestedOrderBy

## SQL 0

```sql
-- @p='3'
SELECT b0."Id", p0."Id", p0."AvailableStock", p0."BrandId", p0."Description", p0."ImageFileName", p0."MaxStockThreshold", p0."Name", p0."OnReorder", p0."Price", p0."RestockThreshold", p0."TypeId", b0."Name"
FROM (
    SELECT b."Id", b."Name", (
        SELECT p."Price"
        FROM "Products" AS p
        WHERE b."Id" = p."BrandId" AND length(b."Name")::int > 0
        ORDER BY p."Price"
        LIMIT 1) AS c
    FROM "Brands" AS b
    ORDER BY (
        SELECT p."Price"
        FROM "Products" AS p
        WHERE b."Id" = p."BrandId" AND length(b."Name")::int > 0
        ORDER BY p."Price"
        LIMIT 1), b."Id"
    LIMIT @p
) AS b0
LEFT JOIN "Products" AS p0 ON b0."Id" = p0."BrandId"
ORDER BY b0.c, b0."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Products.Where(p => (t.Name.Length > 0)).OrderBy(p => p.Price).First().Price).ThenBy(t => t.Id).Select(t => new Brand() {Id = t.Id, Products = t.Products, Name = t.Name}).Take(3)
```

## SQL 1

```sql
-- @value='0'
-- @value2='2'
-- @p='3'
SELECT b0."Id", p2."Id", p2."AvailableStock", p2."BrandId", p2."Description", p2."ImageFileName", p2."MaxStockThreshold", p2."Name", p2."OnReorder", p2."Price", p2."RestockThreshold", p2."TypeId", b0."Name"
FROM (
    SELECT b."Id", b."Name", (
        SELECT p."Price"
        FROM "Products" AS p
        WHERE b."Id" = p."BrandId" AND length(b."Name")::int > 0
        ORDER BY p."Price"
        LIMIT 1) AS c
    FROM "Brands" AS b
    WHERE (
        SELECT p0."Price"
        FROM "Products" AS p0
        WHERE b."Id" = p0."BrandId" AND length(b."Name")::int > 0
        ORDER BY p0."Price"
        LIMIT 1) > @value OR ((
        SELECT p1."Price"
        FROM "Products" AS p1
        WHERE b."Id" = p1."BrandId" AND length(b."Name")::int > 0
        ORDER BY p1."Price"
        LIMIT 1) = @value AND b."Id" > @value2)
    ORDER BY (
        SELECT p."Price"
        FROM "Products" AS p
        WHERE b."Id" = p."BrandId" AND length(b."Name")::int > 0
        ORDER BY p."Price"
        LIMIT 1), b."Id"
    LIMIT @p
) AS b0
LEFT JOIN "Products" AS p2 ON b0."Id" = p2."BrandId"
ORDER BY b0.c, b0."Id"
```

## Expression 1

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Products.Where(p => (t.Name.Length > 0)).OrderBy(p => p.Price).First().Price).ThenBy(t => t.Id).Where(t => ((t.Products.Where(p => (t.Name.Length > 0)).OrderBy(p => p.Price).First().Price.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass16_0`1[System.Decimal]).value) > 0) OrElse ((t.Products.Where(p => (t.Name.Length > 0)).OrderBy(p => p.Price).First().Price.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass16_0`1[System.Decimal]).value) == 0) AndAlso (t.Id.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass16_0`1[System.Int32]).value) > 0)))).Take(3).Select(t => new Brand() {Id = t.Id, Products = t.Products, Name = t.Name})
```

## Page

```json
[
  3,
  4
]
```
