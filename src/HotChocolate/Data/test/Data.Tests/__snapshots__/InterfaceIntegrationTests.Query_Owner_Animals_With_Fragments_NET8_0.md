# Query_Owner_Animals_With_Fragments

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
-- @__keys_0={ '6', '5', '4', '3', '2', ... } (DbType = Object)
SELECT t."OwnerId", t0."Id", t0."AnimalType", t0."Name", t0."OwnerId", t0."IsPurring", t0."IsBarking", t0."Id0"
FROM (
    SELECT p."OwnerId"
    FROM "Owners" AS o
    INNER JOIN "Pets" AS p ON o."Id" = p."OwnerId"
    WHERE o."Id" = ANY (@__keys_0)
    GROUP BY p."OwnerId"
) AS t
LEFT JOIN (
    SELECT t1."Id", t1."AnimalType", t1."Name", t1."OwnerId", t1."IsPurring", t1."IsBarking", t1."Id0"
    FROM (
        SELECT p0."Id", p0."AnimalType", p0."Name", p0."OwnerId", p0."IsPurring", p0."IsBarking", o0."Id" AS "Id0", ROW_NUMBER() OVER(PARTITION BY p0."OwnerId" ORDER BY p0."Name", p0."Id") AS row
        FROM "Owners" AS o0
        INNER JOIN "Pets" AS p0 ON o0."Id" = p0."OwnerId"
        WHERE o0."Id" = ANY (@__keys_0)
    ) AS t1
    WHERE t1.row <= 11
) AS t0 ON t."OwnerId" = t0."OwnerId"
ORDER BY t."OwnerId", t0."OwnerId", t0."Name", t0."Id"
```

## Expression 1

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].Where(t => value(HotChocolate.Data.InterfaceIntegrationTests+AnimalsByOwnerDataLoader+<>c__DisplayClass2_0).keys.Contains(t.Id)).SelectMany(t => t.Pets).GroupBy(t => t.OwnerId).Select(g => new Group`2() {Key = g.Key, Items = g.OrderBy(t => t.Name).ThenBy(t => t.Id).Take(11).ToList()})
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
                "name": "Cat 1",
                "isPurring": false
              },
              {
                "__typename": "Dog",
                "id": 5,
                "name": "Dog 1",
                "isBarking": true
              },
              {
                "__typename": "Dog",
                "id": 6,
                "name": "Dog 2",
                "isBarking": false
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
                "name": "Cat 2",
                "isPurring": false
              },
              {
                "__typename": "Dog",
                "id": 7,
                "name": "Dog 3",
                "isBarking": true
              },
              {
                "__typename": "Dog",
                "id": 8,
                "name": "Dog 4",
                "isBarking": false
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
                "name": "Cat 3 (Not Pure)",
                "isPurring": true
              },
              {
                "__typename": "Dog",
                "id": 9,
                "name": "Dog 5",
                "isBarking": true
              },
              {
                "__typename": "Dog",
                "id": 10,
                "name": "Dog 6",
                "isBarking": false
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
                "name": "Only Cat",
                "isPurring": false
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
                "name": "Only Dog",
                "isBarking": true
              }
            ]
          }
        }
      ]
    }
  }
}
```

