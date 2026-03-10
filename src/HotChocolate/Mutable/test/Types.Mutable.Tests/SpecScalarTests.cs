namespace HotChocolate.Types.Mutable;

public class SpecScalarTests
{
    [Fact]
    public void CreateStringTypeDefinition()
    {
        var type = BuiltIns.String.Create();
        Assert.Equal(SpecScalarNames.String.Name, type.Name);
        Assert.True(type.IsSpecScalar);
    }

    [Fact]
    public void CreateBooleanTypeDefinition()
    {
        var type = BuiltIns.Boolean.Create();
        Assert.Equal(SpecScalarNames.Boolean.Name, type.Name);
        Assert.True(type.IsSpecScalar);
    }

    [Fact]
    public void CreateFloatTypeDefinition()
    {
        var type = BuiltIns.Float.Create();
        Assert.Equal(SpecScalarNames.Float.Name, type.Name);
        Assert.True(type.IsSpecScalar);
    }

    [Fact]
    public void CreateIDTypeDefinition()
    {
        var type = BuiltIns.ID.Create();
        Assert.Equal(SpecScalarNames.ID.Name, type.Name);
        Assert.True(type.IsSpecScalar);
    }

    [Fact]
    public void CreateIntTypeDefinition()
    {
        var type = BuiltIns.Int.Create();
        Assert.Equal(SpecScalarNames.Int.Name, type.Name);
        Assert.True(type.IsSpecScalar);
    }
}
