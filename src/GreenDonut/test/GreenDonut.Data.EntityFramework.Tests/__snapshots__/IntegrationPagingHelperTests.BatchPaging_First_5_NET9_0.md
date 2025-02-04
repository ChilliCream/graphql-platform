# BatchPaging_First_5

## 1

```json
{
  "First": "UHJvZHVjdCAwLTA6MQ==",
  "Last": "UHJvZHVjdCAwLTE6Mg==",
  "Items": [
    {
      "Id": 1,
      "Name": "Product 0-0",
      "Description": null,
      "Price": 0.0,
      "ImageFileName": null,
      "TypeId": 1,
      "Type": null,
      "BrandId": 1,
      "Brand": null,
      "AvailableStock": 0,
      "RestockThreshold": 0,
      "MaxStockThreshold": 0,
      "OnReorder": false
    },
    {
      "Id": 2,
      "Name": "Product 0-1",
      "Description": null,
      "Price": 0.0,
      "ImageFileName": null,
      "TypeId": 1,
      "Type": null,
      "BrandId": 1,
      "Brand": null,
      "AvailableStock": 0,
      "RestockThreshold": 0,
      "MaxStockThreshold": 0,
      "OnReorder": false
    }
  ]
}
```

## 2

```json
{
  "First": "UHJvZHVjdCAxLTA6MTAx",
  "Last": "UHJvZHVjdCAxLTE6MTAy",
  "Items": [
    {
      "Id": 101,
      "Name": "Product 1-0",
      "Description": null,
      "Price": 0.0,
      "ImageFileName": null,
      "TypeId": 1,
      "Type": null,
      "BrandId": 2,
      "Brand": null,
      "AvailableStock": 0,
      "RestockThreshold": 0,
      "MaxStockThreshold": 0,
      "OnReorder": false
    },
    {
      "Id": 102,
      "Name": "Product 1-1",
      "Description": null,
      "Price": 0.0,
      "ImageFileName": null,
      "TypeId": 1,
      "Type": null,
      "BrandId": 2,
      "Brand": null,
      "AvailableStock": 0,
      "RestockThreshold": 0,
      "MaxStockThreshold": 0,
      "OnReorder": false
    }
  ]
}
```

## 3

```json
{
  "First": "UHJvZHVjdCAyLTA6MjAx",
  "Last": "UHJvZHVjdCAyLTE6MjAy",
  "Items": [
    {
      "Id": 201,
      "Name": "Product 2-0",
      "Description": null,
      "Price": 0.0,
      "ImageFileName": null,
      "TypeId": 1,
      "Type": null,
      "BrandId": 3,
      "Brand": null,
      "AvailableStock": 0,
      "RestockThreshold": 0,
      "MaxStockThreshold": 0,
      "OnReorder": false
    },
    {
      "Id": 202,
      "Name": "Product 2-1",
      "Description": null,
      "Price": 0.0,
      "ImageFileName": null,
      "TypeId": 1,
      "Type": null,
      "BrandId": 3,
      "Brand": null,
      "AvailableStock": 0,
      "RestockThreshold": 0,
      "MaxStockThreshold": 0,
      "OnReorder": false
    }
  ]
}
```

## SQL 0

```sql
SELECT p1."BrandId", p3."Id", p3."AvailableStock", p3."BrandId", p3."Description", p3."ImageFileName", p3."MaxStockThreshold", p3."Name", p3."OnReorder", p3."Price", p3."RestockThreshold", p3."TypeId"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    WHERE p."BrandId" IN (1, 2, 3)
    GROUP BY p."BrandId"
) AS p1
LEFT JOIN (
    SELECT p2."Id", p2."AvailableStock", p2."BrandId", p2."Description", p2."ImageFileName", p2."MaxStockThreshold", p2."Name", p2."OnReorder", p2."Price", p2."RestockThreshold", p2."TypeId"
    FROM (
        SELECT p0."Id", p0."AvailableStock", p0."BrandId", p0."Description", p0."ImageFileName", p0."MaxStockThreshold", p0."Name", p0."OnReorder", p0."Price", p0."RestockThreshold", p0."TypeId", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Name", p0."Id") AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = 1 OR p0."BrandId" = 2 OR p0."BrandId" = 3
    ) AS p2
    WHERE p2.row <= 3
) AS p3 ON p1."BrandId" = p3."BrandId"
ORDER BY p1."BrandId", p3."BrandId", p3."Name", p3."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => (((t.BrandId == 1) OrElse (t.BrandId == 2)) OrElse (t.BrandId == 3))).GroupBy(k => k.BrandId).Select(g => new Group`2() {Key = g.Key, Items = g.OrderBy(p => p.Name).ThenBy(p => p.Id).Take(3).ToList()})
```

