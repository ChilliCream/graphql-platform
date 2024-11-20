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
          "cursor": "Q291bnRyeTA6MQ=="
        },
        {
          "cursor": "Q291bnRyeTE6Mg=="
        },
        {
          "cursor": "Q291bnRyeTEwOjEx"
        },
        {
          "cursor": "Q291bnRyeTExOjEy"
        },
        {
          "cursor": "Q291bnRyeTEyOjEz"
        },
        {
          "cursor": "Q291bnRyeTEzOjE0"
        },
        {
          "cursor": "Q291bnRyeTE0OjE1"
        },
        {
          "cursor": "Q291bnRyeTE1OjE2"
        },
        {
          "cursor": "Q291bnRyeTE2OjE3"
        },
        {
          "cursor": "Q291bnRyeTE3OjE4"
        }
      ],
      "nodes": [
        {
          "id": 1,
          "name": "Brand0",
          "displayName": "BrandDisplay0",
          "brandDetails": {
            "country": {
              "name": "Country0"
            }
          }
        },
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
          "id": 11,
          "name": "Brand10",
          "displayName": "BrandDisplay10",
          "brandDetails": {
            "country": {
              "name": "Country10"
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
          "id": 13,
          "name": "Brand12",
          "displayName": "BrandDisplay12",
          "brandDetails": {
            "country": {
              "name": "Country12"
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
          "id": 15,
          "name": "Brand14",
          "displayName": "BrandDisplay14",
          "brandDetails": {
            "country": {
              "name": "Country14"
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
          "id": 17,
          "name": "Brand16",
          "displayName": "BrandDisplay16",
          "brandDetails": {
            "country": {
              "name": "Country16"
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
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": false,
        "startCursor": "Q291bnRyeTA6MQ==",
        "endCursor": "Q291bnRyeTE3OjE4"
      }
    }
  }
}
```

