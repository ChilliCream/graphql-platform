namespace HotChocolate.Data.Neo4J.Language
{
    public enum ClauseKind
    {
        Default,
        Expression,
        AliasedExpression,
        Visitable,
        TypedSubtree,
        Pattern,
        ExcludePattern,
        Operator,
        StatementPrefix,

        KeyValueMapEntry,
        MapExpression,
        Properties,
        KeyValueSeparator,
        Property,
        PropertyLookup,

        Literal,
        BooleanLiteral,
        StringLiteral,

        Node,
        SymbolicName,
        NodeLabel,
        NodeLabels,

        Relationship,

        Match,
        OptionalMatch,
        Return,
        With,
        Unwind,
        Where,
        YieldItems,

        Distinct,
        OrderBy,
        Skip,
        Limit,

        Create,
        Delete,
        Set,
        Remove,
        Foreach,
        Merge,
        Union,
        Use,
        LoadCsv,
        Condition
    }
}
