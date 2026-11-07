# Write_Should_RoundTripEveryField_When_ParsedBack

## Headers

```json
{
  "x-causation-id": "causation-1",
  "x-content-type": "application/json",
  "x-conversation-id": "conversation-1",
  "x-correlation-id": "correlation-1",
  "x-deliver-by": "2026-08-10T12:05:00.0000000\u002B00:00",
  "x-destination-address": "nats://localhost:4222/ORDER_SERVICE/c/order-service_order-created",
  "x-enclosed-message-types": "Contracts.OrderCreated | Contracts.IOrderEvent",
  "x-fault-address": "nats://localhost:4222/ORDER_SERVICE/s/order-created_error",
  "x-forwarded-for": "10.0.0.1 | 10.0.0.2",
  "x-message-id": "01JQ8Z0000000000000000",
  "x-message-type": "Contracts.OrderCreated",
  "x-response-address": "_INBOX.abc123",
  "x-scheduled-time": "2026-08-10T12:01:00.0000000\u002B00:00",
  "x-sent-at": "2026-08-10T12:00:00.0000000\u002B00:00",
  "x-source-address": "nats://localhost:4222/ORDER_SERVICE/s/order-service.order-created",
  "x-tenant": "acme"
}
```

## ParsedEnvelope

```json
{
  "MessageId": "01JQ8Z0000000000000000",
  "CorrelationId": "correlation-1",
  "ConversationId": "conversation-1",
  "CausationId": "causation-1",
  "SourceAddress": "nats://localhost:4222/ORDER_SERVICE/s/order-service.order-created",
  "DestinationAddress": "nats://localhost:4222/ORDER_SERVICE/c/order-service_order-created",
  "ResponseAddress": "_INBOX.abc123",
  "FaultAddress": "nats://localhost:4222/ORDER_SERVICE/s/order-created_error",
  "MessageType": "Contracts.OrderCreated",
  "ContentType": "application/json",
  "SentAt": "2026-08-10T12:00:00+00:00",
  "DeliverBy": "2026-08-10T12:05:00+00:00",
  "ScheduledTime": "2026-08-10T12:01:00+00:00",
  "DeliveryCount": 1,
  "EnclosedMessageTypes": [
    "Contracts.OrderCreated",
    "Contracts.IOrderEvent"
  ],
  "Headers": {
    "x-forwarded-for": "10.0.0.1 | 10.0.0.2",
    "x-tenant": "acme"
  },
  "Body": "{\u0022orderId\u0022:\u00221\u0022}"
}
```
