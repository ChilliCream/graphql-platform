using AfterHotChocolate.Auth;
using AfterHotChocolate.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5102");

// Singleton domain service.
builder.Services.AddSingleton<BookDataStore>();

// Header-based test authentication: the default scheme reads X-Authenticated.
builder.Services
    .AddAuthentication(TestAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
        TestAuthenticationHandler.SchemeName,
        _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
});

builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddAfterHotChocolateTypes()
    .AddMutationConventions()
    .AddInMemorySubscriptions();

var app = builder.Build();

app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL("/graphql");

app.Run();
