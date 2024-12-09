# Plan_With_Conditional_InlineFragment

## Request

```graphql
{
  productById(id: 1) {
    ... Product
  }
}

fragment Product on Product {
  id
  name
  ... @include(if: true) {
    estimatedDelivery(postCode: "12345")
  }
}
```

## Plan

```yaml
nodes:
  - id: 1
    schema: "PRODUCTS"
    operation: >-
      {
        productById(id: 1) {
          id
          name
        }
      }

```

