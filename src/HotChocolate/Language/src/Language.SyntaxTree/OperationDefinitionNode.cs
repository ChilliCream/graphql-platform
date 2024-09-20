using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class OperationDefinitionNode
    : IExecutableDefinitionNode
    , IHasDirectives
{
    public OperationDefinitionNode(
        Location? location,
        NameNode? name,
        OperationType operation,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        IReadOnlyList<DirectiveNode> directives,
        SelectionSetNode selectionSet)
    {
        Location = location;
        Name = name;
        Operation = operation;
        VariableDefinitions = variableDefinitions
            ?? throw new ArgumentNullException(nameof(variableDefinitions));
        Directives = directives
            ?? throw new ArgumentNullException(nameof(directives));
        SelectionSet = selectionSet
            ?? throw new ArgumentNullException(nameof(selectionSet));
    }

    public SyntaxKind Kind => SyntaxKind.OperationDefinition;

    public Location? Location { get; }

    public NameNode? Name { get; }

    public OperationType Operation { get; }

    public IReadOnlyList<VariableDefinitionNode> VariableDefinitions { get; }

    public IReadOnlyList<DirectiveNode> Directives { get; }

    public SelectionSetNode SelectionSet { get; }

    public IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Name is { })
        {
            yield return Name;
        }

        foreach (var variable in VariableDefinitions)
        {
            yield return variable;
        }

        foreach (var directive in Directives)
        {
            yield return directive;
        }

        yield return SelectionSet;
    }

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString() => SyntaxPrinter.Print(this, true);

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <param name="indented">
    /// A value that indicates whether the GraphQL output should be formatted,
    /// which includes indenting nested GraphQL tokens, adding
    /// new lines, and adding white space between property names and values.
    /// </param>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

    public OperationDefinitionNode WithLocation(Location? location)
        => new(
            location, Name, Operation,
            VariableDefinitions,
            Directives, SelectionSet);

    public OperationDefinitionNode WithName(NameNode? name)
        => new(
            Location, name, Operation,
            VariableDefinitions,
            Directives, SelectionSet);

    public OperationDefinitionNode WithOperation(OperationType operation)
        => new(
            Location, Name, operation,
            VariableDefinitions,
            Directives, SelectionSet);

    public OperationDefinitionNode WithVariableDefinitions(
        IReadOnlyList<VariableDefinitionNode> variableDefinitions)
        => new(
            Location, Name, Operation,
            variableDefinitions,
            Directives, SelectionSet);

    public OperationDefinitionNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
        => new(
            Location, Name, Operation,
            VariableDefinitions,
            directives, SelectionSet);

    public OperationDefinitionNode WithSelectionSet(
        SelectionSetNode selectionSet)
        => new(
            Location, Name, Operation,
            VariableDefinitions,
            Directives, selectionSet);
}
