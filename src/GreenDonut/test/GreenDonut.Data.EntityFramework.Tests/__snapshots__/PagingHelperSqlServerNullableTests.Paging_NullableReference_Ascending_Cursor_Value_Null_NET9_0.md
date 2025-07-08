# Paging_NullableReference_Ascending_Cursor_Value_Null

## Result 1

```json
[
  {
    "Id": "68a5c7c2-1234-4def-bc01-9f1a23456789",
    "Date": "2017-10-28",
    "Time": "22:00:00",
    "String": "22:00:00"
  },
  {
    "Id": "d3b7e9f1-4567-4abc-a102-8c2b34567890",
    "Date": "2017-11-03",
    "Time": null,
    "String": null
  }
]
```

## Result 2

```json
[
  {
    "Id": "dd8f3a21-89ab-4cde-a203-7d3c45678901",
    "Date": "2017-11-03",
    "Time": "21:45:00",
    "String": "21:45:00"
  },
  {
    "Id": "62ce9d54-2345-4f01-b304-6e4d56789012",
    "Date": "2017-11-04",
    "Time": "14:00:00",
    "String": "14:00:00"
  }
]
```

## SQL 0

```sql
DECLARE @__p_0 int = 3;

SELECT TOP(@__p_0) [r].[Id], [r].[Date], [r].[String], [r].[Time]
FROM [Records] AS [r]
ORDER BY [r].[Date], [r].[String], [r].[Id]
```

## SQL 1

```sql
DECLARE @__p_2 int = 3;
DECLARE @__value_0 date = '2017-11-03';
DECLARE @__value_1 uniqueIdentifier = 'd3b7e9f1-4567-4abc-a102-8c2b34567890';

SELECT TOP(@__p_2) [r].[Id], [r].[Date], [r].[String], [r].[Time]
FROM [Records] AS [r]
WHERE [r].[Date] > @__value_0 OR ([r].[Date] = @__value_0 AND [r].[String] IS NOT NULL) OR ([r].[Date] = @__value_0 AND [r].[String] IS NULL AND [r].[Id] > @__value_1)
ORDER BY [r].[Date], [r].[String], [r].[Id]
```

