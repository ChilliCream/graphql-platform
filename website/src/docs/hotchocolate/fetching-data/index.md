---
title: "Resolver"
---

# Defining a resolver

# Resolver chain

# Resolver arguments

# Dependency Injection

# Resolver Pipeline

# Error handling

When it comes to fetching data in a GraphQL server you will always end up with a resolver.
A resolver is a generic function that fetches data from an arbitrary data source for a particular field.
This means every field has it's own resolver function in order to fetch data. Even if there wasn't a resolver defined for one field, HotChocolate will create a default resolver for this particular field behind the scenes.

```mermaid
graph TD
  A(field) --> B{has resolver}
  B --> |yes|C(return resolver)
  B --> |no|D(create default resolver)
  D --> C
```

Let's take a look inside the default resolver.

```mermaid
graph TD
  A(default resolver) --> B{has field value}
  B --> |yes|C(return value)
  B --> |no|D(return null)
```

Imagine we have a GraphQL query like the following, which fetches a collection of books that contains the book's title and the author's name.

```graphql
query {
  books {
    author {
      name
    }
    title
  }
}
```

Then a GraphQL server would transform this query into the following resolver tree.

```mermaid
graph TD
  A(Query: QueryType) --> B("books: resolve() => [BookType]")
  B --> C("author: resolve() => AuthorType")
  B --> D("title: resolve() => StringType")
  C --> E("name: resolve() => StringType")
```

# Defining a resolver
