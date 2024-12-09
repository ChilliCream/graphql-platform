# Two_InlineFragments_On_Root_With_Different_Selection

## Request

```graphql
query($slug: String!) {
  ... {
    productBySlug(slug: $slug) {
      name
    }
  }
  ... {
    products {
      nodes {
        description
      }
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
        products {
          nodes {
            description
          }
        }
      }

```

