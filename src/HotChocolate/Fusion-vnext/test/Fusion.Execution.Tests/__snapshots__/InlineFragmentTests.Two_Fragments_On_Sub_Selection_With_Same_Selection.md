# Two_Fragments_On_Sub_Selection_With_Same_Selection

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) {
    ... {
      name
    }
    ... {
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

