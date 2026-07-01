# MO0006_OpenGenericNotificationHandler_ReportsInfo

```json
[
  {
    "Id": "MO0006",
    "Title": "Open generic handler cannot be auto-registered",
    "Severity": "Info",
    "WarningLevel": 1,
    "Location": ": (6,13)-(6,25)",
    "MessageFormat": "Handler '{0}' has unbound type parameters; source-generated registration is skipped. Register the closed form manually (e.g. AddHandler<ConcreteHandler<MyType>>()) if intentional.",
    "Message": "Handler 'global::TestApp.GenericNotif<T>' has unbound type parameters; source-generated registration is skipped. Register the closed form manually (e.g. AddHandler<ConcreteHandler<MyType>>()) if intentional.",
    "Category": "Mediator",
    "CustomTags": []
  }
]
```
