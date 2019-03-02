# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- The stitching layer now batches requests to the remote schemas.
- Introspection schema serializer.
- Introduced auto-stitching capabilities with the new `StitchingBuilder`.
- _GraphQL_ _Voyager_. Special thanks to [@drowhunter](https://github.com/drowhunter) who contributed the middleware.

### Fixed

- Introspection default values are now serialized correctly.
- Added missing validation rule: https://facebook.github.io/graphql/June2018/#sec-All-Variable-Uses-Defined
- The non-null value violation is now propagated correctly. https://facebook.github.io/graphql/June2018/#sec-Errors

## [0.7.0] - 2019-02-03

### Added

- Query Middleware [#338](https://github.com/ChilliCream/hotchocolate/issues/338).
- Field Middleware [#338](https://github.com/ChilliCream/hotchocolate/issues/338).
- Implemented the relay cursor connections [specification](https://facebook.github.io/relay/graphql/connections.htm).
- Added another `ReportError` overload to `IResolverContext` that takes `IError` [#359](https://github.com/ChilliCream/hotchocolate/issues/359).
- Added a schema endpoint that will let you download the server schema file [#370](https://github.com/ChilliCream/hotchocolate/issues/370).
- Added SyntaxRewriter and SyntaxWalker classes to enable developers to extend the execution pipeline more easily.
- Introduced a new execution builder which allows to fully customize the execution pipeline.
- Introduced exception filter [#317](https://github.com/ChilliCream/hotchocolate/issues/317).
- Integrated `RequestTimeoutMiddleware` into default pipeline [#418](https://github.com/ChilliCream/hotchocolate/issues/418).
- Added support for repeatable directive. [Spec](https://github.com/facebook/graphql/pull/472).
- Apollo Tracing Support [#352](https://github.com/ChilliCream/hotchocolate/issues/352).
- Query complexity validation rules [#80](https://github.com/ChilliCream/hotchocolate/issues/80)
- Added support for relay global object identification specification [specification](http://facebook.github.io/relay/graphql/objectidentification.htm).
- Added Source Code Link for NuGet support.
- Added support for a executor scoped field middleware [#482](https://github.com/ChilliCream/hotchocolate/issues/482).
- Added schema stitching capabilities [#341](https://github.com/ChilliCream/hotchocolate/issues/341).
- Added generic interface type [#546](https://github.com/ChilliCream/hotchocolate/issues/546).
- Added directives support for input objects [#548](https://github.com/ChilliCream/hotchocolate/issues/548).
- Added optional totalCount field to the connection type [#558](https://github.com/ChilliCream/hotchocolate/issues/558).
- Added support for dynamic generated schema types [#558](https://github.com/ChilliCream/hotchocolate/issues/558).
- Added generic union type [#552](https://github.com/ChilliCream/hotchocolate/issues/552).
- Added options to AddStitchedSchema [#556](https://github.com/ChilliCream/hotchocolate/issues/556).
- Added scoped context data to the resolver context [#537](https://github.com/ChilliCream/hotchocolate/issues/537).

### Changed

- Merged _ASP.NET core_ and _classic_ codebases [#349](https://github.com/ChilliCream/hotchocolate/issues/349).
- Made the type conversion API extendable and added more default type converter [#384](https://github.com/ChilliCream/hotchocolate/issues/384).
- Separated the schema config from the execution config [#324](https://github.com/ChilliCream/hotchocolate/issues/324)
- Changed how max query depth is validated and configured.
- The DataLoader API is now offering a simpler interface.
- Extended Scalar Types must now be explicitly registered during schema configuration. [#433](https://github.com/ChilliCream/hotchocolate/issues/433)
- Authorization directive is now repeatable and can use the default authorization policy [#485](https://github.com/ChilliCream/hotchocolate/pull/485)
- UsePaging can now be used without specifying the clr type [#558](https://github.com/ChilliCream/hotchocolate/issues/558).
- The edge node type can now be any output type including list [#558](https://github.com/ChilliCream/hotchocolate/issues/558).

### Fixed

- Field merging of node fields did not work properly.
- DateTime is now parsed independent of the current culture [#547](https://github.com/ChilliCream/hotchocolate/pull/547).

### Deprecated

- The `Schema.Execute...` extension methods are depricated and will be removed with the next version.

### Removed

- Execution options from the schema options. They can now be configured with the `QueryExecutionBuilder`.

## [0.6.11] - 2018-12-06

### Added

- _GraphQL_ _Playground_ [#353](https://github.com/ChilliCream/hotchocolate/issues/353). Special thanks to [@akaSybe](https://github.com/akaSybe) who contributed the playground middleware.

### Changed

- Improve `IObjectTypeDescriptor` interface [#390](https://github.com/ChilliCream/hotchocolate/issues/390).

## [0.6.10] - 2018-12-05

### Added

- Non-generic dataloader configration extensions.

## [0.6.9] - 2018-11-30

### Added

- Non-generic method to register a dataloader.

## [0.6.8] - 2018-11-29

### Added

- Non-generic register methods to schema configuration.

### Fixed

- Ignore on `InputObjectType` fields didn't work properly.

## [0.6.7] - 2018-11-25

### Fixed

- Non-nullable arguments are being inferred as nullable bug [#360](https://github.com/ChilliCream/hotchocolate/issues/360).

## [0.6.6] - 2018-11-23

### Fixed

- Middleware bug that prevented the result to be passed along the pipeline.

## [0.6.5] - 2018-11-20

### Added

- Support for `GraphQLNonNullAttribute`.
- Support for `Include` on object type to merge a resolver type into the object type.
- Support for `GraphQLResolverAttribute` and `GraphQLResolverForAttribute`.

## [0.6.4] - 2018-11-20

### Fixed

- The type discoverer ignored a type if it was already discovered in another context [#350](https://github.com/ChilliCream/hotchocolate/issues/350).

## [0.6.3] - 2018-11-19

### Fixed

- Validation issues with `NameString`.

## [0.6.2] - 2018-11-19

### Fixed

- Fixed: `byte[]` cannot be defined as a custom scalar [#345](https://github.com/ChilliCream/hotchocolate/issues/345).

## [0.6.1] - 2018-11-15

### Changed

- `DateTimeType` now serializes UTC `DateTime` to `yyyy-MM-ddTHH\:mm\:ss.fffZ`.

### Fixed

- List Variable Coercion Failed.
- InputTypes are now discovered correctly.

## [0.6.0] - 2018-11-12

### Added

- Separate package providing a _GraphiQL_ middleware. The middleware can serve all of _GraphiQL_ without needing to refer to CDNs making it useful even in closed networks. Moreover, we have configured _GraphiQL_ to work with the _GraphQL-ws_ protocol which is supported by _Hot Chocolate_.
- Initial Support for _GraphQL_ subscriptions. We currently support the _GraphQL-ws_ protocol over web sockets. There will be a lot of additional work in version _0.7.0_ that will harden it.
- Authorization package for ASP.net core which supports policy-base authorization on fields.
- Diagnostic source which can be used to track field execution times and other events.
- Implementing a directive middleware has now become much easier with this release. We have built the authorize-directive with these new APIs.

[unreleased]: https://github.com/ChilliCream/hotchocolate/compare/0.7.0...HEAD
[0.7.0]: https://github.com/ChilliCream/hotchocolate/compare/0.6.11...0.7.0
[0.6.11]: https://github.com/ChilliCream/hotchocolate/compare/0.6.10...0.6.11
[0.6.10]: https://github.com/ChilliCream/hotchocolate/compare/0.6.9...0.6.10
[0.6.9]: https://github.com/ChilliCream/hotchocolate/compare/0.6.8...0.6.9
[0.6.8]: https://github.com/ChilliCream/hotchocolate/compare/0.6.7...0.6.8
[0.6.7]: https://github.com/ChilliCream/hotchocolate/compare/0.6.6...0.6.7
[0.6.6]: https://github.com/ChilliCream/hotchocolate/compare/0.6.5...0.6.6
[0.6.5]: https://github.com/ChilliCream/hotchocolate/compare/0.6.4...0.6.5
[0.6.4]: https://github.com/ChilliCream/hotchocolate/compare/0.6.3...0.6.4
[0.6.3]: https://github.com/ChilliCream/hotchocolate/compare/0.6.2...0.6.3
[0.6.2]: https://github.com/ChilliCream/hotchocolate/compare/0.6.1...0.6.2
[0.6.1]: https://github.com/ChilliCream/hotchocolate/compare/0.6.0...0.6.1
[0.6.0]: https://github.com/ChilliCream/hotchocolate/compare/0.5.2...0.6.0
