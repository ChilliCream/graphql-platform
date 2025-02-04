# Query_Node_Product

## Result

```json
{
  "data": {
    "product": {
      "id": "UHJvZHVjdDox",
      "name": "Wanderer Black Hiking Boots"
    }
  }
}
```

## Query 1

```sql
-- @__ids_0={ '1' } (DbType = Object)
SELECT p."Id", p."Name"
FROM "Products" AS p
WHERE p."Id" = ANY (@__ids_0)
```

