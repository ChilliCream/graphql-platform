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
        var features = new ServerCapabilities();

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
        var features = new ServerCapabilities
        {
            HasArgumentDeprecation = true,
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
        var features = new ServerCapabilities
        {
            HasDirectiveLocations = true,
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
        var features = new ServerCapabilities
        {
            HasRepeatableDirectives = true,
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
        var features = new ServerCapabilities
        {
            HasSchemaDescription = true,
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
        var features = new ServerCapabilities
        {
            HasSubscriptionSupport = true,
        };

        // act
        var document = IntrospectionQueryBuilder.Build(features, options);

        //assert
        document.Print().MatchSnapshot();
    }
}
