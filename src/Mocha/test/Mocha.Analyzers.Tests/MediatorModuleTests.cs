namespace Mocha.Analyzers.Tests;

public class MediatorModuleTests
{
    [Fact]
    public async Task Generate_DefaultAssemblyName_PrefixesWithLastSegment()
    {
        // assemblyName defaults to "Tests" in TestHelper, so module name = "Tests"
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record GetItemQuery(int Id) : IQuery<string>;

            public class GetItemHandler : IQueryHandler<GetItemQuery, string>
            {
                public ValueTask<string> HandleAsync(GetItemQuery query, CancellationToken cancellationToken)
                    => new("item");
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_DottedAssemblyName_UsesLastSegment()
    {
        // "MyCompany.Services.Ordering" -> module name = "Ordering"
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record PingCommand() : ICommand;

            public class PingHandler : ICommandHandler<PingCommand>
            {
                public ValueTask HandleAsync(PingCommand command, CancellationToken cancellationToken)
                    => default;
            }
            """
        ], assemblyName: "MyCompany.Services.Ordering").MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_ModuleFile_ContainsHandlerRegistrations()
    {
        // Verifies the module file ({ModuleName}MediatorModule.g.cs) is generated
        // with handler registrations for composing multiple modules
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record CreateInvoiceCommand(string Name) : ICommand<int>;

            public class CreateInvoiceHandler : ICommandHandler<CreateInvoiceCommand, int>
            {
                public ValueTask<int> HandleAsync(CreateInvoiceCommand command, CancellationToken cancellationToken)
                    => new(1);
            }
            """
        ]).MatchMarkdownAsync();
    }
}
