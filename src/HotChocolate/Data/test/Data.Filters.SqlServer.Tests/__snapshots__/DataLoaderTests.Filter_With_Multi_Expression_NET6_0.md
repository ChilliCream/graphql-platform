# Filter_With_Multi_Expression

## SQL

```text
SELECT "b"."Id", "b"."DisplayName", "b"."Name", "b"."Details_Country_Name"
FROM "Brands" AS "b"
WHERE ("b"."Id" = 1) AND (("b"."Name" LIKE 'Brand%') AND ("b"."Name" LIKE '%0'))
```

## Result

```json
{
  "data": {
    "multiFilterExpression": {
      "name": "Brand0"
    }
  }
}
```

