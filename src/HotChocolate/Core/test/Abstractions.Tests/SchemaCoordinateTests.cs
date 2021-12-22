using System;
using Xunit;

#nullable enable
namespace HotChocolate;

public class SchemaCoordinateTests
{
    [Theory]
    [InlineData(false, "Foo", null, null, "Foo")]
    [InlineData(false, "Foo", "bar", null, "Foo.bar")]
    [InlineData(false, "Foo", "bar", "baz", "Foo.bar(baz:)")]
    [InlineData(true, "foo", null, null, "@foo")]
    [InlineData(true, "foo", null, "bar", "@foo(bar:)")]
    public void ToString_Schema(
        bool ofDirective,
        string name,
        string? memberName,
        string? argumentName,
        string? result)
    {
        // arrange
        SchemaCoordinate coordinate = new(ofDirective, name, memberName, argumentName);

        // act
        // assert
        Assert.Equal(result, coordinate.ToString());
    }

    [Theory]
    [InlineData(true, "Foo", "bar", "baz")]
    [InlineData(false, "foo", null, "baz")]
    public void Ctor_InvalidArguments(
        bool ofDirective,
        string name,
        string? memberName,
        string? argumentName)
    {
        // arrange

        // act
        Exception? ex = Record.Exception(() =>
        {
            new SchemaCoordinate(ofDirective, name, memberName, argumentName);
        });
        // assert

        Assert.IsType<ArgumentException>(ex);
    }

    [Theory]
    [InlineData(false, "Foo", null, null)]
    [InlineData(false, "Foo", "bar", null)]
    [InlineData(false, "Foo", "bar", "baz")]
    [InlineData(true, "foo", null, null)]
    [InlineData(true, "foo", null, "bar")]
    public void GetHashCodeTests(
        bool ofDirective,
        string name,
        string? memberName,
        string? argumentName)
    {
        // arrange
        SchemaCoordinate a = new(ofDirective, name, memberName, argumentName);
        SchemaCoordinate b = new(ofDirective, name, memberName, argumentName);

        // act
        // assert
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void With_Name()
    {
        // arrange
        SchemaCoordinate coordinate = new(false, "abc", "def", "ghj");

        // act
        coordinate = coordinate.With(name: new NameString("xyz"));

        // assert
        Assert.Equal("xyz.def(ghj:)", coordinate.ToString());
    }

    [Fact]
    public void With_OfDirective()
    {
        // arrange
        SchemaCoordinate coordinate = new(false, "abc");

        // act
        coordinate = coordinate.With(ofDirective: true);

        // assert
        Assert.Equal("@abc", coordinate.ToString());
    }

    [Fact]
    public void With_Member()
    {
        // arrange
        SchemaCoordinate coordinate = new(false, "abc", "def", "ghj");

        // act
        coordinate = coordinate.With(memberName: new NameString("xyz"));

        // assert
        Assert.Equal("abc.xyz(ghj:)", coordinate.ToString());
    }

    [Fact]
    public void With_Argument()
    {
        // arrange
        SchemaCoordinate coordinate = new SchemaCoordinate(false, "abc", "def");

        // act
        coordinate = coordinate.With(argumentName: new NameString("xyz"));

        // assert
        Assert.Equal("abc.def(xyz:)", coordinate.ToString());
    }

    [Theory]
    [InlineData(false, "Foo", null, null)]
    [InlineData(false, "Foo", "bar", null)]
    [InlineData(false, "Foo", "bar", "baz")]
    [InlineData(true, "foo", null, null)]
    [InlineData(true, "foo", null, "bar")]
    public void EqualsTests(
        bool ofDirective,
        string name,
        string? memberName,
        string? argumentName)
    {
        // arrange
        SchemaCoordinate a = new(ofDirective, name, memberName, argumentName);
        SchemaCoordinate b = new(ofDirective, name, memberName, argumentName);

        // act
        // assert
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_Schema_False()
    {
        // arrange
        var a = new SchemaCoordinate(false, "abc", "def");
        var b = new SchemaCoordinate(false, "abc", "xyz");

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_Schema_To_Arg_False()
    {
        // arrange
        var a = new SchemaCoordinate(false, "abc", "def");
        var b = new SchemaCoordinate(false, "abc", "def", "ghi");

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_Schema_To_Directive_False()
    {
        // arrange
        var a = new SchemaCoordinate(false, "abc");
        var b = new SchemaCoordinate(true, "abc");

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_Argument_False()
    {
        // arrange
        var a = new SchemaCoordinate(false, "abc", "def", "ghi");
        var b = new SchemaCoordinate(false, "abc", "def", "xyz");

        // act
        // assert
        Assert.False(a.Equals(b));
    }

    [Theory]
    [InlineData(false, "Foo", null, null)]
    [InlineData(false, "Foo", "bar", null)]
    [InlineData(false, "Foo", "bar", "baz")]
    [InlineData(true, "foo", null, null)]
    [InlineData(true, "foo", null, "bar")]
    public void Object_EqualsTests(
        bool ofDirective,
        string name,
        string? memberName,
        string? argumentName)
    {
        // arrange
        SchemaCoordinate a = new(ofDirective, name, memberName, argumentName);
        SchemaCoordinate b = new(ofDirective, name, memberName, argumentName);

        // act
        // assert
        Assert.True(a.Equals((object)b));
    }

    [Fact]
    public void Object_Equals_Schema_False()
    {
        // arrange
        var a = new SchemaCoordinate(false, "abc", "def");
        var b = new SchemaCoordinate(false, "abc", "xyz");

        // act
        // assert
        Assert.False(a.Equals((object)b));
    }

    [Fact]
    public void Object_Equals_Schema_To_Arg_False()
    {
        // arrange
        var a = new SchemaCoordinate(false, "abc", "def");
        var b = new SchemaCoordinate(false, "abc", "def", "ghi");

        // act
        // assert
        Assert.False(a.Equals((object)b));
    }

    [Fact]
    public void Object_Equals_Schema_To_Directive_False()
    {
        // arrange
        var a = new SchemaCoordinate(false, "abc");
        var b = new SchemaCoordinate(true, "abc");

        // act
        // assert
        Assert.False(a.Equals((object)b));
    }

    [Fact]
    public void Object_Equals_Argument_False()
    {
        // arrange
        var a = new SchemaCoordinate(false, "abc", "def", "ghi");
        var b = new SchemaCoordinate(false, "abc", "def", "xyz");

        // act
        // assert
        Assert.False(a.Equals((object)b));
    }

    [Fact]
    public void Deconstruct_Coordinates()
    {
        // arrange
        var a = new SchemaCoordinate(false, "abc", "def", "ghi");

        // act
        (bool ofDirective, NameString name, NameString? memberName, NameString? argumentName) =
            a;

        // assert
        Assert.Equal(a.Name, name);
        Assert.Equal(a.MemberName, memberName);
        Assert.Equal(a.ArgumentName, argumentName);
        Assert.Equal(a.OfDirective, ofDirective);
    }
}
