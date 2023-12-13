using CookieCrumble;
using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public class DeclareDirectiveTests
{
    [Fact]
    public void ArgumentValidation_1_Name_Is_Null()
    {
        Assert.Throws<ArgumentException>(
            () => new DeclareDirective(
                null!,
                new FieldNode(
                    null,
                    new NameNode("a"),
                    null,
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null)));
    }
    
    [Fact]
    public void ArgumentValidation_2_Name_Is_Null()
    {
        Assert.Throws<ArgumentException>(
            () => new DeclareDirective(
                null!,
                "abc"));
    }
    
    [Fact]
    public void ArgumentValidation_1_Name_Is_Empty()
    {
        Assert.Throws<ArgumentException>(
            () => new DeclareDirective(
                string.Empty,
                new FieldNode(
                    null,
                    new NameNode("a"),
                    null,
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null)));
    }
    
    [Fact]
    public void ArgumentValidation_2_Name_Is_Empty()
    {
        Assert.Throws<ArgumentException>(
            () => new DeclareDirective(
                string.Empty,
                "abc"));
    }
    
    [Fact]
    public void ArgumentValidation_1_Name_Is_Invalid()
    {
        Assert.Throws<ArgumentException>(
            () => new DeclareDirective(
                ".",
                new FieldNode(
                    null,
                    new NameNode("a"),
                    null,
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null)));
    }
    
    [Fact]
    public void ArgumentValidation_2_Name_Is_Invalid()
    {
        Assert.Throws<ArgumentException>(
            () => new DeclareDirective(
                ".",
                "abc"));
    }
    
    [Fact]
    public void ArgumentValidation_1_Field_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DeclareDirective(
                "abc",
                default(FieldNode)!));
    }
    
    [Fact]
    public void ArgumentValidation_2_Field_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DeclareDirective(
                "abc",
                default(string)!));
    }
    
    [Fact]
    public void ArgumentValidation_2_Field_Is_Empty()
    {
        Assert.Throws<ArgumentException>(
            () => new DeclareDirective(
                "abc",
                string.Empty));
    }
    
    [Fact]
    public void ArgumentValidation_2_Field_Has_Invalid_Syntax()
    {
        Assert.Throws<ArgumentException>(
            () => new DeclareDirective(
                "{",
                string.Empty));
    }
    
    [Fact]
    public void ArgumentValidation_1_From_Is_Invalid_Subgraph_Name()
    {
        Assert.Throws<ArgumentException>(
            () => new DeclareDirective(
                "abc",
                new FieldNode(
                    null,
                    new NameNode("a"),
                    null,
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null),
                "."));
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
                        new DeclareDirective("ABC_def", "some { field }", "abc_sub").ToDirective(typeContext)
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
                @abc_def_declare(name: "ABC_def", select: "some { field }", from: "abc_sub")
            }
            """);
    }

    [Fact]
    public void GetAllFrom_ValidDirectives_ReturnsAllDirectives()
    {
        // arrange
        var fusionGraph = new Skimmed.Schema();
        var typeContext = new FusionTypes(fusionGraph, "abc_def");
        var member = new HasDirectiveMock(
            new List<Directive>
            {
                new Directive(
                    typeContext.DeclareDirective,
                    new Argument("name", "ABC"),
                    new Argument("select", "field_abc { def }")),
                new Directive(
                    typeContext.DeclareDirective,
                    new Argument("name", "DEF"),
                    new Argument("select", "field_def { ghi }"),
                    new Argument("from", "sub_def"))
            });

        // act
        var result = DeclareDirective.GetAllFrom(member, typeContext).ToList();

        // assert
        Assert.Collection(
            result,
            d =>
            {
                Assert.Equal("ABC", d.Name);
                Assert.Equal("field_abc { def }", d.Select.ToString(false));
                Assert.Null(d.From);
            },
            d =>
            {
                Assert.Equal("DEF", d.Name);
                Assert.Equal("field_def { ghi }", d.Select.ToString(false));
                Assert.Equal("sub_def", d.From);
            });
    }

    [Fact]
    public void ExistsIn_DirectiveExists_ReturnsTrue()
    {
        // arrange
        var fusionGraph = new Skimmed.Schema();
        var typeContext = new FusionTypes(fusionGraph, "abc_def");
        
        var member = new HasDirectiveMock(
            new List<Directive>
            {
                new Directive(
                    typeContext.DeclareDirective,
                    new Argument("name", "ABC"),
                    new Argument("select", "field_abc { def }")),
                new Directive(
                    typeContext.DeclareDirective,
                    new Argument("name", "DEF"),
                    new Argument("select", "field_def { ghi }"),
                    new Argument("from", "sub_def"))
            });

        // act
        var result = DeclareDirective.ExistsIn(member, typeContext);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void ExistsIn_DirectiveDoesNotExist_ReturnsFalse()
    {
        // arrange
        var fusionGraph = new Skimmed.Schema();
        var typeContext = new FusionTypes(fusionGraph, "abc_def");
        var member = new HasDirectiveMock(new List<Directive>());
        
        // act
        var result = DeclareDirective.ExistsIn(member, typeContext);

        // assert
        Assert.False(result);
    }
}