# Fragment_On_Root

## Request

```graphql
query($slug: String!) {
  ... QueryFragment
}

fragment QueryFragment on Query {
  productBySlug(slug: $slug) {
    name
  }
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

