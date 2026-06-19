# Before: GraphQL.NET reference server

This is the "before" application for the GraphQL.NET to Hot Chocolate migration
guide. It is a standalone, runnable GraphQL.NET (graphql-dotnet) ASP.NET Core
server using the idiomatic code-first `ObjectGraphType` style. It is fully
self-contained and does not reference any other project in this repository.

## Prerequisites

- .NET SDK able to target `net10.0` (built and verified with SDK 11.0.100 preview).
- No database. All data is in-memory and deterministically seeded on startup.

## Stack

| Concern             | Package                                      | Version |
| ------------------- | -------------------------------------------- | ------- |
| Core engine         | `GraphQL`                                    | 8.8.4   |
| JSON serialization  | `GraphQL.SystemTextJson`                     | 8.8.4   |
| DI integration      | `GraphQL.MicrosoftDI`                        | 8.8.4   |
| DataLoader (N+1)    | `GraphQL.DataLoader`                         | 8.8.4   |
| ASP.NET Core transport | `GraphQL.Server.Transports.AspNetCore`    | 8.3.3   |
| GraphiQL UI         | `GraphQL.Server.Ui.GraphiQL`                 | 8.3.3   |
| Subscription stream | `System.Reactive`                            | 6.0.1   |

Target framework: `net10.0`, nullable enabled, implicit usings enabled.

## How to run

```bash
dotnet run
```

The server listens on a fixed URL:

```
http://localhost:5101
```

HTTPS redirection is disabled.

## Endpoints

| Endpoint                                | Purpose                                  |
| --------------------------------------- | ---------------------------------------- |
| `POST http://localhost:5101/graphql`    | GraphQL queries and mutations            |
| `ws://localhost:5101/graphql`           | GraphQL subscriptions over WebSockets    |
| `http://localhost:5101/ui/graphiql`     | GraphiQL interactive UI                  |

## Schema

```graphql
enum BookGenre { FICTION NONFICTION FANTASY SCIENCE }

type Book {
  id: ID!
  title: String!
  genre: BookGenre!
  publishedYear: Int!
  author: Author!        # resolved via a batch DataLoader (N+1 fix)
}

type Author {
  id: ID!
  name: String!
  books: [Book!]!
}

union SearchResult = Book | Author

input BookFilterInput {
  genre: BookGenre
  titleContains: String
}

type Query {
  books(filter: BookFilterInput): [Book!]!
  authors: [Author!]!
  bookById(id: ID!): Book
  search(term: String!): [SearchResult!]!
  secret: String!        # protected by the "Authenticated" policy
}

type Mutation {
  addBook(title: String!, authorId: ID!, genre: BookGenre!, publishedYear: Int!): Book!
}

type Subscription {
  onBookAdded: Book!
}
```

### Seed data

Deterministic, seeded once in a singleton in-memory store.

Authors:

- 1: George Orwell
- 2: J.R.R. Tolkien
- 3: Carl Sagan

Books:

- 1: "1984", FICTION, 1949, author 1
- 2: "Animal Farm", FICTION, 1945, author 1
- 3: "The Hobbit", FANTASY, 1937, author 2
- 4: "The Lord of the Rings", FANTASY, 1954, author 2
- 5: "Cosmos", NONFICTION, 1980, author 3
- 6: "The Demon-Haunted World", SCIENCE, 1995, author 3

`addBook` appends a new book with `id = max(id) + 1`.

### DataLoader

`Book.author` resolves through a batch DataLoader
(`GetOrAddBatchLoader<int, Author>`) using an injected
`IDataLoaderContextAccessor`. Selecting `author` across many books results in a
single batched lookup instead of one lookup per book (the classic N+1 fix this
sample exists to demonstrate).

## Authentication and authorization

Only the `secret` field is protected. It requires the `Authenticated`
authorization policy (`RequireAuthenticatedUser()`), wired into GraphQL via
`.AddAuthorizationRule()`.

Authentication is a minimal header-based test handler registered as the default
scheme:

- Send the header `X-Authenticated` with any value to be treated as
  authenticated (a single `test-user` Name claim).
- Omit the header to remain anonymous.

`UseAuthentication()` and `UseAuthorization()` run before `UseGraphQL`, so the
transport sees `HttpContext.User`. Requesting `secret` without the header yields
an authorization error (code `ACCESS_DENIED`) without blocking other fields in
the same request. With the header, `secret` returns its value.

## Sample operations

All examples target `POST http://localhost:5101/graphql` with
`Content-Type: application/json`.

### Q1: all books with author

```graphql
{ books { id title genre publishedYear author { id name } } }
```

### Q2: filter by genre

```graphql
{ books(filter: { genre: FANTASY }) { title genre } }
```

### Q3: book by id

```graphql
{ bookById(id: "1") { title author { name } } }
```

### Q4: authors with their books

```graphql
{ authors { name books { title } } }
```

### Q5: search (union)

```graphql
{ search(term: "a") { __typename ... on Book { title } ... on Author { name } } }
```

### M1: add a book

```graphql
mutation {
  addBook(title: "New Book", authorId: "1", genre: SCIENCE, publishedYear: 2024) {
    id
    title
    author { name }
  }
}
```

### Q6: protected field without auth (authorization error)

```graphql
{ secret }
```

### Q6b: protected field with auth header (succeeds)

Send header `X-Authenticated: true`, then:

```graphql
{ secret }
```

### Subscription: onBookAdded

Connect over WebSockets to `ws://localhost:5101/graphql` (the GraphiQL UI does
this for you), then run:

```graphql
subscription { onBookAdded { id title genre } }
```

While subscribed, run the `addBook` mutation (M1) from another tab or client.
The new book is pushed to the subscription stream.

### curl example

```bash
curl -s -X POST http://localhost:5101/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ books { id title } }"}'

# protected field with auth
curl -s -X POST http://localhost:5101/graphql \
  -H "Content-Type: application/json" \
  -H "X-Authenticated: true" \
  -d '{"query":"{ secret }"}'
```
