# MO0004_OpenGenericCommand_ReportsInfo

```json
[
  {
    "Id": "MO0004",
    "Title": "Open generic message type cannot be dispatched",
    "Severity": "Info",
    "WarningLevel": 1,
    "Location": ": (4,14)-(4,28)",
    "MessageFormat": "Message type '{0}' is an open generic and cannot be dispatched at runtime",
    "Message": "Message type 'global::TestApp.GenericCommand<T>' is an open generic and cannot be dispatched at runtime",
    "Category": "Mediator",
    "CustomTags": []
  },
  {
    "Id": "MO0006",
    "Title": "Open generic handler cannot be auto-registered",
    "Severity": "Info",
    "WarningLevel": 1,
    "Location": ": (6,13)-(6,34)",
    "MessageFormat": "Handler '{0}' is an open generic and cannot be auto-registered",
    "Message": "Handler 'global::TestApp.GenericCommandHandler<T>' is an open generic and cannot be auto-registered",
    "Category": "Mediator",
    "CustomTags": []
  }
]
```
