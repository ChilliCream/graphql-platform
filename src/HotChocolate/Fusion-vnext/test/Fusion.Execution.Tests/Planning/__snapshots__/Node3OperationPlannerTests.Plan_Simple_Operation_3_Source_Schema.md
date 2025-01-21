# Plan_Simple_Operation_3_Source_Schema

## 1 PRODUCTS

```graphql
{
  productBySlug(slug: "1") {
    name
    id
  }
}
```

## 2 REVIEWS

```graphql
{
  productById {
    reviews(first: 10) {
      nodes {
        body
        stars
        author {
          id
        }
      }
    }
  }
}
```

## 3 ACCOUNTS

```graphql
{
  userById {
    displayName
  }
}
```

