# Paging_NullableReference_Descending_Cursor_Value_NonNull

## Result 1

```json
[
  {
    "Id": "a1d5b763-6789-4f23-c405-5f5e67890123",
    "Date": "2017-11-04",
    "Time": "19:40:00",
    "String": "19:40:00"
  },
  {
    "Id": "62ce9d54-2345-4f01-b304-6e4d56789012",
    "Date": "2017-11-04",
    "Time": "14:00:00",
    "String": "14:00:00"
  },
  {
    "Id": "dd8f3a21-89ab-4cde-a203-7d3c45678901",
    "Date": "2017-11-03",
    "Time": "21:45:00",
    "String": "21:45:00"
  }
]
```

## Result 2

```json
[
  {
    "Id": "d3b7e9f1-4567-4abc-a102-8c2b34567890",
    "Date": "2017-11-03",
    "Time": null,
    "String": null
  },
  {
    "Id": "68a5c7c2-1234-4def-bc01-9f1a23456789",
    "Date": "2017-10-28",
    "Time": "22:00:00",
    "String": "22:00:00"
  }
]
```

## SQL 0

```sql
DECLARE @__p_0 int = 4;

SELECT TOP(@__p_0) [r].[Id], [r].[Date], [r].[String], [r].[Time]
FROM [Records] AS [r]
ORDER BY [r].[Date] DESC, [r].[String] DESC, [r].[Id] DESC
```

## SQL 1

```sql
DECLARE @__p_3 int = 4;
DECLARE @__value_0 date = '2017-11-03';
DECLARE @__value_1 nvarchar(4000) = N'21:45:00';
DECLARE @__value_2 uniqueIdentifier = 'dd8f3a21-89ab-4cde-a203-7d3c45678901';

SELECT TOP(@__p_3) [r].[Id], [r].[Date], [r].[String], [r].[Time]
FROM [Records] AS [r]
WHERE [r].[Date] < @__value_0 OR [r].[Date] IS NULL OR ([r].[Date] = @__value_0 AND ([r].[String] < @__value_1 OR [r].[String] IS NULL)) OR ([r].[Date] = @__value_0 AND [r].[String] = @__value_1 AND [r].[Id] < @__value_2)
ORDER BY [r].[Date] DESC, [r].[String] DESC, [r].[Id] DESC
```

