# Skipped_Root_Selection_If_True

## Request

```graphql
query($slug: String!) {
  productBySlug(slug: $slug) @skip(if: true) {
    name
  }
}
```

## Plan

```yaml
nodes:

```

