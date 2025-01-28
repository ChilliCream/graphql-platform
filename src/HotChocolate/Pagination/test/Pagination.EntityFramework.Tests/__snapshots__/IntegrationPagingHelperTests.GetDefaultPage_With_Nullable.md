# GetDefaultPage_With_Nullable

## SQL 0

```sql
-- @__p_0='11'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
ORDER BY b."Name", b."AlwaysNull", b."Id"
LIMIT @__p_0
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.QueryRootExpression].OrderBy(t => t.Name).ThenBy(x => x.AlwaysNull).ThenBy(t => t.Id).Take(11)
```

## Result

```json
{
  "data": {
    "brandsNullable": {
      "edges": [
        {
          "cursor": "QnJhbmRcOjA6XG51bGw6MQ=="
        },
        {
          "cursor": "QnJhbmRcOjE6XG51bGw6Mg=="
        },
        {
          "cursor": "QnJhbmRcOjEwOlxudWxsOjEx"
        },
        {
          "cursor": "QnJhbmRcOjExOlxudWxsOjEy"
        },
        {
          "cursor": "QnJhbmRcOjEyOlxudWxsOjEz"
        },
        {
          "cursor": "QnJhbmRcOjEzOlxudWxsOjE0"
        },
        {
          "cursor": "QnJhbmRcOjE0OlxudWxsOjE1"
        },
        {
          "cursor": "QnJhbmRcOjE1OlxudWxsOjE2"
        },
        {
          "cursor": "QnJhbmRcOjE2OlxudWxsOjE3"
        },
        {
          "cursor": "QnJhbmRcOjE3OlxudWxsOjE4"
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
        "startCursor": "QnJhbmRcOjA6XG51bGw6MQ==",
        "endCursor": "QnJhbmRcOjE3OlxudWxsOjE4"
      }
    }
  }
}
```

