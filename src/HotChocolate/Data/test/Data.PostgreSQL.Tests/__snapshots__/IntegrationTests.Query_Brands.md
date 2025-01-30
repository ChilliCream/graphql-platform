# Query_Brands

## Result

```json
{
  "data": {
    "brands": {
      "nodes": [
        {
          "id": "QnJhbmQ6OQ==",
          "name": "AirStrider"
        },
        {
          "id": "QnJhbmQ6NQ==",
          "name": "B&R"
        },
        {
          "id": "QnJhbmQ6MQ==",
          "name": "Daybird"
        },
        {
          "id": "QnJhbmQ6Mg==",
          "name": "Gravitator"
        },
        {
          "id": "QnJhbmQ6MTA=",
          "name": "Green Equipment"
        },
        {
          "id": "QnJhbmQ6OA==",
          "name": "Grolltex"
        },
        {
          "id": "QnJhbmQ6MTI=",
          "name": "Legend"
        },
        {
          "id": "QnJhbmQ6NA==",
          "name": "Quester"
        },
        {
          "id": "QnJhbmQ6Ng==",
          "name": "Raptor Elite"
        },
        {
          "id": "QnJhbmQ6Nw==",
          "name": "Solstix"
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
ORDER BY b."Name", b."Id"
LIMIT @__p_0
```

