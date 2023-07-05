using System.Reflection;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration;

public class BuiltinScalarsTests
{
    [Fact]
    public void Builtin_ScalarNames_Count()
    {
        // arrange
        var builtinTypesField =
            typeof(BuiltInScalarNames).GetField("_typeNames", BindingFlags.NonPublic | BindingFlags.Static)!;

        // act
        var builtinTypes = (HashSet<string>)builtinTypesField.GetValue(null)!;

        // assert
        Assert.Equal(24, builtinTypes.Count);
    }

    [Theory]
    [InlineData(ScalarNames.String)]
    [InlineData(ScalarNames.ID)]
    [InlineData(ScalarNames.Boolean)]
    [InlineData(ScalarNames.Byte)]
    [InlineData(ScalarNames.Short)]
    [InlineData(ScalarNames.Int)]
    [InlineData(ScalarNames.Long)]
    [InlineData(ScalarNames.Float)]
    [InlineData(ScalarNames.Decimal)]
    [InlineData(ScalarNames.URL)]
    [InlineData("Url")]
    [InlineData("URI")]
    [InlineData("Uri")]
    [InlineData(ScalarNames.UUID)]
    [InlineData("Uuid")]
    [InlineData("Guid")]
    [InlineData(ScalarNames.DateTime)]
    [InlineData(ScalarNames.Date)]
    [InlineData(ScalarNames.MultiplierPath)]
    [InlineData(ScalarNames.Name)]
    [InlineData(ScalarNames.ByteArray)]
    [InlineData(ScalarNames.Any)]
    [InlineData(ScalarNames.TimeSpan)]
    [InlineData(ScalarNames.JSON)]
    public void Scalar_Is_Builtin(string typeName)
    {
        // arrange
        // act
        // assert
        Assert.True(BuiltInScalarNames.IsBuiltInScalar(typeName));
    }

    [Fact]
    public void Scalar_Is_Not_Builtin()
    {
        // arrange
        // act
        // assert
        Assert.False(BuiltInScalarNames.IsBuiltInScalar("Foo"));
    }
}
