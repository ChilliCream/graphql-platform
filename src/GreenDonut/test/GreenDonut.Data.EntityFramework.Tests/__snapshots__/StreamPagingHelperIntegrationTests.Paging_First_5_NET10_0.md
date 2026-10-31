# Paging_First_5

## SQL 0

```sql
-- @p='6'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
ORDER BY b."Name", b."Id"
LIMIT @p
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Take(6).Select(t => new StreamRow`1() {Item = t})
```

## Result 3

```json
{
  "Index": null,
  "TotalCount": null,
  "HasNextPage": true,
  "HasPreviousPage": false,
  "Items": [
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
  ],
  "Cursors": [
    "e31CcmFuZFw6MDox",
    "e31CcmFuZFw6MToy",
    "e31CcmFuZFw6MTA6MTE=",
    "e31CcmFuZFw6MTE6MTI=",
    "e31CcmFuZFw6MTI6MTM="
  ]
}
```
