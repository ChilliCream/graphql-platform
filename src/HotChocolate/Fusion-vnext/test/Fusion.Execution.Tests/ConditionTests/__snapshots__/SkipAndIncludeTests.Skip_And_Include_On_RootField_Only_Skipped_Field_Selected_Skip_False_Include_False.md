# Skip_And_Include_On_RootField_Only_Skipped_Field_Selected_Skip_False_Include_False

## Request

```graphql
query GetProduct($slug: String!) {
  productBySlug(slug: $slug) @skip(if: false) @include(if: false) {
    name
  }
}
```

## Plan

```yaml
nodes:

```

