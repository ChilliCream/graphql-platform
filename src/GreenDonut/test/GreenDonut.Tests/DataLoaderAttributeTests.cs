namespace GreenDonut;

public class DataLoaderAttributeTests
{
    [Fact]
    public void Attribute_ShouldExposeConfiguredProperties_WhenGenericDataLoaderIsSpecified()
    {
        // arrange
        var lookups = new[] { "ById" };
        var attribute = new DataLoaderAttribute<BatchDataLoader<string, string>>("custom")
        {
            Lookups = lookups,
            ServiceScope = DataLoaderServiceScope.DataLoaderScope,
            AccessModifier = DataLoaderAccessModifier.Public,
            MaxBatchSize = 100
        };

        // act

        // assert
        Assert.Equal("custom", attribute.Name);
        Assert.Equal(lookups, attribute.Lookups);
        Assert.Equal(DataLoaderServiceScope.DataLoaderScope, attribute.ServiceScope);
        Assert.Equal(DataLoaderAccessModifier.Public, attribute.AccessModifier);
        Assert.Equal(100, attribute.MaxBatchSize);
    }

    [Fact]
    public void Attribute_ShouldUseDefaultValues_WhenPropertiesAreNotConfigured()
    {
        // arrange
        var attribute = new DataLoaderAttribute<BatchDataLoader<string, string>>();

        // act

        // assert
        Assert.Null(attribute.Name);
        Assert.Empty(attribute.Lookups);
        Assert.Equal(default, attribute.ServiceScope);
        Assert.Equal(default, attribute.AccessModifier);
        Assert.Equal(0, attribute.MaxBatchSize);
    }
}
