# Filter_With_Multi_Expression

## SQL

```text
.param set @keys1 1

SELECT "b"."Id", "b"."DisplayName", "b"."Name", "b"."Details_Country_Name"
FROM "Brands" AS "b"
WHERE "b"."Id" = @keys1 AND "b"."Name" LIKE 'Brand%' AND "b"."Name" LIKE '%0'
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

