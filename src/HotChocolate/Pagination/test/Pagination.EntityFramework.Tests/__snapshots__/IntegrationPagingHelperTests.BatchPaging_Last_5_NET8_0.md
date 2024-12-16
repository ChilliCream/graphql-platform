# BatchPaging_Last_5

## 1

```json
{
  "First": "MTAw",
  "Last": "OTk=",
  "Items": [
    {
      "Id": 100,
      "Name": "Product 0-99",
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
      "Id": 99,
      "Name": "Product 0-98",
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
  "First": "MjAw",
  "Last": "MTk5",
  "Items": [
    {
      "Id": 200,
      "Name": "Product 1-99",
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
      "Id": 199,
      "Name": "Product 1-98",
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
  "First": "MzAw",
  "Last": "Mjk5",
  "Items": [
    {
      "Id": 300,
      "Name": "Product 2-99",
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
      "Id": 299,
      "Name": "Product 2-98",
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
SELECT t."BrandId", t0."Id", t0."AvailableStock", t0."BrandId", t0."Description", t0."ImageFileName", t0."MaxStockThreshold", t0."Name", t0."OnReorder", t0."Price", t0."RestockThreshold", t0."TypeId"
FROM (
    SELECT p."BrandId"
    FROM "Products" AS p
    WHERE p."BrandId" IN (1, 2, 3)
    GROUP BY p."BrandId"
) AS t
LEFT JOIN (
    SELECT t1."Id", t1."AvailableStock", t1."BrandId", t1."Description", t1."ImageFileName", t1."MaxStockThreshold", t1."Name", t1."OnReorder", t1."Price", t1."RestockThreshold", t1."TypeId"
    FROM (
        SELECT p0."Id", p0."AvailableStock", p0."BrandId", p0."Description", p0."ImageFileName", p0."MaxStockThreshold", p0."Name", p0."OnReorder", p0."Price", p0."RestockThreshold", p0."TypeId", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Id" DESC) AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = 1 OR p0."BrandId" = 2 OR p0."BrandId" = 3
    ) AS t1
    WHERE t1.row <= 3
) AS t0 ON t."BrandId" = t0."BrandId"
ORDER BY t."BrandId", t0."BrandId", t0."Id" DESC
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => (((t.BrandId == 1) OrElse (t.BrandId == 2)) OrElse (t.BrandId == 3))).GroupBy(k => k.BrandId).Select(g => new Group`2() {Key = g.Key, Items = g.OrderByDescending(p => p.Id).Take(3).ToList()})
```

