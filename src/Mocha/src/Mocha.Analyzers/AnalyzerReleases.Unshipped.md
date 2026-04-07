### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MO0001 | Mediator | Warning | Message type has no registered handler
MO0002 | Mediator | Error | Message type has multiple handlers
MO0003 | Mediator | Warning | Handler is abstract and will not be registered
MO0004 | Mediator | Info | Open generic message type cannot be dispatched
MO0005 | Mediator | Error | Handler implements multiple mediator handler interfaces
MO0011 | Messaging | Error | Request type has multiple handlers
MO0012 | Messaging | Info | Open generic messaging handler cannot be auto-registered
MO0013 | Messaging | Warning | Messaging handler is abstract
MO0014 | Messaging | Error | Saga must have a public parameterless constructor
