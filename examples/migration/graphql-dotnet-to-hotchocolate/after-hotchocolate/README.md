# After: Hot Chocolate v16 reference server

This is the "after" application for the GraphQL.NET to Hot Chocolate migration
guide. It is a standalone, runnable Hot Chocolate v16 ASP.NET Core server using
the annotation-based / implementation-first style (source generators). It is the
equivalent port of the GraphQL.NET "before" app and reproduces the same query
results over the same Books/Authors domain. It does not reference any other
project in this repository.

## Prerequisites

- .NET SDK able to target `net10.0`.
- No database. All data is in-memory and deterministically seeded on startup.

## Stack

| Concern                | Package                              | Version |
| ---------------------- | ------------------------------------ | ------- |
| Core engine + transport | `HotChocolate.AspNetCore`           | 16.2.1  |
| Source generator       | `HotChocolate.Types.Analyzers`       | 16.2.1  |
| Subscriptions          | `HotChocolate.Subscriptions.InMemory` | 16.2.1 |
| Authorization          | `HotChocolate.AspNetCore.Authorization` | 16.2.1 |

Target framework: `net10.0`, nullable enabled (NRT drives GraphQL nullability),
implicit usings enabled.

`HotChocolate.Types.Analyzers` is required: it emits the `AddAfterHotChocolateTypes()`
registration extension and the `IAuthorByIdDataLoader` from `[QueryType]`,
`[MutationType]`, `[SubscriptionType]`, `[ObjectType<T>]` and `[DataLoader]`.

## How to run

```bash
dotnet run
```

The server listens on a fixed URL:

```
http://localhost:5102
```

Introspection and the SDL endpoint are disabled outside Development by Hot
Chocolate's default security. To introspect or export the SDL, run in Development:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

## Endpoints

| Endpoint                                | Purpose                                  |
| --------------------------------------- | ---------------------------------------- |
| `POST http://localhost:5102/graphql`    | GraphQL queries and mutations            |
| `ws://localhost:5102/graphql`           | GraphQL subscriptions over WebSockets    |
| `http://localhost:5102/graphql`         | Nitro interactive UI (open in a browser) |
| `http://localhost:5102/graphql?sdl`     | SDL export (Development only)            |

The in-browser IDE is **Nitro**, served by `MapGraphQL()` at the same `/graphql`
route. There is no separate UI package and no separate route to configure.

## Schema

```graphql
enum BookGenre { FICTION NONFICTION FANTASY SCIENCE }

type Book {
  id: ID!
  title: String!
  genre: BookGenre!
  publishedYear: Int!
  author: Author!        # resolved via the AuthorById batch DataLoader (N+1 fix)
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
  booksConnection(...): BooksConnection   # cursor pagination via [UsePaging]
}

type Mutation {
  # Reshaped by AddMutationConventions (see note below).
  addBook(input: AddBookInput!): AddBookPayload!
}

type Subscription {
  onBookAdded: Book!
}
```

### Mutation conventions shape difference vs the before app

The before app exposes the mutation as:

```graphql
addBook(title: String!, authorId: ID!, genre: BookGenre!, publishedYear: Int!): Book!
```

This app enables `AddMutationConventions()`, so the single C# resolver is
reshaped into:

```graphql
addBook(input: AddBookInput!): AddBookPayload!
# AddBookInput  { title, authorId, genre, publishedYear }
# AddBookPayload { book: Book }
```

This is an INTENTIONAL, documented difference. Client queries change from
`addBook(...) { id }` to `addBook(input: {...}) { book { id } }`. The created
book is identical; only the request/response wrapping differs.

### Seed data

Deterministic, seeded once in a singleton in-memory store. Identical to the
before app.

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

`Book.author` resolves through the `[DataLoader]` batch method `AuthorByIdAsync`
(generated as `IAuthorByIdDataLoader`). Selecting `author` across many books
results in a single batched lookup instead of one per book (the N+1 fix).

## Authentication and authorization

Only the `secret` field is protected. It requires the `Authenticated`
authorization policy (`RequireAuthenticatedUser()`), wired into GraphQL via
`.AddAuthorization()` on the builder plus `services.AddAuthorization()`. The
field carries `HotChocolate.Authorization.[Authorize(Policy = "Authenticated")]`
(the Hot Chocolate attribute, not the Microsoft one).

Authentication is a minimal header-based test handler registered as the default
scheme:

- Send the header `X-Authenticated` with any value to be treated as
  authenticated (a single `test-user` Name claim).
- Omit the header to remain anonymous.

`UseAuthentication()` and `UseAuthorization()` run before `MapGraphQL`. Requesting
`secret` without the header yields an authorization error (HC code
`AUTH_NOT_AUTHENTICATED`); with the header, `secret` returns its value.

## Sample operations

All examples target `POST http://localhost:5102/graphql` with
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

### P1 / P2: cursor pagination (booksConnection)

`booksConnection` is a `[UsePaging]` field; the connection is named after the field. Page over the
seeded books, stable order by id. Cursors are opaque and HC-specific (not portable from GraphQL.NET);
HC also enables backward paging (`last`/`before`) and an opt-in `totalCount`.

```graphql
# first page
{ booksConnection(first: 2) { edges { cursor node { id title } } pageInfo { hasNextPage endCursor } } }

# next page, using the endCursor returned above
{ booksConnection(first: 2, after: "MQ==") { edges { node { id title } } pageInfo { hasNextPage } } }
```

### M1: add a book (mutation conventions shape)

```graphql
mutation {
  addBook(input: { title: "New Book", authorId: "1", genre: SCIENCE, publishedYear: 2024 }) {
    book { id title author { name } }
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

Open Nitro at `http://localhost:5102/graphql`, then run:

```graphql
subscription { onBookAdded { id title genre } }
```

While subscribed, run the `addBook` mutation (M1) from another tab or client.
The new book is pushed to the subscription stream via `ITopicEventSender` and the
in-memory provider.

### curl example

```bash
curl -s -X POST http://localhost:5102/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ books { id title } }"}'

# protected field with auth
curl -s -X POST http://localhost:5102/graphql \
  -H "Content-Type: application/json" \
  -H "X-Authenticated: true" \
  -d '{"query":"{ secret }"}'
```
