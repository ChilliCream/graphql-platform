using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLParser
{
    private static readonly List<DirectiveNode> _emptyDirectives = new();

    private DirectiveDefinitionNode ParseDirectiveDefinition()
    {
        TokenInfo start = Start();

        StringValueNode? description = ParseDescription();

        ExpectDirectiveKeyword();
        ExpectAt();

        NameNode name = ParseName();
        List<InputValueDefinitionNode> arguments =
            ParseArgumentDefinitions();

        var isRepeatable = SkipRepeatableKeyword();
        ExpectOnKeyword();

        List<NameNode> locations = ParseDirectiveLocations();

        Location? location = CreateLocation(in start);

        return new DirectiveDefinitionNode
        (
            location,
            name,
            description,
            isRepeatable,
            arguments,
            locations
        );
    }

    private List<NameNode> ParseDirectiveLocations()
    {
        var list = new List<NameNode>();

        // skip optional leading pipe.
        SkipPipe();

        do
        {
            list.Add(ParseDirectiveLocation());
        }
        while (SkipPipe());

        return list;
    }

    private NameNode ParseDirectiveLocation()
    {
        TokenKind kind = _reader.Kind;
        NameNode name = ParseName();

        if (DirectiveLocation.IsValidName(name.Value))
        {
            return name;
        }

        throw Unexpected(kind);
    }

    private List<DirectiveNode> ParseDirectives(bool isConstant, bool isQueryLocation = false)
    {
        if (_reader.Kind == TokenKind.At)
        {
            var list = new List<DirectiveNode>();

            while (_reader.Kind == TokenKind.At)
            {
                list.Add(ParseDirective(isConstant));

                if (isQueryLocation && list.Count > _maxAllowedDirectives)
                {
                    throw new SyntaxException(
                        _reader,
                        string.Format(
                            Utf8GraphQLParser_ParseDirective_MaxAllowedDirectivesReached,
                            _maxAllowedDirectives));
                }
            }

            return list;
        }

        return _emptyDirectives;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DirectiveNode ParseDirective(bool isConstant)
    {
        TokenInfo start = Start();

        ExpectAt();
        NameNode name = ParseName();
        List<ArgumentNode> arguments = ParseArguments(isConstant);

        Location? location = CreateLocation(in start);

        return new DirectiveNode
        (
            location,
            name,
            arguments
        );
    }
}
