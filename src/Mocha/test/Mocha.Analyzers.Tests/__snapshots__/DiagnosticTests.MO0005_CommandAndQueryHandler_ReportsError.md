# MO0005_CommandAndQueryHandler_ReportsError

```json
[
  {
    "Id": "MO0001",
    "Title": "Missing handler for message type",
    "Severity": "Warning",
    "WarningLevel": 1,
    "Location": ": (4,14)-(4,32)",
    "MessageFormat": "Message type '{0}' has no registered handler",
    "Message": "Message type 'global::TestApp.DoSomethingCommand' has no registered handler",
    "Category": "Mediator",
    "CustomTags": []
  },
  {
    "Id": "MO0001",
    "Title": "Missing handler for message type",
    "Severity": "Warning",
    "WarningLevel": 1,
    "Location": ": (5,14)-(5,31)",
    "MessageFormat": "Message type '{0}' has no registered handler",
    "Message": "Message type 'global::TestApp.GetSomethingQuery' has no registered handler",
    "Category": "Mediator",
    "CustomTags": []
  },
  {
    "Id": "MO0005",
    "Title": "Handler implements multiple mediator handler interfaces",
    "Severity": "Error",
    "WarningLevel": 0,
    "Location": ": (7,13)-(7,25)",
    "MessageFormat": "Handler '{0}' must implement exactly one mediator handler interface",
    "Message": "Handler 'global::TestApp.MultiHandler' must implement exactly one mediator handler interface",
    "Category": "Mediator",
    "CustomTags": []
  }
]
```
