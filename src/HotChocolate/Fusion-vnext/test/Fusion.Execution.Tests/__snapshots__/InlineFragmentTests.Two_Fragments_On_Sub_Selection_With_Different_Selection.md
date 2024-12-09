# Two_Fragments_On_Sub_Selection_With_Different_Selection

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    ... {
      name
    }
    ... {
      description
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
          description
        }
      }

```

