# Skipped_Root_Selection_Other_Not_Skipped_Root_Selection_From_Same_Subgraph_If_True

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) @skip(if: true) {
    name
  }
  products {
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
      {
        products {
          nodes {
            name
          }
        }
      }

```

