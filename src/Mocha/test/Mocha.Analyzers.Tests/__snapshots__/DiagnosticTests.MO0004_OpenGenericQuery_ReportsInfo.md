# MO0004_OpenGenericQuery_ReportsInfo

```json
[
  {
    "Id": "MO0004",
    "Title": "Open generic message type cannot be dispatched",
    "Severity": "Info",
    "WarningLevel": 1,
    "Location": ": (4,14)-(4,26)",
    "MessageFormat": "Message type '{0}' is an open generic and cannot be dispatched at runtime",
    "Message": "Message type 'global::TestApp.GenericQuery<T>' is an open generic and cannot be dispatched at runtime",
    "Category": "Mediator",
    "CustomTags": []
  },
  {
    "Id": "MO0006",
    "Title": "Open generic handler cannot be auto-registered",
    "Severity": "Info",
    "WarningLevel": 1,
    "Location": ": (6,13)-(6,32)",
    "MessageFormat": "Handler '{0}' has unbound type parameters; source-generated registration is skipped. Register the closed form manually (e.g. AddHandler<ConcreteHandler<MyType>>()) if intentional.",
    "Message": "Handler 'global::TestApp.GenericQueryHandler<T>' has unbound type parameters; source-generated registration is skipped. Register the closed form manually (e.g. AddHandler<ConcreteHandler<MyType>>()) if intentional.",
    "Category": "Mediator",
    "CustomTags": []
  }
]
```
