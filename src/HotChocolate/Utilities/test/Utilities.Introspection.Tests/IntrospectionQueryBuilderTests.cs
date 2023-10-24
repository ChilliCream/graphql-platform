using CookieCrumble;
using HotChocolate.Language.Utilities;
using Xunit;

namespace HotChocolate.Utilities.Introspection;

public class IntrospectionQueryBuilderTests
{
    [Fact]
    public void Create_Default_Query()
    {
        // arrange
        var options = new IntrospectionOptions();
        var features = new SchemaFeatures();

        // act
        var document = IntrospectionQueryBuilder.Build(features, options);
        
        //assert
        document.Print().MatchSnapshot();
    }
    
    [Fact]
    public void Create_Query_With_ArgumentDeprecation()
    {
        // arrange
        var options = new IntrospectionOptions();
        var features = new SchemaFeatures
        {
            HasArgumentDeprecation = true
        };

        // act
        var document = IntrospectionQueryBuilder.Build(features, options);
        
        //assert
        document.Print().MatchSnapshot();
    }
    
    [Fact]
    public void Create_Query_With_DirectiveLocations()
    {
        // arrange
        var options = new IntrospectionOptions();
        var features = new SchemaFeatures
        {
            HasDirectiveLocations = true
        };

        // act
        var document = IntrospectionQueryBuilder.Build(features, options);
        
        //assert
        document.Print().MatchSnapshot();
    }
    
    [Fact]
    public void Create_Query_With_RepeatableDirectives()
    {
        // arrange
        var options = new IntrospectionOptions();
        var features = new SchemaFeatures
        {
            HasRepeatableDirectives = true
        };

        // act
        var document = IntrospectionQueryBuilder.Build(features, options);
        
        //assert
        document.Print().MatchSnapshot();
    }
    
    [Fact]
    public void Create_Query_With_SchemaDescription()
    {
        // arrange
        var options = new IntrospectionOptions();
        var features = new SchemaFeatures
        {
            HasSchemaDescription = true
        };

        // act
        var document = IntrospectionQueryBuilder.Build(features, options);
        
        //assert
        document.Print().MatchSnapshot();
    }
    
    [Fact]
    public void Create_Query_With_SubscriptionSupport()
    {
        // arrange
        var options = new IntrospectionOptions();
        var features = new SchemaFeatures
        {
            HasSubscriptionSupport = true
        };

        // act
        var document = IntrospectionQueryBuilder.Build(features, options);
        
        //assert
        document.Print().MatchSnapshot();
    }
}