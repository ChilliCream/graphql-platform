# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Merged _ASP.NET core_ and _classic_ codebases [#349](https://github.com/ChilliCream/hotchocolate/issues/349).

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

[unreleased]: https://github.com/ChilliCream/hotchocolate/compare/0.6.10...HEAD
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
