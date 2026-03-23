# MO0003_AbstractHandler_ReportsWarning

```json
[
  {
    "Id": "MO0001",
    "Title": "Missing handler for message type",
    "Severity": "Warning",
    "WarningLevel": 1,
    "Location": ": (4,14)-(4,32)",
    "MessageFormat": "Message type '{0}' has no registered handler",
    "Message": "Message type 'global::TestApp.DeleteOrderCommand' has no registered handler",
    "Category": "Mediator",
    "CustomTags": []
  },
  {
    "Id": "MO0003",
    "Title": "Handler is abstract",
    "Severity": "Warning",
    "WarningLevel": 1,
    "Location": ": (6,22)-(6,44)",
    "MessageFormat": "Handler '{0}' is abstract and will not be registered",
    "Message": "Handler 'global::TestApp.BaseDeleteOrderHandler' is abstract and will not be registered",
    "Category": "Mediator",
    "CustomTags": []
  }
]
```
