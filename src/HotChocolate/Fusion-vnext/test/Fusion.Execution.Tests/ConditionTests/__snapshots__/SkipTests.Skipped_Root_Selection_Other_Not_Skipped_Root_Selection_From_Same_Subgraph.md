# Skipped_Root_Selection_Other_Not_Skipped_Root_Selection_From_Same_Subgraph

## Request

```graphql
query($slug: String!, $skip: Boolean!) {
  productBySlug(slug: $slug) @skip(if: $skip) {
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
      query($skip: Boolean!, $slug: String!) {
        productBySlug(slug: $slug) @skip(if: $skip) {
          name
        }
        products {
          nodes {
            name
          }
        }
      }

```

