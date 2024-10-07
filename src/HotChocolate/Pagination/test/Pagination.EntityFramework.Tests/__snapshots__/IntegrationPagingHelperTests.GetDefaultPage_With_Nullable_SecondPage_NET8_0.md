# GetDefaultPage_With_Nullable_SecondPage

## SQL 0

```sql
-- @__p_0='Brand10'
-- @__p_2='11'
-- @__p_3='3'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE b."Name" > @__p_0 OR (b."Name" = @__p_0 AND b."AlwaysNull" > NULL) OR (b."Name" = @__p_0 AND b."AlwaysNull" IS NULL AND b."Id" > @__p_2)
ORDER BY b."Name", b."AlwaysNull", b."Id"
LIMIT @__p_3
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(x => x.AlwaysNull).ThenBy(t => t.Id).Where(t => (((t.Name.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.String]).value, String)) > 0) OrElse ((t.Name.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.String]).value, String)) == 0) AndAlso (t.AlwaysNull.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.String]).value, String)) > 0))) OrElse (((t.Name.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.String]).value, String)) == 0) AndAlso (t.AlwaysNull.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.String]).value, String)) == 0)) AndAlso (t.Id.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.Int32]).value, Int32)) > 0)))).Take(3)
```

## Result

```json
{
  "data": {
    "brandsNullable": {
      "edges": [
        {
          "cursor": "QnJhbmQxMTpcbnVsbDoxMg=="
        },
        {
          "cursor": "QnJhbmQxMjpcbnVsbDoxMw=="
        }
      ],
      "nodes": [
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
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": true,
        "startCursor": "QnJhbmQxMTpcbnVsbDoxMg==",
        "endCursor": "QnJhbmQxMjpcbnVsbDoxMw=="
      }
    }
  }
}
```

