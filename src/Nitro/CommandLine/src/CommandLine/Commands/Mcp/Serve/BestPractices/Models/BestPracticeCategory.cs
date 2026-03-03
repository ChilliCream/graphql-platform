using System.ComponentModel;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

internal enum BestPracticeCategory
{
    [Description("DataLoader patterns for batching and caching")]
    DataLoader,

    [Description("Defining GraphQL object types, input types, interfaces")]
    DefiningTypes,

    [Description("Field resolver patterns and best practices")]
    Resolvers,

    [Description("Request pipeline middleware")]
    Middleware,

    [Description("Error handling and error filtering")]
    ErrorHandling,

    [Description("Cursor and offset pagination patterns")]
    Pagination,

    [Description("Testing with CookieCrumble and xUnit")]
    Testing,

    [Description("Schema naming, nullability, evolution")]
    SchemaDesign,

    [Description("Authorization, validation, rate limiting")]
    Security,

    [Description("HotChocolate Data filtering and sorting")]
    FilteringSorting,

    [Description("Real-time GraphQL subscriptions")]
    Subscriptions,

    [Description("Server setup, options, and configuration")]
    Configuration
}
