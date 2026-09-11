namespace HotChocolate.CostAnalysis;

public class TypeRegionTests
{
    private const string Sdl =
        """
        type Dog { name: String }
        type Cat { name: String }
        type Fox { name: String }
        type Query { pet: Dog }
        """;

    private static CostSchemaSnapshot BuildSnapshot() => ConditionTreeTestHelpers.BuildSnapshot(Sdl);

    private static PossibleTypeSet Scope(CostSchemaSnapshot snapshot, params string[] typeNames)
    {
        var indices = new int[typeNames.Length];

        for (var i = 0; i < typeNames.Length; i++)
        {
            indices[i] = snapshot.GetObjectTypeIndex(typeNames[i]);
        }

        return PossibleTypeSet.Create(snapshot.ObjectTypeCount, indices);
    }

    [Fact]
    public void Partition_Should_Return_One_Region_When_No_Condition_Narrows_The_Scope()
    {
        // arrange
        var snapshot = BuildSnapshot();
        var scope = Scope(snapshot, "Dog", "Cat", "Fox");

        // act
        var regions = TypeRegionPartitioner.Partition(snapshot, scope, []);

        // assert
        var region = Assert.Single(regions);
        Assert.Equal(scope, region);
    }

    [Fact]
    public void Partition_Should_Split_Into_Singletons_When_Every_Type_Is_Distinguished()
    {
        // arrange: {Dog,Cat,Fox} with conditions {Dog,Cat} and {Cat,Fox} -> {Dog},{Cat},{Fox}
        var snapshot = BuildSnapshot();
        var scope = Scope(snapshot, "Dog", "Cat", "Fox");
        PossibleTypeSet[] conditions = [Scope(snapshot, "Dog", "Cat"), Scope(snapshot, "Cat", "Fox")];

        // act
        var regions = TypeRegionPartitioner.Partition(snapshot, scope, conditions);

        // assert
        Assert.Equal(3, regions.Count);
        Assert.Contains(Scope(snapshot, "Dog"), regions);
        Assert.Contains(Scope(snapshot, "Cat"), regions);
        Assert.Contains(Scope(snapshot, "Fox"), regions);
    }

    [Fact]
    public void Partition_Should_Keep_Untouched_Types_Together_When_A_Condition_Narrows_Only_Part_Of_The_Scope()
    {
        // arrange: only Dog is ever distinguished, so Cat and Fox stay one region
        var snapshot = BuildSnapshot();
        var scope = Scope(snapshot, "Dog", "Cat", "Fox");
        PossibleTypeSet[] conditions = [Scope(snapshot, "Dog")];

        // act
        var regions = TypeRegionPartitioner.Partition(snapshot, scope, conditions);

        // assert
        Assert.Equal(2, regions.Count);
        Assert.Contains(Scope(snapshot, "Dog"), regions);
        Assert.Contains(Scope(snapshot, "Cat", "Fox"), regions);
    }

    [Fact]
    public void Partition_Should_Not_Exceed_The_MinScopeConditions_Bound_When_Conditions_Multiply()
    {
        // arrange: five distinguishing conditions over a 3-member scope
        var snapshot = BuildSnapshot();
        var scope = Scope(snapshot, "Dog", "Cat", "Fox");
        PossibleTypeSet[] conditions =
        [
            Scope(snapshot, "Dog"),
            Scope(snapshot, "Cat"),
            Scope(snapshot, "Fox"),
            Scope(snapshot, "Dog", "Cat"),
            Scope(snapshot, "Cat", "Fox")
        ];

        // act
        var regions = TypeRegionPartitioner.Partition(snapshot, scope, conditions);

        // assert
        Assert.True(regions.Count <= scope.Count, $"expected at most {scope.Count} regions, got {regions.Count}");
    }

    [Fact]
    public void Partition_Should_Ignore_A_Condition_Outside_The_Scope()
    {
        // arrange: Fox never appears in the scope being partitioned
        var snapshot = BuildSnapshot();
        var scope = Scope(snapshot, "Dog", "Cat");
        PossibleTypeSet[] conditions = [Scope(snapshot, "Fox")];

        // act
        var regions = TypeRegionPartitioner.Partition(snapshot, scope, conditions);

        // assert
        var region = Assert.Single(regions);
        Assert.Equal(scope, region);
    }
}
