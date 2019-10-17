# Roadmap

This document shall give and outlook of what we have planned for the next releases and what we have already implemented.

## [June 2018 Specification](http://facebook.github.io/graphql/June2018/)

### Types

- [x] Object Type
- [x] Interface Type
- [x] Union Type
- [x] Enum Type
- [x] Input Object Type

### Scalar Types

- [x] Int
- [x] Float
- [x] String
- [x] Boolean
- [x] ID

### Directives

- [x] Skip
- [x] Continue
- [x] Deprecated
- [ ] Locations
  - [x] Query
  - [x] Mutation
  - [x] Subscription
  - [x] Field
  - [x] FragmentDefinition
  - [x] FragmentSpread
  - [x] InlineFragment
  - [x] Schema
  - [ ] Scalar (in development - 9.0.0)
  - [x] Object
  - [x] FieldDefinition
  - [x] ArgumentDefinition
  - [x] Interface
  - [x] Union
  - [x] Enum
  - [x] EnumValue
  - [x] InputObject
  - [x] InputFieldDefinition
  - [x] VariableDefinition

### Validation

- [x] [Validation](https://github.com/ChilliCream/hotchocolate/projects/3)

### Execution

- [x] Query
- [x] Mutation
- [x] Subscription

### Introspection

- Fields

  - [x] \_\_typename
  - [x] \_\_type
  - [x] \_\_schema

- \_\_Schema

  - [x] types
  - [x] queryType
  - [x] mutationType
  - [x] subscriptionType
  - [x] directives

- \_\_Type
  - [x] kind
  - [x] name
  - [x] fields
  - [x] interfaces
  - [x] possibleTypes
  - [x] enumValues
  - [x] inputFields
  - [x] ofType

## [Working Draft Specification](http://facebook.github.io/graphql/draft/)

We are currently working on the following features that are proposed for the next GraphQL specification.

- [x] [Limit directive uniqueness to explicitly marked directives](https://github.com/facebook/graphql/pull/472)
- [x] ["Directive order is significant" section](https://github.com/facebook/graphql/pull/470)
- [ ] [Add rules for how circular references in Input Objects are handled](https://github.com/facebook/graphql/pull/445) (in development - 11.0.0)
- [x] [Add description to Schema](https://github.com/facebook/graphql/pull/466)

## Experimental Features

- [ ] [Flat chain syntax](https://github.com/facebook/graphql/issues/174)

## Additional Scalar Types

- [x] DateTime
- [x] Date
- [x] URL
- [x] UUID
- [x] Decimal
- [x] Short (Int16)
- [x] Long (Int64)
- [x] Custom Scalars

## Additional Directives

- [x] Schema Stitching
- [ ] HTTP Directives (in development - 12.0.0)
- [x] Custom Schema Directives
- [x] Custom Query Directives

## Execution Engine

- [x] Data Loader Integration
- [x] Batched Operations

### Schema Creation

- [x] Schema-First approach
- [x] Code-First approach
- [x] Schema Builder

## Supported Frameworks

- [x] ASP.NET Core
  - [x] Get
  - [x] Post
  - [x] WebSockets
