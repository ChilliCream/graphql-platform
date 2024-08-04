using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Directives;

internal static class RequiredDirectiveParser
{
    public static Requirement Parse(DirectiveNode directiveNode)
    {
        string? schemaName = null;
        ImmutableArray<RequiredArgument>? arguments = null;
        ImmutableArray<RequiredField>? fields = null;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case "schema":
                    schemaName = ((EnumValueNode)argument.Value).Value;
                    break;

                case "arguments":
                    arguments = ParseArguments(argument.Value);
                    break;

                case "fields":
                    fields = ParseFields(argument.Value);
                    break;

                default:
                    throw new DirectiveParserException(
                        $"The argument `{argument.Name.Value}` is not supported on @require.");
            }
        }

        if (string.IsNullOrEmpty(schemaName))
        {
            throw new DirectiveParserException(
                "The `schema` argument is required on the @require directive.");
        }

        if (arguments is null)
        {
            throw new DirectiveParserException(
                "The `arguments` argument is required on the @require directive.");
        }

        if (fields is null)
        {
            throw new DirectiveParserException(
                "The `fields` argument is required on the @require directive.");
        }

        return new Requirement(schemaName, arguments.Value, fields.Value);
    }

    private static ImmutableArray<RequiredArgument> ParseArguments(IValueNode value)
    {
        var fieldDefinition = Utf8GraphQLParser.Syntax.ParseFieldDefinition(((StringValueNode)value).Value);

        var arguments = ImmutableArray.CreateBuilder<RequiredArgument>();

        foreach (var argument in fieldDefinition.Arguments)
        {
            arguments.Add(new RequiredArgument(argument.Name.Value, argument.Type));
        }

        return arguments.ToImmutable();
    }

    private static ImmutableArray<RequiredField> ParseFields(IValueNode value)
    {
        if (value is ListValueNode listValue)
        {
            var fields = ImmutableArray.CreateBuilder<RequiredField>();

            foreach (var item in listValue.Items)
            {
                fields.Add(RequiredField.Parse(((StringValueNode)item).Value));
            }

            return fields.ToImmutable();
        }

        if (value is StringValueNode stringValue)
        {
            return ImmutableArray<RequiredField>.Empty.Add(RequiredField.Parse(stringValue.Value));
        }

        throw new DirectiveParserException(
            "The value is expected to be a list of strings or a string.");
    }
}
