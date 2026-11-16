# Paging_First_5_Before_Id_96

## SQL 0

```sql
-- @__value_0='Brand95'
-- @__value_1='96'
-- @__p_2='5'
SELECT b0."Id", b0."AlwaysNull", b0."DisplayName", b0."Name", b0."BrandDetails_Country_Name", EXISTS (
    SELECT 1
    FROM "Brands" AS b1
    WHERE b1."Name" < @__value_0 OR (b1."Name" = @__value_0 AND b1."Id" < @__value_1)
    ORDER BY b1."Name" DESC, b1."Id" DESC
    OFFSET 5)
FROM (
    SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
    FROM "Brands" AS b
    WHERE b."Name" < @__value_0 OR (b."Name" = @__value_0 AND b."Id" < @__value_1)
    ORDER BY b."Name" DESC, b."Id" DESC
    LIMIT @__p_2
) AS b0
ORDER BY b0."Name", b0."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderByDescending(t => t.Name).ThenByDescending(t => t.Id).Where(t => ((t.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass16_0`1[System.String]).value) < 0) OrElse ((t.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass16_0`1[System.String]).value) == 0) AndAlso (t.Id.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass16_0`1[System.Int32]).value) < 0)))).Take(5).OrderBy(t => t.Name).ThenBy(t => t.Id).Select(t => new StreamRow`1() {Item = t, HasMore = Convert(value(GreenDonut.Data.StreamPagingQueryableExtensions+<>c__DisplayClass2_0`1[GreenDonut.Data.TestContext.Brand]).hasMoreQuery.Any(), Nullable`1)})
```

## Result 3

```json
{
  "Index": null,
  "TotalCount": null,
  "HasNextPage": true,
  "HasPreviousPage": true,
  "Items": [
    {
      "Id": 92,
      "Name": "Brand:91",
      "DisplayName": null,
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": {
        "Country": {
          "Name": "Country91"
        }
      }
    },
    {
      "Id": 93,
      "Name": "Brand:92",
      "DisplayName": "BrandDisplay92",
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": {
        "Country": {
          "Name": "Country92"
        }
      }
    },
    {
      "Id": 94,
      "Name": "Brand:93",
      "DisplayName": null,
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": {
        "Country": {
          "Name": "Country93"
        }
      }
    },
    {
      "Id": 95,
      "Name": "Brand:94",
      "DisplayName": "BrandDisplay94",
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": {
        "Country": {
          "Name": "Country94"
        }
      }
    },
    {
      "Id": 96,
      "Name": "Brand:95",
      "DisplayName": null,
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": {
        "Country": {
          "Name": "Country95"
        }
      }
    }
  ],
  "Cursors": [
    "e31CcmFuZFw6OTE6OTI=",
    "e31CcmFuZFw6OTI6OTM=",
    "e31CcmFuZFw6OTM6OTQ=",
    "e31CcmFuZFw6OTQ6OTU=",
    "e31CcmFuZFw6OTU6OTY="
  ]
}
```
