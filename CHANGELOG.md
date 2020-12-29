# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [11.0.7]

## [11.0.6]

## [11.0.5]

## [11.0.4]

## [11.0.3]

## [11.0.2]

## [11.0.1]

## [11.0.0]

### Added

- Added FirstOrDefault Middleware & improved EF docs (#2635)
- Added support for spatial types and spatial filters (#2541, #2566, #2567, #2637, #2638, #2639)
- Added new data integration API (#2633, #2631, #2629, #2618, #2619)
- Introduced new conventions API ()
- Added Banana Cake Pop middleware which replaces the Voyager, Playground and GraphiQL middleware.
- Added better error when root value cannot be created. (#2598)
- Added schema error interceptor (#2581)
- Added new Dataloader middleware (#2548)
- Add support for IExecutable to Data (#2527)


- SPEC: Custom Scalar Specification URLs  (#2614)

### Changed

- Refined converter API (#2593)
- Changed schema endpoint route from `http://localhost/graphql/schema` to `http://localhost/graphql?sdl`
- Changed ASP.NET Core integration by using the ASP.NET Core routing API.
- Refined .NET Templates (#2570)
- SPEC: Rewrote GraphQL middleware to align with current GraphQL over HTTP spec draft.
- SPEC: Re-implemented request validation to align better GraphQL spec and graphql-js.

### Removed

- Removed Voyager middleware.
- Removed Playground middleware.
- Removed GraphiQL middleware.

### Fixed

- Allow to annotate enums and enum values with GraphQLNameAttribute (#2315)
- Fixed xml documentation inheritance issue. (#2645)
- The type trimming now correctly handles executable directives (#2605)
- Fixed issue when using subscriptions with variables. (#2596)
- Fixed name convention interference with the introspection types. (#2588)

* 5a1d67ddbaa1b9975064ca4f82d0c86994d00406 Added support to use a page type directly (#2540)
* 60c8fb9b2a57f1b5714b2adbb7b0817caa43827b Fix duplicated filter/sorting type names (#2530)
* e65e07a87424714d673176557bbd62b601b2cdea Bump gatsby-plugin-manifest from 2.5.1 to 2.5.2 in /website (#2537)
* 1272189c97cadce932da17bc0ca72fb04af9fe3b Bump gatsby from 2.25.1 to 2.25.2 in /website (#2538)
* f93f97c8cf21cc5042661efe56b907de320e9c26 Bump gatsby-remark-autolink-headers from 2.4.0 to 2.4.1 in /website (#2539)
* b28feeb3f98abf6549fe43199ba393232b9b3540 Fixed a few website issues and upgraded packages (#2535)
* 115f47c887634737e0b8f36ccd821341a05898ce Reworked Stitching Variable Handling (#2533)
* 1de7cf865771789ca824814c5023806addca605c Add conventions to executor builder (#2532)
* 300d01bf7f146c2d7444af381fa74b1a0b724eb4 Reworked Stitching Error Handling (#2529)
* 601190bce1ad69b7a3fd63d43b15cb36bdded17c Schema Stitching Refinements (#2526)
* 5a351058f27799de7b5867dcd73fc67bcca077f9 Fixed field resolver respect inheritance (#2515)
* 4d87c535f9c7d6e0f3a1ece4375a3c449afc39f2 Fixes stitching issues when fetching from downstream services. (#2511)
* f70c344d62a028fd108300667c9912c2654d6ca6 Fixed async node resolvers (#2493)
* f491d2ff769695ad69ead1abac73e329ce7fdd91 Fixed dictionary middleware. (#2492)
* ab0edc3fd5ecf2a9c3823597aed7c49d06295f99 Fixed Stitching Issues (#2491)
* 8cfda3807fb8a7d10659fd45a12d0a7f343fea74 Fixed nullref issue with the result map (#2489)
* 91dab88a907f72fa83ca738f88e0723f7678e328 Fixed issue that directives were dropped when using the schema printer. (#2486)
* a1b249970149193911a5de4362aa60df48facab7 BCP config, error details and middleware improvements (#2483)
* 4ac43f5ee5499f6a984a55ab8a18d2b8873a7a8a Fixed: Projection when using offset paging (#2476)
* 0de8364d2b4996d61e0fd2248d45ed089b95c17e Add more tests for federated stitching. (#2474)
* 32bfa7fa2878244fc6c2fb1e9ef3bb88d8c02682 Remove legacy selections (#2478)
* ec060126bbb7950d3502850b732b2dc5f9b2bc79 Prefetch TotalCount  (#2480)
* 650c80b9342686a968f9e9aad839f8b8e09cab22 Fix value coercion of enum values (#2477)
* 80572670005e06ed476177265d6eba298c91f66c Refactored IExecutable (#2479)
* 3d6a617d3fb87f601f13f74402a8db057010515d Added support for federated stitching. (#2471)
* 27c0fac210da27989bf1e8b49e43d901b4c43b89 Properly infer the enum values from GraphQL SDL
* cb939227a22cefddbd6ec88357b18c77ba84c461 Updated BCP (#2467)
* 93e9ff6cc502f760802155188b9b4036fb3d3baa Fixed schema stitching issues with enum naming
* 0dee68ff53dae5730e665e3253b6665d355545c8 Updated bcp resources (#2464)
* 5f7d3b6a348f54fb844ff910fbe4999ee91e57ab Fixed projection convention issues
* 014784c2d517c24b4149c7170e073cf74012fe3f Integrated projections into new configuration API
* baef95ecdb56acc76d5f84ce4b12f84667552557 Compile nullable attributes as internal
* f925bfe6721d65f97e6dfe27b5c80f3402c42986 Fixed Mutation Executor issue
* 68a5a1fc3db981a002ded1bf8d28de8bb5ba3fbb EntityFramework now references HotChocolate.Data
* 00b1234b88eb690ec315dfc9289293538f52a2ca Added attributes for filtering and sorting (#2453)
* b8b4b0bb040094ce83b5ef77b1f3b1b5919776cc Removes ReportError On ConventionContext (#2455)
* a9e91a9b1955875ca9f22b5d668a548dbf24665f Improved error messages for missing conventions (#2457)
* 5c419eb8d687d097484723b60a5cf9cfdd7b3324 Convention Extensions (#2451)
* 5360b701992b036480e2d1920a4b75628451167f Added projections support to version 11 (#2419)
* 762b09e23702b9e43562343c0f23818e584e6924 Added ziosk and e2m logo; set 1 year expiration for cookie consent
* 21f7642742b532bf818eb9370220bb24091191b9 Merge pull request #2412 from AradAral/patch-1
* c508b34c22d8d251977641afbaa310a716731260 Merge branch 'develop' into patch-1
* 911db2a81fe928be570bc35accf7e53c520e271b Add list input for sorting
* a4946fa6681dfa9d5ff92e1329108ec8b5b98db8 Merge branch 'develop' into pse/sorting-as-array
* 548b964f30576dbf8a71d8231aef7c6392d7070c Fix message on the attribute obsolete in NodeObjectTypeExtensions.
* 02d6789d5e692944fef809cee0cc743522395c2c Merge branch 'develop' into patch-1
* 6d355dc65c556c3c28e3e52f9a52ad9eed255e00 Merge branch 'develop' into vbornand-patch-1
* be06ce5c7914e73165166341b3cb80b739cdc38b Merge branch 'develop' into pse/sorting-as-array
* 8ceda1a3a753685b2fbeed9bcda833b55d30bad2 Adds IExecutable abstraction to core
* 8d32a4afa37d90a339f8459ccb047b14a4bbf3a2 Merge branch 'develop' into pse/query-abstraction
* 70a4741f6c9bfe044286c8ee3e2d6448277cdd4a fix tests
* 317531017d5c8703a6e6116f5b451b07e3671b69 Really make defintions read-only
* fc6cc326e7e4c9bb65557c919a3fb75a66fc9463 really make defintions readonly
* 250649878e5640586d3210771378ff6da6e95449 make definitions readonly (#2447)
* e0e5fda21d0d2dd3077a02e3a8c4613b20b681b4 Merge branch 'develop' into pse/readonly-data-definitions
* b13a2f44c177633b84e5ff0b844f8c435afd49a2 Added tests that show we can rewrite scalar types (#2444)
* dd2120a5b99ef070257e770b9df5772dec7062eb make definitions readonly
* b902c2a3a0fbfa77a5dadeb642f8086b87657e63 rename to executable
* 3ca2178e8c9cabad1e9f0680d42332c9d0b1620a rename to executable
* b44f9998ff25e1231198c35396f1b708eb9c9cc2 fix refactoring issue
* 0bd2c532ce47ca36f6e2008ef010a3b9276e5ede add query abstraction
* 66ced99a405a6261662ececa7b099d1383cea32f Fixed issue with non-null arguments that have defaults (#2441)
* ce3568d1deebf3129a3869b45f2b483bab727f83 fix format
* d9278777c5948e9ae2c76cf318afe7672181de71 feat: add list support to sorting
* e1560c78335d9d50b5e0d7de97f503c3afa40119 Update NodeObjectTypeExtensions.cs
* 27c68fa145c065433c4484b408134048eeeea5ee Fixed HotChocolate.Data.EntityFramework assembly name.
* 2a0605af9bbf24e0d74310ace5fc2f1623e28a86 Added entity framework helpers. (#2435)
* 6d795038eb2e0839b8583c5807a9717d81d8cc86 Fixed issue where empty selection sets lead to execution errors. (#2432)
* 6a78a9b2bdaf2ac97c6f693bc74f072edd905157 Added pure code-first Node resolver (#2434)
* 1b34d9a04968c651c000724ca0dcfc304c214477 Get default value from constructor when type is a record. (#2428)
* d7eba9d1ea435236d4819abf06030c8e0638fb57 Added Filter Binding Tests (#2431)
* 9ef8f5fb2ccbf06b88d401e0066d1abcf737d30e Fix: Added Enum to GraphQLDescriptionAttribute (#2430)
* 8299a944864a07bc0881ef06ce68818d801533d2 Allow custom remote schema fetchers (#2425)
* 3ddde4893ca443c7112fbe985bef4492778ad695 Allow for scalars to be renamed when stitching a schema. (#2424)
* f80ec34be1148c13ff29a24a6d5eaaeeb69b23f0 Allow for list aggregations when delegating. (#2418)
* 8673af5f5cd5833c9f07b75962c11f20cffc7050 Banana Cake Pop Middleware (#2417)
* b0e061e6a5729e2047f2c626adb817d8ed759b6c Migrates stitching to the new configuration API (#2403)
* 9816d61a18432f68e4c398193d411968056b767c Changing type="solution" to type="project" in template.json (#2413)
* 1f7ff19a08e3b4c27fb95c4f93216b41ad688a13 Correcting the link to the execution options
* 59da40c9bea05b975e886353202aa49d8fce85a8 Added carmmunity logo (#2410)
* 289f0dcf54822ccf4d5b68a2a4862e60ada39ebd Changed Package Project URL
* 249770920cafc6a48e34208252b822549f759687 Migrated spatial types to new type system core. (#2304)
* ed032ed397f9c168d7a26b461a43a5189c0ffcae Updated Benchmark Results
* afc0237b1ee0a1812db2bccb68aaf669e0cfc5e5 Added SwissLife logo (#2402)
* 6149d2862d79f86b7ad5af2e7937f3a810ba7b8f issue lock
* db24b425f7da3dd2d04d64c78fae40847bb9855e Adds Apollo Federation Types (#2361)
* c5f26e856feb59dfb8c3aef82a5c8f76286571db Reversed accidental move of the type tests
* f7d6af69271bcbb3d0dfdad2614c44051bd054e8 Added integration tests for sorting (#2313)
* 5b478a9c6e092291b180db3927c9251a96f76556 Added tests for filtering in combination with paging. (#2317)
* a1277798df08ecacb42cf15fbfaa0ec342c0ff94 Fixed issue where the result helper was not correctly cleared after usage. (#2380)
* 2419fa0bf73797e820a7775724f7ae4e0ce639ce Fixed end boundare for incremental results
* 689c52bf251970f14860b0f9db68798a52daeef2 Adds @defer transport layer. (#2377)
* 91d6801e2bd8e2f2a45339f20d96c92587607539 Added @defer to the GraphQL core. (#2359)
* 0b42aeff89f9d7e34a988b2805df316393a19109 Added Configuration API for the Global ID serializer. (#2370)
* 82a07bd9d25e78bb1b8cc0be8f5cc170d0e223a2 Added pushpay logo (#2368)
* 929ae9859f00ec1e1236aa8bbe9d432a0f9c9666 Refined Selection Optimizer (#2354)
* 03088d76bc393211e7942e9e272046d339cddd3b Fixed link to subscription docs (#2366)
* 1b4eb36d2d428baf104f8ae720b562286f3b1c32 Fixed issue where we normalized schema references incorrectly (#2363)
* b04ea1fac71409537860ab7717c3c64628dcf2ec Updated Website Menu
* 6ee13366a05ba11ed9ed47874e05e5caead47c36 Update index.md
* e601cbd8d431e055b87a56f2ddea766320f4d8ea Track the types that are dependant on unresolvable references (#2360)
* ea2b940fe01c77cf097cc8623fa81956209d40ca Update README.md (#2357)
* c81548b4e6c52c6f74641b7ab7ad41e0455dd640 Deleted Filtering.md (#2352)
* 34bbe343a21204ec01f5356a320b752a431cbd7c Added Hot Chocolate Docs Introduction
* 4f6638b295ed2204e6a5978ec574e743e108cb42 Fixed two links (#2356)
* 5b85fd2e654b2f5f03caa036e295fe0ed3c1fba5 New Platform Readme (#2355)
* 5c10aedccb10b34dfca8806245bc3a85cf0b455a Added test snapshot
* 4737777f0150254b8e5165e3ee9709654b47fad8 [Security] Bump dot-prop from 4.2.0 to 4.2.1 in /website (#2256)
* d5a4f0067d54637709e1ce1133df1c773da38312 Updated startpage (#2353)
* bdabf0b919fd50bfff3713ff825ee01720928a4d Reworked Build and Merged Changed
* a254da9cdf57c960580df41c8dc04bd85a74d176 Build Settings
* b5950660092a07b931d58a1cb154726cfc02d324 Fixed broken link.
* 6f64af27cf2991ede17fbe348ba3bdce0beefb21 Added test that proofs that elements can be variables (#2349)
* be7c71748e3706e999797915be70f53c61920881 Added test that ensures that objects can be variables (#2350)
* 5be38a78318533430ad6a1d2b83abc9ca8265501 Fix typos (#2346)
* b00cc42ecbffa5d110b4742e2c1841317fee3603 Introduced new IPageType interface (#2348)
* a12d0ec93706895fc6120109a6b9bbbf5afda237 /docs redirect to HC V10 (#2347)
* 4527b3e766e0b0ffaf96f1d65da1018b06c90c2c CNAME (#2345)
* ead7fab4250b5b6e90c1383a8aaa10e59642ec0e Fixed Build Scripts
* 1b9023501f0783890c00d9313f5f33d3c26e8719 test: add object tests for sorting (#2312)
* 04f2b9b3b0a30a03995893f657a6b11b8ce674cb Optional parameter for useFiltering (#2341)
* c8583702891b3e873f6ee22ac845440aec924387 Updated url (#2342)
* ec4e6e3bfc0915be0363bb341fa8fefd8f224a7b Added v10 docs (#2340)
* f327145ecb943f5528ac22f17cc66c12626c9e87 Adds extension for System.Type to filtering (#2316)
* 38241e53bc7e66f7b662927271a6aee9ac0631f7 Added some tests that ensure middlewares compile with ValueTask and Task (#2337)
* 8001dd8461168f1af3e15299b58930102e7f9393 Changed the build so that we again publish templates (#2336)
* f84ac7a178411390be127797bcd792dd4150eca7 Updated Templates (#2335)
* 838ce4906a3e26f86c159943d594805355a2abc8 Refined Persisted Query Support (#2334)
* 40d9772f7f03050fdc5cd204c206a1cdba15cc3f Refined support for records. (#2331)
* b6d7dd9afb7b8d2a19212604dfbe29b0cea7779b Fixed offset paging issue where the middleware through when the items in the list where less than the page size. (#2330)
* d0b72ed8c6b101c89d5c59b703266fa1172c11a5 moved attributes to types namespace (#2328)
* fe4e043f37a5a59709cb3c2d18891952bfec6bb9 Fixed issues with the variable deserialization. (#2326)
* 08ffd724ef7c50f224bca03affe4e19a34a2d0e2 Improved footer and added redirects (#2324)
* 29965cd8e2b35152b06ec346813e8ecacf8ea795 Update Tooling Packages (#2323)
* 6e562e85f10b6849b784ecf122b3114d9d37da30 Added offset (skip-take) paging. (#2096)
* 5375190af449490338c6aa53d561175dd08f899e Finalized start page (#2321)
* 4b1fa569c13ec0db0b7430b6788711e912ab9f36 Article Design & Marketing Pages (#2318)

* 129635560bf10c02e9eb704f9c73ba6c44aa2256 Optimized Database Integration Tests (#2314)
* 8307aedbb6677696c361ec8ac47a8bbe2e5bf657 Fixed Sorting Member Inference Issue (#2311)
* f2fc0d0aff3c778b28a12595185783ee51c1d1e1 Fixed type binding issues with filtering. (#2310)
* 76554a562013224bcd01e04bb2455725352e6219 Fixed target framework for the authorization package
* 535646b6dff44f1cd1ffa9d5df37033e79125a8a Fixed authorization target frameworks
* 98d29f9aa9838b95fe959175f39df502a0826448 Adds sorting to the data package (#2300)
* 538a94e29077a7be5dbd97a3ee5087ecd897412e Migrated Schema Middleware (#2307)
* 4776afc56385a3d2ac1b3a9fd919e6811d06c059 Website: Feeds and Design (#2308)
* a0690579e0264a267fc5c8e64a4666f30f8a9411 Migrated authorization directives back into the ASP.NET Core packages. (#2306)
* 93f21fa971fc47e2099c14b8c848425a30229128 Removes Stackable list (#2295)
* 8999d51d880bd0b8361acb3faa658914fc4d829e Migrate version 10.5 features to version 11. (#2293)
* c8b37e6079ffef2dd021ab7d397515289149ee93 Fix nullability issue on filter descriptor  (#2296)
* 7427498794f72645514fb361aaaed0373fbd60d6 Refined Global ID handling. (#2292)
* 4058f4c5a5658df28a048d91fd62a820a269fcda Spell character correctly (#2287)
* 6f687f4f200da90c399d39b167320b21d0cab3e5 Added SchemaFirst test with resolver (#2291)
* 794c727aa9b94272cd058110b67d4805b36c583e Added some helpers and legacy APIs back to the core (#2290)
* 3178999ff44d4e013f9ddd364db3a9ac83b445f9 Added GetRequestExecutorAsync convenience
* 8ab6ce878ede4939ad47795f571d2fa19ca3e154 Removed interface IDiagnosticObserver
* 59edbef71c8df8c5abbd9f3b1c51de7756a75326 Fixed executor eviction (#2286)
* d0c214edaa77549887eb55942907de2a4c8788b0 Added new HTTP GET middleware (#2282)
* d435e9861f55c7ff46a8906a3a40736208c0ef80 Introduced a input value formatter that can be bound to input fields and arguments. (#2277)
* 719ce7d681633ecad10a189389c6b96895e5260d Migrated Subscription Middleware (#2280)
* 5e070ffc69a104125369deec299bc51fa861eeb5 Removed unused variables from build
* 6d64f129729db3b5a08007e0c1d1ef2a40737083 Reconfigured Sonar Build Settings
* 66618263ef328f7adfd3b8a9d9fbedb029d57b09 Migrated batching to the new ASP.NET Core middleware. (#2274)
* 5c28395077e2734395c3ef4cc540ac90763ffbe7 Add version 10 filter API for backward compatibility. (#2255)
* 99b3eb6fb22d6d7c082ddeba1c65d8dda434c45d Update azure-pipelines.test-pr-hotchocolate.yml for Azure Pipelines
* 431d6e987658c0623677b347d29456809c3703bb Adds the filter APIs 2.0 (#2228)
* 39e1260851fe9a7fe70b1bbcfd68204b421474ba Fix Contribution link in readme.md (#2247)

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