using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language;

public ref partial struct Utf8GraphQLParser
{
    private static readonly List<DirectiveNode> _emptyDirectives = [];

    private DirectiveDefinitionNode ParseDirectiveDefinition()
    {
        var start = Start();

        ExpectDirectiveKeyword();
        ExpectAt();

        var name = ParseName();
        var arguments =
            ParseArgumentDefinitions();

        var isRepeatable = SkipRepeatableKeyword();
        ExpectOnKeyword();

        var locations = ParseDirectiveLocations();

        var location = CreateLocation(in start);

        return new DirectiveDefinitionNode
        (
            location,
            name,
            TakeDescription(),
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
        var kind = _reader.Kind;
        var name = ParseName();

        if (DirectiveLocation.IsValidName(name.Value))
        {
            return name;
        }

        throw Unexpected(kind);
    }

    private List<DirectiveNode> ParseDirectives(bool isConstant)
    {
        if (_reader.Kind == TokenKind.At)
        {
            var list = new List<DirectiveNode>();

            while (_reader.Kind == TokenKind.At)
            {
                list.Add(ParseDirective(isConstant));
            }

            return list;
        }

        return _emptyDirectives;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DirectiveNode ParseDirective(bool isConstant)
    {
        var start = Start();

        ExpectAt();
        var name = ParseName();
        var arguments = ParseArguments(isConstant);

        var location = CreateLocation(in start);

        return new DirectiveNode
        (
            location,
            name,
            arguments
        );
    }
}
