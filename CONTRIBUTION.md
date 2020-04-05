# Contribution

## Code Style

### Rules

#### Extension Methods

- Extension files should always begin with the type name and end with `Extensions`.
  Examples:
  - `StringExtensions.cs`
  - `ServiceCollectionExtensions.cs`
- Write for every type a separate extension file.
- When writing extension methods that register services (e.g. AddGraphQL), extend from the `Microsoft.Extensions.DependencyInjection` namespace as per [Microsoft recommendations](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection#overview-of-dependency-injection).
  Example:

  ```csharp
  using HotChocolate;

  namespace Microsoft.Extensions.DependencyInjection
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

- Otherwise, use the origin namespace of the type; use `HotChocolate.AspNetCore` when extending a ASP .Net core specific type like `Microsoft.AspNetCore.Builder.IApplicationBuilder`.
  Example:

  ```csharp
  using Microsoft.AspNetCore.Builder;

  namespace HotChocolate.AspNetCore
  {
      public static class ApplicationBuilderExtensions
      {
          public static IApplicationBuilder UseGraphQL(
              this IApplicationBuilder applicationBuilder)
          {
              // omitted for brevity
          }
      }
  }
  ```
