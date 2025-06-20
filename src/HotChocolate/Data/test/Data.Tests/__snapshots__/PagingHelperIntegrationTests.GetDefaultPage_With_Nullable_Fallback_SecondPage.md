# GetDefaultPage_With_Nullable_Fallback_SecondPage

## SQL 0

```sql
-- @__value_0='Brand:11'
-- @__value_1='12'
-- @__p_2='3'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE COALESCE(b."DisplayName", b."Name") > @__value_0 OR (COALESCE(b."DisplayName", b."Name") = @__value_0 AND b."Id" > @__value_1)
ORDER BY COALESCE(b."DisplayName", b."Name"), b."Id"
LIMIT @__p_2
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => (t.DisplayName ?? t.Name)).ThenBy(t => t.Id).Where(t => (((t.DisplayName ?? t.Name).CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) > 0) OrElse (((t.DisplayName ?? t.Name).CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) == 0) AndAlso (t.Id.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.Int32]).value) > 0)))).Take(3)
```

## Result

```json
{
  "data": {
    "brandsNullableFallback": {
      "edges": [
        {
          "cursor": "e31CcmFuZFw6MTM6MTQ="
        },
        {
          "cursor": "e31CcmFuZFw6MTU6MTY="
        }
      ],
      "nodes": [
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
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": true,
        "startCursor": "e31CcmFuZFw6MTM6MTQ=",
        "endCursor": "e31CcmFuZFw6MTU6MTY="
      }
    }
  }
}
```

