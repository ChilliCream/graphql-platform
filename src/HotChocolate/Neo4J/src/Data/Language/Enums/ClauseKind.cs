namespace HotChocolate.Data.Neo4J.Language
{
    public enum ClauseKind
    {
        Default,
        Arguments,
        Expression,
        DistinctExpression,
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
        PatternComprehension,
        RelationshipChain,
        RelationshipPatternCondition,

        SortItem,

        Literal,
        BooleanLiteral,
        StringLiteral,
        ExpressionList,

        Node,
        SymbolicName,
        NodeLabel,
        NodeLabels,

        Operation,
        Statement,

        Relationship,
        RelationshipLength,
        RelationshipTypes,
        RelationshipDetails,

        Match,
        OptionalMatch,
        Return,
        With,
        Unwind,
        Where,
        YieldItems,

        Exists,
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
