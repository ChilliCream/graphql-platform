# Query_Owner_Animals

## SQL 0

```sql
-- @p='11'
SELECT o."Id", o."Name"
FROM "Owners" AS o
ORDER BY o."Name", o."Id"
LIMIT @p
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Select(root => new Owner() {Id = root.Id, Name = root.Name}).Take(11)
```

## SQL 1

```sql
-- @keys={ '1', '2', '3', '4', '5', ... } (DbType = Object)
SELECT s."OwnerId", s1.c, s1."Id", s1."Name", s1.c0, s1."Id0"
FROM (
    SELECT p."OwnerId"
    FROM "Owners" AS o
    INNER JOIN "Pets" AS p ON o."Id" = p."OwnerId"
    WHERE o."Id" = ANY (@keys)
    GROUP BY p."OwnerId"
) AS s
LEFT JOIN (
    SELECT s0.c, s0."Id", s0."Name", s0.c0, s0."Id0", s0."OwnerId"
    FROM (
        SELECT p0."AnimalType" = 'Dog' AS c, p0."Id", p0."Name", p0."AnimalType" = 'Cat' AS c0, o0."Id" AS "Id0", p0."OwnerId", ROW_NUMBER() OVER(PARTITION BY p0."OwnerId" ORDER BY p0."Name", p0."Id") AS row
        FROM "Owners" AS o0
        INNER JOIN "Pets" AS p0 ON o0."Id" = p0."OwnerId"
        WHERE o0."Id" = ANY (@keys)
    ) AS s0
    WHERE s0.row <= 11
) AS s1 ON s."OwnerId" = s1."OwnerId"
ORDER BY s."OwnerId", s1."OwnerId", s1."Name", s1."Id"
```

## Expression 1

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => value(HotChocolate.Data.InterfaceIntegrationTests+AnimalsByOwnerDataLoader+<>c__DisplayClass2_0).keys.Contains(t.Id)).SelectMany(t => t.Pets).GroupBy(t => t.OwnerId).Select(g => new Group`2() {Key = g.Key, Items = g.OrderBy(y => y.Name).ThenBy(y => y.Id).Select(root => IIF((root Is Dog), Convert(new Dog() {Id = Convert(root, Dog).Id, Name = Convert(root, Dog).Name}, Animal), IIF((root Is Cat), Convert(new Cat() {Id = Convert(root, Cat).Id, Name = Convert(root, Cat).Name}, Animal), null))).Take(11).ToList()})
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

