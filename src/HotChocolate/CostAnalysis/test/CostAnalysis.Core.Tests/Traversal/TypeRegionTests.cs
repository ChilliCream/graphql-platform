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

    private static CostSchemaIndex BuildSchemaIndex() => ConditionTreeTestHelpers.BuildSchemaIndex(Sdl);

    private static PossibleTypeSet Scope(CostSchemaIndex schemaIndex, params string[] typeNames)
    {
        var indices = new int[typeNames.Length];

        for (var i = 0; i < typeNames.Length; i++)
        {
            indices[i] = schemaIndex.GetObjectTypeIndex(typeNames[i]);
        }

        return PossibleTypeSet.Create(schemaIndex.ObjectTypeCount, indices);
    }

    [Fact]
    public void Partition_Should_Return_One_Region_When_No_Condition_Narrows_The_Scope()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex();
        var scope = Scope(schemaIndex, "Dog", "Cat", "Fox");

        // act
        var regions = TypeRegionPartitioner.Partition(schemaIndex, scope, []);

        // assert
        var region = Assert.Single(regions);
        Assert.Equal(scope, region);
    }

    [Fact]
    public void Partition_Should_Split_Into_Singletons_When_Every_Type_Is_Distinguished()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex();
        var scope = Scope(schemaIndex, "Dog", "Cat", "Fox");
        PossibleTypeSet[] conditions = [Scope(schemaIndex, "Dog", "Cat"), Scope(schemaIndex, "Cat", "Fox")];

        // act
        var regions = TypeRegionPartitioner.Partition(schemaIndex, scope, conditions);

        // assert
        Assert.Equal(3, regions.Count);
        Assert.Contains(Scope(schemaIndex, "Dog"), regions);
        Assert.Contains(Scope(schemaIndex, "Cat"), regions);
        Assert.Contains(Scope(schemaIndex, "Fox"), regions);
    }

    [Fact]
    public void Partition_Should_Keep_Untouched_Types_Together_When_A_Condition_Narrows_Only_Part_Of_The_Scope()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex();
        var scope = Scope(schemaIndex, "Dog", "Cat", "Fox");
        PossibleTypeSet[] conditions = [Scope(schemaIndex, "Dog")];

        // act
        var regions = TypeRegionPartitioner.Partition(schemaIndex, scope, conditions);

        // assert
        Assert.Equal(2, regions.Count);
        Assert.Contains(Scope(schemaIndex, "Dog"), regions);
        Assert.Contains(Scope(schemaIndex, "Cat", "Fox"), regions);
    }

    [Fact]
    public void Partition_Should_Not_Exceed_The_MinScopeConditions_Bound_When_Conditions_Multiply()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex();
        var scope = Scope(schemaIndex, "Dog", "Cat", "Fox");
        PossibleTypeSet[] conditions =
        [
            Scope(schemaIndex, "Dog"),
            Scope(schemaIndex, "Cat"),
            Scope(schemaIndex, "Fox"),
            Scope(schemaIndex, "Dog", "Cat"),
            Scope(schemaIndex, "Cat", "Fox")
        ];

        // act
        var regions = TypeRegionPartitioner.Partition(schemaIndex, scope, conditions);

        // assert
        Assert.True(regions.Count <= scope.Count, $"expected at most {scope.Count} regions, got {regions.Count}");
    }

    [Fact]
    public void Partition_Should_Ignore_A_Condition_Outside_The_Scope()
    {
        // arrange
        var schemaIndex = BuildSchemaIndex();
        var scope = Scope(schemaIndex, "Dog", "Cat");
        PossibleTypeSet[] conditions = [Scope(schemaIndex, "Fox")];

        // act
        var regions = TypeRegionPartitioner.Partition(schemaIndex, scope, conditions);

        // assert
        var region = Assert.Single(regions);
        Assert.Equal(scope, region);
    }
}
