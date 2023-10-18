using CookieCrumble;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;

namespace HotChocolate.Fusion.Composition;

public class FusionDirectiveTests
{
    [Fact]
    public void ArgumentValidation_Version_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(
            () => new FusionDirective("prefix", true, null!));
    }
    
    [Fact]
    public void ArgumentValidation_Version_Is_Empty()
    {
        Assert.Throws<ArgumentException>(
            () => new FusionDirective("prefix", true, ""));
    }
    
    [Fact]
    public void ToDirective()
    {
        // arrange
        var fusionGraph = new Skimmed.Schema();
        var typeContext = new FusionTypes(fusionGraph, "abc_def");

        // act
        var type = new ObjectType("Foo")
        {
            Fields =
            {
                new OutputField("bar", fusionGraph.Types["String"])
                {
                    Directives =
                    {
                        new FusionDirective("prefix", true, "2024-12").ToDirective(typeContext)
                    }
                }
            }
        };

        fusionGraph.Types.Add(type);

        // assert
        fusionGraph.ToString().MatchInlineSnapshot(
            """
            type Foo {
              bar: String
                @fusion(version: "2024-12", prefix: "prefix", prefixSelf: true)
            }
            """);
    }

    [Fact]
    public void ToDirective_No_Prefix()
    {
        // arrange
        var fusionGraph = new Skimmed.Schema();
        var typeContext = new FusionTypes(fusionGraph);

        // act
        var type = new ObjectType("Foo")
        {
            Fields =
            {
                new OutputField("bar", fusionGraph.Types["String"])
                {
                    Directives =
                    {
                        new FusionDirective().ToDirective(typeContext)
                    }
                }
            }
        };

        fusionGraph.Types.Add(type);

        // assert
        fusionGraph.ToString().MatchInlineSnapshot(
            """
            type Foo {
              bar: String
                @fusion(version: "2023-12")
            }
            """);
    }

    [Fact]
    public void TryParse_ValidDirective_ParsedCorrectly()
    {
        // arrange
        var fusionGraph = new Skimmed.Schema();
        var typeContext = new FusionTypes(fusionGraph, "abc_def");

        var directiveNode = new Directive(
            typeContext.FusionDirective,
            new Argument("prefix", new StringValueNode("prefix")),
            new Argument("prefixSelf", new BooleanValueNode(true)),
            new Argument("version", new StringValueNode("2023-12")));

        // act
        var result = FusionDirective.TryParse(directiveNode, typeContext, out var parsedDirective);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedDirective);
        Assert.Equal("prefix", parsedDirective?.Prefix);
        Assert.True(parsedDirective?.PrefixSelf);
        Assert.Equal("2023-12", parsedDirective?.Version);
    }
    
    [Fact]
    public void TryParse_InvalidDirectiveName_ReturnsFalse()
    {
        // arrange
        var fusionGraph = new Skimmed.Schema();
        var typeContext = new FusionTypes(fusionGraph, "abc_def");
        
        fusionGraph.DirectiveTypes.Add(
            new DirectiveType("invalid_name")
            {
                IsRepeatable = false,
                Locations = DirectiveLocation.Executable
            });

        var directiveNode = new Directive(
            fusionGraph.DirectiveTypes["invalid_name"],
            new Argument("prefix", new StringValueNode("prefix")),
            new Argument("prefixSelf", new BooleanValueNode(true)),
            new Argument("version", new StringValueNode("2023-12")));

        // act
        var result = FusionDirective.TryParse(directiveNode, typeContext, out _);

        // assert
        Assert.False(result);
    }
}

