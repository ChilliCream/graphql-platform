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
    Alias = 12,
    Argument = 13,
    Directive = 14,
    ListValue = 15,
    ObjectValue = 16,
    ObjectField = 17,
    Variable = 18,
    IntValue = 19,
    FloatValue = 20,
    StringValue = 21,
    EnumValue = 22,
    NamedType = 23,
    ListType = 24,
    NonNullType = 25
}
