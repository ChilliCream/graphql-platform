# Filter_With_Expression_Null

## SQL

```text
.param set @keys '[1]'

SELECT "b"."Id", "b"."DisplayName", "b"."Name", "b"."Details_Country_Name"
FROM "Brands" AS "b"
WHERE "b"."Id" IN (
    SELECT "k"."value"
    FROM json_each(@keys) AS "k"
)
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

