#if NET9_0_OR_GREATER
using CookieCrumble.Resources;
using GreenDonut.Data.TestContext;

namespace GreenDonut.Data;

// Mirrors PagingInheritanceTests.cs one for one against ToStreamPageAsync, so a failure names the
// API. The batch cases (BatchPaging_With_TPC_Selector_And_Navigation_Property,
// BatchPaging_With_TPC_Selector_And_Scalar_Property, BatchPaging_With_TPH_Selector_After_Cursor)
// use ToBatchPageAsync and are out of scope for this suite (hc-fork-1-o47.4, ToBatchStreamPageAsync,
// does not exist yet).
[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamPagingInheritanceTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task Paging_With_TPH_Selector_After_Cursor()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedAnimalsAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new AnimalContext(connectionString);

        var query = new QueryContext<Animal>(
            Selector: e =>
                e is Dog
                    ? new Dog { Id = ((Dog)e).Id, Name = ((Dog)e).Name }
                    : e is Cat
                        ? (Animal)new Cat { Id = ((Cat)e).Id, Name = ((Cat)e).Name }
                        : null!);

        var arguments = new PagingArguments(2);

        // act
        var firstPage = await context.Pets
            .With(query, sort => sort.AddDescending(e => e.Name))
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);
        var endCursor = await firstPage.CreateEndCursorAsync(cancellationToken);

        var secondPage = await context.Pets
            .With(query, sort => sort.AddDescending(e => e.Name))
            .ToStreamPageAsync(
                arguments with { After = endCursor },
                cancellationToken: cancellationToken);
        var items = new List<Animal>();

        await foreach (var animal in secondPage.WithCancellation(cancellationToken))
        {
            items.Add(animal);
        }

        // assert
        Assert.Equal(["epsilon", "delta"], items.Select(a => a.Name));
    }

    private static async Task SeedAnimalsAsync(string connectionString)
    {
        await using var context = new AnimalContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        var owner = new Owner { Id = 1, Name = "owner-1" };

        context.Owners.Add(owner);
        context.Pets.AddRange(
            new Dog { Id = 1, Name = "zeta", OwnerId = owner.Id, IsBarking = true },
            new Cat { Id = 2, Name = "epsilon", OwnerId = owner.Id, IsPurring = true },
            new Dog { Id = 3, Name = "delta", OwnerId = owner.Id, IsBarking = false },
            new Cat { Id = 4, Name = "gamma", OwnerId = owner.Id, IsPurring = false },
            new Dog { Id = 5, Name = "beta", OwnerId = owner.Id, IsBarking = true });

        await context.SaveChangesAsync();
    }
}
#endif
