using CookieCrumble;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public class FusionTypeContextTests
{
    [Fact]
    public void AddSourceDirective()
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
                        new SourceDirective("Subgraph_Abc").ToDirective(typeContext)
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
                @abc_def_source(subgraph: "Subgraph_Abc")
            }
            """);
    }
    
    [Fact]
    public void AddRequireDirective()
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
                    Arguments =
                    {
                        new InputField("baz", fusionGraph.Types["String"])
                        {
                            Directives =
                            {
                                new RequireDirective("this { is { my { field } } }").ToDirective(typeContext)
                            }
                        }
                    }
                }
            }
        };
        
        fusionGraph.Types.Add(type);
        
        // assert
        fusionGraph.ToString().MatchInlineSnapshot(
            """
            type Foo {
              bar(baz: String
                @abc_def_require(field: "this { is { my { field } } }")): String
            }
            """);
    }
   
}