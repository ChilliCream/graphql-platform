# QueryContext_Simple_Selector_Include_Brand

## SQL 0

```sql
-- @p='2'
SELECT p0."Id", p0."Name", b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name", EXISTS (
    SELECT 1
    FROM "Products" AS p1
    ORDER BY p1."Id"
    OFFSET 2)
FROM (
    SELECT p."Id", p."BrandId", p."Name"
    FROM "Products" AS p
    ORDER BY p."Id"
    LIMIT @p
) AS p0
INNER JOIN "Brands" AS b ON p0."BrandId" = b."Id"
ORDER BY p0."Id" DESC
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Id).Select(root => new Product() {Id = root.Id, Name = root.Name, Brand = root.Brand}).Take(2).OrderByDescending(t => t.Id).Select(t => new StreamRow`1() {Item = t, HasMore = Convert(value(GreenDonut.Data.StreamPagingQueryableExtensions+<>c__DisplayClass2_0`1[GreenDonut.Data.TestContext.Product]).hasMoreQuery.Any(), Nullable`1)})
```

## Result 3

```json
{
  "Index": null,
  "TotalCount": null,
  "HasNextPage": false,
  "HasPreviousPage": true,
  "Items": [
    {
      "Id": 2,
      "Name": "Product 0-1",
      "Description": null,
      "Price": 0.0,
      "ImageFileName": null,
      "TypeId": 0,
      "Type": null,
      "BrandId": 0,
      "Brand": {
        "Id": 1,
        "Name": "Brand0",
        "DisplayName": "BrandDisplay0",
        "AlwaysNull": null,
        "Products": [],
        "BrandDetails": {
          "Country": {
            "Name": "Country0"
          }
        }
      },
      "AvailableStock": 0,
      "RestockThreshold": 0,
      "MaxStockThreshold": 0,
      "OnReorder": false
    },
    {
      "Id": 1,
      "Name": "Product 0-0",
      "Description": null,
      "Price": 0.0,
      "ImageFileName": null,
      "TypeId": 0,
      "Type": null,
      "BrandId": 0,
      "Brand": {
        "Id": 1,
        "Name": "Brand0",
        "DisplayName": "BrandDisplay0",
        "AlwaysNull": null,
        "Products": [],
        "BrandDetails": {
          "Country": {
            "Name": "Country0"
          }
        }
      },
      "AvailableStock": 0,
      "RestockThreshold": 0,
      "MaxStockThreshold": 0,
      "OnReorder": false
    }
  ],
  "Cursors": [
    "e30y",
    "e30x"
  ]
}
```
