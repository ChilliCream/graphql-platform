# Describe_Should_NotThrow_When_DeclareQueueAndEndpointShareName

## WithDeclareQueue

```json
{
  "Schema": "rabbitmq",
  "TransportType": "RabbitMQMessagingTransport",
  "Entities": [
    {
      "Kind": "exchange",
      "Name": "mocha.test-helpers.order-created",
      "AutoProvision": true,
      "Origin": "convention"
    },
    {
      "Kind": "exchange",
      "Name": "order-created",
      "AutoProvision": true,
      "Origin": "convention"
    },
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
  "Links": [
    {
      "Kind": "bind",
      "From": "rabbitmq://localhost:5672/e/mocha.test-helpers.order-created",
      "To": "rabbitmq://localhost:5672/e/order-created",
      "AutoProvision": true,
      "Origin": "convention"
    },
    {
      "Kind": "bind",
      "From": "rabbitmq://localhost:5672/e/order-created",
      "To": "rabbitmq://localhost:5672/q/orders",
      "AutoProvision": true,
      "Origin": "convention"
    }
  ]
}
```

## WithoutDeclareQueue

```json
{
  "Schema": "rabbitmq",
  "TransportType": "RabbitMQMessagingTransport",
  "Entities": [
    {
      "Kind": "exchange",
      "Name": "mocha.test-helpers.order-created",
      "AutoProvision": true,
      "Origin": "convention"
    },
    {
      "Kind": "exchange",
      "Name": "order-created",
      "AutoProvision": true,
      "Origin": "convention"
    },
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
  "Links": [
    {
      "Kind": "bind",
      "From": "rabbitmq://localhost:5672/e/mocha.test-helpers.order-created",
      "To": "rabbitmq://localhost:5672/e/order-created",
      "AutoProvision": true,
      "Origin": "convention"
    },
    {
      "Kind": "bind",
      "From": "rabbitmq://localhost:5672/e/order-created",
      "To": "rabbitmq://localhost:5672/q/orders",
      "AutoProvision": true,
      "Origin": "convention"
    }
  ]
}
```
