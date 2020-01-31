# Core

The core contains the parser, the type system and the query engine.

- Utilities
  Internal utility classes that are shared between all the core APIs.

- Language
  The language API represents the parser, lexer, syntax visitor base classes and syntax rewriter base classes.

- Abstractions
  Fundamental classes and interfaces that are use by the type system and the execution.

- Types
  The type API contains the type system, resolvers and the field middleware.

- Core
  The core project hosts the query engine (execution) and validation. We plan to spearate those two in the future.

- Subscriptions
  The subscription API provides interfaces defining a Pub/Sub-system for the execution engine and a default InMemory implementation for the Pub/Sub-system.

- Stitching
  The stitching API contains a middleware that allows to merge remote schemas and other services into a single executable schema.


