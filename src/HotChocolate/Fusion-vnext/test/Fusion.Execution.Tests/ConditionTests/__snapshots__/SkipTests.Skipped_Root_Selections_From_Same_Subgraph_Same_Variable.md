# Skipped_Root_Selections_From_Same_Subgraph_Same_Variable

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) @skip(if: $skip) {
    name
  }
  products @skip(if: $skip) {
    nodes {
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
        products {
          nodes {
            name
          }
        }
      }
    skipIf: "skip"

```

