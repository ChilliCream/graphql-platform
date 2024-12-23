# Download_GraphQL_Schema

```text
Headers:
ETag: "1-zgi5AzGsi9KCkeA00b2KpL3HoZ++qVVoP05qFxiKUig="
Cache-Control: public, must-revalidate, max-age=3600
Content-Type: application/graphql; charset=utf-8
Content-Disposition: attachment; filename="schema.graphql"
Last-Modified: Fri, 01 Jan 2021 00:00:00 GMT
Content-Length: 7567
-------------------------->
Status Code: OK
-------------------------->
schema {
  query: Query
  mutation: Mutation
  subscription: Subscription
}

interface Character {
  id: ID!
  name: String!
  friends("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String "Returns the last _n_ elements from the list." last: Int "Returns the elements in the list that come before the specified cursor." before: String): FriendsConnection
  appearsIn: [Episode]
  traits: JSON
  height(unit: Unit): Float
}

type Droid implements Character {
  id: ID!
  name: String!
  appearsIn: [Episode]
  friends("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String "Returns the last _n_ elements from the list." last: Int "Returns the elements in the list that come before the specified cursor." before: String): FriendsConnection @listSize(assumedSize: 50, slicingArguments: [ "first", "last" ], slicingArgumentDefaultValue: 10, sizedFields: [ "edges", "nodes" ], requireOneSlicingArgument: false)
  height(unit: Unit): Float
  primaryFunction: String
  traits: JSON
}

"A connection to a list of items."
type FriendsConnection {
  "Information to aid in pagination."
  pageInfo: PageInfo!
  "A list of edges."
  edges: [FriendsEdge!]
  "A flattened list of the nodes."
  nodes: [Character]
}

"An edge in a connection."
type FriendsEdge {
  "A cursor for use in pagination."
  cursor: String!
  "The item at the end of the edge."
  node: Character
}

type Human implements Character {
  id: ID!
  name: String!
  appearsIn: [Episode]
  friends("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String "Returns the last _n_ elements from the list." last: Int "Returns the elements in the list that come before the specified cursor." before: String): FriendsConnection @listSize(assumedSize: 50, slicingArguments: [ "first", "last" ], slicingArgumentDefaultValue: 10, sizedFields: [ "edges", "nodes" ], requireOneSlicingArgument: false)
  otherHuman: Human
  height(unit: Unit): Float
  homePlanet: String
  traits: JSON
}

type Mutation {
  createReview(episode: Episode! review: ReviewInput!): Review! @cost(weight: "10")
  complete(episode: Episode!): Boolean! @cost(weight: "10")
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
  hero(episode: Episode! = NEW_HOPE): Character
  heroByTraits(traits: JSON!): Character
  heroes(episodes: [Episode!]!): [Character!]
  character(characterIds: [String!]!): [Character!]! @cost(weight: "10")
  search(text: String!): [SearchResult]
  human(id: String!): Human
  droid(id: String!): Droid
  time: Long!
  evict: Boolean!
  wait(m: Int!): Boolean! @cost(weight: "10")
  someDeprecatedField(deprecatedArg: String! = "foo" @deprecated(reason: "use something else")): String! @deprecated(reason: "use something else")
}

type Review {
  commentary: String @cost(weight: "10")
  stars: Int!
}

type Starship {
  id: ID!
  name: String!
  length(unit: Unit): Float!
}

type Subscription {
  onReview(episode: Episode!): Review!
  onNext: String! @cost(weight: "10")
  onException: String! @cost(weight: "10")
  delay(delay: Int! count: Int!): String! @cost(weight: "10")
}

union SearchResult = Starship | Human | Droid

input ReviewInput {
  stars: Int!
  commentary: String
}

enum Episode {
  NEW_HOPE
  EMPIRE
  JEDI
}

enum Unit {
  FOOT
  METERS
}

"The purpose of the `cost` directive is to define a `weight` for GraphQL types, fields, and arguments. Static analysis can use these weights when calculating the overall cost of a query or response."
directive @cost("The `weight` argument defines what value to add to the overall cost for every appearance, or possible appearance, of a type, field, argument, etc." weight: String!) on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

"The `@defer` directive may be provided for fragment spreads and inline fragments to inform the executor to delay the execution of the current fragment to indicate deprioritization of the current fragment. A query with `@defer` directive will cause the request to potentially return multiple responses, where non-deferred data is delivered in the initial response and data deferred is delivered in a subsequent response. `@include` and `@skip` take precedence over `@defer`."
directive @defer("If this argument label has a value other than null, it will be passed on to the result of this defer directive. This label is intended to give client applications a way to identify to which fragment a deferred result belongs to." label: String "Deferred when true." if: Boolean) on FRAGMENT_SPREAD | INLINE_FRAGMENT

directive @foo(bar: Int!) on SUBSCRIPTION

"The purpose of the `@listSize` directive is to either inform the static analysis about the size of returned lists (if that information is statically available), or to point the analysis to where to find that information."
directive @listSize("The `assumedSize` argument can be used to statically define the maximum length of a list returned by a field." assumedSize: Int "The `slicingArguments` argument can be used to define which of the field's arguments with numeric type are slicing arguments, so that their value determines the size of the list returned by that field. It may specify a list of multiple slicing arguments." slicingArguments: [String!] "The `slicingArgumentDefaultValue` argument can be used to define a default value for a slicing argument, which is used if the argument is not present in a query." slicingArgumentDefaultValue: Int "The `sizedFields` argument can be used to define that the value of the `assumedSize` argument or of a slicing argument does not affect the size of a list returned by a field itself, but that of a list returned by one of its sub-fields." sizedFields: [String!] "The `requireOneSlicingArgument` argument can be used to inform the static analysis that it should expect that exactly one of the defined slicing arguments is present in a query. If that is not the case (i.e., if none or multiple slicing arguments are present), the static analysis may throw an error." requireOneSlicingArgument: Boolean! = true) on FIELD_DEFINITION

"The `@stream` directive may be provided for a field of `List` type so that the backend can leverage technology such as asynchronous iterators to provide a partial list in the initial response, and additional list items in subsequent responses. `@include` and `@skip` take precedence over `@stream`."
directive @stream("If this argument label has a value other than null, it will be passed on to the result of this stream directive. This label is intended to give client applications a way to identify to which fragment a streamed result belongs to." label: String "The initial elements that shall be send down to the consumer." initialCount: Int! = 0 "Streamed when true." if: Boolean) on FIELD

scalar JSON

"The `Long` scalar type represents non-fractional signed whole 64-bit numeric values. Long can represent values between -(2^63) and 2^63 - 1."
scalar Long
```
