# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Addtional HotChocolate scalars  (HotChocolate.Types.Scalars)
    - PhoneNumber (#2995)
    - EmailAddress (#2989)
    - NegativeFloat (#2996)
    - NonPositiveFloat (#3024)
    - NonNegativeFloat (#3015)
    - PositiveInt (#2929)
    - NegativeInt (#2940)
    - NonNegativeInt (#3020)
    - NonEmptyString (#2940)

### Fixed

- Fixed issue where the PagingHelper introduced a self-reference which cause type system initialization issues.

## [11.0.9]

### Added

- Added result formatter options to minify the JSON payload. (#2897)

### Changed

- Relaxed UUID deserialization (by default we now allow for any UUID format). (#2896)

### Fixed

- Fixed selection optimizer were not resolved correctly. (#2889)
- Fixed projection of edge type (#2888) 

## [11.0.8]

### Added

- Added support for field coordinates. (#2881)
- Added in-memory cache for persisted queries. (#2872)
- Added a way to intercept the initialization flow (#2868)
- Allow for re-fetching on mutation payloads. (#2851)
- Allow local schemas to be used in schema stitching (#2835)
- Allow to enumerate variables (#2833)

### Changed

- Reworked Type Inference to be stricter. (#2842)

### Fixed

- Fixed projections with multiple interceptors. (#2836)
- Fixed string literals in places of enum values have to raise a query errors. (#2846)
- Fixed issues with Apollo active persisted queries flow. (#2864)
    

## [11.0.7]

### Added

- Add HasErrors to IResolverContext (#2814)
- Added GraphQL descriptions to delegate directive. (#2783)

### Fixed

- Fixed argument renaming during schema stitching. (#2784)
- Fixed cursor backward pagination with two list elements (#2777)

## [11.0.6]

### Fixed

- Fixed error filters not being activated (#2774)

## [11.0.5]

### Fixed

- Fixes query rewriting when fields are merged during stitching. (#2765)

## [11.0.4]

### Fixed

- Fixed executable detection (#2762)

## [11.0.3]

### Fixed

- Added back the syntax serializers for backward compatibility (#2758)

## [11.0.2]

### Fixed

- Fixed PagingAmountRewriter for stitching in migration guide (#2737)
- Fixed execution of batch requests (#2726)

## [11.0.1]

### Added

<<<<<<< HEAD
- Added result formatter options to minify the JSON payload. (#2897)

### Changed

- Relaxed UUID deserialization (by default we now allow for any UUID format). (#2896)

### Fixed

- Fixed selection optimizer were not resolved correctly. (#2889)
- Fixed projection of edge type (#2888) 

## [11.0.8]

### Added

- Added support for field coordinates. (#2881)
- Added in-memory cache for persisted queries. (#2872)
- Added a way to intercept the initialization flow (#2868)
- Allow for re-fetching on mutation payloads. (#2851)
- Allow local schemas to be used in schema stitching (#2835)
- Allow to enumerate variables (#2833)

### Changed

- Reworked Type Inference to be stricter. (#2842)

### Fixed

- Fixed projections with multiple interceptors. (#2836)
- Fixed string literals in places of enum values have to raise a query errors. (#2846)
- Fixed issues with Apollo active persisted queries flow. (#2864)
    

## [11.0.7]

### Added

- Add HasErrors to IResolverContext (#2814)
- Added GraphQL descriptions to delegate directive. (#2783)

### Fixed

- Fixed argument renaming during schema stitching. (#2784)
- Fixed cursor backward pagination with two list elements (#2777)

## [11.0.6]

### Fixed

- Fixed error filters not being activated (#2774)

## [11.0.5]

### Fixed

- Fixes query rewriting when fields are merged during stitching. (#2765)

## [11.0.4]

### Fixed

- Fixed executable detection (#2762)

## [11.0.3]

### Fixed

- Added back the syntax serializers for backward compatibility (#2758)

## [11.0.2]

### Fixed

- Fixed PagingAmountRewriter for stitching in migration guide (#2737)
- Fixed execution of batch requests (#2726)

## [11.0.1]

### Added

=======
>>>>>>> develop
- Added backward compatibility for BindClrType (#2709)

### Fixed

- Fixed issue where interfaces cause filtering to fail (#2686)
- Fixed ignoring fields on filtering and sorting types (#2690)
- Fixed SubscribeAndResolve with ISourceStream (#2659)

### Removed

- Removed legacy syntax printer and ensured that only the new one is used. (#2711)

## [11.0.0]

### Added

- Added FirstOrDefault Middleware & improved EF docs (#2635)
- Added support for spatial types and spatial filters (#2419, #2541, #2566, #2567, #2637, #2638, #2639)
- Added new data integration API (#2453, #2633, #2631, #2629, #2618, #2619, #2314, #2311, #2310, #2300, #2296, #2228, #2255)
- Introduced new conventions API (#2455, #2457, #2451)
- Added Banana Cake Pop middleware which replaces the Voyager, Playground and GraphiQL middleware. (#2417)
- Added better error when root value cannot be created. (#2598)
- Added schema error interceptor (#2581)
- Added new Dataloader middleware (#2548)
- Added support for IExecutable to Data (#2527)
- Added support to use a page type directly (#2540)
- Added conventions to executor builder (#2532)
- Added support for federated stitching. (#2471)
- Added pure code-first Node resolver (#2434)
- Added support for records (#2428, #2331)
- Added support for enums to GraphQLDescriptionAttribute (#2430)
- Added Configuration API for the Global ID serializer. (#2370)
- Added offset paging support (#2330).
- Added a input value formatter that can be bound to input fields and arguments. (#2277)
- SPEC: Custom Scalar Specification URLs  (#2614)
- SPEC: Adds @defer. (#2359, #2377)

### Changed

- Refined converter API (#2593)
- Changed schema endpoint route from `http://localhost/graphql/schema` to `http://localhost/graphql?sdl`
- Changed ASP.NET Core integration by using the ASP.NET Core routing API.
- Refined .NET Templates (#2570)
- Reworked Stitching Variable Handling (#2533)
- Reworked Stitching Error Handling (#2529)
- Reworked diagnostic API.
- Stitching: Allow for scalars to be renamed when stitching a schema. (#2424)
- Stitching: Allow for list aggregations when delegating. (#2418)
- Stitching: Migrates stitching to the new configuration API (#2403)
- SPEC: Rewrote GraphQL middleware to align with current GraphQL over HTTP spec draft. (#2280, #2282, #2274)
- SPEC: Re-implemented request validation to align better GraphQL spec and graphql-js.

### Removed

- Removed Voyager middleware.
- Removed Playground middleware.
- Removed GraphiQL middleware.
- Remove legacy selections (#2478)

### Fixed

- Allow to annotate enums and enum values with GraphQLNameAttribute (#2315)
- Fixed xml documentation inheritance issue. (#2645)
- The type trimming now correctly handles executable directives (#2605)
- Fixed issue when using subscriptions with variables. (#2596)
- Fixed name convention interference with the introspection types. (#2588)
- Fixed field resolver respect inheritance (#2515)
- Fixed issue that directives were dropped when using the schema printer. (#2486)
- Fixed Projection when using offset paging (#2476)
- Fixed value coercion of enum values (#2477)
- Fixed inference of enum values from GraphQL SDL
- Fixed issue with non-null arguments that have defaults (#2441)

## [10.5.5]

### Fixed

- Fixed operation serialization [#2646](https://github.com/ChilliCream/hotchocolate/pull/2646)

## [10.5.4]

### Fixed

- Fixed QueryRequestBuild handling of extensions. [#2608](https://github.com/ChilliCream/hotchocolate/pull/2608)

## [10.5.3]

### Fixed

- Fixed ConnectionMiddleware and IEnumerable + IConnection [#2378](https://github.com/ChilliCream/hotchocolate/pull/2378)

## [10.5.2]

### Fixed

- Fixed ID serialization on input types [#2174](https://github.com/ChilliCream/hotchocolate/pull/2174)

## [10.5.1]

### Fixed

- Fixed field discovery [#2167](https://github.com/ChilliCream/hotchocolate/pull/2167)

## [10.5.0]

### Added

- Added new `ResolveWith` descriptor method. [#1892](https://github.com/ChilliCream/hotchocolate/pull/1892)
- Added support for local schema authentication and multi delegation.
- Added nullable detection with Required attribute.
- Added support for expression syntax on field selectors `descriptor.Field(t => t.Foo.Bar)`. [#2157](https://github.com/ChilliCream/hotchocolate/pull/2157)
- Added TimeSpan scalar.
- Added support for field-scoped services (services that only live for the duration of the field execution).
- Added new `ID` attribute to streamline global object identifiers. [#2165](https://github.com/ChilliCream/hotchocolate/pull/2165)
- Added bew `ID` descriptor to streamline global object identifiers. [#2166](https://github.com/ChilliCream/hotchocolate/pull/2166)

### Changed

- Impoved the connection API for easier integration. [#1887](https://github.com/ChilliCream/hotchocolate/pull/1887)
- Unsealed AuthorizeAttribute. [#1993](https://github.com/ChilliCream/hotchocolate/pull/1993)
- Expose character-set in content-type.
- Use invariant culture when parsing numbers in AnyType [#2134](https://github.com/ChilliCream/hotchocolate/pull/2134)
- Changed behavior of `SubscribeAttribute` to align better with the behaviour of version 11.
- Changed ID serializer to align better with the behaviour of version 11.

### Fixed

- Fixed ambiguous Nullable Attribute. [#1982](https://github.com/ChilliCream/hotchocolate/pull/1982)
- Fixed projection of __typename. [#2009](https://github.com/ChilliCream/hotchocolate/pull/2009)
- Fix stitching serialization invalid json. [#2024](https://github.com/ChilliCream/hotchocolate/pull/2024) [#1972](https://github.com/ChilliCream/hotchocolate/pull/1972) [#2091](https://github.com/ChilliCream/hotchocolate/pull/2091)
- Fixed the serialization formatter for decimals. [#1940](https://github.com/ChilliCream/hotchocolate/pull/1940)
- Fixed deprecation delegation for schema stitching.
- Fixed optional handling when deserializing input values. [#2133](https://github.com/ChilliCream/hotchocolate/pull/2133) [#2153](https://github.com/ChilliCream/hotchocolate/pull/2153) [#2158](https://github.com/ChilliCream/hotchocolate/pull/2158)
- Fixed compile error in templates.
- Fixed schema type discovery issues.
- Fixed field discovery for object type extensions.

## [10.4.0]

### Added

- We now infer default values from parameters and properties. [#1471](https://github.com/ChilliCream/hotchocolate/pull/1471)
- Added support for un-ignore [#1458](https://github.com/ChilliCream/hotchocolate/pull/1458)
- Introduced new state attributes [#1478](https://github.com/ChilliCream/hotchocolate/pull/1478)
- Added new subscription pub/sub system API. [#1480](https://github.com/ChilliCream/hotchocolate/pull/1480)
- Added support for value task to the type discovery.
- Added DataLoader Base Classes [#1505](https://github.com/ChilliCream/hotchocolate/pull/1505)
- Added support for IQueryable Projections [#1446](https://github.com/ChilliCream/hotchocolate/pull/1446)

### Changed

- Changed type member discovery [#1502](https://github.com/ChilliCream/hotchocolate/pull/1502)
- The context data on types now use less memory.
- Changed result serialization from Json.NET to System.Text.Json.

### Fixed

- Fixed issue where the backing type was rejected during deserialization.
- Fixed issue where nullable properties lead to errors in the sorting middleware [#1419](https://github.com/ChilliCream/hotchocolate/pull/1419)
- Fixed introspection issue where the `__type` field caused an error when the type specified in the argument did not exist.
- Fixed the generation of not equal expression for `IComparable` types.
- Fixed issue where the type dependencies of type extension where not correctly merged with the target type.
- Fixed `UUID` type serialization.

## [10.3.6]

### Fixed

- Fixed handling of variables when delegating data fetching through the stitching context. [#1390](https://github.com/ChilliCream/hotchocolate/pull/1390)

## [10.3.5]

### Fixed

- Fixed issue that caused errors when collecting fields on a directive context

## [10.3.4]

### Fixed

- Fixed default hash provider dependency injection configuration [#1363](https://github.com/ChilliCream/hotchocolate/pull/1363)


## [10.3.3]

### Fixed

- Fixed argument non-null validation.
- Fixed variable coercion.

## [10.3.2]

### Fixed

- Fixed issue where input fields were no longer automatically converted.
- Fixed issue where the float was rounded when provided as variable.

## [10.3.1]

### Fixed

- Fixed issue that private setters where not used during input deserialization.

## [10.3.0]

### Added

- Infer non-nullability from C# ref types enhancement. [#1236](https://github.com/ChilliCream/hotchocolate/pull/1236)
- Descriptor Attributes. [#1238](https://github.com/ChilliCream/hotchocolate/pull/1238)
- Introduced Subscribe Resolver for IAsyncEnumerable [#1262](https://github.com/ChilliCream/hotchocolate/pull/1262)
- Added support for generic object type extensions. [#1297](https://github.com/ChilliCream/hotchocolate/pull/1297)
- Added authorize attribute. [#1238](https://github.com/ChilliCream/hotchocolate/pull/1307)
- Added paging attribute. [#1306](https://github.com/ChilliCream/hotchocolate/pull/1306)
- Added filter attribute. [#1306](https://github.com/ChilliCream/hotchocolate/pull/1306)
- Added sorting attribute. [#1306](https://github.com/ChilliCream/hotchocolate/pull/1306)
- Added initial support for `Optional<T>`. [#1317](https://github.com/ChilliCream/hotchocolate/pull/1317)
- Added support for immutable input types. [#1317](https://github.com/ChilliCream/hotchocolate/pull/1317)

### Changed

- Stop adding the __typename field when it's in selection on schema stitching. [#1248](https://github.com/ChilliCream/hotchocolate/pull/1248)
- Improved Type Discovery. [#1281](https://github.com/ChilliCream/hotchocolate/pull/1281)

### Fixed

- Create a new service scope when cloning RequestContext in subscriptions. [#1211](https://github.com/ChilliCream/hotchocolate/pull/1211)
- Explicit binding of sorting types lead to errors. [#1055](https://github.com/ChilliCream/hotchocolate/pull/1055)
- Detect if an `IQueryable` already has a sorting. [#1253](https://github.com/ChilliCream/hotchocolate/pull/1253)
- Fixed issue with custom scalar types in delegated fields (schema stitching). [#1221](https://github.com/ChilliCream/hotchocolate/pull/1221)
- Fixed issue where the rented buffer was too early returned. [#1277](https://github.com/ChilliCream/hotchocolate/pull/1277)
- Fixed handling of rewritten non-null reference types. [#1288](https://github.com/ChilliCream/hotchocolate/pull/1288)
- Fixed clr type binding for issue with new type discovery. [#1304](https://github.com/ChilliCream/hotchocolate/pull/1304)
- Fixed parser error handling in middleware. [#1028](https://github.com/ChilliCream/hotchocolate/pull/1028)
- Fixed directive delegation (@skip/@include) in stitching. [#937](https://github.com/ChilliCream/hotchocolate/pull/937)

## [10.2.0]

### Added

- Added `Any` type. [#1055](https://github.com/ChilliCream/hotchocolate/pull/1055)
- Added non-generic `Type(Type type)` methods on field descriptors to allow for more dynamic schema generation. [#1079](https://github.com/ChilliCream/hotchocolate/issues/1079)
- Added `ArgumentKind(name)` to resolver context (#1134)
- Added FilterInputType customization methods. (#1150)

### Changed

- Made filter operation fields public and introduced interfaces.
- Use original operation name in stitched queries. [#1124](https://github.com/ChilliCream/hotchocolate/pull/1124)

### Fixed

- FilterTypes produce schema errors when filters properties are nullable. [#1034](https://github.com/ChilliCream/hotchocolate/pull/1034)
- MongoDB & Filter on `Boolean` property: the "_not" filter throws an exception. [#1033](https://github.com/ChilliCream/hotchocolate/pull/1033)
- Input object is not validated when given entirely as a variable [#1074](https://github.com/ChilliCream/hotchocolate/pull/1074)
- Variables parsing: Issue with nested `DateTime` fields in variables [#1037](https://github.com/ChilliCream/hotchocolate/pull/1037)
- DateTime Filters not working. [#1036](https://github.com/ChilliCream/hotchocolate/pull/1036)
- Date Filters not returning any result for equals filter [#1035](https://github.com/ChilliCream/hotchocolate/pull/1035)
- Subscription is not working with variables [#1176](https://github.com/ChilliCream/hotchocolate/pull/1176)
- Relay node field did not show in SDL [#1175](https://github.com/ChilliCream/hotchocolate/pull/1175)
- Filter issues and added more filter tests. [#1170](https://github.com/ChilliCream/hotchocolate/pull/1170)
- SelectionSetNode.AddSelections() did not add the new selections but duplicated the old ones. [#1142](https://github.com/ChilliCream/hotchocolate/pull/1142)
- The complecity middleware when multiplier where activated did only take the firs level into account. [#1137](https://github.com/ChilliCream/hotchocolate/pull/1137)
- Errors when attempting to filter on nullable types. [#1121](https://github.com/ChilliCream/hotchocolate/pull/1121)

## [10.1.0]

### Added

- Added more error codes. [#1030](https://github.com/ChilliCream/hotchocolate/pull/1030)
- Added better exceptions to the service factory. [#1040](https://github.com/ChilliCream/hotchocolate/pull/1040)

### Changed

- Distinguish between HTTP and remote schema errors with schema stitching. [#1063](https://github.com/ChilliCream/hotchocolate/pull/1063)

### Fixed

- Fixed issue with the request parser when requests are issued from relay-modern-http-transport. [#1024](https://github.com/ChilliCream/hotchocolate/pull/1024)
- Fixed Utf8GraphQLRequestParser handling of Apollo AQP signature query.
- Fixed Apollo Active Query Persistence Flow [#1049](https://github.com/ChilliCream/hotchocolate/pull/1049)
- Fixed scoped service handling. [#1066](https://github.com/ChilliCream/hotchocolate/pull/1066)
- Fixed Duplicate service registration. [#1066](https://github.com/ChilliCream/hotchocolate/pull/1066)

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
