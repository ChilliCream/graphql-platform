using CookieCrumble;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.Tests;

public class SortInputStructTypesTest : SortTestBase
{

    [Fact]
    public void SortInputType_RecordStructSortableProperties()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddQueryType<AccountsQuery>()
            .AddSorting();

        // act
        // assert
        builder.Create().MatchSnapshot();
    }

    [Fact]
    public void SortInputType_RecordStructSortableNestedWithSortInputType()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddQueryType<TestObjectsQuery>()
            .AddSorting();

        // act
        // assert
        builder.Create().MatchSnapshot();
    }


    public class AccountsQuery
    {
        [UseSorting]
        public IQueryable<Account> Accounts() => new List<Account>().AsQueryable();
    }

    public record struct Account(Guid CorrelationId, string AccountKey, DateOnly BalanceDate, DateTime Created);

    public class TestObjectsQuery
    {
        [UseSorting<TestObjectSortInputType>]
        public IQueryable<TestObject> TestObjects() => new List<TestObject>().AsQueryable();
    }


    public record struct TestObject
    {
        public Guid ObjectId { get; init; }
        public bool IsRoot { get; init; }
        public ChildObject Child { get; init; }
    }

    public record struct ChildObject(NestedId Id, double Amount, decimal Sum);

    public record NestedId
    {
        public Guid ObjectId { get; init; }
    }

    public class TestObjectSortInputType : SortInputType<TestObject>
    {
        protected override void Configure(ISortInputTypeDescriptor<TestObject> descriptor)
        {
            descriptor.Ignore(x => x.IsRoot);
        }
    }
}
