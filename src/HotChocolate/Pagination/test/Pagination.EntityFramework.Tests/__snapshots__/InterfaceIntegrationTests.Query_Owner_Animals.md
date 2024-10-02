# Query_Owner_Animals

## SQL 0

```sql
SELECT o."Id", o."Name"
FROM "Owners" AS o
ORDER BY o."Name", o."Id"
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Select(root => new Owner() {Id = root.Id, Name = root.Name}).OrderBy(t => t.Name).ThenBy(t => t.Id)
```

## SQL 1

```sql
-- @__keys_0={ '6', '5', '4', '3', '2', ... } (DbType = Object)
SELECT a."Id", a."AnimalType", a."Name", a."OwnerId", a."IsPurring", a."IsBarking"
FROM "Owners" AS o
INNER JOIN "Animal" AS a ON o."Id" = a."OwnerId"
WHERE o."Id" = ANY (@__keys_0)
ORDER BY a."Name", a."Id"
```

## Expression 1

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => value(HotChocolate.Data.InterfaceIntegrationTests+AnimalsByOwnerDataLoader+<>c__DisplayClass2_0).keys.Contains(t.Id)).SelectMany(t => t.Pets).OrderBy(t => t.Name).ThenBy(t => t.Id)
```

## Result 5

```json
{
  "data": {
    "owners": {
      "nodes": [
        {
          "id": 1,
          "name": "Owner 1",
          "pets": {
            "nodes": [
              {
                "__typename": "Cat",
                "id": 1,
                "name": "Cat 1"
              },
              {
                "__typename": "Dog",
                "id": 5,
                "name": "Dog 1"
              },
              {
                "__typename": "Dog",
                "id": 6,
                "name": "Dog 2"
              }
            ]
          }
        },
        {
          "id": 2,
          "name": "Owner 2",
          "pets": {
            "nodes": [
              {
                "__typename": "Cat",
                "id": 2,
                "name": "Cat 2"
              },
              {
                "__typename": "Dog",
                "id": 7,
                "name": "Dog 3"
              },
              {
                "__typename": "Dog",
                "id": 8,
                "name": "Dog 4"
              }
            ]
          }
        },
        {
          "id": 3,
          "name": "Owner 3",
          "pets": {
            "nodes": [
              {
                "__typename": "Cat",
                "id": 3,
                "name": "Cat 3 (Not Pure)"
              },
              {
                "__typename": "Dog",
                "id": 9,
                "name": "Dog 5"
              },
              {
                "__typename": "Dog",
                "id": 10,
                "name": "Dog 6"
              }
            ]
          }
        },
        {
          "id": 4,
          "name": "Owner 4 - No Pets",
          "pets": {
            "nodes": []
          }
        },
        {
          "id": 5,
          "name": "Owner 5 - Only Cat",
          "pets": {
            "nodes": [
              {
                "__typename": "Cat",
                "id": 4,
                "name": "Only Cat"
              }
            ]
          }
        },
        {
          "id": 6,
          "name": "Owner 6 - Only Dog",
          "pets": {
            "nodes": [
              {
                "__typename": "Dog",
                "id": 11,
                "name": "Only Dog"
              }
            ]
          }
        }
      ]
    }
  }
}
```

