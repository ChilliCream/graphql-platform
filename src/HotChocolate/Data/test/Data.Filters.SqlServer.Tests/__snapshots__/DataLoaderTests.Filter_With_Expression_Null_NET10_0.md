# Filter_With_Expression_Null

## SQL

```text
.param set @keys1 1

SELECT "b"."Id", "b"."DisplayName", "b"."Name", "b"."Details_Country_Name"
FROM "Brands" AS "b"
WHERE "b"."Id" = @keys1
```

## Result

```json
{
  "data": {
    "brandByIdFilterNull": {
      "name": "Brand0"
    }
  }
}
```

