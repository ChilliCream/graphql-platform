# Ensure_Nullable_Connections_Dont_Throw_2

## SQL 0

```sql
-- @p='11'
SELECT f0."Id", f0."Name", b."Id" IS NULL, b."Id", b."Description", b."SomeField1", b."SomeField2"
FROM (
    SELECT f."Id", f."BarId", f."Name"
    FROM "Foos" AS f
    ORDER BY f."Name", f."Id"
    LIMIT @p
) AS f0
LEFT JOIN "Bars" AS b ON f0."BarId" = b."Id"
ORDER BY f0."Name", f0."Id"
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

