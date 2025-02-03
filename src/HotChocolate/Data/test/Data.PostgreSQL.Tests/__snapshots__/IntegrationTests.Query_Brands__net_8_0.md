# Query_Brands

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "id": "QnJhbmQ6MTE=",
          "name": "Zephyr"
        },
        {
          "id": "QnJhbmQ6MTM=",
          "name": "XE"
        },
        {
          "id": "QnJhbmQ6Mw==",
          "name": "WildRunner"
        },
        {
          "id": "QnJhbmQ6Nw==",
          "name": "Solstix"
        },
        {
          "id": "QnJhbmQ6Ng==",
          "name": "Raptor Elite"
        },
        {
          "id": "QnJhbmQ6NA==",
          "name": "Quester"
        },
        {
          "id": "QnJhbmQ6MTI=",
          "name": "Legend"
        },
        {
          "id": "QnJhbmQ6OA==",
          "name": "Grolltex"
        },
        {
          "id": "QnJhbmQ6MTA=",
          "name": "Green Equipment"
        },
        {
          "id": "QnJhbmQ6Mg==",
          "name": "Gravitator"
        }
      ]
    }
  }
}
```

## Query 1

```sql
-- @__p_0='11'
SELECT b."Id", b."Name"
FROM "Brands" AS b
ORDER BY b."Name" DESC, b."Id"
LIMIT @__p_0
```

