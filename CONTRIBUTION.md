# Contribution

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
