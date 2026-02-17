# Query_Owner_Animals_With_TotalCount

## SQL 0

```sql
-- @__p_0='11'
SELECT o."Id", o."Name"
FROM "Owners" AS o
ORDER BY o."Name", o."Id"
LIMIT @__p_0
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Select(root => new Owner() {Id = root.Id, Name = root.Name}).Take(11)
```

## SQL 1

```sql
-- @__keys_0={ '1', '2', '3', '4', '5', ... } (DbType = Object)
SELECT p."OwnerId" AS "Key", count(*)::int AS "Count"
FROM "Owners" AS o
INNER JOIN "Pets" AS p ON o."Id" = p."OwnerId"
WHERE o."Id" = ANY (@__keys_0)
GROUP BY p."OwnerId"
```

## Expression 1

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => value(HotChocolate.Data.InterfaceIntegrationTests+AnimalsByOwnerWithCountDataLoader+<>c__DisplayClass2_0).keys.Contains(t.Id)).SelectMany(t => t.Pets).OrderBy(y => y.Name).ThenBy(y => y.Id).GroupBy(t => t.OwnerId).Select(g => new CountResult`1() {Key = g.Key, Count = g.Count()})
```

## SQL 2

```sql
-- @__keys_0={ '1', '2', '3', '4', '5', ... } (DbType = Object)
SELECT t."OwnerId", t0.c, t0."Id", t0."Name", t0.c0, t0."Id0"
FROM (
    SELECT p."OwnerId"
    FROM "Owners" AS o
    INNER JOIN "Pets" AS p ON o."Id" = p."OwnerId"
    WHERE o."Id" = ANY (@__keys_0)
    GROUP BY p."OwnerId"
) AS t
LEFT JOIN (
    SELECT t1.c, t1."Id", t1."Name", t1.c0, t1."Id0", t1."OwnerId"
    FROM (
        SELECT p0."AnimalType" = 'Dog' AS c, p0."Id", p0."Name", p0."AnimalType" = 'Cat' AS c0, o0."Id" AS "Id0", p0."OwnerId", ROW_NUMBER() OVER(PARTITION BY p0."OwnerId" ORDER BY p0."Name", p0."Id") AS row
        FROM "Owners" AS o0
        INNER JOIN "Pets" AS p0 ON o0."Id" = p0."OwnerId"
        WHERE o0."Id" = ANY (@__keys_0)
    ) AS t1
    WHERE t1.row <= 11
) AS t0 ON t."OwnerId" = t0."OwnerId"
ORDER BY t."OwnerId", t0."OwnerId", t0."Name", t0."Id"
```

## Expression 2

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => value(HotChocolate.Data.InterfaceIntegrationTests+AnimalsByOwnerWithCountDataLoader+<>c__DisplayClass2_0).keys.Contains(t.Id)).SelectMany(t => t.Pets).GroupBy(t => t.OwnerId).Select(g => new Group`2() {Key = g.Key, Items = g.OrderBy(y => y.Name).ThenBy(y => y.Id).Select(root => IIF((root Is Dog), Convert(new Dog() {Id = Convert(root, Dog).Id, Name = Convert(root, Dog).Name}, Animal), IIF((root Is Cat), Convert(new Cat() {Id = Convert(root, Cat).Id, Name = Convert(root, Cat).Name}, Animal), null))).Take(11).ToList()})
```

## Result 7

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
            ],
            "totalCount": 3
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
            ],
            "totalCount": 3
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
            ],
            "totalCount": 3
          }
        },
        {
          "id": 4,
          "name": "Owner 4 - No Pets",
          "pets": {
            "nodes": [],
            "totalCount": 0
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
            ],
            "totalCount": 1
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
            ],
            "totalCount": 1
          }
        }
      ]
    }
  }
}
```

