# GetDefaultPage_With_Deep

## SQL 0

```sql
-- @__p_0='11'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
ORDER BY b."BrandDetails_Country_Name", b."Id"
LIMIT @__p_0
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(x => x.BrandDetails.Country.Name).ThenBy(t => t.Id).Take(11)
```

## Result

```json
{
  "data": {
    "brandsDeep": {
      "edges": [
        {
          "cursor": "e31Db3VudHJ5MDox"
        },
        {
          "cursor": "e31Db3VudHJ5MToy"
        },
        {
          "cursor": "e31Db3VudHJ5MTA6MTE="
        },
        {
          "cursor": "e31Db3VudHJ5MTE6MTI="
        },
        {
          "cursor": "e31Db3VudHJ5MTI6MTM="
        },
        {
          "cursor": "e31Db3VudHJ5MTM6MTQ="
        },
        {
          "cursor": "e31Db3VudHJ5MTQ6MTU="
        },
        {
          "cursor": "e31Db3VudHJ5MTU6MTY="
        },
        {
          "cursor": "e31Db3VudHJ5MTY6MTc="
        },
        {
          "cursor": "e31Db3VudHJ5MTc6MTg="
        }
      ],
      "nodes": [
        {
          "id": 1,
          "name": "Brand:0",
          "displayName": "BrandDisplay0",
          "brandDetails": {
            "country": {
              "name": "Country0"
            }
          }
        },
        {
          "id": 2,
          "name": "Brand:1",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country1"
            }
          }
        },
        {
          "id": 11,
          "name": "Brand:10",
          "displayName": "BrandDisplay10",
          "brandDetails": {
            "country": {
              "name": "Country10"
            }
          }
        },
        {
          "id": 12,
          "name": "Brand:11",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country11"
            }
          }
        },
        {
          "id": 13,
          "name": "Brand:12",
          "displayName": "BrandDisplay12",
          "brandDetails": {
            "country": {
              "name": "Country12"
            }
          }
        },
        {
          "id": 14,
          "name": "Brand:13",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country13"
            }
          }
        },
        {
          "id": 15,
          "name": "Brand:14",
          "displayName": "BrandDisplay14",
          "brandDetails": {
            "country": {
              "name": "Country14"
            }
          }
        },
        {
          "id": 16,
          "name": "Brand:15",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country15"
            }
          }
        },
        {
          "id": 17,
          "name": "Brand:16",
          "displayName": "BrandDisplay16",
          "brandDetails": {
            "country": {
              "name": "Country16"
            }
          }
        },
        {
          "id": 18,
          "name": "Brand:17",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country17"
            }
          }
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": false,
        "startCursor": "e31Db3VudHJ5MDox",
        "endCursor": "e31Db3VudHJ5MTc6MTg="
      }
    }
  }
}
```

