# Paging_First_5_After_Id_13

## SQL 0

```sql
-- @value='Brand12'
-- @value2='13'
-- @p='6'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE b."Name" > @value OR (b."Name" = @value AND b."Id" > @value2)
ORDER BY b."Name", b."Id"
LIMIT @p
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Where(t => ((t.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass16_0`1[System.String]).value) > 0) OrElse ((t.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass16_0`1[System.String]).value) == 0) AndAlso (t.Id.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass16_0`1[System.Int32]).value) > 0)))).Take(6).Select(t => new StreamRow`1() {Item = t})
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
      "Id": 14,
      "Name": "Brand:13",
      "DisplayName": null,
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": {
        "Country": {
          "Name": "Country13"
        }
      }
    },
    {
      "Id": 15,
      "Name": "Brand:14",
      "DisplayName": "BrandDisplay14",
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": {
        "Country": {
          "Name": "Country14"
        }
      }
    },
    {
      "Id": 16,
      "Name": "Brand:15",
      "DisplayName": null,
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": {
        "Country": {
          "Name": "Country15"
        }
      }
    },
    {
      "Id": 17,
      "Name": "Brand:16",
      "DisplayName": "BrandDisplay16",
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": {
        "Country": {
          "Name": "Country16"
        }
      }
    },
    {
      "Id": 18,
      "Name": "Brand:17",
      "DisplayName": null,
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": {
        "Country": {
          "Name": "Country17"
        }
      }
    }
  ],
  "Cursors": [
    "e31CcmFuZFw6MTM6MTQ=",
    "e31CcmFuZFw6MTQ6MTU=",
    "e31CcmFuZFw6MTU6MTY=",
    "e31CcmFuZFw6MTY6MTc=",
    "e31CcmFuZFw6MTc6MTg="
  ]
}
```
