using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Definitions;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerSerializeAsDirectiveTests : SourceSchemaMergerTestBase
{
    // Merge @serializeAs directives when the definitions match the canonical definition.
    [Fact]
    public void Merge_SerializeAsDirectives_MatchesSnapshot()
    {
        AssertMatches(
            [
                $"""
                # Schema A
                scalar Foo @serializeAs(type: [STRING, INT])
                scalar Bar @serializeAs(type: STRING)
                scalar Baz @serializeAs(type: STRING, pattern: "[a-z]+")

                {s_scalarSerializationTypeEnum}
                {s_serializeAsDirective}
                """,
                $"""
                # Schema B
                scalar Foo @serializeAs(type: [INT, STRING]) # Order doesn't matter.
                scalar Bar # The directive does not need to be present in all source schemas.
                scalar Baz @serializeAs(type: STRING, pattern: "[a-z]+")

                {s_scalarSerializationTypeEnum}
                {s_serializeAsDirective}
                """
            ],
            """
            scalar Bar
                @serializeAs(type: STRING)
                @fusion__type(schema: A)
                @fusion__type(schema: B)

            scalar Baz
                @serializeAs(type: STRING, pattern: "\"[a-z]+\"")
                @fusion__type(schema: A)
                @fusion__type(schema: B)

            scalar Foo
                @serializeAs(type: [ STRING, INT ])
                @fusion__type(schema: A)
                @fusion__type(schema: B)
            """,
            modifySchema: s_removeSerializeAsDirective);
    }

    // Do not merge @serializeAs directives when the definitions do not match the canonical
    // definition.
    [Fact]
    public void Merge_SerializeAsDirectivesNonMatching_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                scalar Foo @serializeAs(string: true)

                directive @serializeAs(string: Boolean) repeatable on SCALAR
                """,
                """
                # Schema B
                scalar Foo @serializeAs(regex: "[a-z]+")

                directive @serializeAs(regex: String) repeatable on SCALAR
                """
            ],
            """
            scalar Foo
                @fusion__type(schema: A)
                @fusion__type(schema: B)
            """,
            modifySchema: s_removeSerializeAsDirective);
    }

    // Only merge if all directives have the same type and pattern.
    [Fact]
    public void Merge_SerializeAsDirectivesOnlyWhenAllEqual_MatchesSnapshot()
    {
        AssertMatches(
            [
                $"""
                # Schema A
                scalar Foo @serializeAs(type: [STRING, INT])
                scalar Bar @serializeAs(type: STRING)
                scalar Baz @serializeAs(type: STRING, pattern: "[a-z]+")

                {s_scalarSerializationTypeEnum}
                {s_serializeAsDirective}
                """,
                $"""
                # Schema B
                scalar Foo @serializeAs(type: [STRING, FLOAT])
                scalar Bar @serializeAs(type: INT)
                scalar Baz @serializeAs(type: STRING, pattern: "[0-9]+")

                {s_scalarSerializationTypeEnum}
                {s_serializeAsDirective}
                """
            ],
            """
            scalar Bar
                @fusion__type(schema: A)
                @fusion__type(schema: B)

            scalar Baz
                @fusion__type(schema: A)
                @fusion__type(schema: B)

            scalar Foo
                @fusion__type(schema: A)
                @fusion__type(schema: B)
            """,
            modifySchema: s_removeSerializeAsDirective);
    }

    private static readonly ScalarSerializationTypeMutableEnumTypeDefinition s_scalarSerializationTypeEnum
        = new();

    private static readonly SerializeAsMutableDirectiveDefinition s_serializeAsDirective
        = new(s_scalarSerializationTypeEnum, BuiltIns.String.Create());

    private static readonly Action<MutableSchemaDefinition> s_removeSerializeAsDirective
        = schema =>
        {
            schema.DirectiveDefinitions.Remove(WellKnownDirectiveNames.SerializeAs);
            schema.Types.Remove(WellKnownTypeNames.ScalarSerializationType);
        };
}
