# ToBatchStreamPageAsync_Should_ReturnOneFlatQueryAndEmptyPage_When_ForwardFirstIncludesAnUnmatchedKey

## Result 1

```json
{
  "Keys": [
    "A",
    "B",
    "C",
    "D"
  ],
  "A": {
    "Items": [
      "A-Item01",
      "A-Item02"
    ],
    "HasNext": true,
    "HasPrevious": false
  },
  "B": {
    "Items": [
      "B-Item01",
      "B-Item02"
    ]
  },
  "C": {
    "Items": [
      "C-Item01",
      "C-Item02"
    ]
  },
  "D": {
    "Items": [],
    "HasNext": false,
    "HasPrevious": false
  }
}
```

## SQL 0

```sql
SELECT i2."Key", i2."Id", i2."GroupKey", i2."Name"
FROM (
    SELECT DISTINCT i."GroupKey"
    FROM "Items" AS i
    WHERE i."GroupKey" IN ('A', 'B', 'C', 'D')
) AS i1
JOIN LATERAL (
    SELECT i1."GroupKey" AS "Key", i0."Id", i0."GroupKey", i0."Name"
    FROM "Items" AS i0
    WHERE i0."GroupKey" IN ('A', 'B', 'C', 'D') AND i0."GroupKey" = i1."GroupKey"
    ORDER BY i0."Name", i0."Id"
    LIMIT 3
) AS i2 ON TRUE
ORDER BY i2."Key", i2."Name", i2."Id"
```
