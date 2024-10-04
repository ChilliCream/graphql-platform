# GetDefaultPage_With_Deep_SecondPage

## SQL 0

```sql
-- @__p_0='Country1'
-- @__p_1='2'
-- @__p_2='3'
SELECT b."Id", b."AlwaysNull", b."DisplayName", b."Name", b."BrandDetails_Country_Name"
FROM "Brands" AS b
WHERE b."BrandDetails_Country_Name" > @__p_0 OR (b."BrandDetails_Country_Name" = @__p_0 AND b."Id" > @__p_1)
ORDER BY b."BrandDetails_Country_Name", b."Id"
LIMIT @__p_2
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(x => x.BrandDetails.Country.Name).ThenBy(t => t.Id).Where(t => ((t.BrandDetails.Country.Name.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.String]).value, String)) > 0) OrElse ((t.BrandDetails.Country.Name.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.String]).value, String)) == 0) AndAlso (t.Id.CompareTo(Convert(value(HotChocolate.Pagination.Expressions.ExpressionHelpers+<>c__DisplayClass7_0`1[System.Int32]).value, Int32)) > 0)))).Take(3)
```

## Result

```json
{
  "data": {
    "brandsDeep": {
      "edges": [
        {
          "cursor": "Q291bnRyeTEwOjEx"
        },
        {
          "cursor": "Q291bnRyeTExOjEy"
        }
      ],
      "nodes": [
        {
          "id": 11,
          "name": "Brand10",
          "displayName": "BrandDisplay10",
          "brandDetails": {
            "country": {
              "name": "Country10"
            }
          }
        },
        {
          "id": 12,
          "name": "Brand11",
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
        "startCursor": "Q291bnRyeTEwOjEx",
        "endCursor": "Q291bnRyeTExOjEy"
      }
    }
  }
}
```

