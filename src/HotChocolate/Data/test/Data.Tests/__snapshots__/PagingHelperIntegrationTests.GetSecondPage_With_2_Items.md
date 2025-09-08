# GetSecondPage_With_2_Items

## SQL 0

```sql
-- @__value_0='Brand17'
-- @__value_1='18'
-- @__p_2='3'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE b."Name" > @__value_0 OR (b."Name" = @__value_0 AND b."Id" > @__value_1)
ORDER BY b."Name", b."Id"
LIMIT @__p_2
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Where(t => ((t.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) > 0) OrElse ((t.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) == 0) AndAlso (t.Id.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.Int32]).value) > 0)))).Take(3)
```

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "id": 19,
          "name": "Brand:18"
        },
        {
          "id": 20,
          "name": "Brand:19"
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": true,
        "startCursor": "e31CcmFuZFw6MTg6MTk=",
        "endCursor": "e31CcmFuZFw6MTk6MjA="
      }
    }
  }
}
```

