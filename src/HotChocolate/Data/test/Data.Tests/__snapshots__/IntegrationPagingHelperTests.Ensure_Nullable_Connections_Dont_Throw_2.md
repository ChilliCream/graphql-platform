# Ensure_Nullable_Connections_Dont_Throw_2

## SQL 0

```sql
-- @__p_0='11'
SELECT t."Id", t."Name", b."Id" IS NULL, b."Id", b."Description", b."SomeField1", b."SomeField2"
FROM (
    SELECT f."Id", f."BarId", f."Name"
    FROM "Foos" AS f
    ORDER BY f."Name", f."Id"
    LIMIT @__p_0
) AS t
LEFT JOIN "Bars" AS b ON t."BarId" = b."Id"
ORDER BY t."Name", t."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Select(root => new Foo() {Id = root.Id, Name = root.Name, Bar = IIF((root.Bar == null), null, new Bar() {Id = root.Bar.Id, Description = IIF((root.Bar.Description == null), null, root.Bar.Description), SomeField1 = root.Bar.SomeField1, SomeField2 = IIF((root.Bar.SomeField2 == null), null, root.Bar.SomeField2)})}).Take(11)
```

## Result

```json
{
  "data": {
    "foos": {
      "edges": [
        {
          "cursor": "e31Gb28gMTox"
        },
        {
          "cursor": "e31Gb28gMjoy"
        }
      ],
      "nodes": [
        {
          "id": 1,
          "name": "Foo 1",
          "bar": null
        },
        {
          "id": 2,
          "name": "Foo 2",
          "bar": {
            "id": 1,
            "description": "Bar 1",
            "someField1": "abc",
            "someField2": null
          }
        }
      ],
      "pageInfo": {
        "hasNextPage": false,
        "hasPreviousPage": false,
        "startCursor": "e31Gb28gMTox",
        "endCursor": "e31Gb28gMjoy"
      }
    }
  }
}
```

