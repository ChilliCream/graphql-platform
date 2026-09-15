using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class BatchResolverFormatterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BatchResolver_Should_EncodeScalarAndListIds_When_IdAttributeApplied(bool generated)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL()
            .AddGlobalObjectIdentification(false)
            .AddQueryType(d => d.Field("products")
                .Type<ListType<ObjectType<FormatterProduct>>>()
                .Resolve(new[] { new FormatterProduct(1), new FormatterProduct(2) }));

        if (generated)
        {
            builder.AddObjectType<FormatterProduct>(FormatterProductType.Initialize);
        }
        else
        {
            builder.AddObjectType<FormatterProduct>(d =>
            {
                d.Field<FormatterProductResolvers>(t => t.GetExternalId(null!));
                d.Field<FormatterProductResolvers>(t => t.GetRelatedIds(null!));
            });
        }

        var executor = await builder.BuildRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync("{ products { externalId relatedIds } }",
            cancellationToken: TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        result.MatchSnapshot(postFix: generated.ToString());
    }
}

public sealed record FormatterProduct(int Id);

[ObjectType<FormatterProduct>]
public static partial class FormatterProductType
{
    [BatchResolver]
    [ID("Product")]
    public static IReadOnlyList<int> GetExternalId([Parent] List<FormatterProduct> products)
        => products.Select(t => t.Id).ToArray();

    [BatchResolver]
    [ID("Product")]
    public static IReadOnlyList<int[]> GetRelatedIds([Parent] List<FormatterProduct> products)
        => products.Select(t => new[] { t.Id, t.Id + 10 }).ToArray();
}

public sealed class FormatterProductResolvers
{
    [BatchResolver]
    [ID("Product")]
    public IReadOnlyList<int> GetExternalId([Parent] List<FormatterProduct> products)
        => products.Select(t => t.Id).ToArray();

    [BatchResolver]
    [ID("Product")]
    public IReadOnlyList<int[]> GetRelatedIds([Parent] List<FormatterProduct> products)
        => products.Select(t => new[] { t.Id, t.Id + 10 }).ToArray();
}
