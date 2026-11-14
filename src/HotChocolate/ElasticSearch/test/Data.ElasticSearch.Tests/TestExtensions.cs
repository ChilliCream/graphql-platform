using Elastic.Esql;
using Elastic.Esql.Extensions;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.ElasticSearch;

public static class TestExtensions
{
    public static void MatchQuerySnapshot(this IExecutionResult field)
        => field.ToJson().MatchSnapshot();

    public static IObjectFieldDescriptor ResolveTestData<T>(
        this IObjectFieldDescriptor field,
        TestClient client)
        where T : class
        => field.Resolve(_ => client.Esql.CreateQuery<T>().From(client.Index, MetadataField.Source).AsElasticSearchExecutable());

    public static ValueTask<IRequestExecutor> BuildTestExecutorAsync(this IRequestExecutorBuilder builder)
        => builder.ModifyRequestOptions(o => o.IncludeExceptionDetails = true).BuildRequestExecutorAsync();

    public static IObjectFieldDescriptor UseTestReport(this IObjectFieldDescriptor descriptor) => descriptor;
}
