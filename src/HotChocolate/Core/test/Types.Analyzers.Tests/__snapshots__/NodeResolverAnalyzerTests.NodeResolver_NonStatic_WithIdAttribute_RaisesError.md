# NodeResolver_NonStatic_WithIdAttribute_RaisesError

## Compilation Diagnostics

```json
[
  {
    "Id": "CS0050",
    "Title": "",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (10,26)-(10,41)",
    "HelpLinkUri": "https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS0050)",
    "MessageFormat": "Inconsistent accessibility: return type '{1}' is less accessible than method '{0}'",
    "Message": "Inconsistent accessibility: return type 'Task<Product?>' is less accessible than method 'ProductService.GetProductAsync(int)'",
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
    "Id": "CS0050",
    "Title": "",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (10,26)-(10,41)",
    "HelpLinkUri": "https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS0050)",
    "MessageFormat": "Inconsistent accessibility: return type '{1}' is less accessible than method '{0}'",
    "Message": "Inconsistent accessibility: return type 'Task<Product?>' is less accessible than method 'ProductService.GetProductAsync(int)'",
    "Category": "Compiler",
    "CustomTags": [
      "Compiler",
      "Telemetry",
      "NotConfigurable"
    ]
  },
  {
    "Id": "HC0092",
    "Title": "ID Attribute Not Allowed",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (10,43)-(10,45)",
    "MessageFormat": "The [ID] attribute should not be used on node resolver parameters as the NodeResolver attribute already declares the parameter as an ID type",
    "Message": "The [ID] attribute should not be used on node resolver parameters as the NodeResolver attribute already declares the parameter as an ID type",
    "Category": "TypeSystem",
    "CustomTags": []
  }
]
```

## Assembly Emit Diagnostics

```json
[
  {
    "Id": "CS0050",
    "Title": "",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (10,26)-(10,41)",
    "HelpLinkUri": "https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(CS0050)",
    "MessageFormat": "Inconsistent accessibility: return type '{1}' is less accessible than method '{0}'",
    "Message": "Inconsistent accessibility: return type 'Task<Product?>' is less accessible than method 'ProductService.GetProductAsync(int)'",
    "Category": "Compiler",
    "CustomTags": [
      "Compiler",
      "Telemetry",
      "NotConfigurable"
    ]
  }
]
```

