using HotChocolate.Language;
using ArgumentNames = HotChocolate.Types.DirectiveNames.SerializeAs.Arguments;

namespace HotChocolate.Types.Mutable.Directives;

public sealed class SerializeAsDirective(ScalarSerializationType type, string? pattern)
{
    public ScalarSerializationType Type { get; } = type;

    public string? Pattern { get; } = pattern;

    public static SerializeAsDirective From(IDirective directive)
    {
        var typeArg = directive.Arguments[ArgumentNames.Type];
        var type = ScalarSerializationType.Undefined;

        switch (typeArg)
        {
            case ListValueNode typeList
                when typeList.Items.All(t => t.Kind is SyntaxKind.EnumValue):
                foreach (var item in typeList.Items)
                {
                    var value = (EnumValueNode)item;
                    if (Enum.TryParse<ScalarSerializationType>(
                        value.Value,
                        ignoreCase: true,
                        out var parsedType))
                    {
                        type |= parsedType;
                    }
                }

                break;

            case EnumValueNode singleType
                when Enum.TryParse<ScalarSerializationType>(
                    singleType.Value,
                    ignoreCase: true,
                    out var parsedType):
                type = parsedType;
                break;

            default:
                throw new InvalidOperationException();
        }

        var pattern =
            ((StringValueNode?)directive.Arguments.GetValueOrDefault(ArgumentNames.Pattern))?.ToString();

        return new SerializeAsDirective(type, pattern);
    }
}
