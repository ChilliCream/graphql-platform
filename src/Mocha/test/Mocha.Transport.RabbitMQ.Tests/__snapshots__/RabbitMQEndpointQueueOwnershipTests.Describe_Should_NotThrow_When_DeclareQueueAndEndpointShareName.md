# Describe_Should_NotThrow_When_DeclareQueueAndEndpointShareName

## WithDeclareQueue

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

## WithoutDeclareQueue

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
