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
DECLARE @__Any_3 bit = CAST(0 AS bit);
DECLARE @__p_2 int = 1;
DECLARE @__value_0 date = '2017-11-03';
DECLARE @__value_1 uniqueIdentifier = 'd3b7e9f1-4567-4abc-a102-8c2b34567890';

SELECT [t].[Id], [t].[Date], [t].[String], [t].[Time], @__Any_3 AS [HasMore]
FROM (
    SELECT TOP(@__p_2) [r].[Id], [r].[Date], [r].[String], [r].[Time]
    FROM [Records] AS [r]
    WHERE [r].[Date] IS NULL OR [r].[Date] < @__value_0 OR ([r].[Date] = @__value_0 AND [r].[Time] IS NULL AND [r].[Id] < @__value_1)
    ORDER BY [r].[Date] DESC, [r].[Time] DESC, [r].[Id] DESC
) AS [t]
ORDER BY [t].[Date], [t].[Time], [t].[Id]
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
