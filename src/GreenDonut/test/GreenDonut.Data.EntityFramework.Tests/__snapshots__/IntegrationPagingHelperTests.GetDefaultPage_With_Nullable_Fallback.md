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
          "cursor": "QnJhbmRcOjE6Mg=="
        },
        {
          "cursor": "QnJhbmRcOjExOjEy"
        },
        {
          "cursor": "QnJhbmRcOjEzOjE0"
        },
        {
          "cursor": "QnJhbmRcOjE1OjE2"
        },
        {
          "cursor": "QnJhbmRcOjE3OjE4"
        },
        {
          "cursor": "QnJhbmRcOjE5OjIw"
        },
        {
          "cursor": "QnJhbmRcOjIxOjIy"
        },
        {
          "cursor": "QnJhbmRcOjIzOjI0"
        },
        {
          "cursor": "QnJhbmRcOjI1OjI2"
        },
        {
          "cursor": "QnJhbmRcOjI3OjI4"
        }
      ],
      "nodes": [
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
          "id": 18,
          "name": "Brand:17",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country17"
            }
          }
        },
        {
          "id": 20,
          "name": "Brand:19",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country19"
            }
          }
        },
        {
          "id": 22,
          "name": "Brand:21",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country21"
            }
          }
        },
        {
          "id": 24,
          "name": "Brand:23",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country23"
            }
          }
        },
        {
          "id": 26,
          "name": "Brand:25",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country25"
            }
          }
        },
        {
          "id": 28,
          "name": "Brand:27",
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
        "startCursor": "QnJhbmRcOjE6Mg==",
        "endCursor": "QnJhbmRcOjI3OjI4"
      }
    }
  }
}
```

