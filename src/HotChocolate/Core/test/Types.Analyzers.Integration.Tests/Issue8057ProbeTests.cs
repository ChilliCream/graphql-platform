using HotChocolate.Execution;
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
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }
}
