# QueryContext_Simple_Selector

## SQL 0

```sql
-- @__p_0='2'
SELECT p0."Id", p0."Name", EXISTS (
    SELECT 1
    FROM "Products" AS p1
    ORDER BY p1."Id"
    OFFSET 2) AS "HasMore"
FROM (
    SELECT p."Id", p."Name"
    FROM "Products" AS p
    ORDER BY p."Id"
    LIMIT @__p_0
) AS p0
ORDER BY p0."Id" DESC
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Id).Select(t => new Product() {Id = t.Id, Name = t.Name}).Take(2).OrderByDescending(t => t.Id).Select(t => new StreamRow`1() {Item = t, HasMore = Convert(value(GreenDonut.Data.StreamPagingQueryableExtensions+<>c__DisplayClass2_0`1[GreenDonut.Data.TestContext.Product]).hasMoreQuery.Any(), Nullable`1)})
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
      "Brand": null,
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
      "Brand": null,
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
