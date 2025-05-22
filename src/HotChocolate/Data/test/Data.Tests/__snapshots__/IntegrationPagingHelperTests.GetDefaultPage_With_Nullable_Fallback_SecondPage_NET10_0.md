# GetDefaultPage_With_Nullable_Fallback_SecondPage

## SQL 0

```sql
-- @value='Brand:11'
-- @value1='12'
-- @p='3'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE COALESCE(b."DisplayName", b."Name") > @value OR (COALESCE(b."DisplayName", b."Name") = @value AND b."Id" > @value1)
ORDER BY COALESCE(b."DisplayName", b."Name"), b."Id"
LIMIT @p
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

