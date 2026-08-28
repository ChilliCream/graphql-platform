# InvalidGenericDataLoaderContracts_RaiseExpectedDiagnostics

```json
[
  {
    "Id": "HC0122",
    "Title": "Invalid DataLoader Type",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (17,5)-(17,31)",
    "MessageFormat": "The type argument of [DataLoader<T>] must be an interface that derives from exactly one closed IBatchDataLoader<TKey, TValue> or ICacheDataLoader<TKey, TValue> contract",
    "Message": "The type argument of [DataLoader<T>] must be an interface that derives from exactly one closed IBatchDataLoader<TKey, TValue> or ICacheDataLoader<TKey, TValue> contract",
    "Category": "DataLoader",
    "CustomTags": []
  },
  {
    "Id": "HC0122",
    "Title": "Invalid DataLoader Type",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (20,5)-(20,37)",
    "MessageFormat": "The type argument of [DataLoader<T>] must be an interface that derives from exactly one closed IBatchDataLoader<TKey, TValue> or ICacheDataLoader<TKey, TValue> contract",
    "Message": "The type argument of [DataLoader<T>] must be an interface that derives from exactly one closed IBatchDataLoader<TKey, TValue> or ICacheDataLoader<TKey, TValue> contract",
    "Category": "DataLoader",
    "CustomTags": []
  },
  {
    "Id": "HC0123",
    "Title": "Invalid DataLoader Key Parameter",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (24,83)-(24,86)",
    "MessageFormat": "The first parameter of a [DataLoader<T>] method must be {0}",
    "Message": "The first parameter of a [DataLoader<T>] method must be IReadOnlyList<TKey>",
    "Category": "DataLoader",
    "CustomTags": []
  },
  {
    "Id": "HC0123",
    "Title": "Invalid DataLoader Key Parameter",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (28,57)-(28,75)",
    "MessageFormat": "The first parameter of a [DataLoader<T>] method must be {0}",
    "Message": "The first parameter of a [DataLoader<T>] method must be TKey",
    "Category": "DataLoader",
    "CustomTags": []
  },
  {
    "Id": "HC0124",
    "Title": "Invalid DataLoader Return Type",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (32,20)-(32,46)",
    "MessageFormat": "The return type of a [DataLoader<T>] method must be {0}",
    "Message": "The return type of a [DataLoader<T>] method must be Task/ValueTask of IReadOnlyDictionary<TKey, TValue> or IDictionary<TKey, TValue>",
    "Category": "DataLoader",
    "CustomTags": []
  },
  {
    "Id": "HC0124",
    "Title": "Invalid DataLoader Return Type",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (37,20)-(37,29)",
    "MessageFormat": "The return type of a [DataLoader<T>] method must be {0}",
    "Message": "The return type of a [DataLoader<T>] method must be Task/ValueTask of TValue",
    "Category": "DataLoader",
    "CustomTags": []
  }
]
```
