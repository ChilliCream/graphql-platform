using System.Globalization;
using static HotChocolate.Language.Properties.LangUtf8Resources;

namespace HotChocolate.Language;

// Implements the parsing rules in the Operations section.
public ref partial struct Utf8GraphQLParser
{
    private static readonly List<VariableDefinitionNode> _emptyVariableDefinitions = [];
    private static readonly List<ArgumentNode> _emptyArguments = [];

    /// <summary>
    /// Parses an operation definition.
    /// <see cref="OperationDefinitionNode" />:
    /// OperationType? OperationName? ($x : Type = DefaultValue?)? SelectionSet
    /// </summary>
    private OperationDefinitionNode ParseOperationDefinition()
    {
        var start = Start();

        var operation = ParseOperationType();
        var name = _reader.Kind == TokenKind.Name ? ParseName() : null;
        var variableDefinitions = ParseVariableDefinitions();
        var directives = ParseDirectives(false);
        var selectionSet = ParseSelectionSet();
        var location = CreateLocation(in start);

        return new OperationDefinitionNode(
            location,
            name,
            operation,
            variableDefinitions,
            directives,
            selectionSet);
    }

    /// <summary>
    /// Parses a shorthand form operation definition.
    /// <see cref="OperationDefinitionNode" />:
    /// SelectionSet
    /// </summary>
    private OperationDefinitionNode ParseShortOperationDefinition()
    {
        var start = Start();
        var selectionSet = ParseSelectionSet();
        var location = CreateLocation(in start);

        return new OperationDefinitionNode(
            location,
            name: null,
            OperationType.Query,
            Array.Empty<VariableDefinitionNode>(),
            Array.Empty<DirectiveNode>(),
            selectionSet);
    }

    /// <summary>
    /// Parses the <see cref="OperationType" />.
    /// </summary>
    private OperationType ParseOperationType()
    {
        if (_reader.Kind == TokenKind.Name)
        {
            if (_reader.Value.SequenceEqual(GraphQLKeywords.Query))
            {
                MoveNext();
                return OperationType.Query;
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Mutation))
            {
                MoveNext();
                return OperationType.Mutation;
            }

            if (_reader.Value.SequenceEqual(GraphQLKeywords.Subscription))
            {
                MoveNext();
                return OperationType.Subscription;
            }
        }

        throw Unexpected(TokenKind.Name);
    }

    /// <summary>
    /// Parses variable definitions.
    /// <see cref="IEnumerable{VariableDefinitionNode}" />:
    /// ( VariableDefinition+ )
    /// </summary>
    private List<VariableDefinitionNode> ParseVariableDefinitions()
    {
        if (_reader.Kind == TokenKind.LeftParenthesis)
        {
            var list = new List<VariableDefinitionNode>();

            // skip opening token
            MoveNext();

            while (_reader.Kind != TokenKind.RightParenthesis)
            {
                list.Add(ParseVariableDefinition());
            }

            // skip closing token
            ExpectRightParenthesis();

            return list;
        }

        return _emptyVariableDefinitions;
    }

    /// <summary>
    /// Parses a variable definition.
    /// <see cref="VariableDefinitionNode" />:
    /// $variable : Type = DefaultValue?
    /// </summary>
    private VariableDefinitionNode ParseVariableDefinition()
    {
        var start = Start();

        var variable = ParseVariable();
        ExpectColon();
        var type = ParseTypeReference();
        var defaultValue = SkipEqual()
            ? ParseValueLiteral(true)
            : null;
        var directives =
            ParseDirectives(isConstant: true);

        var location = CreateLocation(in start);

        return new VariableDefinitionNode(
            location,
            variable,
            type,
            defaultValue,
            directives);
    }

    /// <summary>
    /// Parse a variable.
    /// <see cref="VariableNode" />:
    /// $Name
    /// </summary>
    private VariableNode ParseVariable()
    {
        var start = Start();
        ExpectDollar();
        var name = ParseName();
        var location = CreateLocation(in start);

        return new VariableNode(
            location,
            name);
    }

    /// <summary>
    /// Parses a selection set.
    /// <see cref="SelectionSetNode" />:
    /// { Selection+ }
    /// </summary>
    private SelectionSetNode ParseSelectionSet()
    {
        var start = Start();

        if (_reader.Kind != TokenKind.LeftBrace)
        {
            throw new SyntaxException(_reader,
                string.Format(
                    CultureInfo.InvariantCulture,
                    ParseMany_InvalidOpenToken,
                    TokenKind.LeftBrace,
                    TokenPrinter.Print(ref _reader)));
        }

        var selections = new List<ISelectionNode>();

        // skip opening token
        MoveNext();

        while (_reader.Kind != TokenKind.RightBrace
            && _reader.Kind != TokenKind.EndOfFile)
        {
            selections.Add(ParseSelection());
        }

        // skip closing token
        ExpectRightBrace();

        var location = CreateLocation(in start);

        return new SelectionSetNode(
            location,
            selections);
    }

    /// <summary>
    /// Parses a selection.
    /// <see cref="ISelectionNode" />:
    /// - Field
    /// - FragmentSpread
    /// - InlineFragment
    /// </summary>
    private ISelectionNode ParseSelection()
    {
        if (_reader.Kind == TokenKind.Spread)
        {
            return ParseFragment();
        }
        return ParseField();
    }

    /// <summary>
    /// Parses a field.
    /// <see cref="FieldNode"  />:
    /// Alias? : Name Arguments? Directives? SelectionSet?
    /// </summary>
    private FieldNode ParseField()
    {
        if (++_parsedFields > _maxAllowedFields)
        {
            throw new SyntaxException(
                _reader,
                string.Format(
                    Utf8GraphQLParser_Start_MaxAllowedFieldsReached,
                    _maxAllowedFields));
        }

        var start = Start();

        var name = ParseName();
        NameNode? alias = null;

        if (SkipColon())
        {
            alias = name;
            name = ParseName();
        }

        var arguments = ParseArguments(false);
        var directives = ParseDirectives(false);
        var selectionSet = _reader.Kind == TokenKind.LeftBrace
            ? ParseSelectionSet()
            : null;

        var location = CreateLocation(in start);

        return new FieldNode(
            location,
            name,
            alias,
            directives,
            arguments,
            selectionSet);
    }

    /// <summary>
    /// Parses an argument.
    /// <see cref="ArgumentNode" />:
    /// Name : Value[isConstant]
    /// </summary>
    private List<ArgumentNode> ParseArguments(bool isConstant)
    {
        if (_reader.Kind == TokenKind.LeftParenthesis)
        {
            var list = new List<ArgumentNode>();

            // skip opening token
            MoveNext();

            while (_reader.Kind != TokenKind.RightParenthesis)
            {
                list.Add(ParseArgument(isConstant));
            }

            // skip closing token
            ExpectRightParenthesis();

            return list;
        }
        return _emptyArguments;
    }

    /// <summary>
    /// Parses an argument.
    /// <see cref="ArgumentNode" />:
    /// Name : Value[isConstant]
    /// </summary>
    private ArgumentNode ParseArgument(bool isConstant)
    {
        var start = Start();

        var name = ParseName();
        ExpectColon();
        var value = ParseValueLiteral(isConstant);

        var location = CreateLocation(in start);

        return new ArgumentNode(
            location,
            name,
            value);
    }
}
