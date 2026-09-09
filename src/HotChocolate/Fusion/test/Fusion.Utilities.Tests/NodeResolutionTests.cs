namespace HotChocolate.Fusion;

public sealed class NodeResolutionTests
{
    [Fact]
    public void Router_Should_EqualZero()
    {
        Assert.Equal(0, (int)NodeResolution.Router);
    }

    [Fact]
    public void SourceSchema_Should_EqualOne()
    {
        Assert.Equal(1, (int)NodeResolution.SourceSchema);
    }

    [Fact]
    public void Gateway_Should_AliasRouter()
    {
        var field = typeof(NodeResolution).GetField("Gateway");

        Assert.NotNull(field);
        Assert.Equal(NodeResolution.Router, (NodeResolution)field.GetValue(null)!);
        Assert.True(Enum.IsDefined(typeof(NodeResolution), "Gateway"));
        Assert.True(Enum.IsDefined(typeof(NodeResolution), "Router"));
    }

    [Fact]
    public void Gateway_Should_BeObsoleteWithWarning_When_Inspected()
    {
        var attribute = typeof(NodeResolution)
            .GetField("Gateway")!
            .GetCustomAttributes(typeof(System.ObsoleteAttribute), inherit: false)
            .OfType<System.ObsoleteAttribute>()
            .Single();

        Assert.False(attribute.IsError);
        Assert.Contains("Router", attribute.Message);
    }
}
