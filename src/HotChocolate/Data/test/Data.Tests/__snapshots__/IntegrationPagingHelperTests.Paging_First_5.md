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
  "FirstCursor": "e31CcmFuZFw6MDox",
  "Last": 13,
  "LastCursor": "e31CcmFuZFw6MTI6MTM="
}
```

## Result 4

```json
[
  {
    "Id": 1,
    "Name": "Brand:0",
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
    "Name": "Brand:1",
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
    "Name": "Brand:10",
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
    "Name": "Brand:11",
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
    "Name": "Brand:12",
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

