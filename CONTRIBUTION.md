# Contribution

Hot Chocolate is a mono-repo, this means that we have all components co-located in one repository. This makes it easy to debug and publish the repository but it also might make it harder to get into it since we have so many code files in one repository.

## Repository Structure

The repository is divided into four main parts ....

### Core

The core projects represent the GraphQL core engine and its associated components. Everything here is transport agnostic. The core consists of roughly six major parts:

- Abstractions
  The abstractions provide interfaces, base classes and simple types that are used throughout the API.

- Language
  The language apis represent the parser, lexer, syntax tree and related APIs.

- Validation
  The validation API are valudators that are executed on the syntax tree and represent the validation rules described in the GraphQL specification.

- Types
  The types API represents the type system with out the execution. So, this APIs will include resolvers, resolver compilers, types, the inrospection and the schema.

- Execution
  The execution engine contains the executors and handles things like argument coercion, variable coercion and everything related with the execution of queries.

- Subscriptions
  The subscription APIs contain abstraction for the backend subscription system and the varous pub/sub-system provider.

There are more APIs that are related to the core implementation like persisted query storage providers, Filter APIs and everything that is related to the core.

## Server

The server handles everything that is related with the transport. We currently support three server implementations.

- AspNetClassic
  The AspNetClassic API represents a OWIN based ASP .Net Framework implementation.

- AspNetCore
  The AspNetCore API represents a server implementation ontop of ASP .Net Core. This is our lead platform. We have generalized the code in a way that it also compiles to the ASP .Net Framework. So, the AspNetClassic projects reference a lot of the code files form the corresponding AspNetCore folders.

- Azure Functions





## Code Style

### Rules

#### Extension Methods

- Extension files should always begin with the type name and end with `Extensions`.
  Examples:
  - `StringExtensions.cs`
  - `ServiceCollectionExtensions.cs`
- Write for every type a separate extension file.
- Use the origin namespace of the type; use `HotChocolate` when extending an external type like `Microsoft.Ectensions.DependencyInjection.IServiceCollection` or `HotChocolate.AspNetCore` when extending a ASP .Net core specific type like `Microsoft.AspNetCore.Builder.IApplicationBuilder`.
  Example:
  ```csharp
  using Microsoft.Extensions.DependencyInjection;

  namespace HotChocolate
  {
      public static class ServiceCollectionExtensions
      {
          public static IServiceCollection AddGraphQL(
              this IServiceCollection serviceCollection,
              ISchema schema)
          {
              // omitted for brevity
          }
      }
  }
  ```
