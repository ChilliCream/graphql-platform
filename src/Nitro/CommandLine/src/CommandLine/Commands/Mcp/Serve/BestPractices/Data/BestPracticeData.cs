using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static partial class BestPracticeData
{
    public static IReadOnlyList<BestPracticeDocument> GetAll()
    {
        var docs = new List<BestPracticeDocument>(43);

        AddConfigurationDocuments(docs);
        AddDataLoaderDocuments(docs);
        AddDefiningTypesDocuments(docs);
        AddErrorHandlingDocuments(docs);
        AddFilteringSortingDocuments(docs);
        AddMiddlewareDocuments(docs);
        AddPaginationDocuments(docs);
        AddResolversDocuments(docs);
        AddSchemaDesignDocuments(docs);
        AddSecurityDocuments(docs);
        AddSubscriptionsDocuments(docs);
        AddTestingDocuments(docs);

        return docs;
    }
}
