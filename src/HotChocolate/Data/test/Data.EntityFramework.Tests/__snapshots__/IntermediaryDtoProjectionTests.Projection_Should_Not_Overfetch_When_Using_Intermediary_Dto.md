# Projection_Should_Not_Overfetch_When_Using_Intermediary_Dto

## SQL

```text
SELECT "b"."Name", "a"."Id" IS NOT NULL, "a"."Name"
FROM "Books" AS "b"
LEFT JOIN "Authors" AS "a" ON "b"."AuthorId" = "a"."Id"
ORDER BY "b"."Id"
```

## Result

```json
{
  "data": {
    "books": [
      {
        "name": "Without Author",
        "author": null
      },
      {
        "name": "With Author",
        "author": {
          "name": "A"
        }
      }
    ]
  }
}
```
