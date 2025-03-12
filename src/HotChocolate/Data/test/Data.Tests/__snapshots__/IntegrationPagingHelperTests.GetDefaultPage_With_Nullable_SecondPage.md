# GetDefaultPage_With_Nullable_SecondPage

## SQL 0

```sql
-- @__value_0='Brand10'
-- @__value_2='11'
-- @__p_3='3'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE b."Name" > @__value_0 OR (b."Name" = @__value_0 AND b."AlwaysNull" > NULL) OR (b."Name" = @__value_0 AND b."AlwaysNull" IS NULL AND b."Id" > @__value_2)
ORDER BY b."Name", b."AlwaysNull", b."Id"
LIMIT @__p_3
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(x => x.AlwaysNull).ThenBy(t => t.Id).Where(t => (((t.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) > 0) OrElse ((t.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) == 0) AndAlso (t.AlwaysNull.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) > 0))) OrElse (((t.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) == 0) AndAlso (t.AlwaysNull.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) == 0)) AndAlso (t.Id.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.Int32]).value) > 0)))).Take(3)
```

## Result

```json
{
  "data": {
    "brandsNullable": {
      "edges": [
        {
          "cursor": "e31CcmFuZFw6MTE6XG51bGw6MTI="
        },
        {
          "cursor": "e31CcmFuZFw6MTI6XG51bGw6MTM="
        }
      ],
      "nodes": [
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
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": true,
        "startCursor": "e31CcmFuZFw6MTE6XG51bGw6MTI=",
        "endCursor": "e31CcmFuZFw6MTI6XG51bGw6MTM="
      }
    }
  }
}
```

