# Skipped_Root_Fragment_Other_Not_Skipped_Root_Fragment_From_Same_Subgraph_If_True

## Request

```graphql
query($slug: String!) {
  ... QueryFragment1 @skip(if: true)
  ... QueryFragment2
}

fragment QueryFragment1 on Query {
  productBySlug(slug: $slug) {
    name
  }
}

fragment QueryFragment2 on Query {
  products {
    nodes {
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
      {
        products {
          nodes {
            description
          }
        }
      }

```

