namespace HotChocolate.Resolvers;

public class FieldReferenceTests
{
    [Fact]
    public void Create()
    {
        // arrange
        var typeName = TestUtils.CreateTypeName();
        var fieldName = TestUtils.CreateFieldName();

        // act
        var fieldReference = new FieldReference(typeName, fieldName);

        // assert
        Assert.Equal(typeName, fieldReference.TypeName);
        Assert.Equal(fieldName, fieldReference.FieldName);
    }

    [Fact]
    public void CreateTypeNull()
    {
        // arrange
        var fieldName = TestUtils.CreateFieldName();

        // act
        Action action = () => new FieldReference(null, fieldName);

        // assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void CreateFieldNull()
    {
        // arrange
        var typeName = TestUtils.CreateTypeName();

        // act
        Action action = () => new FieldReference(typeName, null);

        // assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void WithTypeName()
    {
        // arrange
        var originalTypeName = TestUtils.CreateTypeName();
        var newTypeName = TestUtils.CreateTypeName();
        var fieldName = TestUtils.CreateFieldName();
        var fieldReference = new FieldReference(
            originalTypeName, fieldName);

        // act
        fieldReference = fieldReference.WithTypeName(newTypeName);

        // assert
        Assert.Equal(newTypeName, fieldReference.TypeName);
    }

    [Fact]
    public void WithTypeNameNull()
    {
        // arrange
        var originalTypeName = TestUtils.CreateTypeName();
        var fieldName = TestUtils.CreateFieldName();
        var fieldReference = new FieldReference(
            originalTypeName, fieldName);

        // act
        Action action = () => fieldReference.WithTypeName(null);

        // assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void WithFieldName()
    {
        // arrange
        var typeName = TestUtils.CreateTypeName();
        var originalFieldName = TestUtils.CreateFieldName();
        var newFieldName = TestUtils.CreateFieldName();
        var fieldReference = new FieldReference(
            typeName, originalFieldName);

        // act
        fieldReference = fieldReference.WithFieldName(newFieldName);

        // assert
        Assert.Equal(newFieldName, fieldReference.FieldName);
    }

    [Fact]
    public void WithFieldNameNull()
    {
        // arrange
        var typeName = TestUtils.CreateTypeName();
        var originalFieldName = TestUtils.CreateFieldName();
        var fieldReference = new FieldReference(
            typeName, originalFieldName);

        // act
        Action action = () => fieldReference.WithFieldName(null);

        // assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void EqualsObjectNull()
    {
        // arrange
        var fieldReference = new FieldReference(
            TestUtils.CreateTypeName(),
            TestUtils.CreateFieldName());

        // act
        var result = fieldReference.Equals(default(object));

        // assert
        Assert.False(result);
    }

    [Fact]
    public void EqualsObjectReferenceEquals()
    {
        // arrange
        var fieldReference = new FieldReference(
            TestUtils.CreateTypeName(),
            TestUtils.CreateFieldName());

        // act
        var result = fieldReference.Equals((object)fieldReference);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void EqualsObjectFieldsAreEqual()
    {
        // arrange
        var fieldReference_a = new FieldReference(
            TestUtils.CreateTypeName(),
            TestUtils.CreateFieldName());

        var fieldReference_b = new FieldReference(
            fieldReference_a.TypeName,
            fieldReference_a.FieldName);

        // act
        var result = fieldReference_a.Equals((object)fieldReference_b);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void EqualsObjectWithIncompatibleType()
    {
        // arrange
        var fieldReference = new FieldReference(
            TestUtils.CreateTypeName(),
            TestUtils.CreateFieldName());

        // act
        var result = fieldReference.Equals(new object());

        // assert
        Assert.False(result);
    }

    [Fact]
    public void EqualsObjectTypeNotEqual()
    {
        // arrange
        var fieldReference_a = new FieldReference(
            TestUtils.CreateTypeName(),
            TestUtils.CreateFieldName());

        var fieldReference_b = new FieldReference(
            TestUtils.CreateTypeName(),
            fieldReference_a.FieldName);

        // act
        var result = fieldReference_a.Equals((object)fieldReference_b);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void EqualsObjectFieldNotEqual()
    {
        // arrange
        var fieldReference_a = new FieldReference(
            TestUtils.CreateTypeName(),
            TestUtils.CreateFieldName());

        var fieldReference_b = new FieldReference(
            fieldReference_a.TypeName,
            TestUtils.CreateFieldName());

        // act
        var result = fieldReference_a.Equals((object)fieldReference_b);

        // assert
        Assert.False(result);
    }
}
