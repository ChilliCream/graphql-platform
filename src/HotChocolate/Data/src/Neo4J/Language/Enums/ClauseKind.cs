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
        Comparison,
        CompoundCondition,
        NestedExpression,
        KeyValueMapEntry,
        MapExpression,
        MapProjection,
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

        Operation,

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
