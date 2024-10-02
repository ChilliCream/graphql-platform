# Brand_With_Name_Selector_is_Null

## SQL

```text
SELECT b."Id", b."DisplayName", b."Name", b."Details_Country_Name"
FROM "Brands" AS b
WHERE b."Id" = 1
```

## Result

```json
{
  "data": {
    "brandByIdSelectorNull": {
      "name": "Brand0"
    }
  }
}
```

