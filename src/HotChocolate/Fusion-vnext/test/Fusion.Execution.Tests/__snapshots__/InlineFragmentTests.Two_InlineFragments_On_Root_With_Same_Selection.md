# Two_InlineFragments_On_Root_With_Same_Selection

## Request

```graphql
query($slug: String!) {
  ... {
    productBySlug(slug: $slug) {
      name
    }
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

