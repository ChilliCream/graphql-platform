using HotChocolate.Types;

#nullable enable

namespace HotChocolate;

public class SchemaCoordinateTests
{
    [Fact]
    public void GetMember_ObjectType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("Baz");

        // assert
        Assert.Equal("Baz", Assert.IsType<ObjectType>(member).Name);
    }

    [Fact]
    public void GetMember_ObjectType_Field()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("Baz.name");

        // assert
        var field = Assert.IsType<ObjectField>(member);
        Assert.Equal("name", field.Name);
        Assert.Equal("Baz", field.DeclaringType.Name);
    }

    [Fact]
    public void GetMember_ObjectType_FieldArg()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("Baz.name(baz:)");

        // assert
        var arg = Assert.IsType<Argument>(member);
        Assert.Equal("baz", arg.Name);
        Assert.Equal("name", Assert.IsType<ObjectField>(arg.DeclaringMember).Name);
        Assert.Equal("Baz", arg.DeclaringType.Name);
    }

    [Fact]
    public void GetMember_Object_Invalid_FieldName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        void Fail() => schema.GetMember("Baz.foo");

        // assert
        Assert.Throws<InvalidSchemaCoordinateException>(Fail).Message.MatchSnapshot();
    }

    [Fact]
    public void GetMember_Object_Invalid_FieldArgName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        void Fail() => schema.GetMember("Baz.name(bar:)");

        // assert
        Assert.Throws<InvalidSchemaCoordinateException>(Fail).Message.MatchSnapshot();
    }

    [Fact]
    public void GetMember_InterfaceType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("Bar");

        // assert
        Assert.Equal("Bar", Assert.IsType<InterfaceType>(member).Name);
    }

    [Fact]
    public void GetMember_InterfaceType_Field()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("Bar.id");

        // assert
        var field = Assert.IsType<InterfaceField>(member);
        Assert.Equal("id", field.Name);
        Assert.Equal("Bar", field.DeclaringType.Name);
    }

    [Fact]
    public void GetMember_InterfaceType_FieldArg()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("Bar.name(baz:)");

        // assert
        var arg = Assert.IsType<Argument>(member);
        Assert.Equal("baz", arg.Name);
        Assert.Equal("name", Assert.IsType<InterfaceField>(arg.DeclaringMember).Name);
        Assert.Equal("Bar", arg.DeclaringType.Name);
    }

    [Fact]
    public void GetMember_Interface_Invalid_FieldName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        void Fail() => schema.GetMember("Bar.xyz");

        // assert
        Assert.Throws<InvalidSchemaCoordinateException>(Fail).Message.MatchSnapshot();
    }

    [Fact]
    public void GetMember_Interface_Invalid_FieldArgName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        void Fail() => schema.GetMember("Bar.name(bar:)");

        // assert
        Assert.Throws<InvalidSchemaCoordinateException>(Fail).Message.MatchSnapshot();
    }

    [Fact]
    public void GetMember_UnionType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("FooOrBaz");

        // assert
        Assert.Equal("FooOrBaz", Assert.IsType<UnionType>(member).Name);
    }

    [Fact]
    public void GetMember_UnionType_Invalid_MemberName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        void Fail() => schema.GetMember("FooOrBaz.Foo");

        // assert
        Assert.Throws<InvalidSchemaCoordinateException>(Fail).Message.MatchSnapshot();
    }

    [Fact]
    public void GetMember_InputObjectType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("BazInput");

        // assert
        Assert.Equal("BazInput", Assert.IsType<InputObjectType>(member).Name);
    }

    [Fact]
    public void GetMember_InputObjectType_Field()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("BazInput.name");

        // assert
        var argument = Assert.IsType<InputField>(member);
        Assert.Equal("name", argument.Name);
        Assert.Equal("BazInput", argument.DeclaringType.Name);
    }

    [Fact]
    public void GetMember_InputObjectType_Invalid_FieldName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        void Fail() => schema.GetMember("BazInput.abc");

        // assert
        Assert.Throws<InvalidSchemaCoordinateException>(Fail).Message.MatchSnapshot();
    }

    [Fact]
    public void GetMember_InputObjectType_Invalid_FieldArgName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        void Fail() => schema.GetMember("BazInput.name(a:)");

        // assert
        Assert.Throws<InvalidSchemaCoordinateException>(Fail).Message.MatchSnapshot();
    }

    [Fact]
    public void GetMember_EnumType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("Abc");

        // assert
        Assert.Equal("Abc", Assert.IsType<EnumType>(member).Name);
    }

    [Fact]
    public void GetMember_EnumType_Value()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("Abc.DEF");

        // assert
        Assert.Equal("DEF", Assert.IsType<EnumValue>(member).Name);
    }

    [Fact]
    public void GetMember_EnumType_Invalid_Value()
    {
        // arrange
        var schema = CreateSchema();

        // act
        void Fail() => schema.GetMember("Abc.XYZ");

        // assert
        Assert.Throws<InvalidSchemaCoordinateException>(Fail).Message.MatchSnapshot();
    }

    [Fact]
    public void GetMember_ScalarType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("String");

        // assert
        Assert.Equal("String", Assert.IsType<StringType>(member).Name);
    }

    [Fact]
    public void GetMember_DirectiveType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("@qux");

        // assert
        Assert.Equal("qux", Assert.IsType<DirectiveType>(member).Name);
    }

    [Fact]
    public void GetMember_DirectiveType_Argument()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var member = schema.GetMember("@qux(a:)");

        // assert
        var argument = Assert.IsType<DirectiveArgument>(member);
        Assert.Equal("a", argument.Name);
        Assert.Equal("qux", argument.DeclaringType.Name);
    }

    [Fact]
    public void GetMember_DirectiveType_Invalid_ArgumentName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        void Fail() => schema.GetMember("@qux(b:)");

        // assert
        Assert.Throws<InvalidSchemaCoordinateException>(Fail).Message.MatchSnapshot();
    }

    [Fact]
    public void GetMember_Invalid_TypeName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        void Fail() => schema.GetMember("Abc123");

        // assert
        Assert.Throws<InvalidSchemaCoordinateException>(Fail).Message.MatchSnapshot();
    }

    [Fact]
    public void GetMember_Invalid_DirectiveName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        void Fail() => schema.GetMember("@foo123");

        // assert
        Assert.Throws<InvalidSchemaCoordinateException>(Fail).Message.MatchSnapshot();
    }

    [Fact]
    public void TryGetMember_ObjectType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Baz", out var member);

        // assert
        Assert.True(success);
        Assert.Equal("Baz", Assert.IsType<ObjectType>(member).Name);
    }

    [Fact]
    public void TryGetMember_ObjectType_Field()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Baz.name", out var member);

        // assert
        Assert.True(success);
        var field = Assert.IsType<ObjectField>(member);
        Assert.Equal("name", field.Name);
        Assert.Equal("Baz", field.DeclaringType.Name);
    }

    [Fact]
    public void TryGetMember_ObjectType_FieldArg()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Baz.name(baz:)", out var member);

        // assert
        Assert.True(success);
        var arg = Assert.IsType<Argument>(member);
        Assert.Equal("baz", arg.Name);
        Assert.Equal("name", Assert.IsType<ObjectField>(arg.DeclaringMember).Name);
        Assert.Equal("Baz", arg.DeclaringType.Name);
    }

    [Fact]
    public void TryGetMember_Object_Invalid_FieldName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Baz.foo", out var member);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void TryGetMember_Object_Invalid_FieldArgName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Baz.name(bar:)", out var member);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void TryGetMember_InterfaceType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Bar", out var member);

        // assert
        Assert.True(success);
        Assert.Equal("Bar", Assert.IsType<InterfaceType>(member).Name);
    }

    [Fact]
    public void TryGetMember_InterfaceType_Field()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Bar.id", out var member);

        // assert
        Assert.True(success);
        var field = Assert.IsType<InterfaceField>(member);
        Assert.Equal("id", field.Name);
        Assert.Equal("Bar", field.DeclaringType.Name);
    }

    [Fact]
    public void TryGetMember_InterfaceType_FieldArg()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Bar.name(baz:)", out var member);

        // assert
        Assert.True(success);
        var arg = Assert.IsType<Argument>(member);
        Assert.Equal("baz", arg.Name);
        Assert.Equal("name", Assert.IsType<InterfaceField>(arg.DeclaringMember).Name);
        Assert.Equal("Bar", arg.DeclaringType.Name);
    }

    [Fact]
    public void TryGetMember_Interface_Invalid_FieldName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Baz.xyz", out var member);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void TryGetMember_Interface_Invalid_FieldArgName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Bar.name(bar:)", out var member);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void TryGetMember_UnionType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("FooOrBaz", out var member);

        // assert
        Assert.True(success);
    }

    [Fact]
    public void TryGetMember_UnionType_Invalid_MemberName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("FooOrBaz.Foo", out var member);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void TryGetMember_InputObjectType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("BazInput", out var member);

        // assert
        Assert.True(success);
        Assert.Equal("BazInput", Assert.IsType<InputObjectType>(member).Name);
    }

    [Fact]
    public void TryGetMember_InputObjectType_Field()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("BazInput.name", out var member);

        // assert
        Assert.True(success);
        var argument = Assert.IsType<InputField>(member);
        Assert.Equal("name", argument.Name);
        Assert.Equal("BazInput", argument.DeclaringType.Name);
    }

    [Fact]
    public void TryGetMember_InputObjectType_Invalid_FieldName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("BazInput.abc", out var member);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void TryGetMember_InputObjectType_Invalid_FieldArgName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("BazInput.name(a:)", out var member);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void TryGetMember_EnumType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Abc", out var member);

        // assert
        Assert.True(success);
        Assert.Equal("Abc", Assert.IsType<EnumType>(member).Name);
    }

    [Fact]
    public void TryGetMember_EnumType_Value()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Abc.DEF", out var member);

        // assert
        Assert.True(success);
        Assert.Equal("DEF", Assert.IsType<EnumValue>(member).Name);
    }

    [Fact]
    public void TryGetMember_EnumType_Invalid_Value()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Abc.XYZ", out var member);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void TryGetMember_ScalarType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("String", out var member);

        // assert
        Assert.True(success);
        Assert.Equal("String", Assert.IsType<StringType>(member).Name);
    }

    [Fact]
    public void TryGetMember_DirectiveType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("@qux", out var member);

        // assert
        Assert.True(success);
        Assert.Equal("qux", Assert.IsType<DirectiveType>(member).Name);
    }

    [Fact]
    public void TryGetMember_DirectiveType_Argument()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("@qux(a:)", out var member);

        // assert
        Assert.True(success);
        var argument = Assert.IsType<DirectiveArgument>(member);
        Assert.Equal("a", argument.Name);
        Assert.Equal("qux", argument.DeclaringType.Name);
    }

    [Fact]
    public void TryGetMember_DirectiveType_Invalid_ArgumentName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("@qux(b:)", out var member);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void TryGetMember_Invalid_TypeName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("Abc123", out var member);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void TryGetMember_Invalid_DirectiveName()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var success = schema.TryGetMember("@abc", out var member);

        // assert
        Assert.False(success);
    }

    private ISchema CreateSchema()
    {
        return SchemaBuilder.New()
            .AddDocumentFromString(FileResource.Open("schema_coordinates.graphql"))
            .ModifyOptions(
                o =>
                {
                    o.StrictValidation = false;
                    o.RemoveUnreachableTypes = false;
                    o.RemoveUnusedTypeSystemDirectives = false;
                })
            .Use(_ => _ => default)
            .Create();
    }
}
