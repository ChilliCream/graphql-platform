using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public class Issue9760Probe
{
    public required string Id { get; set; }
}

[ObjectType<Issue9760Probe>]
[Issue9760Prefix("dup")]
public static partial class Issue9760ProbeTypeOne
{
    public static string FieldOne() => "one";

    static partial void Configure(IObjectTypeDescriptor<Issue9760Probe> descriptor)
    {
        descriptor.Field("configuredOne").Type<NonNullType<StringType>>().Resolve("from-one");
    }
}

[ObjectType<Issue9760Probe>]
[Issue9760Prefix("dup")]
public static partial class Issue9760ProbeTypeTwo
{
    public static string FieldTwo() => "two";

    static partial void Configure(IObjectTypeDescriptor<Issue9760Probe> descriptor)
    {
        descriptor.Field("configuredTwo").Type<NonNullType<StringType>>().Resolve("from-two");
    }
}

public class Issue9760MultiValueProbe
{
    public required string Id { get; set; }
}

[ObjectType<Issue9760MultiValueProbe>]
[Issue9760Prefix("first")]
public static partial class Issue9760MultiValueProbeTypeOne
{
    public static string FieldOne() => "one";
}

[ObjectType<Issue9760MultiValueProbe>]
[Issue9760Prefix("second")]
public static partial class Issue9760MultiValueProbeTypeTwo
{
    public static string FieldTwo() => "two";
}

public sealed class Issue9760PrefixAttribute(string prefix) : ObjectTypeDescriptorAttribute
{
    public string Prefix { get; } = prefix;

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectTypeDescriptor descriptor,
        Type? type)
    {
        if (type is null)
        {
            return;
        }

        var capturedPrefix = Prefix;
        descriptor
            .Extend()
            .OnBeforeNaming((_, cfg) => cfg.Name = $"{capturedPrefix}_{cfg.Name}");
    }
}
