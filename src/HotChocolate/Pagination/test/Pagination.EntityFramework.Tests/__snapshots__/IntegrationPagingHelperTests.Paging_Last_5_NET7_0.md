# Paging_Last_5

## SQL 0

```sql
-- @__p_0='6'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
ORDER BY b."Name" DESC, b."Id" DESC
LIMIT @__p_0
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Reverse().Take(6)
```

## Result 3

```json
{
  "HasNextPage": false,
  "HasPreviousPage": true,
  "First": 96,
  "FirstCursor": "QnJhbmQ5NTo5Ng==",
  "Last": 100,
  "LastCursor": "QnJhbmQ5OToxMDA="
}
```

## Result 4

```json
[
  {
    "Id": 96,
    "Name": "Brand95",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country95"
      }
    }
  },
  {
    "Id": 97,
    "Name": "Brand96",
    "DisplayName": "BrandDisplay96",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country96"
      }
    }
  },
  {
    "Id": 98,
    "Name": "Brand97",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country97"
      }
    }
  },
  {
    "Id": 99,
    "Name": "Brand98",
    "DisplayName": "BrandDisplay98",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country98"
      }
    }
  },
  {
    "Id": 100,
    "Name": "Brand99",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country99"
      }
    }
  }
]
```

