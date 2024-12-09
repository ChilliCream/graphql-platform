# InlineFragment_On_Root_Next_To_Same_Selection

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    name
  }
  ... {
    productBySlug(slug: $slug) {
      name
    }
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

