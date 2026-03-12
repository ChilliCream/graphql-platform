using System;
using HotChocolate.Execution;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class Issue8057ProbeTests
{
    [Fact]
    public async Task NodeResolver_And_Query_On_Same_Method_With_Strongly_Typed_Id_Does_Not_Throw()
    {
        var exception = await Record.ExceptionAsync(
            async () => await new ServiceCollection()
                .AddGraphQLServer(disableDefaultSecurity: true)
                .AddIntegrationTestTypes()
                .AddGlobalObjectIdentification()
                .BuildSchemaAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task NodeResolver_And_Query_On_Same_Method_With_Guid_Id_Does_Not_Throw_On_Execution()
    {
        var executor = await new ServiceCollection()
            .AddGraphQLServer(disableDefaultSecurity: true)
            .AddIntegrationTestTypes()
            .AddGlobalObjectIdentification()
            .BuildRequestExecutorAsync();

        var serializer = executor.Schema.Services.GetRequiredService<INodeIdSerializer>();
        var internalId = Guid.Parse("7a8b4f69-6404-4cf6-9768-9abf6f9a6e53");
        var id = serializer.Format("Issue8057GuidEntity", internalId);

        var result = await executor.ExecuteAsync(
            $$"""
              {
                issue8057GuidEntityById(id: "{{id}}") {
                  id
                }
              }
              """);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
        Assert.NotNull(operationResult.Data);
    }
}
