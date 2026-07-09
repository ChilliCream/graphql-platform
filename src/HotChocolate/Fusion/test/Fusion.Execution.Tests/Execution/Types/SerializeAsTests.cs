using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Types;

public class SerializeAsTests : FusionTestBase
{
    [Fact]
    public void SerializeAs_Will_Not_Be_In_The_Schema()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("serializeAs.graphql"));
        var options = new FusionOptions { ApplySerializeAsToScalars = false };
        var features = new FeatureCollection();
        features.Set<IFusionSchemaOptions>(options);

        // act
        var schema = FusionSchemaDefinition.Create(compositeSchemaDoc, features: features);

        // assert
        var type = schema.Types.GetType<IScalarTypeDefinition>("Custom");
        Assert.Equal(ScalarSerializationType.String, type.SerializationType);
        schema.MatchSnapshot();
    }

    [Fact]
    public void SerializeAs_Will_Be_In_The_Schema()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("serializeAs.graphql"));
        var options = new FusionOptions { ApplySerializeAsToScalars = true };
        var features = new FeatureCollection();
        features.Set<IFusionSchemaOptions>(options);

        // act
        var schema = FusionSchemaDefinition.Create(compositeSchemaDoc, features: features);

        // assert
        var type = schema.Types.GetType<IScalarTypeDefinition>("Custom");
        Assert.Equal(ScalarSerializationType.String, type.SerializationType);
        schema.MatchSnapshot();
    }
}
