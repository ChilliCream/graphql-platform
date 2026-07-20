# Query_Brands_With_BatchResolver_ProductCount

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "id": "QnJhbmQ6MTE=",
          "name": "Zephyr",
          "productCount": 6
        },
        {
          "id": "QnJhbmQ6MTM=",
          "name": "XE",
          "productCount": 3
        },
        {
          "id": "QnJhbmQ6Mw==",
          "name": "WildRunner",
          "productCount": 11
        },
        {
          "id": "QnJhbmQ6Nw==",
          "name": "Solstix",
          "productCount": 9
        },
        {
          "id": "QnJhbmQ6Ng==",
          "name": "Raptor Elite",
          "productCount": 11
        }
      ]
    }
  }
}
```

## Query 1

```sql
-- @__p_0='6'
SELECT b."Id", b."Name"
FROM "Brands" AS b
ORDER BY b."Name" DESC, b."Id"
LIMIT @__p_0
```
