# Use_Default_Page_Size_When_Default_Is_Specified

## Operation

```graphql
{
  books {
    nodes {
      title
    }
  }
}
```

## Response

```json
{
  "data": {
    "books": {
      "nodes": []
    }
  },
  "extensions": {
    "operationCost": {
      "fieldCost": 11,
      "typeCost": 4
    }
  }
}
```

## Schema

```graphql
schema {
  query: Query
}

type Author {
  name: String!
}

"A connection to a list of items."
type AuthorsConnection {
  "Information to aid in pagination."
  pageInfo: PageInfo!
  "A list of edges."
  edges: [AuthorsEdge!]
  "A flattened list of the nodes."
  nodes: [Author!]
}

"An edge in a connection."
type AuthorsEdge {
  "A cursor for use in pagination."
  cursor: String!
  "The item at the end of the edge."
  node: Author!
}

type Book {
  title: String!
  authors("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String "Returns the last _n_ elements from the list." last: Int "Returns the elements in the list that come before the specified cursor." before: String): AuthorsConnection @listSize(assumedSize: 50, slicingArguments: [ "first", "last" ], slicingArgumentDefaultValue: 2, sizedFields: [ "edges", "nodes" ], requireOneSlicingArgument: false)
}

"A connection to a list of items."
type BooksConnection {
  "Information to aid in pagination."
  pageInfo: PageInfo!
  "A list of edges."
  edges: [BooksEdge!]
  "A flattened list of the nodes."
  nodes: [Book!]
}

"An edge in a connection."
type BooksEdge {
  "A cursor for use in pagination."
  cursor: String!
  "The item at the end of the edge."
  node: Book!
}

"A segment of a collection."
type BooksOffsetCollectionSegment {
  "Information to aid in pagination."
  pageInfo: CollectionSegmentInfo!
  "A flattened list of the items."
  items: [Book!]
}

"A segment of a collection."
type BooksTotalCollectionSegment {
  "Information to aid in pagination."
  pageInfo: CollectionSegmentInfo!
  "A flattened list of the items."
  items: [Book!]
  totalCount: Int! @cost(weight: "10")
}

"A connection to a list of items."
type BooksTotalConnection {
  "Information to aid in pagination."
  pageInfo: PageInfo!
  "A list of edges."
  edges: [BooksTotalEdge!]
  "A flattened list of the nodes."
  nodes: [Book!]
  "Identifies the total count of items in the connection."
  totalCount: Int! @cost(weight: "10")
}

"An edge in a connection."
type BooksTotalEdge {
  "A cursor for use in pagination."
  cursor: String!
  "The item at the end of the edge."
  node: Book!
}

"Information about the offset pagination."
type CollectionSegmentInfo {
  "Indicates whether more items exist following the set defined by the clients arguments."
  hasNextPage: Boolean!
  "Indicates whether more items exist prior the set defined by the clients arguments."
  hasPreviousPage: Boolean!
}

"Information about pagination in a connection."
type PageInfo {
  "Indicates whether more edges exist following the set defined by the clients arguments."
  hasNextPage: Boolean!
  "Indicates whether more edges exist prior the set defined by the clients arguments."
  hasPreviousPage: Boolean!
  "When paginating backwards, the cursor to continue."
  startCursor: String
  "When paginating forwards, the cursor to continue."
  endCursor: String
}

type Query {
  books("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String "Returns the last _n_ elements from the list." last: Int "Returns the elements in the list that come before the specified cursor." before: String where: BookFilterInput @cost(weight: "10") order: [BookSortInput!] @cost(weight: "10")): BooksConnection @listSize(assumedSize: 50, slicingArguments: [ "first", "last" ], slicingArgumentDefaultValue: 2, sizedFields: [ "edges", "nodes" ], requireOneSlicingArgument: false) @cost(weight: "10")
  booksWithTotalCount("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String "Returns the last _n_ elements from the list." last: Int "Returns the elements in the list that come before the specified cursor." before: String where: BookFilterInput @cost(weight: "10") order: [BookSortInput!] @cost(weight: "10")): BooksTotalConnection @listSize(assumedSize: 50, slicingArguments: [ "first", "last" ], slicingArgumentDefaultValue: 2, sizedFields: [ "edges", "nodes" ], requireOneSlicingArgument: false) @cost(weight: "10")
  booksOffset(skip: Int take: Int where: BookFilterInput @cost(weight: "10") order: [BookSortInput!] @cost(weight: "10")): BooksOffsetCollectionSegment @listSize(assumedSize: 50, slicingArguments: [ "take" ], slicingArgumentDefaultValue: 2, sizedFields: [ "items" ], requireOneSlicingArgument: false) @cost(weight: "10")
  booksOffsetWithTotalCount(skip: Int take: Int where: BookFilterInput @cost(weight: "10") order: [BookSortInput!] @cost(weight: "10")): BooksTotalCollectionSegment @listSize(assumedSize: 50, slicingArguments: [ "take" ], slicingArgumentDefaultValue: 2, sizedFields: [ "items" ], requireOneSlicingArgument: false) @cost(weight: "10")
}

input BookFilterInput {
  and: [BookFilterInput!]
  or: [BookFilterInput!]
  title: StringOperationFilterInput
}

input BookSortInput {
  title: SortEnumType @cost(weight: "10")
}

input StringOperationFilterInput {
  and: [StringOperationFilterInput!]
  or: [StringOperationFilterInput!]
  eq: String @cost(weight: "10")
  neq: String @cost(weight: "10")
  contains: String @cost(weight: "20")
  ncontains: String @cost(weight: "20")
  in: [String] @cost(weight: "10")
  nin: [String] @cost(weight: "10")
  startsWith: String @cost(weight: "20")
  nstartsWith: String @cost(weight: "20")
  endsWith: String @cost(weight: "20")
  nendsWith: String @cost(weight: "20")
}

enum SortEnumType {
  ASC
  DESC
}

"The purpose of the `cost` directive is to define a `weight` for GraphQL types, fields, and arguments. Static analysis can use these weights when calculating the overall cost of a query or response."
directive @cost("The `weight` argument defines what value to add to the overall cost for every appearance, or possible appearance, of a type, field, argument, etc." weight: String!) on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

"The purpose of the `@listSize` directive is to either inform the static analysis about the size of returned lists (if that information is statically available), or to point the analysis to where to find that information."
directive @listSize("The `assumedSize` argument can be used to statically define the maximum length of a list returned by a field." assumedSize: Int "The `slicingArguments` argument can be used to define which of the field's arguments with numeric type are slicing arguments, so that their value determines the size of the list returned by that field. It may specify a list of multiple slicing arguments." slicingArguments: [String!] "The `slicingArgumentDefaultValue` argument can be used to define a default value for a slicing argument, which is used if the argument is not present in a query." slicingArgumentDefaultValue: Int "The `sizedFields` argument can be used to define that the value of the `assumedSize` argument or of a slicing argument does not affect the size of a list returned by a field itself, but that of a list returned by one of its sub-fields." sizedFields: [String!] "The `requireOneSlicingArgument` argument can be used to inform the static analysis that it should expect that exactly one of the defined slicing arguments is present in a query. If that is not the case (i.e., if none or multiple slicing arguments are present), the static analysis may throw an error." requireOneSlicingArgument: Boolean! = true) on FIELD_DEFINITION
```

