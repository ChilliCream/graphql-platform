# GetSecondPage_With_2_Items

## SQL 0

```sql
-- @__p_0='Brand17'
-- @__p_1='18'
-- @__p_2='3'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE b."Name" > @__p_0 OR (b."Name" = @__p_0 AND b."Id" > @__p_1)
ORDER BY b."Name", b."Id"
LIMIT @__p_2
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Where(t => ((t.Name.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.String]).value, String)) > 0) OrElse ((t.Name.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.String]).value, String)) == 0) AndAlso (t.Id.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.Int32]).value, Int32)) > 0)))).Take(3)
```

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "id": 19,
          "name": "Brand18"
        },
        {
          "id": 20,
          "name": "Brand19"
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": true,
        "startCursor": "QnJhbmQxODoxOQ==",
        "endCursor": "QnJhbmQxOToyMA=="
      }
    }
  }
}
```

