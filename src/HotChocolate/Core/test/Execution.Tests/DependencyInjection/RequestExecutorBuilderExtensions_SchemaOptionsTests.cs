using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.DependencyInjection;

public class RequestExecutorBuilderExtensionsSchemaOptionsTests
{
    [Fact]
    public async Task ModifyOptions_ValidatePipelineOrder_False()
    {
        var interceptor = new OptionsInterceptor();

        await new ServiceCollection()
            .AddGraphQLServer()
            .AddType<Query>()
            .ModifyOptions(o => o.ValidatePipelineOrder = false)
            .TryAddTypeInterceptor(interceptor)
            .BuildRequestExecutorAsync();

        Assert.False(interceptor.Options.ValidatePipelineOrder);
    }

    private sealed class OptionsInterceptor : TypeInterceptor
    {
        public IReadOnlySchemaOptions Options { get; private set; } = default!;

        internal override void OnBeforeCreateSchemaInternal(
            IDescriptorContext context,
            ISchemaBuilder schemaBuilder)
        {
            Options = context.Options;
        }
    }

    public class Query
    {
        public string Abc() => "abc";
    }
}
