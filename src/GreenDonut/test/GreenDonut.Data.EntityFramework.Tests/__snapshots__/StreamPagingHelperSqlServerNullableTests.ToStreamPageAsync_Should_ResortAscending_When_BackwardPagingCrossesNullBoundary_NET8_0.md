# ToStreamPageAsync_Should_ResortAscending_When_BackwardPagingCrossesNullBoundary

## SQL 0

```sql
DECLARE @__p_0 int = 5;

SELECT TOP(@__p_0) [r].[Id], [r].[Date], [r].[String], [r].[Time]
FROM [Records] AS [r]
ORDER BY [r].[Date], [r].[Time], [r].[Id]
```

## SQL 1

```sql
DECLARE @__value_0 date = '2017-11-04';
DECLARE @__value_1 time = '14:00:00';
DECLARE @__value_2 uniqueIdentifier = '62ce9d54-2345-4f01-b304-6e4d56789012';
DECLARE @__p_3 int = 2;

SELECT [r].[Id], [r].[Date], [r].[String], [r].[Time]
FROM [Records] AS [r]
WHERE [r].[Date] IS NULL OR [r].[Date] < @__value_0 OR ([r].[Date] = @__value_0 AND ([r].[Time] IS NULL OR [r].[Time] < @__value_1)) OR ([r].[Date] = @__value_0 AND [r].[Time] IS NOT NULL AND [r].[Time] = @__value_1 AND [r].[Id] < @__value_2)
ORDER BY [r].[Date] DESC, [r].[Time] DESC, [r].[Id] DESC
OFFSET @__p_3 ROWS
```

## SQL 2

```sql
DECLARE @__8__locals1_hasMoreValue_4 bit = CAST(1 AS bit);
DECLARE @__p_3 int = 2;
DECLARE @__value_0 date = '2017-11-04';
DECLARE @__value_1 time = '14:00:00';
DECLARE @__value_2 uniqueIdentifier = '62ce9d54-2345-4f01-b304-6e4d56789012';

SELECT [t].[Id], [t].[Date], [t].[String], [t].[Time], @__8__locals1_hasMoreValue_4 AS [HasMore]
FROM (
    SELECT TOP(@__p_3) [r].[Id], [r].[Date], [r].[String], [r].[Time]
    FROM [Records] AS [r]
    WHERE [r].[Date] IS NULL OR [r].[Date] < @__value_0 OR ([r].[Date] = @__value_0 AND ([r].[Time] IS NULL OR [r].[Time] < @__value_1)) OR ([r].[Date] = @__value_0 AND [r].[Time] IS NOT NULL AND [r].[Time] = @__value_1 AND [r].[Id] < @__value_2)
    ORDER BY [r].[Date] DESC, [r].[Time] DESC, [r].[Id] DESC
) AS [t]
ORDER BY [t].[Date], [t].[Time], [t].[Id]
```

## Result 4

```json
{
  "Index": null,
  "TotalCount": null,
  "HasNextPage": true,
  "HasPreviousPage": true,
  "Items": [
    {
      "Id": "d3b7e9f1-4567-4abc-a102-8c2b34567890",
      "Date": "2017-11-03",
      "Time": null,
      "String": null
    },
    {
      "Id": "dd8f3a21-89ab-4cde-a203-7d3c45678901",
      "Date": "2017-11-03",
      "Time": "21:45:00",
      "String": "21:45:00"
    }
  ],
  "Cursors": [
    "e30yMDE3MTEwMzpcbnVsbDpkM2I3ZTlmMS00NTY3LTRhYmMtYTEwMi04YzJiMzQ1Njc4OTA=",
    "e30yMDE3MTEwMzoyMTQ1MDAwMDAwMDAwOmRkOGYzYTIxLTg5YWItNGNkZS1hMjAzLTdkM2M0NTY3ODkwMQ=="
  ]
}
```
