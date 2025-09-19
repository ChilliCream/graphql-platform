# Query_Pets

## SQL 0

```sql
-- @__p_0='11'
SELECT p."AnimalType" = 'Cat', p."Id", p."Name", p."AnimalType" = 'Dog'
FROM "Pets" AS p
ORDER BY p."Name", p."Id"
LIMIT @__p_0
```

## Expression 0

```text
[Microsoft.EntityFrameworkCore.Query.EntityQueryRootExpression].OrderBy(t => t.Name).ThenBy(t => t.Id).Select(root => IIF((root Is Cat), Convert(new Cat() {Id = Convert(root, Cat).Id, Name = Convert(root, Cat).Name}, Animal), IIF((root Is Dog), Convert(new Dog() {Id = Convert(root, Dog).Id, Name = Convert(root, Dog).Name}, Animal), null))).Take(11)
```

## Result 3

```json
{
  "data": {
    "pets": {
      "nodes": [
        {
          "id": 1,
          "name": "Cat 1"
        },
        {
          "id": 2,
          "name": "Cat 2"
        },
        {
          "id": 3,
          "name": "Cat 3 (Not Pure)"
        },
        {
          "id": 5,
          "name": "Dog 1"
        },
        {
          "id": 6,
          "name": "Dog 2"
        },
        {
          "id": 7,
          "name": "Dog 3"
        },
        {
          "id": 8,
          "name": "Dog 4"
        },
        {
          "id": 9,
          "name": "Dog 5"
        },
        {
          "id": 10,
          "name": "Dog 6"
        },
        {
          "id": 4,
          "name": "Only Cat"
        }
      ]
    }
  }
}
```

