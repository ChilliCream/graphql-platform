# GetSecondPage_With_2_Items

## SQL 0

```sql
-- @value='Brand17'
-- @value1='18'
-- @p='3'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE b."Name" > @value OR (b."Name" = @value AND b."Id" > @value1)
ORDER BY b."Name", b."Id"
LIMIT @p
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

