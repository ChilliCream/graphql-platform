# InlineFragment_On_Root_Next_To_Different_Selection

## Request

```graphql
query($slug: String!) {
  products {
    nodes {
      description
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
        products {
          nodes {
            description
          }
        }
        productBySlug(slug: $slug) {
          name
        }
      }

```

