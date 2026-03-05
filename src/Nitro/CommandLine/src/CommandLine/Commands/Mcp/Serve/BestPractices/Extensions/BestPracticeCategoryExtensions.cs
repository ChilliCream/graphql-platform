using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Extensions;

internal static class BestPracticeCategoryExtensions
{
    public static string ToDisplayString(this BestPracticeCategory category)
        => category switch
        {
            BestPracticeCategory.DataLoader => "dataloader",
            BestPracticeCategory.DefiningTypes => "defining_types",
            BestPracticeCategory.Resolvers => "resolvers",
            BestPracticeCategory.Middleware => "middlewares",
            BestPracticeCategory.ErrorHandling => "error_handling",
            BestPracticeCategory.Pagination => "pagination",
            BestPracticeCategory.Testing => "testing",
            BestPracticeCategory.SchemaDesign => "schema_design",
            BestPracticeCategory.Security => "security",
            BestPracticeCategory.FilteringSorting => "filtering_sorting",
            BestPracticeCategory.Subscriptions => "subscriptions",
            BestPracticeCategory.Configuration => "configuration",
            _ => category.ToString()
        };
}
