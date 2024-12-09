# Plan_Simple_Operation_2_Source_Schema

## Request

```graphql
{
  productBySlug(slug: "1") {
    ... Product
  }
}

fragment Product on Product {
  id
  name
  estimatedDelivery(postCode: "12345")
}
```

## Plan

```yaml
nodes:
  - id: 1
    schema: "PRODUCTS"
    operation: >-
      {
        productBySlug(slug: "1") {
          id
          name
          id
        }
      }
  - id: 2
    schema: "SHIPPING"
    operation: >-
      query($__fusion_requirement_1: ID!) {
        productById(id: $__fusion_requirement_1) {
          estimatedDelivery(postCode: "12345")
        }
      }
    requirements:
      - name: "__fusion_requirement_1"
        dependsOn: "1"
        selectionSet: "productBySlug"
        field: "id"
        type: "ID!"

```

