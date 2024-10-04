# Paging_First_5

## SQL 0

```sql
-- @__p_0='6'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
ORDER BY b."Name", b."Id"
LIMIT @__p_0
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Take(6)
```

## Result 3

```json
{
  "HasNextPage": true,
  "HasPreviousPage": false,
  "First": 1,
  "FirstCursor": "QnJhbmQwOjE=",
  "Last": 13,
  "LastCursor": "QnJhbmQxMjoxMw=="
}
```

## Result 4

```json
[
  {
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
  {
    "Id": 2,
    "Name": "Brand1",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country1"
      }
    }
  },
  {
    "Id": 11,
    "Name": "Brand10",
    "DisplayName": "BrandDisplay10",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country10"
      }
    }
  },
  {
    "Id": 12,
    "Name": "Brand11",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country11"
      }
    }
  },
  {
    "Id": 13,
    "Name": "Brand12",
    "DisplayName": "BrandDisplay12",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country12"
      }
    }
  }
]
```

