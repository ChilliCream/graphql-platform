# Paging_First_5_Before_Id_96

## SQL 0

```sql
-- @__p_0='Brand95'
-- @__p_1='96'
-- @__p_2='6'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE b."Name" < @__p_0 OR (b."Name" = @__p_0 AND b."Id" < @__p_1)
ORDER BY b."Name" DESC, b."Id" DESC
LIMIT @__p_2
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Where(t => ((t.Name.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.String]).value, String)) < 0) OrElse ((t.Name.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.String]).value, String)) == 0) AndAlso (t.Id.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.Int32]).value, Int32)) < 0)))).Reverse().Take(6)
```

## Result 3

```json
{
  "HasNextPage": true,
  "HasPreviousPage": true,
  "First": 91,
  "FirstCursor": "QnJhbmQ5MDo5MQ==",
  "Last": 95,
  "LastCursor": "QnJhbmQ5NDo5NQ=="
}
```

## Result 4

```json
[
  {
    "Id": 91,
    "Name": "Brand90",
    "DisplayName": "BrandDisplay90",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country90"
      }
    }
  },
  {
    "Id": 92,
    "Name": "Brand91",
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
    "Name": "Brand92",
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
    "Name": "Brand93",
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
    "Name": "Brand94",
    "DisplayName": "BrandDisplay94",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country94"
      }
    }
  }
]
```

