# Describe_Should_InheritParentQueueAutoProvisionOnInfraQueues_When_ParentDeclared

## ParentDeclaredTrue

```json
{
  "Schema": "rabbitmq",
  "TransportType": "RabbitMQMessagingTransport",
  "Entities": [
    {
      "Kind": "exchange",
      "Name": "mocha.test-helpers.order-created",
      "AutoProvision": false,
      "Source": "convention"
    },
    {
      "Kind": "exchange",
      "Name": "order-created",
      "AutoProvision": false,
      "Source": "convention"
    },
    {
      "Kind": "queue",
      "Name": "orders",
      "AutoProvision": true,
      "Source": "declared"
    },
    {
      "Kind": "queue",
      "Name": "orders_error",
      "AutoProvision": true,
      "Source": "convention"
    },
    {
      "Kind": "queue",
      "Name": "orders_skipped",
      "AutoProvision": true,
      "Source": "convention"
    }
  ],
  "Links": [
    {
      "Kind": "bind",
      "From": "rabbitmq://localhost:5672/e/mocha.test-helpers.order-created",
      "To": "rabbitmq://localhost:5672/e/order-created",
      "AutoProvision": false,
      "Source": "convention"
    },
    {
      "Kind": "bind",
      "From": "rabbitmq://localhost:5672/e/order-created",
      "To": "rabbitmq://localhost:5672/q/orders",
      "AutoProvision": false,
      "Source": "convention"
    }
  ]
}
```

## ParentDeclaredFalse

```json
{
  "Schema": "rabbitmq",
  "TransportType": "RabbitMQMessagingTransport",
  "Entities": [
    {
      "Kind": "exchange",
      "Name": "mocha.test-helpers.order-created",
      "AutoProvision": true,
      "Source": "convention"
    },
    {
      "Kind": "exchange",
      "Name": "order-created",
      "AutoProvision": true,
      "Source": "convention"
    },
    {
      "Kind": "queue",
      "Name": "orders",
      "AutoProvision": false,
      "Source": "declared"
    },
    {
      "Kind": "queue",
      "Name": "orders_error",
      "AutoProvision": false,
      "Source": "convention"
    },
    {
      "Kind": "queue",
      "Name": "orders_skipped",
      "AutoProvision": false,
      "Source": "convention"
    }
  ],
  "Links": [
    {
      "Kind": "bind",
      "From": "rabbitmq://localhost:5672/e/mocha.test-helpers.order-created",
      "To": "rabbitmq://localhost:5672/e/order-created",
      "AutoProvision": true,
      "Source": "convention"
    },
    {
      "Kind": "bind",
      "From": "rabbitmq://localhost:5672/e/order-created",
      "To": "rabbitmq://localhost:5672/q/orders",
      "AutoProvision": true,
      "Source": "convention"
    }
  ]
}
```
