namespace HotChocolate.Types.Mutable;

public class SpecScalarTests
{
    [Fact]
    public void CreateStringTypeDefinition()
    {
        var type = BuiltIns.String.Create();
        Assert.Equal(BuiltIns.String.Name, type.Name);
        Assert.True(type.IsSpecScalar);
    }

    [Fact]
    public void CreateBooleanTypeDefinition()
    {
        var type = BuiltIns.Boolean.Create();
        Assert.Equal(BuiltIns.Boolean.Name, type.Name);
        Assert.True(type.IsSpecScalar);
    }

    [Fact]
    public void CreateFloatTypeDefinition()
    {
        var type = BuiltIns.Float.Create();
        Assert.Equal(BuiltIns.Float.Name, type.Name);
        Assert.True(type.IsSpecScalar);
    }

    [Fact]
    public void CreateIDTypeDefinition()
    {
        var type = BuiltIns.ID.Create();
        Assert.Equal(BuiltIns.ID.Name, type.Name);
        Assert.True(type.IsSpecScalar);
    }

    [Fact]
    public void CreateIntTypeDefinition()
    {
        var type = BuiltIns.Int.Create();
        Assert.Equal(BuiltIns.Int.Name, type.Name);
        Assert.True(type.IsSpecScalar);
    }
}
