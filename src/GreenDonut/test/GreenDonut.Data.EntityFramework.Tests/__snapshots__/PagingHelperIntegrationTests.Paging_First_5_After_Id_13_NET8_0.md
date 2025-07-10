# Paging_First_5_After_Id_13

## SQL 0

```sql
-- @__value_0='Brand12'
-- @__value_1='13'
-- @__p_2='6'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE b."Name" > @__value_0 OR (b."Name" = @__value_0 AND b."Id" > @__value_1)
ORDER BY b."Name", b."Id"
LIMIT @__p_2
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Where(t => ((t.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) > 0) OrElse ((t.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) == 0) AndAlso (t.Id.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.Int32]).value) > 0)))).Take(6)
```

## Result 3

```json
{
  "HasNextPage": true,
  "HasPreviousPage": true,
  "First": 14,
  "FirstCursor": "e31CcmFuZFw6MTM6MTQ=",
  "Last": 18,
  "LastCursor": "e31CcmFuZFw6MTc6MTg="
}
```

## Result 4

```json
[
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
]
```

