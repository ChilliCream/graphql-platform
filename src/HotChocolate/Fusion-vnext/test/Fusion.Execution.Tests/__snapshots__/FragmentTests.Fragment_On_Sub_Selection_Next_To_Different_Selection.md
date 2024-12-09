# Fragment_On_Sub_Selection_Next_To_Different_Selection

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    name
    ... ProductFragment
  }
}

fragment ProductFragment on Product {
  description
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
          description
        }
      }

```

