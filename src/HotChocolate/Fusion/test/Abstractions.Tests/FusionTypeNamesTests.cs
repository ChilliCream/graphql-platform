using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

public class FusionTypeNamesTests
{
    [Fact]
    public void Create_DefaultPrefix_ReturnsDefaultNames()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create();

        // act/assert
        Assert.Null(fusionTypeNames.Prefix);
        Assert.Equal("variable", fusionTypeNames.VariableDirective);
        Assert.Equal("resolver", fusionTypeNames.ResolverDirective);
        Assert.Equal("source", fusionTypeNames.SourceDirective);
        Assert.Equal("is", fusionTypeNames.IsDirective);
        Assert.Equal("transport", fusionTypeNames.TransportDirective);
        Assert.Equal("fusion", fusionTypeNames.FusionDirective);
        Assert.Equal("_Selection", fusionTypeNames.SelectionScalar);
        Assert.Equal("_SelectionSet", fusionTypeNames.SelectionSetScalar);
        Assert.Equal("_TypeName", fusionTypeNames.TypeNameScalar);
        Assert.Equal("_Type", fusionTypeNames.TypeScalar);
        Assert.Equal("_Uri", fusionTypeNames.UriScalar);
    }

    [Fact]
    public void Create_CustomPrefix_ReturnsCustomNames()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create("MyPrefix", prefixSelf: true);

        // act/assert
        Assert.Equal("MyPrefix", fusionTypeNames.Prefix);
        Assert.Equal("MyPrefix_variable", fusionTypeNames.VariableDirective);
        Assert.Equal("MyPrefix_resolver", fusionTypeNames.ResolverDirective);
        Assert.Equal("MyPrefix_source", fusionTypeNames.SourceDirective);
        Assert.Equal("MyPrefix_is", fusionTypeNames.IsDirective);
        Assert.Equal("MyPrefix_transport", fusionTypeNames.TransportDirective);
        Assert.Equal("MyPrefix_fusion", fusionTypeNames.FusionDirective);
        Assert.Equal("MyPrefix_Selection", fusionTypeNames.SelectionScalar);
        Assert.Equal("MyPrefix_SelectionSet", fusionTypeNames.SelectionSetScalar);
        Assert.Equal("MyPrefix_TypeName", fusionTypeNames.TypeNameScalar);
        Assert.Equal("MyPrefix_Type", fusionTypeNames.TypeScalar);
        Assert.Equal("MyPrefix_Uri", fusionTypeNames.UriScalar);
    }

    [Fact]
    public void From_SchemaWithFusionDirective_ReturnsCustomNames()
    {
        // arrange
        var schema = "schema @fusion(prefix: \"MyPrefix\") {}";
        var document = Utf8GraphQLParser.Parse(schema);
        var fusionTypeNames = FusionTypeNames.From(document);

        // act/assert
        Assert.Equal("MyPrefix", fusionTypeNames.Prefix);
        Assert.Equal("MyPrefix_variable", fusionTypeNames.VariableDirective);
        Assert.Equal("MyPrefix_resolver", fusionTypeNames.ResolverDirective);
        Assert.Equal("MyPrefix_source", fusionTypeNames.SourceDirective);
        Assert.Equal("MyPrefix_is", fusionTypeNames.IsDirective);
        Assert.Equal("MyPrefix_transport", fusionTypeNames.TransportDirective);
        Assert.Equal("fusion", fusionTypeNames.FusionDirective);
        Assert.Equal("MyPrefix_Selection", fusionTypeNames.SelectionScalar);
        Assert.Equal("MyPrefix_SelectionSet", fusionTypeNames.SelectionSetScalar);
        Assert.Equal("MyPrefix_TypeName", fusionTypeNames.TypeNameScalar);
        Assert.Equal("MyPrefix_Type", fusionTypeNames.TypeScalar);
        Assert.Equal("MyPrefix_Uri", fusionTypeNames.UriScalar);
    }

    [Fact]
    public void From_SchemaWithPrefixedFusionDirective_ReturnsCustomNames()
    {
        // arrange
        var schema = "schema @MyOtherPrefix_fusion(prefixSelf: true, prefix: \"MyOtherPrefix\") {}";
        var document = Utf8GraphQLParser.Parse(schema);
        var fusionTypeNames = FusionTypeNames.From(document);

        // act/assert
        Assert.Equal("MyOtherPrefix", fusionTypeNames.Prefix);
        Assert.Equal("MyOtherPrefix_variable", fusionTypeNames.VariableDirective);
        Assert.Equal("MyOtherPrefix_resolver", fusionTypeNames.ResolverDirective);
        Assert.Equal("MyOtherPrefix_source", fusionTypeNames.SourceDirective);
        Assert.Equal("MyOtherPrefix_is", fusionTypeNames.IsDirective);
        Assert.Equal("MyOtherPrefix_transport", fusionTypeNames.TransportDirective);
        Assert.Equal("MyOtherPrefix_fusion", fusionTypeNames.FusionDirective);
        Assert.Equal("MyOtherPrefix_Selection", fusionTypeNames.SelectionScalar);
        Assert.Equal("MyOtherPrefix_SelectionSet", fusionTypeNames.SelectionSetScalar);
        Assert.Equal("MyOtherPrefix_TypeName", fusionTypeNames.TypeNameScalar);
        Assert.Equal("MyOtherPrefix_Type", fusionTypeNames.TypeScalar);
        Assert.Equal("MyOtherPrefix_Uri", fusionTypeNames.UriScalar);
    }

    [Fact]
    public void IsFusionType_ValidFusionType_ReturnsTrue()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create();
        var typeName = "_Selection";

        // act
        var isFusionType = fusionTypeNames.IsFusionType(typeName);

        // assert
        Assert.True(isFusionType);
    }

    [Fact]
    public void IsFusionType_InvalidFusionType_ReturnsFalse()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create();
        var typeName = "InvalidType";

        // act
        var isFusionType = fusionTypeNames.IsFusionType(typeName);

        // assert
        Assert.False(isFusionType);
    }

    [Fact]
    public void IsFusionType_ValidFusionTypeWithPrefix_ReturnsTrue()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create("prefix");
        var typeName = "prefix_Selection";

        // act
        var isFusionType = fusionTypeNames.IsFusionType(typeName);

        // assert
        Assert.True(isFusionType);
    }

    [Fact]
    public void IsFusionType_ValidFusionTypeWithPrefixSelf_ReturnsTrue()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create("prefix", prefixSelf: true);
        var typeName = "prefix_type";

        // act
        var isFusionType = fusionTypeNames.IsFusionType(typeName);

        // assert
        Assert.True(isFusionType);
    }

    [Fact]
    public void IsFusionType_InvalidFusionTypeWithPrefix_ReturnsFalse()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create("prefix");
        var typeName = "invalid_type";

        // act
        var isFusionType = fusionTypeNames.IsFusionType(typeName);

        // assert
        Assert.False(isFusionType);
    }

    [Fact]
    public void IsFusionType_InvalidFusionTypeWithPrefixSelf_ReturnsFalse()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create("prefix", prefixSelf: true);
        var typeName = "invalid_type";

        // act
        var isFusionType = fusionTypeNames.IsFusionType(typeName);

        // assert
        Assert.False(isFusionType);
    }

    [Fact]
    public void IsFusionDirective_ValidFusionDirective_ReturnsTrue()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create();
        var directiveName = "variable";

        // act
        var isFusionDirective = fusionTypeNames.IsFusionDirective(directiveName);

        // assert
        Assert.True(isFusionDirective);
    }

    [Fact]
    public void IsFusionDirective_InvalidFusionDirective_ReturnsFalse()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create();
        var directiveName = "InvalidDirective";

        // act
        var isFusionDirective = fusionTypeNames.IsFusionDirective(directiveName);

        // assert
        Assert.False(isFusionDirective);
    }

    [Fact]
    public void IsFusionDirective_ValidFusionDirectiveWithPrefix_ReturnsTrue()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create("prefix");
        var directiveName = "prefix_variable";

        // act
        var isFusionDirective = fusionTypeNames.IsFusionDirective(directiveName);

        // assert
        Assert.True(isFusionDirective);
    }

    [Fact]
    public void IsFusionDirective_ValidFusionDirectiveWithPrefixSelf_ReturnsTrue()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create("prefix", prefixSelf: true);
        var directiveName = "prefix_fusion";

        // act
        var isFusionDirective = fusionTypeNames.IsFusionDirective(directiveName);

        // assert
        Assert.True(isFusionDirective);
    }

    [Fact]
    public void IsFusionDirective_InvalidFusionDirectiveWithPrefix_ReturnsFalse()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create("prefix");
        var directiveName = "invalid_directive";

        // act
        var isFusionDirective = fusionTypeNames.IsFusionDirective(directiveName);

        // assert
        Assert.False(isFusionDirective);
    }

    [Fact]
    public void IsFusionDirective_InvalidFusionDirectiveWithPrefixSelf_ReturnsFalse()
    {
        // arrange
        var fusionTypeNames = FusionTypeNames.Create("prefix", prefixSelf: true);
        var directiveName = "invalid_directive";

        // act
        var isFusionDirective = fusionTypeNames.IsFusionDirective(directiveName);

        // assert
        Assert.False(isFusionDirective);
    }
}
