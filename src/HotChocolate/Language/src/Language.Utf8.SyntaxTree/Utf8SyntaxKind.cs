namespace HotChocolate.Language;

internal enum Utf8SyntaxKind
{
    None = 0,
    OperationQuery = 1,
    OperationMutation = 2,
    OperationSubscription = 3,
    FragmentDefinition = 4,
    VariableDefinition = 5,
    SelectionSet = 6,
    Field = 7,
    FragmentSpread = 8,
    InlineFragment = 9,
    TypeCondition = 10,
    Name = 11,
    Alias = 12
}
