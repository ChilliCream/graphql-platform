# GetDefaultPage_With_Nullable_Fallback

## SQL 0

```sql
-- @__p_0='11'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
ORDER BY COALESCE(b."DisplayName", b."Name"), b."Id"
LIMIT @__p_0
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => (t.DisplayName ?? t.Name)).ThenBy(t => t.Id).Take(11)
```

## Result

```json
{
  "data": {
    "brandsNullableFallback": {
      "edges": [
        {
          "cursor": "QnJhbmQxOjI="
        },
        {
          "cursor": "QnJhbmQxMToxMg=="
        },
        {
          "cursor": "QnJhbmQxMzoxNA=="
        },
        {
          "cursor": "QnJhbmQxNToxNg=="
        },
        {
          "cursor": "QnJhbmQxNzoxOA=="
        },
        {
          "cursor": "QnJhbmQxOToyMA=="
        },
        {
          "cursor": "QnJhbmQyMToyMg=="
        },
        {
          "cursor": "QnJhbmQyMzoyNA=="
        },
        {
          "cursor": "QnJhbmQyNToyNg=="
        },
        {
          "cursor": "QnJhbmQyNzoyOA=="
        }
      ],
      "nodes": [
        {
          "id": 2,
          "name": "Brand1",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country1"
            }
          }
        },
        {
          "id": 12,
          "name": "Brand11",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country11"
            }
          }
        },
        {
          "id": 14,
          "name": "Brand13",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country13"
            }
          }
        },
        {
          "id": 16,
          "name": "Brand15",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country15"
            }
          }
        },
        {
          "id": 18,
          "name": "Brand17",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country17"
            }
          }
        },
        {
          "id": 20,
          "name": "Brand19",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country19"
            }
          }
        },
        {
          "id": 22,
          "name": "Brand21",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country21"
            }
          }
        },
        {
          "id": 24,
          "name": "Brand23",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country23"
            }
          }
        },
        {
          "id": 26,
          "name": "Brand25",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country25"
            }
          }
        },
        {
          "id": 28,
          "name": "Brand27",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country27"
            }
          }
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": false,
        "startCursor": "QnJhbmQxOjI=",
        "endCursor": "QnJhbmQyNzoyOA=="
      }
    }
  }
}
```

