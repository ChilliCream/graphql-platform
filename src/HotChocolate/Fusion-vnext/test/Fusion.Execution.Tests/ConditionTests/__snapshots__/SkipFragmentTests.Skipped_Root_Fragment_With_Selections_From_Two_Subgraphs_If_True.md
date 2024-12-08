# Skipped_Root_Fragment_With_Selections_From_Two_Subgraphs_If_True

## Request

```graphql
query($slug: String!) {
  ... QueryFragment @skip(if: true)
}

fragment QueryFragment on Query {
  productBySlug(slug: $slug) {
    name
  }
  viewer {
    displayName
  }
}
```

## Plan

```json
{
  "kind": "Root"
}
```

