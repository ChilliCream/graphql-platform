# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

## [10.0.0]

### Added

- Added support to infer if a field or enum value is deprecated. [#826](https://github.com/ChilliCream/hotchocolate/pull/826)
- Added filter types. [#861](https://github.com/ChilliCream/hotchocolate/pull/861)
- Added UTF-8 request parser. [#869](https://github.com/ChilliCream/hotchocolate/pull/869)
- Added new syntax visitor API.
- Added Redis subscription provider [#902](https://github.com/ChilliCream/hotchocolate/pull/902)
- Added support for batching over HTTP [#933](https://github.com/ChilliCream/hotchocolate/pull/933)
- Added support for persisted queries and added a middleware to enable the active persisted query flow. [#858](https://github.com/ChilliCream/hotchocolate/pull/858)
- Provide access to variables through IResolverContext. [#958](https://github.com/ChilliCream/hotchocolate/pull/958)
- Added ability to control when the schem stitching will pull in the remote schemas. [#964](https://github.com/ChilliCream/hotchocolate/pull/964)
- Added support to register class DataLoader with the standard dependency injection. [#966](https://github.com/ChilliCream/hotchocolate/pull/966)
- Added support for ObsoleteAttribute. Fields & enum values that are annotated with ObsoleteAttribute become deprecated.
- Add Redis Provider for Subscriptions [#902](https://github.com/ChilliCream/hotchocolate/pull/902)
- Added not authenticated error code.
- Added TryAddProperty, TryAddExtension and TryAddVariable to query request builder
- Added support for persisted queries with providers for the file system and Redis. [#858](https://github.com/ChilliCream/hotchocolate/pull/858)
- Added support for middleware on introspection fields. [#962](https://github.com/ChilliCream/hotchocolate/pull/962)
- Added default binding behavior option to schema options. [#963](https://github.com/ChilliCream/hotchocolate/pull/963)
- Added support for directives on enum values with schema first [#815](https://github.com/ChilliCream/hotchocolate/pull/815)
- Added DataLoader Dependency Injection Support. [#966](https://github.com/ChilliCream/hotchocolate/pull/966)
- Added AddDocumentFromFile to SchemaBuilder. [#974](https://github.com/ChilliCream/hotchocolate/pull/974)
- Added error filter ServiceCollection extensions [#973](https://github.com/ChilliCream/hotchocolate/pull/973)
- Added schema validation rule that ensures that interfaces are implemented. [#979](https://github.com/ChilliCream/hotchocolate/pull/979)

### Changed

- Subscription now uses pipeline API to abstract sockets. [#807](https://github.com/ChilliCream/hotchocolate/pull/807)
- Improved parser performance. [#806](https://github.com/ChilliCream/hotchocolate/pull/806)
- Roles collection on authorization directive is now interpreted as OR.
- The type conversion API is now better integreated with dependency injection.
- The server is now more modularized and the various server middlewares can be added separably.
- context.Parent() converts now the result if the source object is not of the type of T
- Update GraphQL-Voyager to 1.0.0-rc.27 [#972](https://github.com/ChilliCream/hotchocolate/pull/972)

### Fixed

- The parent method on the resolver context now uses the converters if the source object type does not align with the requested type.
- Aligned the deprecation handling with the GraphQL spec. [#876](https://github.com/ChilliCream/hotchocolate/pull/876)
- Fixed the StateAttribute for resolvers. [#887](https://github.com/ChilliCream/hotchocolate/pull/887)
- Order of types in a serialized schema is now consistent. [#891](https://github.com/ChilliCream/hotchocolate/pull/891)
- Respect UseXmlDocumentation with Schema.Create [#897](https://github.com/ChilliCream/hotchocolate/pull/897)
- Variables now work in lists and input objects [#896](https://github.com/ChilliCream/hotchocolate/pull/896)
- Fixed url scalar now correctly detects url strings.
- Support directives declared stitched schemas [#936](https://github.com/ChilliCream/hotchocolate/pull/936)
- Fixed issues with filters and variables. [#960](https://github.com/ChilliCream/hotchocolate/pull/960)
- Fixed issues stitching lists. [#946](https://github.com/ChilliCream/hotchocolate/pull/946)
- Fixed source link support. [#943](https://github.com/ChilliCream/hotchocolate/pull/943)
- Fixed dead-lock issues with the DataLoader [#942](https://github.com/ChilliCream/hotchocolate/pull/942)
- Fixed UrlType IsInstanceOfType method to correctly return true for a Url StringValueNode.
- Fixed Date and DateTime handling in the stitching layer.
- Fixed subscriptions with InputObject arguments [#975](https://github.com/ChilliCream/hotchocolate/pull/975)
- Fixed Playground, GraphiQL and Voyager path logic in middleware options object. [#984](https://github.com/ChilliCream/hotchocolate/pull/984)
- Exceptions are now correctly removed from the remote error data structure so that no exception information leaks.
- Fixed IResolverContext.CollectFields issue where the provided selection set was not passed to the underlying field collection algoritm. [#994](https://github.com/ChilliCream/hotchocolate/pull/994)
- Fixed variable coercion so that we are now throwing GraphQL errors if the wrong type is passed. [#991](https://github.com/ChilliCream/hotchocolate/pull/991)
- Fixed type rename issue in the stitching layer when types where nun-null types. [#998](https://github.com/ChilliCream/hotchocolate/pull/998)

## [9.0.4] - 2019-06-16

### Fixed

- Fixed paging flaws that in some cases lead to the connection type being registered twice. [#842](https://github.com/ChilliCream/hotchocolate/pull/842)

## [9.0.3] - 2019-06-13

### Fixed

- Fixed issues where the type initializer would swallow schema errors.

## [9.0.2] - 2019-06-12

### Fixed

- Fixed issues with list input types.

## [9.0.1] - 2019-06-09

### Changed

- Better error message when failing TypeConvertion enhancement [#819](https://github.com/ChilliCream/hotchocolate/issues/819)

### Fixed

- Fixed list argument conversion issue. [#823](https://github.com/ChilliCream/hotchocolate/issues/823)

## [9.0.0] - 2019-06-04

### Added

- Added new SchemaBuilder. [#369](https://github.com/ChilliCream/hotchocolate/issues/369)
- Added code-first type extensions. [#683](https://github.com/ChilliCream/hotchocolate/issues/683)
- Added support for schema description. [spec](https://github.com/graphql/graphql-spec/pull/466)
- Added resolver overloads to schema builder.
- Added new UTF-8 parser
- Added support for schema directives. [spec](https://graphql.github.io/graphql-spec/June2018/#sec-Schema)
- Added two phase argument coercion.
- Added two phase field collection.
- Added GraphQL attributes support on parameters. [726](https://github.com/ChilliCream/hotchocolate/issues/726)
- Added support for directives on variable definitions [spec](https://github.com/graphql/graphql-spec/pull/510)
- Added support for directives on enum values [spec](https://graphql.github.io/graphql-spec/June2018/#EnumValueDefinition)
- Added support for directives on arguments [spec](https://graphql.github.io/graphql-spec/June2018/#ArgumentsDefinition)
- Added support for XML documentation [715](https://github.com/ChilliCream/hotchocolate/issues/715)
- Added access to stitched http response headers (e.g. Set-Cookie) [679](https://github.com/ChilliCream/hotchocolate/issues/679)
- Added helper to add delegation paths to a field.
- It is now possible to bind .net types explicitly to schema types with SchemaBuilder.New().BindClrType<ClrType, SchemaType>(). [756](https://github.com/ChilliCream/hotchocolate/issues/756)
- Added support for schema-first bindings on the `SchemaBuilder` API. [781](https://github.com/ChilliCream/hotchocolate/issues/781)

### Changed

- Replaced roslyn compiler with the expression compiler. This will reduce the memory footprint of the server.
- Changed how the server caches queries.
- `DiagnosticNames` is now public.
- Reworked how input object arguments are cached [#805](https://github.com/ChilliCream/hotchocolate/pull/805)

### Removed

- Removed obsolete QueryDocument from IResolverContext.
- Removed obsolete CancellationToken from IResolverContext

### Fixed

- Includes directive definitions in serialized schema [#717](https://github.com/ChilliCream/hotchocolate/issues/717)
- Field types are now validated. [#713](https://github.com/ChilliCream/hotchocolate/issues/713)
- Variables in object values and lists are now correctly recognised [#215](https://github.com/ChilliCream/hotchocolate/issues/215) and [#745](https://github.com/ChilliCream/hotchocolate/issues/745).
- Fixed issue with input type arguments on the stitching layer
- Delegate Directive not being assigned correctly with ITypeRewriter. [#766](https://github.com/ChilliCream/hotchocolate/issues/766)
- Format Exception when registering types. [#787](https://github.com/ChilliCream/hotchocolate/issues/787)
- The schema factory does not throw an exception if an annotated directive is not correct. [#619](https://github.com/ChilliCream/hotchocolate/issues/619)
- Temprarily fixed issues with type system directives. We will add a final patch with the next release.
- Fixed issues with external resolver overwrites.
- Fixed authorization directive validation issues [#804](https://github.com/ChilliCream/hotchocolate/issues/804)
- Fixed schema initialization issues
- Fixed paging extension overloads [#811](https://github.com/ChilliCream/hotchocolate/issues/811)
- Fixed null reference exception in Path.Equals [#802](https://github.com/ChilliCream/hotchocolate/issues/802)

## [0.8.2] - 2019-04-10

### Fixed

- Some scalars did not work in stitched schemas and lead to a serialization exception.
- IntValueNode IValueNode.Equals was not implemented correctly and lead always to false. [#681](https://github.com/ChilliCream/hotchocolate/issues/681)

## [0.8.1] - 2019-03-29

### Added

- Added operation start/stop event.
- Added error filter support for schema stitching.

### Changed

- Default complexity calculation functions are now public.
- Diagnostic observers can now be defined as schema services.
- auto-stitching diagnostic call is now implemented in two phases.

### Fixed

- Custom diagnostic observer registration issue [#629](https://github.com/ChilliCream/hotchocolate/issues/629).
- Authorization argument coercion is now fixed. [#624](https://github.com/ChilliCream/hotchocolate/issues/624)
- The request path is now compared correctly.
- IErrorFilter is not given the exception unless IncludeExceptionDetails is enabled. [#637](https://github.com/ChilliCream/hotchocolate/issues/638)
- Parse and validation event tracked wrong duration.
- Schema-First descriptions are now correctly included into the schema. [#647](https://github.com/ChilliCream/hotchocolate/issues/647)
- __type argument was named `type` instead of `name`. [spec](https://facebook.github.io/graphql/June2018/#sec-Introspection)
- The server template is now working again. [#657](https://github.com/ChilliCream/hotchocolate/issues/657)
- Non-nullable types are now validated when query uses variables. [#651](https://github.com/ChilliCream/hotchocolate/issues/651)
- Variable handling im middleware does not convert the DateTime value anymore. [#664](https://github.com/ChilliCream/hotchocolate/issues/664)
- Directives are now correctly merged when declared in an extension file. [#665](https://github.com/ChilliCream/hotchocolate/issues/665)
- Subscription is now optional in the 2-phase introspection call [#668](https://github.com/ChilliCream/hotchocolate/issues/668)

## [0.8.0] - 2019-03-03

### Added

- The stitching layer now batches requests to the remote schemas.
- Introspection schema serializer.
- Introduced auto-stitching capabilities with the new `StitchingBuilder`.
- _GraphQL_ _Voyager_. Special thanks to [@drowhunter](https://github.com/drowhunter) who contributed the middleware.

### Changed

- The authoization directive is now more aligned how the authorize attribute in ASP .Net works.

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
- Authorization package for ASP .Net core which supports policy-base authorization on fields.
- Diagnostic source which can be used to track field execution times and other events.
- Implementing a directive middleware has now become much easier with this release. We have built the authorize-directive with these new APIs.

[unreleased]: https://github.com/ChilliCream/hotchocolate/compare/0.8.2...HEAD
[0.8.2]: https://github.com/ChilliCream/hotchocolate/compare/0.8.1...0.8.2
[0.8.1]: https://github.com/ChilliCream/hotchocolate/compare/0.8.0...0.8.1
[0.8.0]: https://github.com/ChilliCream/hotchocolate/compare/0.7.0...0.8.0
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
