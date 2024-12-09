# Plan_Simple_Operation_1_Source_Schema

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
        }
      }

```

