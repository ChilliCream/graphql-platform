# Fragment_On_Sub_Selection

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    ... ProductFragment
  }
}

fragment ProductFragment on Product {
  name
}
```

## Plan

```yaml
nodes:
  - id: 1
    schema: "PRODUCTS"
    operation: >-
      query($slug: String!) {
        productBySlug(slug: $slug) {
          name
        }
      }

```

