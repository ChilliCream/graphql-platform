# Skipped_Root_Fragment_If_True

## Request

```graphql
query($slug: String!) {
  ... QueryFragment @skip(if: true)
}

fragment QueryFragment on Query {
  productBySlug(slug: $slug) {
    name
  }
}
```

## Plan

```yaml
nodes:

```

