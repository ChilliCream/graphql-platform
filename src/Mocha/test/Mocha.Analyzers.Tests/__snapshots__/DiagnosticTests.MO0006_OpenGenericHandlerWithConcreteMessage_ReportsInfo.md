# MO0006_OpenGenericHandlerWithConcreteMessage_ReportsInfo

```json
[
  {
    "Id": "MO0001",
    "Title": "Missing handler for message type",
    "Severity": "Warning",
    "WarningLevel": 1,
    "Location": ": (4,14)-(4,23)",
    "MessageFormat": "Message type '{0}' has no registered handler",
    "Message": "Message type 'global::TestApp.MyCommand' has no registered handler",
    "Category": "Mediator",
    "CustomTags": []
  },
  {
    "Id": "MO0006",
    "Title": "Open generic handler cannot be auto-registered",
    "Severity": "Info",
    "WarningLevel": 1,
    "Location": ": (6,13)-(6,27)",
    "MessageFormat": "Handler '{0}' has unbound type parameters; source-generated registration is skipped. Register the closed form manually (e.g. AddHandler<ConcreteHandler<MyType>>()) if intentional.",
    "Message": "Handler 'global::TestApp.GenericHandler<T>' has unbound type parameters; source-generated registration is skipped. Register the closed form manually (e.g. AddHandler<ConcreteHandler<MyType>>()) if intentional.",
    "Category": "Mediator",
    "CustomTags": []
  }
]
```
