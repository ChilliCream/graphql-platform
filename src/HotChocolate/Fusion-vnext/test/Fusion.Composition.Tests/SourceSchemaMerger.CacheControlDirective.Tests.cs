using HotChocolate.Fusion.Options;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Definitions;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerCacheControlDirectiveTests : SourceSchemaMergerTestBase
{
    // Ignore @cacheControl directives.
    [Fact]
    public void Merge_CacheControlDirectivesIgnore_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Foo {
                    field: Int @cacheControl(maxAge: 500)
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """,
                $$"""
                # Schema B
                type Foo {
                    field: Int @cacheControl(maxAge: 500)
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """
            ],
            """
            type Foo
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            options => options.CacheControlMergeBehavior = DirectiveMergeBehavior.Ignore,
            s_removeCacheControlDirective);
    }

    // Merge @cacheControl directives when the definitions match the canonical definition.
    [Fact]
    public void Merge_CacheControlDirectives_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type FooObject @cacheControl(maxAge: 500) {
                    field: Int @cacheControl(maxAge: 500)
                }

                interface FooInterface @cacheControl(maxAge: 500) {
                    field: Int
                }

                union FooUnion @cacheControl(maxAge: 500) = FooObject

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """,
                $$"""
                # Schema B
                type FooObject @cacheControl(maxAge: 500) {
                    field: Int @cacheControl(maxAge: 500)
                }

                interface FooInterface @cacheControl(maxAge: 500) {
                    field: Int
                }

                union FooUnion @cacheControl(maxAge: 500) = FooObject

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """
            ],
            """
            type FooObject
                @cacheControl(maxAge: 500)
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @cacheControl(maxAge: 500)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            interface FooInterface
                @cacheControl(maxAge: 500)
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            union FooUnion
                @cacheControl(maxAge: 500)
                @fusion__type(schema: A)
                @fusion__type(schema: B)
                @fusion__unionMember(schema: A, member: "FooObject")
                @fusion__unionMember(schema: B, member: "FooObject") = FooObject
            """,
            options => options.CacheControlMergeBehavior = DirectiveMergeBehavior.Include,
            s_removeCacheControlDirective);
    }

    // Do not merge @cacheControl directives when the definitions do not match the canonical
    // definition.
    [Fact]
    public void Merge_CacheControlDirectivesNonMatching_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Foo {
                    field: Int @cacheControl(age: "500")
                }

                directive @cacheControl(age: String) repeatable on SCALAR
                """,
                """
                # Schema B
                type Foo {
                    field: Int @cacheControl(lifetime: 500)
                }

                directive @cacheControl(lifetime: Int) repeatable on SCALAR
                """
            ],
            """
            type Foo
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            options => options.CacheControlMergeBehavior = DirectiveMergeBehavior.Include,
            s_removeCacheControlDirective);
    }

    // Merge @cacheControl directives privately.
    [Fact]
    public void Merge_CacheControlDirectivesPrivately_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Foo {
                    field: Int @cacheControl(maxAge: 500)
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """,
                $$"""
                # Schema B
                type Foo {
                    field: Int @cacheControl(maxAge: 500)
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """
            ],
            """
            type Foo
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @fusion__cacheControl(maxAge: 500)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            enum fusion__CacheControlScope {
                PRIVATE
                PUBLIC
            }

            directive @fusion__cacheControl(inheritMaxAge: Boolean maxAge: Int scope: fusion__CacheControlScope sharedMaxAge: Int vary: [String]) on OBJECT | FIELD_DEFINITION | INTERFACE | UNION
            """,
            options => options.CacheControlMergeBehavior = DirectiveMergeBehavior.IncludePrivate,
            s_removeCacheControlDirective);
    }

    // Merge @cacheControl directives using the lowest "maxAge" and "sharedMaxAge". If "maxAge" or
    // "sharedMaxAge" is not specified, then the values are omitted in the merged directive.
    [Fact]
    public void Merge_CacheControlDirectivesLowestMaxAgeAndSharedMaxAge_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Foo {
                    field1: Int @cacheControl(maxAge: 500, sharedMaxAge: 800)
                    field2: Int @cacheControl(maxAge: 500, sharedMaxAge: 800)
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """,
                $$"""
                # Schema C
                type Foo {
                    field1: Int @cacheControl(maxAge: 1000, sharedMaxAge: 600)
                    field2: Int @cacheControl
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """
            ],
            """
            type Foo
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field1: Int
                    @cacheControl(maxAge: 500, sharedMaxAge: 600)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                field2: Int
                    @cacheControl
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            options => options.CacheControlMergeBehavior = DirectiveMergeBehavior.Include,
            s_removeCacheControlDirective);
    }

    // Merge @cacheControl directives with "inheritMaxAge".
    // "inheritMaxAge" is set to false, if not all directives have "inheritMaxAge: true".
    [Fact]
    public void Merge_CacheControlDirectivesWithInheritMaxAge_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Foo {
                    field1: Int @cacheControl(inheritMaxAge: true)
                    field2: Int @cacheControl(inheritMaxAge: true)
                    field3: Int @cacheControl(inheritMaxAge: true)
                    field4: Int @cacheControl
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """,
                $$"""
                # Schema B
                type Foo {
                    field1: Int @cacheControl(inheritMaxAge: true)
                    field2: Int @cacheControl(inheritMaxAge: false)
                    field3: Int @cacheControl
                    field4: Int @cacheControl
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """
            ],
            """
            type Foo
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field1: Int
                    @cacheControl(inheritMaxAge: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                field2: Int
                    @cacheControl(inheritMaxAge: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                field3: Int
                    @cacheControl(inheritMaxAge: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                field4: Int
                    @cacheControl
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            options => options.CacheControlMergeBehavior = DirectiveMergeBehavior.Include,
            s_removeCacheControlDirective);
    }

    // Merge @cacheControl directives with "scope".
    // "scope" is PRIVATE, if any directive has PRIVATE as the value.
    [Fact]
    public void Merge_CacheControlDirectivesWithScope_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Foo {
                    field1: Int @cacheControl(scope: PUBLIC)
                    field2: Int @cacheControl(scope: PUBLIC)
                    field3: Int @cacheControl(scope: PUBLIC)
                    field4: Int @cacheControl
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """,
                $$"""
                # Schema B
                type Foo {
                    field1: Int @cacheControl(scope: PUBLIC)
                    field2: Int @cacheControl(scope: PRIVATE)
                    field3: Int @cacheControl
                    field4: Int @cacheControl
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """
            ],
            """
            type Foo
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field1: Int
                    @cacheControl(scope: PUBLIC)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                field2: Int
                    @cacheControl(scope: PRIVATE)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                field3: Int
                    @cacheControl(scope: PUBLIC)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                field4: Int
                    @cacheControl
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            options => options.CacheControlMergeBehavior = DirectiveMergeBehavior.Include,
            s_removeCacheControlDirective);
    }

    // Merge @cacheControl directives with "vary".
    // "vary" is the union set of the string values of all directives.
    [Fact]
    public void Merge_CacheControlDirectivesWithVary_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Foo {
                    field1: Int @cacheControl(vary: ["Accept-Encoding", "User-Agent"])
                    field2: Int @cacheControl(vary: ["Accept-Encoding"])
                    field3: Int @cacheControl(vary: [])
                    field4: Int @cacheControl
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """,
                $$"""
                # Schema B
                type Foo {
                    field1: Int @cacheControl(vary: ["User-Agent", "Accept-Language"])
                    field2: Int @cacheControl(vary: [])
                    field3: Int @cacheControl
                    field4: Int @cacheControl
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """
            ],
            """
            type Foo
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field1: Int
                    @cacheControl(vary: [ "Accept-Encoding", "User-Agent", "Accept-Language" ])
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                field2: Int
                    @cacheControl(vary: [ "Accept-Encoding" ])
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                field3: Int
                    @cacheControl
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                field4: Int
                    @cacheControl
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            options => options.CacheControlMergeBehavior = DirectiveMergeBehavior.Include,
            s_removeCacheControlDirective);
    }

    // Only merge if all member definitions have the @cacheControl directive.
    [Fact]
    public void Merge_CacheControlDirectivesOnlyWhenAllDefined_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Foo {
                    field: Int @cacheControl(maxAge: 500)
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """,
                $$"""
                # Schema B
                type Foo {
                    field: Int
                }

                {{s_cacheControlScopeEnum}}
                {{s_cacheControlDirective}}
                """
            ],
            """
            type Foo
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            options => options.CacheControlMergeBehavior = DirectiveMergeBehavior.Include,
            s_removeCacheControlDirective);
    }

    private static readonly CacheControlScopeMutableEnumTypeDefinition s_cacheControlScopeEnum
        = new();

    private static readonly CacheControlMutableDirectiveDefinition s_cacheControlDirective
        = new(
            BuiltIns.Int.Create(),
            BuiltIns.Boolean.Create(),
            s_cacheControlScopeEnum,
            BuiltIns.String.Create());

    private static readonly Action<MutableSchemaDefinition> s_removeCacheControlDirective
        = schema =>
        {
            schema.DirectiveDefinitions.Remove(DirectiveNames.CacheControl.Name);
            schema.Types.Remove(WellKnownTypeNames.CacheControlScope);
        };
}
