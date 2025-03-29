# GetDefaultPage_With_Deep_SecondPage

## SQL 0

```sql
-- @__value_0='Country1'
-- @__value_1='2'
-- @__p_2='3'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE b."BrandDetails_Country_Name" > @__value_0 OR (b."BrandDetails_Country_Name" = @__value_0 AND b."Id" > @__value_1)
ORDER BY b."BrandDetails_Country_Name", b."Id"
LIMIT @__p_2
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(x => x.BrandDetails.Country.Name).ThenBy(t => t.Id).Where(t => ((t.BrandDetails.Country.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) > 0) OrElse ((t.BrandDetails.Country.Name.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) == 0) AndAlso (t.Id.CompareTo(value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.Int32]).value) > 0)))).Take(3)
```

## Result

```json
{
  "data": {
    "brandsDeep": {
      "edges": [
        {
          "cursor": "e31Db3VudHJ5MTA6MTE="
        },
        {
          "cursor": "e31Db3VudHJ5MTE6MTI="
        }
      ],
      "nodes": [
        {
          "id": 11,
          "name": "Brand:10",
          "displayName": "BrandDisplay10",
          "brandDetails": {
            "country": {
              "name": "Country10"
            }
          }
        },
        {
          "id": 12,
          "name": "Brand:11",
          "displayName": null,
          "brandDetails": {
            "country": {
              "name": "Country11"
            }
          }
        }
      ],
      "pageInfo": {
        "hasNextPage": true,
        "hasPreviousPage": true,
        "startCursor": "e31Db3VudHJ5MTA6MTE=",
        "endCursor": "e31Db3VudHJ5MTE6MTI="
      }
    }
  }
}
```

