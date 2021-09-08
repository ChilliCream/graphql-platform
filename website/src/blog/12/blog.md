Performance

Query Plan

- Resolver Types
- Control behavior or plan serial/parallel

Entity Framework

Resolver Compiler

Dynamic Schemas

-> Type Modules -> Added support for type modules.
-> UnsafeCreate
-> TypeInterceptors

DataLoader
-> caching
-> update dl cache
-> Moved DataLoader code out of `HotChocolate.Types` into `GreenDonut`. (#4015)

Cursor Paging
-> Introduced option to require paging boundaries #4074
-> Add more capabilities to control how the connection name is created #4081

Validation

Middleware

- Order Validation

Relay

- nodes field
- Split the` EnableRelaySupport` configuration method into two separate APIs that allow to opt-into specific relay schema features. (#3972)

AggregateError
Enhanced error handling for variables to better pinpoint the actual error

ASP.NET Core improvements

Schema-First Support

Banana Cake Pop
