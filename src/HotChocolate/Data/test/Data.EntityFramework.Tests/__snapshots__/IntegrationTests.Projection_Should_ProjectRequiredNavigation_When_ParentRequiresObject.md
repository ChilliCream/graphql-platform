# Projection_Should_ProjectRequiredNavigation_When_ParentRequiresObject

## SQL

```text
SELECT "b"."Title", "a"."Id", "a"."Name"
FROM "Books" AS "b"
INNER JOIN "Authors" AS "a" ON "b"."AuthorId" = "a"."Id"
```

## Result

```json
{
  "data": {
    "books": [
      {
        "title": "Foo1",
        "authorInfo": {
          "name": "Foo"
        }
      }
    ]
  }
}
```
