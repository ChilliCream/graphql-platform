# GetDefaultPage_With_Nullable_Fallback_SecondPage

## SQL 0

```sql
-- @__p_0='Brand11'
-- @__p_1='12'
-- @__p_2='3'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE COALESCE(b."DisplayName", b."Name") > @__p_0 OR (COALESCE(b."DisplayName", b."Name") = @__p_0 AND b."Id" > @__p_1)
ORDER BY COALESCE(b."DisplayName", b."Name"), b."Id"
LIMIT @__p_2
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => (t.DisplayName ?? t.Name)).ThenBy(t => t.Id).Where(t => (((t.DisplayName ?? t.Name).CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.String]).value, String)) > 0) OrElse (((t.DisplayName ?? t.Name).CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.String]).value, String)) == 0) AndAlso (t.Id.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.Int32]).value, Int32)) > 0)))).Take(3)
```

## Result

```json
{
  "data": {
    "brandsNullableFallback": {
      "edges": [
        {
          "cursor": "QnJhbmQxMzoxNA=="
        },
        {
          "cursor": "QnJhbmQxNToxNg=="
        }
      ],
      "nodes": [
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
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": true,
        "startCursor": "QnJhbmQxMzoxNA==",
        "endCursor": "QnJhbmQxNToxNg=="
      }
    }
  }
}
```

