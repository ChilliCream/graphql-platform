# Product_With_Nullable_Reference_Property_Set_To_Null

## SQL

```text
-- @__id_0='1'
SELECT p."Name"
FROM "Products" AS p
WHERE p."Id" = @__id_0
```

## Result

```json
{
  "data": {
    "productById": {
      "name": "Product 0-0",
      "type": null
    }
  }
}
```

