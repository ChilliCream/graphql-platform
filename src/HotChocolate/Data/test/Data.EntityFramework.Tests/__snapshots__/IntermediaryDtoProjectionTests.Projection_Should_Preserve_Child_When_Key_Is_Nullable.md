# Projection_Should_Preserve_Child_When_Key_Is_Nullable

## SQL

```text
SELECT "b"."Name", "a"."Id" IS NULL, "a"."Name", "a"."Age"
FROM "Books" AS "b"
LEFT JOIN "Authors" AS "a" ON "b"."AuthorId" = "a"."Id"
ORDER BY "b"."Id"
```

## Result

```json
{
  "data": {
    "booksWithNullableAuthorId": [
      {
        "name": "Book",
        "author": {
          "name": "Author"
        }
      }
    ]
  }
}
```
