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

SELECT [r0].[Id], [r0].[Date], [r0].[String], [r0].[Time], CASE
    WHEN EXISTS (
        SELECT 1
        FROM [Records] AS [r1]
        WHERE [r1].[Date] IS NULL OR [r1].[Date] < @__value_0 OR ([r1].[Date] = @__value_0 AND ([r1].[Time] IS NULL OR [r1].[Time] < @__value_1)) OR ([r1].[Date] = @__value_0 AND [r1].[Time] IS NOT NULL AND [r1].[Time] = @__value_1 AND [r1].[Id] < @__value_2)
        ORDER BY [r1].[Date] DESC, [r1].[Time] DESC, [r1].[Id] DESC
        OFFSET 2 ROWS) THEN CAST(1 AS bit)
    ELSE CAST(0 AS bit)
END AS [HasMore]
FROM (
    SELECT TOP(@__p_3) [r].[Id], [r].[Date], [r].[String], [r].[Time]
    FROM [Records] AS [r]
    WHERE [r].[Date] IS NULL OR [r].[Date] < @__value_0 OR ([r].[Date] = @__value_0 AND ([r].[Time] IS NULL OR [r].[Time] < @__value_1)) OR ([r].[Date] = @__value_0 AND [r].[Time] IS NOT NULL AND [r].[Time] = @__value_1 AND [r].[Id] < @__value_2)
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
