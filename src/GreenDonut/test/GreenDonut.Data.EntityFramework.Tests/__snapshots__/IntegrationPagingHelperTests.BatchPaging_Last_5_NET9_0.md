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
        SELECT p0."Id", p0."AvailableStock", p0."BrandId", p0."Description", p0."ImageFileName", p0."MaxStockThreshold", p0."Name", p0."OnReorder", p0."Price", p0."RestockThreshold", p0."TypeId", ROW_NUMBER() OVER(PARTITION BY p0."BrandId" ORDER BY p0."Id" DESC) AS row
        FROM "Products" AS p0
        WHERE p0."BrandId" = 1 OR p0."BrandId" = 2 OR p0."BrandId" = 3
    ) AS p2
    WHERE p2.row <= 3
) AS p3 ON p1."BrandId" = p3."BrandId"
ORDER BY p1."BrandId", p3."BrandId", p3."Id" DESC
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => (((t.BrandId == 1) OrElse (t.BrandId == 2)) OrElse (t.BrandId == 3))).GroupBy(k => k.BrandId).Select(g => new Group`2() {Key = g.Key, Items = g.OrderByDescending(p => p.Id).Take(3).ToList()})
```

