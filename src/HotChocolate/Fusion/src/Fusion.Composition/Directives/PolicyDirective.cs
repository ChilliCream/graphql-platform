using HotChocolate.Language;
using HotChocolate.Types;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Directives;

internal sealed class PolicyDirective(string name, string onDenied)
{
    public string Name { get; } = name;

    public string OnDenied { get; } = onDenied;

    public static PolicyDirective From(IDirective directive)
    {
        string? name = null;
        var onDenied = "NULL";

        if (directive.Arguments.TryGetValue(ArgumentNames.Name, out var nameArg))
        {
            name = nameArg switch
            {
                StringValueNode stringValueNode => stringValueNode.Value,
                _ => throw new InvalidOperationException(
                    "The `name` argument on @policy must be a string.")
            };
        }

        if (name is null)
        {
            throw new InvalidOperationException(
                "The `name` argument is required on the @policy directive.");
        }

        if (directive.Arguments.TryGetValue(ArgumentNames.OnDenied, out var onDeniedArg))
        {
            onDenied = onDeniedArg switch
            {
                EnumValueNode enumValueNode => GetOnDenied(enumValueNode.Value),
                _ => throw new InvalidOperationException(
                    "The `onDenied` argument on @policy must be an enum value.")
            };
        }

        return new PolicyDirective(name, onDenied);
    }

    private static string GetOnDenied(string onDeniedValue)
    {
        return onDeniedValue switch
        {
            "NULL" => "NULL",
            "ERROR" => "ERROR",
            "ABORT" => "ABORT",
            _ => throw new InvalidOperationException(
                $"The value `{onDeniedValue}` is not supported by @policy onDenied.")
        };
    }
}
