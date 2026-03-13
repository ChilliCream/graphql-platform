# Demo Scripts

HTTP files for [VS Code REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) to interact with the e-commerce demo services.

## Setup

1. Install the REST Client extension in VS Code
2. Start the Demo AppHost (Aspire)
3. Update the `@catalogUrl`, `@billingUrl`, `@shippingUrl` variables in each file to match your Aspire ports

## Files

| File                         | Description                                              |
| ---------------------------- | -------------------------------------------------------- |
| `01-place-single-order.http` | Basic order flow: Place order → Payment → Shipment       |
| `02-quick-refund.http`       | Quick refund saga (no physical return)                   |
| `03-return-processing.http`  | Full return saga with inspection and parallel processing |

## Usage

1. Open any `.http` file
2. Click "Send Request" above each request
3. Follow the steps in order (wait 2-3 seconds between steps for async processing)

Variables like `@orderId` are automatically captured from responses and used in subsequent requests.

## Sample Product IDs

| Product             | ID                                     |
| ------------------- | -------------------------------------- |
| Wireless Headphones | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` |
| Mechanical Keyboard | `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb` |
| Clean Code (Book)   | `cccccccc-cccc-cccc-cccc-cccccccccccc` |

## Event Flow

```
┌─────────────┐     OrderPlacedEvent      ┌─────────────┐
│   Catalog   │ ─────────────────────────▶│   Billing   │
│   Service   │                           │   Service   │
└─────────────┘                           └─────────────┘
       ▲                                         │
       │                                         │
       │    ShipmentCreatedEvent                 │ PaymentCompletedEvent
       │         ShipmentShippedEvent            │
       │                                         ▼
       │                                  ┌─────────────┐
       └──────────────────────────────────│   Shipping  │
                                          │   Service   │
                                          └─────────────┘
```
