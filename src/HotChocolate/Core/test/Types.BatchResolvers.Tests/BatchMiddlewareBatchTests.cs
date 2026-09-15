using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

/// <summary>
/// Proves that <see cref="ObjectFieldDescriptorAttribute"/>-driven <c>UseBatch</c> registration
/// (class-based, factory-based, and directive-based) actually composes and executes for a
/// <c>[BatchResolver]</c> field when forwarded through the source generator's
/// <c>ConfigurationHelper.ApplyConfiguration</c> path, not only when it is hand-written fluent
/// code (hc-0-bpl.1 comment 160): the foundation ticket proved class/factory/directive ordering
/// natively and proved only that the source-generated attribute hook fires, not that the
/// resulting batch middleware pipeline behaves correctly.
/// </summary>
public sealed partial class BatchMiddlewareBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    private MiddlewareTrace Trace { get; } = new();

    [Theory]
    [BatchMatrix]
    public async Task UseBatch_Should_ComposeInOrder_When_ClassAndFactoryMiddlewareAreStacked(
        DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(
            style, b => b.Services.AddSingleton(Trace), TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteAsync(
            executor, "{ tracedById(id: 1) }", TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["class:before", "factory:before", "factory:after", "class:after"], Trace.Events);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "tracedById": "class(factory(value))"
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task UseBatch_Should_ComposeInOrder_When_DirectiveMiddlewareIsRepeated(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(
            style, b => b.Services.AddSingleton(Trace), TestContext.Current.CancellationToken);

        // act
        var result = await ExecuteAsync(
            executor, "{ directiveTracedById(id: 1) }", TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["A:before", "B:before", "B:after", "A:after"], Trace.Events);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "directiveTracedById": "A(B(value))"
              }
            }
            """);
    }
}
