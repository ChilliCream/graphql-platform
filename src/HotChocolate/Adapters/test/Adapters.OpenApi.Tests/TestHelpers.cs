using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

internal static class TestHelpers
{
    public static IRequestExecutorBuilder AddBasicServer(this IRequestExecutorBuilder builder)
    {
        return builder.AddAuthorization()
            .ModifyOptions(o => o.SortFieldsByName = true)
            .AddQueryType<TestSchema.Query>()
            .AddMutationType<TestSchema.Mutation>()
            .AddInterfaceType<TestSchema.IPet>()
            .AddUnionType<TestSchema.IPet>()
            .AddObjectType<TestSchema.Cat>()
            .AddObjectType<TestSchema.Dog>();
    }
}
