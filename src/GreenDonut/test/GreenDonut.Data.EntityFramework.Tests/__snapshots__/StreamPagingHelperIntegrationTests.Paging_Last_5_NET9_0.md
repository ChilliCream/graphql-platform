# Paging_Last_5

## SQL 0

```sql
-- @__p_0='5'
SELECT b0."Id", b0."AlwaysNull", b0."DisplayName", b0."Name", b0."BrandDetails_Country_Name", EXISTS (
    SELECT 1
    FROM "Brands" AS b1
    ORDER BY b1."Name" DESC, b1."Id" DESC
    OFFSET 5)
FROM (
    SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
    FROM "Brands" AS b
    ORDER BY b."Name" DESC, b."Id" DESC
    LIMIT @__p_0
) AS b0
ORDER BY b0."Name", b0."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderByDescending(t => t.Name).ThenByDescending(t => t.Id).Take(5).OrderBy(t => t.Name).ThenBy(t => t.Id).Select(t => new StreamRow`1() {Item = t, HasMore = Convert(value(GreenDonut.Data.StreamPagingQueryableExtensions+<>c__DisplayClass2_0`1[GreenDonut.Data.TestContext.Brand]).hasMoreQuery.Any(), Nullable`1)})
```

## Result 3

```json
{
  "Index": null,
  "TotalCount": null,
  "HasNextPage": false,
  "HasPreviousPage": true,
  "Items": [
    {
      "Id": 96,
      "Name": "Brand:95",
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
      "Name": "Brand:96",
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
      "Name": "Brand:97",
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
      "Name": "Brand:98",
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
      "Name": "Brand:99",
      "DisplayName": null,
      "AlwaysNull": null,
      "Products": [],
      "BrandDetails": {
        "Country": {
          "Name": "Country99"
        }
      }
    }
  ],
  "Cursors": [
    "e31CcmFuZFw6OTU6OTY=",
    "e31CcmFuZFw6OTY6OTc=",
    "e31CcmFuZFw6OTc6OTg=",
    "e31CcmFuZFw6OTg6OTk=",
    "e31CcmFuZFw6OTk6MTAw"
  ]
}
```
