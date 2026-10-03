# Paging_Empty_PagingArgs

## SQL 0

```sql
-- @p='11'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
ORDER BY b."Name", b."Id"
LIMIT @p
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Take(11).Select(t => new StreamRow`1() {Item = t})
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
    },
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
    "e31CcmFuZFw6MDox",
    "e31CcmFuZFw6MToy",
    "e31CcmFuZFw6MTA6MTE=",
    "e31CcmFuZFw6MTE6MTI=",
    "e31CcmFuZFw6MTI6MTM=",
    "e31CcmFuZFw6MTM6MTQ=",
    "e31CcmFuZFw6MTQ6MTU=",
    "e31CcmFuZFw6MTU6MTY=",
    "e31CcmFuZFw6MTY6MTc=",
    "e31CcmFuZFw6MTc6MTg="
  ]
}
```
