# ToStreamPageAsync_Should_ResortAscending_When_BackwardPagingStartsFromNullCursor

## SQL 0

```sql
DECLARE @__p_0 int = 3;

SELECT TOP(@__p_0) [r].[Id], [r].[Date], [r].[String], [r].[Time]
FROM [Records] AS [r]
ORDER BY [r].[Date], [r].[Time], [r].[Id]
```

## SQL 1

```sql
DECLARE @__value_0 date = '2017-11-03';
DECLARE @__value_1 uniqueIdentifier = 'd3b7e9f1-4567-4abc-a102-8c2b34567890';
DECLARE @__p_2 int = 1;

SELECT [r0].[Id], [r0].[Date], [r0].[String], [r0].[Time], CASE
    WHEN EXISTS (
        SELECT 1
        FROM [Records] AS [r1]
        WHERE [r1].[Date] IS NULL OR [r1].[Date] < @__value_0 OR ([r1].[Date] = @__value_0 AND [r1].[Time] IS NULL AND [r1].[Id] < @__value_1)
        ORDER BY [r1].[Date] DESC, [r1].[Time] DESC, [r1].[Id] DESC
        OFFSET 1 ROWS) THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END AS [HasMore]
FROM (
    SELECT TOP(@__p_2) [r].[Id], [r].[Date], [r].[String], [r].[Time]
    FROM [Records] AS [r]
    WHERE [r].[Date] IS NULL OR [r].[Date] < @__value_0 OR ([r].[Date] = @__value_0 AND [r].[Time] IS NULL AND [r].[Id] < @__value_1)
    ORDER BY [r].[Date] DESC, [r].[Time] DESC, [r].[Id] DESC
) AS [r0]
ORDER BY [r0].[Date], [r0].[Time], [r0].[Id]
```

## Result 3

```json
{
  "Index": null,
  "TotalCount": null,
  "HasNextPage": true,
  "HasPreviousPage": false,
  "Items": [
    {
      "Id": "68a5c7c2-1234-4def-bc01-9f1a23456789",
      "Date": "2017-10-28",
      "Time": "22:00:00",
      "String": "22:00:00"
    }
  ],
  "Cursors": [
    "e30yMDE3MTAyODoyMjAwMDAwMDAwMDAwOjY4YTVjN2MyLTEyMzQtNGRlZi1iYzAxLTlmMWEyMzQ1Njc4OQ=="
  ]
}
```
