
# Controlling nullability

## Configure Nullability

The GraphQL type system distinguishes between nullable and non-nullable types. This helps the consumer of the API by providing guarantees when a field value can be trusted to never be null or when an input is not allowed to be null. The ability to rely on such type information simplifies the code of the null since we do not have to write a ton of null checks for things that will never be null.

1. Open the project file of your GraphQL server project `GraphQL.csproj` and add the following property:

   ```xml
   <Nullable>enable</Nullable>
   ```

   You project file now look like the following:

   ```xml
   <Project Sdk="Microsoft.NET.Sdk.Web">

     <PropertyGroup>
       <TargetFramework>net5.0</TargetFramework>
       <RootNamespace>ConferencePlanner.GraphQL</RootNamespace>
       <Nullable>enable</Nullable>
     </PropertyGroup>

     <ItemGroup>
       <PackageReference Include="HotChocolate.AspNetCore" Version="11.0.0" />
       <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="5.0.0" />
       <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="5.0.0">
         <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
         <PrivateAssets>all</PrivateAssets>
       </PackageReference>
     </ItemGroup>

   </Project>
   ```

1. Build your project.
   1. `dotnet build`

   > The compiler will now output a lot of warnings about properties that are now not nullable that are likely to be null. In GraphQL types are by default nullable whereas in C# types are per default not nullable.

1. The compiler is complaining that the `ApplicationDBContext` property `Speakers` might be null when the type is created. The Entity Framework is setting this field dynamically so the compiler can not see that this field will actually be set. So, in order to fix this lets tell the compiler not to worry about it by assigning `default!` to it:

   ```csharp
   public DbSet<Speaker> Speakers { get; set; } = default!;
   ```

1. Next update the speaker model by marking all the reference types as nullable.

   > The schema still will infer nullability correct since the schema understands the data annotations.

    ```csharp
    using System.ComponentModel.DataAnnotations;

    namespace ConferencePlanner.GraphQL.Data
    {
        public class Speaker
        {
            public int Id { get; set; }

            [Required]
            [StringLength(200)]
            public string? Name { get; set; }

            [StringLength(4000)]
            public string? Bio { get; set; }

            [StringLength(1000)]
            public virtual string? WebSite { get; set; }
        }
    }
    ```

1. Now update the input type by marking nullable fields.

    ```csharp
    namespace ConferencePlanner.GraphQL
    {
        public record AddSpeakerInput(
            string Name,
            string? Bio,
            string? WebSite);
    }
    ```

   > The payload type can stay for now as it is.

1. Start your server again and verify the nullability changes in your schema explorer.

   1. `dotnet run --project GraphQL`

   ![Query speaker names](images/39-bcp-verify-nullability.png)

## Summary

In this session, we have further discovered the GraphQL type system, by understanding how nullability works in GraphQL and how Hot Chocolate infers nullability from .NET types.

[**<< Session #1 - Building a basic GraphQL server API**](1-creating-a-graphql-server-project.md) | [**Session #3 - Understanding GraphQL query execution and DataLoader >>**](3-understanding-dataLoader.md) 