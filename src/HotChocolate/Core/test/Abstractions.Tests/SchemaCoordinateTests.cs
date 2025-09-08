using HotChocolate.Language;

namespace HotChocolate;

public class SchemaCoordinateTests
{
    [Fact]
    public void Create_Type_SchemaCoordinate()
    {
        // arrange & act
        var coordinate = new SchemaCoordinate("Abc");

        // assert
        Assert.Equal("Abc", coordinate.Name);
        Assert.Equal("Abc", coordinate.ToString());
    }

    [Fact]
    public void Create_Field_SchemaCoordinate()
    {
        // arrange & act
        var coordinate = new SchemaCoordinate("Abc", "def");

        // assert
        Assert.Equal("Abc", coordinate.Name);
        Assert.Equal("def", coordinate.MemberName);
        Assert.Equal("Abc.def", coordinate.ToString());
    }

    [Fact]
    public void Create_Field_Argument_SchemaCoordinate()
    {
        // arrange & act
        var coordinate = new SchemaCoordinate("Abc", "def", "ghi");

        // assert
        Assert.Equal("Abc", coordinate.Name);
        Assert.Equal("def", coordinate.MemberName);
        Assert.Equal("ghi", coordinate.ArgumentName);
        Assert.Equal("Abc.def(ghi:)", coordinate.ToString());
    }

    [Fact]
    public void Create_Field_Argument_SchemaCoordinate_Without_MemberName()
    {
        // arrange & act
        void Fail() => new SchemaCoordinate("abc", argumentName: "def");

        // assert
        var ex = Assert.Throws<ArgumentException>(Fail);
        Assert.Equal("argumentName", ex.ParamName);
        Assert.StartsWith(
            "A argument name without a member name is only allowed on directive coordinates",
            ex.Message);
    }

    [Fact]
    public void Create_Directive_SchemaCoordinate()
    {
        // arrange & act
        var coordinate = new SchemaCoordinate("abc", ofDirective: true);

        // assert
        Assert.Equal("abc", coordinate.Name);
        Assert.Equal("@abc", coordinate.ToString());
    }

    [Fact]
    public void Create_Directive_Argument_SchemaCoordinate()
    {
        // arrange & act
        var coordinate = new SchemaCoordinate("abc", argumentName: "def", ofDirective: true);

        // assert
        Assert.Equal("abc", coordinate.Name);
        Assert.Equal("def", coordinate.ArgumentName);
        Assert.Equal("@abc(def:)", coordinate.ToString());
    }

    [Fact]
    public void Create_Directive_SchemaCoordinate_With()
    {
        // arrange & act
        void Fail() => new SchemaCoordinate("abc", memberName: "def", ofDirective: true);

        // assert
        var ex = Assert.Throws<ArgumentException>(Fail);
        Assert.Equal("memberName", ex.ParamName);
        Assert.StartsWith("A directive cannot contain a member name.", ex.Message);
    }

    [Fact]
    public void Parse_SchemaCoordinate()
    {
        // arrange & act
        var coordinate = SchemaCoordinate.Parse("Abc.def");

        // assert
        Assert.Equal("Abc", coordinate.Name);
        Assert.Equal("def", coordinate.MemberName);
        Assert.Equal("Abc.def", coordinate.ToString());
    }

    [Fact]
    public void Parse_Invalid_SchemaCoordinate()
    {
        // arrange & act
        void Fail() => SchemaCoordinate.Parse("...");

        // assert
        Assert.Throws<SyntaxException>(Fail);
    }

    [Fact]
    public void TryParse_SchemaCoordinate()
    {
        // arrange & act
        var success = SchemaCoordinate.TryParse("Abc.def", out var coordinate);

        // assert
        Assert.True(success);
        Assert.Equal("Abc", coordinate?.Name);
        Assert.Equal("def", coordinate?.MemberName);
        Assert.Equal("Abc.def", coordinate?.ToString());
    }

    [InlineData(null)]
    [InlineData("")]
    [InlineData("...")]
    [Theory]
    public void TryParse_Invalid_SchemaCoordinate(string? s)
    {
        // arrange & act
        var success = SchemaCoordinate.TryParse(s!, out var coordinate);

        // assert
        Assert.False(success);
        Assert.Null(coordinate);
    }

    [Fact]
    public void FromSyntax_Type_SchemaCoordinate()
    {
        // arrange
        var node = new SchemaCoordinateNode(null, false, new("Abc"), null, null);

        // act
        var coordinate = SchemaCoordinate.FromSyntax(node);

        // assert
        Assert.Equal("Abc", coordinate.Name);
        Assert.Equal("Abc", coordinate.ToString());
    }

    [Fact]
    public void FromSyntax_Field_SchemaCoordinate()
    {
        // arrange
        var node = new SchemaCoordinateNode(null, false, new("Abc"), new("def"), null);

        // act
        var coordinate = SchemaCoordinate.FromSyntax(node);

        // assert
        Assert.Equal("Abc", coordinate.Name);
        Assert.Equal("def", coordinate.MemberName);
        Assert.Equal("Abc.def", coordinate.ToString());
    }

    [Fact]
    public void FromSyntax_Field_Argument_SchemaCoordinate()
    {
        // arrange
        var node = new SchemaCoordinateNode(null, false, new("Abc"), new("def"), new("ghi"));

        // act
        var coordinate = SchemaCoordinate.FromSyntax(node);

        // assert
        Assert.Equal("Abc", coordinate.Name);
        Assert.Equal("def", coordinate.MemberName);
        Assert.Equal("ghi", coordinate.ArgumentName);
        Assert.Equal("Abc.def(ghi:)", coordinate.ToString());
    }

    [Fact]
    public void FromSyntax_Directive_SchemaCoordinate()
    {
        // arrange
        var node = new SchemaCoordinateNode(null, true, new("abc"), null, null);

        // act
        var coordinate = SchemaCoordinate.FromSyntax(node);

        // assert
        Assert.Equal("abc", coordinate.Name);
        Assert.Equal("@abc", coordinate.ToString());
    }

    [Fact]
    public void FromSyntax_Directive_Argument_SchemaCoordinate()
    {
        // arrange
        var node = new SchemaCoordinateNode(null, true, new("abc"), null, new("def"));

        // act
        var coordinate = SchemaCoordinate.FromSyntax(node);

        // assert
        Assert.Equal("abc", coordinate.Name);
        Assert.Equal("def", coordinate.ArgumentName);
        Assert.Equal("@abc(def:)", coordinate.ToString());
    }
}
