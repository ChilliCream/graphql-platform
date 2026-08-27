# InvalidGenericDataLoaderContracts_RaiseExpectedDiagnostics

## Compilation Diagnostics

```json
[
  {
    "Id": "CS0311",
    "Title": "",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (17,16)-(17,30)",
    "HelpLinkUri": "https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS0311)",
    "MessageFormat": "The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no implicit reference conversion from '{3}' to '{1}'.",
    "Message": "The type 'TestNamespace.INotDataLoader' cannot be used as type parameter 'T' in the generic type or method 'DataLoaderAttribute<T>'. There is no implicit reference conversion from 'TestNamespace.INotDataLoader' to 'GreenDonut.IDataLoader'.",
    "Category": "Compiler",
    "CustomTags": [
      "Compiler",
      "Telemetry",
      "NotConfigurable"
    ]
  }
]
```

## Analyzer Diagnostics

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
    "Id": "CS0311",
    "Title": "",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (17,16)-(17,30)",
    "HelpLinkUri": "https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS0311)",
    "MessageFormat": "The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no implicit reference conversion from '{3}' to '{1}'.",
    "Message": "The type 'TestNamespace.INotDataLoader' cannot be used as type parameter 'T' in the generic type or method 'DataLoaderAttribute<T>'. There is no implicit reference conversion from 'TestNamespace.INotDataLoader' to 'GreenDonut.IDataLoader'.",
    "Category": "Compiler",
    "CustomTags": [
      "Compiler",
      "Telemetry",
      "NotConfigurable"
    ]
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

## Assembly Emit Diagnostics

```json
[
  {
    "Id": "CS0311",
    "Title": "",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (17,16)-(17,30)",
    "HelpLinkUri": "https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS0311)",
    "MessageFormat": "The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no implicit reference conversion from '{3}' to '{1}'.",
    "Message": "The type 'TestNamespace.INotDataLoader' cannot be used as type parameter 'T' in the generic type or method 'DataLoaderAttribute<T>'. There is no implicit reference conversion from 'TestNamespace.INotDataLoader' to 'GreenDonut.IDataLoader'.",
    "Category": "Compiler",
    "CustomTags": [
      "Compiler",
      "Telemetry",
      "NotConfigurable"
    ]
  }
]
```
