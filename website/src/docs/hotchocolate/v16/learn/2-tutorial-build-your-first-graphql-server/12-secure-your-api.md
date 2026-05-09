---
title: "Secure your API"
description: "Add authentication and field-level authorization to the tutorial server, protect one report field with a policy, and verify public and private GraphQL results."
---

In the previous chapter, you connected your client to the GraphQL server. At this point, your server supports queries, filtering, paging, mutations, subscriptions, and client calls. Now, it is time to introduce a security boundary.

In this chapter, you will:
- Keep catalog data public
- Add local JWT authentication for development
- Register Hot Chocolate authorization
- Protect a single report field with an ASP.NET Core policy
- Test the difference between unauthenticated and authenticated requests

By the end, you will have:
- Decided which parts of the API remain public
- Created a local development JWT using `dotnet user-jwts`
- Enabled JWT bearer authentication in ASP.NET Core
- Enabled Hot Chocolate field-level authorization
- Protected a GraphQL field with a named policy
- Seen how a single query can return both public data and authorization errors

For more details after this chapter, see [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication), [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization), [Public API guidance](/docs/hotchocolate/v16/guides/public-api), and [Private API guidance](/docs/hotchocolate/v16/guides/private-api).

# Decide what is public and private

Before adding `[Authorize]` attributes, consider what each field should reveal. In this tutorial, use the following boundaries:

| Area | Access | Reason |
| --- | --- | --- |
| `books` | Public | Anyone can browse the library catalog. |
| `authors` (via books) | Public | Author data is part of the public catalog. |
| `addBook` and subscriptions | Unchanged | These were added earlier. This chapter focuses on securing a read-side field. |
| `totalBookCount` | Private | This is a report field, visible only to authenticated users. |

This setup demonstrates field-level security in GraphQL. A single query can request both public and private fields. Hot Chocolate checks authorization at the field level, so a client can receive public data and an authorization error for a protected field in the same response.

You will use a policy-based approach. The resolver does not contain permission logic. Instead, the field references an ASP.NET Core policy named `CanViewLibraryReports`. Policies centralize authorization rules and make it easier to evolve requirements, such as adding roles or custom checks.

# Add authentication

Authentication identifies the caller. Authorization determines what that caller can access.

First, add the JWT bearer authentication package. In your project folder (where the `.csproj` file is located), run:

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.0
```

You should see:

```text
PackageReference for package 'Microsoft.AspNetCore.Authentication.JwtBearer' added
```

The tutorial project targets `net10.0` by default. If you use `net9.0` or `net8.0`, match the package version to your target framework.

Next, create a local development token:

```bash
dotnet user-jwts create --name tutorial-user
```

The output will include a token value:

```text
New JWT saved with ID ...
Name: tutorial-user
Token: eyJ...
```

Copy the value after `Token:`. You will use it as the `Authorization: Bearer <token>` header when testing the protected field.

`dotnet user-jwts` is for local development. In production, use your organization's identity provider or authorization server, and configure JWT bearer authentication for that issuer.

Open `Program.cs` and add:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
```

Register JWT bearer authentication after your database registration and before the GraphQL builder:

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
```

The `.AddJwtBearer()` call reads local development settings created by `dotnet user-jwts` through ASP.NET Core configuration.

# Enable Hot Chocolate authorization

Authentication validates the token. Authorization enforces access rules. You need to register two things:

1. ASP.NET Core authorization services, where you define the policy
2. Hot Chocolate authorization, so GraphQL fields can use `[Authorize]`

Add the Hot Chocolate authorization package:

```bash
dotnet add package HotChocolate.AspNetCore.Authorization
```

You should see:

```text
PackageReference for package 'HotChocolate.AspNetCore.Authorization' added
```

Add the policy to `Program.cs`:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanViewLibraryReports", policy =>
        policy.RequireAuthenticatedUser());
});
```

This policy requires an authenticated user. The policy name is important, as the GraphQL field will reference it.

Now, add Hot Chocolate authorization to the GraphQL builder before `.AddTypes()`:

```csharp
builder
    .AddGraphQL()
    .AddFiltering()
    .AddMutationConventions(applyToAllMutations: true)
    .AddInMemorySubscriptions()
    .AddAuthorization()
    .AddTypes();
```

At this point, your `Program.cs` should look like this:

```csharp
using LibraryServer.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LibraryDbContext>(
    options => options.UseSqlite("Data Source=library.db"));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanViewLibraryReports", policy =>
        policy.RequireAuthenticatedUser());
});

builder
    .AddGraphQL()
    .AddFiltering()
    .AddMutationConventions(applyToAllMutations: true)
    .AddInMemorySubscriptions()
    .AddAuthorization()
    .AddTypes();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();
app.MapGraphQL();
app.RunWithGraphQLCommands(args);
```

Middleware order is important. `UseAuthentication()` must come before `UseAuthorization()`, and both must run before `MapGraphQL()`.

Build the project:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

# Protect a single field

Create a new file at `Types/LibraryReports.cs`:

```csharp
using HotChocolate.Authorization;
using LibraryServer.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryServer.Types;

[QueryType]
public static partial class LibraryReports
{
    [Authorize(Policy = "CanViewLibraryReports")]
    [GraphQLDescription("Returns a private count of books in the library catalog.")]
    public static async Task<int?> GetTotalBookCountAsync(
        LibraryDbContext db,
        CancellationToken cancellationToken)
        => await db.Books.CountAsync(cancellationToken);
}
```

Use `HotChocolate.Authorization.AuthorizeAttribute` for GraphQL resolvers. Do not use the ASP.NET Core MVC authorization attribute here.

This file follows the source generator pattern used throughout the tutorial:
- `[QueryType]` adds fields to the root `Query` type
- `partial` allows the source generator to add registration code
- `GetTotalBookCountAsync` becomes the `totalBookCount` GraphQL field
- `[Authorize(Policy = "CanViewLibraryReports")]` attaches the policy to this field
- `LibraryDbContext` is injected and does not appear as a GraphQL argument

If your server is running, restart it:

```bash
dotnet run
```

You should see:

```text
Now listening on: http://localhost:...
```

Open Nitro at your local GraphQL endpoint, for example:

```text
http://localhost:5095/graphql
```

Refresh Nitro's schema. The root `Query` type should now include:

```graphql
type Query {
  totalBookCount: Int
}
```

You will also see the existing `books` field and others from earlier chapters.

# Test access without authentication

Run this query without an `Authorization` header:

```graphql
query ReadCatalogAndReports {
  books(first: 2) {
    nodes {
      id
      title
      author {
        name
      }
    }
  }
  totalBookCount
}
```

You should receive a response like this:

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "path": [
        "totalBookCount"
      ],
      "extensions": {
        "code": "AUTH_NOT_AUTHENTICATED"
      }
    }
  ],
  "data": {
    "books": {
      "nodes": [
        {
          "id": 1,
          "title": "The Left Hand of Darkness",
          "author": {
            "name": "Ursula K. Le Guin"
          }
        },
        {
          "id": 2,
          "title": "A Wizard of Earthsea",
          "author": {
            "name": "Ursula K. Le Guin"
          }
        }
      ]
    },
    "totalBookCount": null
  }
}
```

Your book data may differ if you changed earlier chapters. The important points are:
- `books` returns public data
- `totalBookCount` is `null`
- The error code is `AUTH_NOT_AUTHENTICATED`

This response is field-aware. Hot Chocolate does not reject the entire operation. It returns public data and reports an error only for the protected field.

# Test access with authentication

Now, use the token you created with `dotnet user-jwts`.

In Nitro, open the request headers panel and add:

```json
{
  "Authorization": "Bearer <token>"
}
```

Replace `<token>` with your JWT value. Run the same query:

```graphql
query ReadCatalogAndReports {
  books(first: 2) {
    nodes {
      id
      title
      author {
        name
      }
    }
  }
  totalBookCount
}
```

You should see a response like this:

```json
{
  "data": {
    "books": {
      "nodes": [
        {
          "id": 1,
          "title": "The Left Hand of Darkness",
          "author": {
            "name": "Ursula K. Le Guin"
          }
        },
        {
          "id": 2,
          "title": "A Wizard of Earthsea",
          "author": {
            "name": "Ursula K. Le Guin"
          }
        }
      ]
    },
    "totalBookCount": 5
  }
}
```

If you have not changed the seeded catalog, `totalBookCount` will be 5. If you added books in previous chapters, your value may be higher. The key points are:
- No authorization error is present
- `books` still returns public data
- `totalBookCount` returns an integer

You now have policy-based security for your GraphQL API. The resolver requests the `CanViewLibraryReports` policy, and ASP.NET Core determines if the user meets the requirement.

Authentication also gives you access to the current `ClaimsPrincipal` in resolvers. This is useful for fields that return data for the signed-in user. For example, a future `me` field could accept `ClaimsPrincipal claimsPrincipal` and extract a user identifier from the token:

```csharp
using System.Security.Claims;
using HotChocolate.Authorization;

namespace LibraryServer.Types;

[QueryType]
public static partial class ViewerQueries
{
    [Authorize]
    public static string? GetCurrentUserId(ClaimsPrincipal claimsPrincipal)
        => claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? claimsPrincipal.FindFirstValue("sub");
}
```

Keep shared or business-specific permission logic in policies. Use `ClaimsPrincipal` in resolvers for user-specific data after authorization has succeeded.

# Checkpoint

At this stage, your project should have:
- `Microsoft.AspNetCore.Authentication.JwtBearer` installed
- `HotChocolate.AspNetCore.Authorization` installed
- JWT bearer authentication registered in `Program.cs`
- An ASP.NET Core policy named `CanViewLibraryReports`
- Hot Chocolate authorization registered with `.AddAuthorization()` before `.AddTypes()`
- Authentication and authorization middleware before `MapGraphQL()`
- A protected `totalBookCount` field in `Types/LibraryReports.cs`
- A clear difference between unauthenticated and authenticated GraphQL responses

Run a final build to confirm:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

# Troubleshooting

**`totalBookCount` does not appear in the schema**

Check that `Types/LibraryReports.cs` is in your project, uses `namespace LibraryServer.Types;`, and declares `[QueryType] public static partial class LibraryReports`. Rebuild and refresh Nitro's schema.

**The build cannot find `AuthorizeAttribute`**

Make sure the project references `HotChocolate.AspNetCore.Authorization` and that `Types/LibraryReports.cs` includes:

```csharp
using HotChocolate.Authorization;
```

**The protected field returns data without a token**

Verify that the resolver uses `[Authorize(Policy = "CanViewLibraryReports")]`, the GraphQL builder calls `.AddAuthorization()` before `.AddTypes()`, and the endpoint pipeline includes `app.UseAuthentication();` followed by `app.UseAuthorization();` before `app.MapGraphQL();`.

**Public fields return authorization errors**

Check if the endpoint was mapped with endpoint-level authorization:

```csharp
app.MapGraphQL().RequireAuthorization();
```

This protects the entire GraphQL endpoint, so Hot Chocolate cannot resolve public fields. For this tutorial, use field-level authorization:

```csharp
app.MapGraphQL();
```

Keep `[Authorize(Policy = "CanViewLibraryReports")]` on `totalBookCount`.

**The authenticated request still returns `AUTH_NOT_AUTHENTICATED`**

Create a new token with:

```bash
dotnet user-jwts create --name tutorial-user
```

Then use the copied value as:

```json
{
  "Authorization": "Bearer <token>"
}
```

Also check that `Program.cs` uses `JwtBearerDefaults.AuthenticationScheme` and calls `.AddJwtBearer();`.

**The token is authenticated but the policy still fails**

If you later update `CanViewLibraryReports` to require a role, scope, or other claim, make sure the JWT contains the expected claim. You can create a local token with a claim:

```bash
dotnet user-jwts create --name tutorial-user --claim scope=library.reports
```

Update the policy and token together. A policy that requires a missing claim will fail, even if the token is valid.

**The token works in one project but not another**

Run `dotnet user-jwts create --name tutorial-user` from the folder containing your tutorial `.csproj` file. Development JWT settings are project-specific.

**Package version conflicts**

Keep Hot Chocolate package versions aligned with your project. Match `Microsoft.AspNetCore.Authentication.JwtBearer` to your target framework's major version.

# Next steps

You have now secured a field without making the entire API private. This is the main pattern for GraphQL security: protect only the fields that require it, keep public fields open, and use policies for permission rules that may grow.

Continue to [Prepare for production](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/13-prepare-for-production/) to add cost controls, introspection settings, transport configuration, and deployment defaults.

For further reference:
- [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication): identity provider setup and `ClaimsPrincipal` access
- [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization): roles, policies, repeatable directives, and type-level rules
- [Public API guidance](/docs/hotchocolate/v16/guides/public-api): for APIs accessed by external clients
- [Private API guidance](/docs/hotchocolate/v16/guides/private-api): for APIs where you control the clients and can use trusted documents
