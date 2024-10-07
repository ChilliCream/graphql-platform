# Brand_With_Default_Field_Over_Node

## SQL

```text
SELECT b."Id"
FROM "Brands" AS b
WHERE b."Id" = 1
```

## Result

```json
{
  "data": {
    "node": {
      "__typename": "Brand"
    }
  }
}
```

