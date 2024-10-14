# Filter_With_Expression

## SQL

```text
SELECT "b"."Id", "b"."DisplayName", "b"."Name", "b"."Details_Country_Name"
FROM "Brands" AS "b"
WHERE "b"."Id" = 1 AND ("b"."Name" LIKE 'Brand%')
```

## Result

```json
{
  "data": {
    "filterExpression": {
      "name": "Brand0"
    }
  }
}
```

