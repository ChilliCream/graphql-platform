using System.Diagnostics;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

[DirectiveType(
    DirectiveNames.SerializeAs.Name,
    DirectiveLocation.Scalar,
    IsRepeatable = false)]
[Serialization]
public class SerializeAs
{
    public SerializeAs(ScalarSerializationType type, string? pattern = null)
    {
        if (type is ScalarSerializationType.Undefined)
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "The type is undefined.");
        }

        if ((ScalarSerializationType.String & type) != ScalarSerializationType.String
            && !string.IsNullOrEmpty(pattern))
        {
            throw new ArgumentException(
                "A pattern can only be specified when the scalar serializes as string.",
                nameof(pattern));
        }

        Type = type;
        Pattern = pattern;
    }

    [GraphQLName(DirectiveNames.SerializeAs.Arguments.Type)]
    [GraphQLDescription("The primitive type a scalar is serialized to.")]
    [GraphQLType<NonNullType<ListType<NonNullType<EnumType<ScalarSerializationType>>>>>]
    public ScalarSerializationType Type { get; }

    [GraphQLName(DirectiveNames.SerializeAs.Arguments.Pattern)]
    [GraphQLDescription("The ECMA-262 regex pattern that the serialized scalar value conforms to.")]
    [GraphQLType<StringType>]
    public string? Pattern { get; }
}

file sealed class SerializationAttribute : DirectiveTypeDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IDirectiveTypeDescriptor descriptor,
        Type? type)
    {
        descriptor.ExtendWith(static extension =>
        {
            var map = new Dictionary<ScalarSerializationType, EnumValueNode>();

            foreach (var possibleValue in Enum.GetValues<ScalarSerializationType>())
            {
                if (possibleValue is ScalarSerializationType.Undefined)
                {
                    continue;
                }

                map.Add(possibleValue, new EnumValueNode(extension.Context.Naming.GetEnumValueName(possibleValue)));
            }

            extension.Configuration.Format = directive =>
            {
                var serializeAs = (SerializeAs)directive;
                using var types = GetSetTypes(serializeAs.Type).GetEnumerator();

                IValueNode? typeArg = null;
                List<EnumValueNode>? listValue = null;

                while (types.MoveNext())
                {
                    if (listValue is null && typeArg is null)
                    {
                        typeArg = map[types.Current];
                    }
                    else if (typeArg is not null && listValue is null)
                    {
                        listValue = [(EnumValueNode)typeArg, map[types.Current]];
                    }
                    else
                    {
                        listValue?.Add(map[types.Current]);
                    }
                }

                if (listValue is null && typeArg is null)
                {
                    throw new InvalidOperationException("The @serializeAs directive has an invalid state.");
                }

                if (typeArg is null)
                {
                    Debug.Assert(listValue is not null);
                    typeArg = new ListValueNode(listValue);
                }

                var patternArg = string.IsNullOrEmpty(serializeAs.Pattern)
                    ? (IValueNode)NullValueNode.Default
                    : new StringValueNode(serializeAs.Pattern);

                return new DirectiveNode(
                    DirectiveNames.SerializeAs.Name,
                    new ArgumentNode(DirectiveNames.SerializeAs.Arguments.Type, typeArg),
                    new ArgumentNode(DirectiveNames.SerializeAs.Arguments.Pattern, patternArg));
            };

            extension.Configuration.Parse = static directiveNode =>
            {
                var type = ScalarSerializationType.Undefined;
                string? pattern = null;

                var typeArg = directiveNode.Arguments.FirstOrDefault(
                    t => t.Name.Value.Equals(DirectiveNames.SerializeAs.Arguments.Type));
                var patternArg = directiveNode.Arguments.FirstOrDefault(
                    t => t.Name.Value.Equals(DirectiveNames.SerializeAs.Arguments.Pattern));

                switch (typeArg?.Value)
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
                        throw new InvalidOperationException(
                            "Cannot parse the @serializeAs directive as it is missing the type argument.");
                }

                if (patternArg?.Value is StringValueNode patterValue)
                {
                    pattern = patterValue.Value;
                }

                return new SerializeAs(type, pattern);
            };
        });
    }

    public static IEnumerable<ScalarSerializationType> GetSetTypes(
        ScalarSerializationType value)
    {
        var intValue = (int)value;

        for (var bit = 1; bit <= 32; bit <<= 1)
        {
            if ((intValue & bit) != 0)
            {
                yield return (ScalarSerializationType)bit;
            }
        }
    }
}
