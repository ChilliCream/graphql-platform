using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Projections;

public class QueryableProjectionComplexTypeTests
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = new Bar { Baz = "testatest", } },
        new() { Bar = new Bar { Baz = "testbtest", } },
    ];

    private readonly SchemaCache _cache = new SchemaCache();

    [Fact]
    public async Task Create_Complex_Type()
    {
        // arrange
        var tester = _cache.CreateSchema(_fooEntities, OnModelCreating);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                        {
                            root {
                                bar {
                                    baz
                                }
                            }
                        }")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    private static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Foo>().ComplexProperty(f => f.Bar);
    }

    public class Foo
    {
        public int Id { get; set; }

        public Bar Bar { get; set; } = null!;
    }

    public record Bar
    {
        public string Baz { get; set; } = null!;
    }
}
