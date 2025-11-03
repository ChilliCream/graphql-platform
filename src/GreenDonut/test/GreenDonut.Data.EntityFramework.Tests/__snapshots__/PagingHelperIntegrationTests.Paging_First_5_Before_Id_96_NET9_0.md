# Paging_First_5_Before_Id_96

## SQL 0

```sql
-- @__value_0='Brand95'
-- @__value_1='96'
-- @__p_2='6'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE b."Name" < @__value_0 OR (b."Name" = @__value_0 AND b."Id" < @__value_1)
ORDER BY b."Name" DESC, b."Id" DESC
LIMIT @__p_2
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderByDescending(t => t.Name).ThenByDescending(t => t.Id).Where(t => ((t.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) < 0) OrElse ((t.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) == 0) AndAlso (t.Id.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.Int32]).value) < 0)))).Take(6)
```

## Result 3

```json
{
  "HasNextPage": true,
  "HasPreviousPage": true,
  "First": 92,
  "FirstCursor": "e31CcmFuZFw6OTE6OTI=",
  "Last": 96,
  "LastCursor": "e31CcmFuZFw6OTU6OTY="
}
```

## Result 4

```json
[
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
]
```

