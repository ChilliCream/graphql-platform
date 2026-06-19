using BeforeGraphQLDotNet.Auth;
using BeforeGraphQLDotNet.Schema;
using BeforeGraphQLDotNet.Services;
using GraphQL;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5101");

// Singleton domain services.
builder.Services.AddSingleton<BookDataStore>();
builder.Services.AddSingleton<IBookEventService, BookEventService>();

// GraphQL schema graph types resolved from DI.
builder.Services.AddSingleton<Query>();
builder.Services.AddSingleton<Mutation>();
builder.Services.AddSingleton<Subscription>();
builder.Services.AddSingleton<AppSchema>();

builder.Services.AddGraphQL(b => b
    .AddSchema<AppSchema>()
    .AddSystemTextJson()
    .AddDataLoader()
    .AddGraphTypes()
    .AddAuthorizationRule());

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

var app = builder.Build();

app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();

app.UseGraphQL("/graphql");
app.UseGraphQLGraphiQL("/ui/graphiql");

app.Run();
