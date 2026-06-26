# Queue_Endpoint_DeclareQueue_Should_ConvergeToOneEntity_When_SameName

## AllThreePaths

```json
{
  "Schema": "rabbitmq",
  "TransportType": "RabbitMQMessagingTransport",
  "Entities": [
    {
      "Kind": "queue",
      "Name": "orders",
      "AutoProvision": false,
      "Origin": "declared"
    },
    {
      "Kind": "queue",
      "Name": "orders_error",
      "AutoProvision": false,
      "Origin": "convention"
    },
    {
      "Kind": "queue",
      "Name": "orders_skipped",
      "AutoProvision": false,
      "Origin": "convention"
    }
  ],
  "Links": []
}
```

## EndpointOnly

```json
{
  "Schema": "rabbitmq",
  "TransportType": "RabbitMQMessagingTransport",
  "Entities": [
    {
      "Kind": "queue",
      "Name": "orders",
      "AutoProvision": true,
      "Origin": "declared"
    },
    {
      "Kind": "queue",
      "Name": "orders_error",
      "AutoProvision": true,
      "Origin": "convention"
    },
    {
      "Kind": "queue",
      "Name": "orders_skipped",
      "AutoProvision": true,
      "Origin": "convention"
    }
  ],
  "Links": []
}
```
