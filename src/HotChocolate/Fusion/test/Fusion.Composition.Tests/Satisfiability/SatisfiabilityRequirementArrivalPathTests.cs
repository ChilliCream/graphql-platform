using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityRequirementArrivalPathTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Compose_Should_Fail_When_LookupKeyOfRequirementIsOnlyReachableOnOtherArrivalPath(
        bool goodDeclaredFirst)
    {
        // arrange
        // P.k is only served by D, which re-traverses good but not bad.
        var rootFields = goodDeclaredFirst
            ? "good: X @shareable\n    bad: X"
            : "bad: X\n    good: X @shareable";

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "a",
                $$"""
                type Query {
                    {{rootFields}}
                }

                type X {
                    p: P @shareable
                }

                type P {
                    t: T @shareable
                }

                type T {
                    f(q: String @require(field: "q")): String
                }
                """),
            new SourceSchemaText(
                "c",
                """
                type Query {
                    pByK(k: String!): P @lookup @internal
                }

                type P {
                    k: String @shareable
                    t: T @shareable
                }

                type T {
                    q: String
                }
                """),
            new SourceSchemaText(
                "d",
                """
                type Query {
                    good: X @shareable
                }

                type X {
                    p: P @shareable
                }

                type P {
                    k: String @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.False(result.IsSuccess);
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'P.k' on path 'A:Query.bad<X> -> A:X.p<P>'.
              Unable to transition between schemas 'A' and 'C' for access to field 'C:P.k<String>'.
                Unable to satisfy the requirement '{ k }' for lookup 'pByK' in schema 'C'.
                  Unable to satisfy the requirement 'k'.
                    Unable to access the required field 'P.k' on path 'A:X.p<P>'.
                      Unable to transition between schemas 'A' and 'D' for access to required field 'D:P.k<String>'.
                        No lookups found for type 'P' in schema 'D'.
              Unable to transition between schemas 'A' and 'D' for access to field 'D:P.k<String>'.
                No lookups found for type 'P' in schema 'D'.
            """,
            """
            Unable to access the field 'T.f' on path 'A:Query.bad<X> -> A:X.p<P> -> A:P.t<T>'.
              Unable to satisfy the requirement '{ q }' on field 'A:T.f<String>'.
                Unable to satisfy the requirement 'q'.
                  Unable to access the required field 'T.q' on path 'A:P.t<T>'.
                    Unable to transition between schemas 'A' and 'C' for access to required field 'C:T.q<String>'.
                      Unable to satisfy the requirement '{ k }' for lookup 'pByK' in schema 'C'.
                        Unable to satisfy the requirement 'k'.
                          Unable to access the required field 'P.k' on path 'A:X.p<P>'.
                            Unable to transition between schemas 'A' and 'D' for access to required field 'D:P.k<String>'.
                              No lookups found for type 'P' in schema 'D'.
            """,
            """
            Unable to access the field 'T.q' on path 'A:Query.bad<X> -> A:X.p<P> -> A:P.t<T>'.
              Unable to transition between schemas 'A' and 'C' for access to field 'C:T.q<String>'.
                Unable to satisfy the requirement '{ k }' for lookup 'pByK' in schema 'C'.
                  Unable to satisfy the requirement 'k'.
                    Unable to access the required field 'P.k' on path 'A:X.p<P>'.
                      Unable to transition between schemas 'A' and 'D' for access to required field 'D:P.k<String>'.
                        No lookups found for type 'P' in schema 'D'.
            """
        ]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Compose_Should_Fail_When_NestedLookupKeyOfRequirementIsOnlyReachableOnOtherArrivalPath(
        bool viaBDeclaredFirst)
    {
        // arrange
        // T.x needs T.k from B, which T reaches through the lookup of U, whose key U.kU only X serves.
        const string viaA = "    viaA: V @shareable\n";
        const string viaB = "    viaB: V\n";

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "main",
                "type Query {\n"
                + (viaBDeclaredFirst ? viaB + viaA : viaA + viaB)
                + """
                      tById(id: ID!): T @lookup @internal
                  }

                  type V {
                      q: U @shareable
                  }

                  type U {
                      id: ID!
                      p: T @shareable
                  }

                  type T {
                      id: ID! @shareable
                      f(x: String @require(field: "x")): String
                  }
                  """),
            new SourceSchemaText(
                "a",
                """
                type Query {
                    tByK(k: String!): T @lookup @internal
                }

                type T {
                    k: String! @inaccessible @shareable
                    x: String @inaccessible
                }
                """),
            new SourceSchemaText(
                "b",
                """
                type Query {
                    uByKU(kU: String!): U @lookup @internal
                }

                type U {
                    kU: String! @inaccessible @shareable
                    p: T @shareable
                }

                type T {
                    id: ID! @shareable
                    k: String! @inaccessible @shareable
                }
                """),
            new SourceSchemaText(
                "x",
                """
                type Query {
                    viaA: V @shareable
                }

                type V {
                    q: U @shareable
                }

                type U {
                    kU: String! @inaccessible @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.False(result.IsSuccess);
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'T.f' on path 'MAIN:Query.viaB<V> -> MAIN:V.q<U> -> MAIN:U.p<T>'.
              Unable to satisfy the requirement '{ x }' on field 'MAIN:T.f<String>'.
                Unable to satisfy the requirement 'x'.
                  Unable to access the required field 'T.x' on path 'MAIN:U.p<T>'.
                    Unable to transition between schemas 'MAIN' and 'A' for access to required field 'A:T.x<String>'.
                      Unable to satisfy the requirement '{ k }' for lookup 'tByK' in schema 'A'.
                        Unable to satisfy the requirement 'k'.
                          Unable to access the required field 'T.k' on path 'MAIN:U.p<T>'.
                            Unable to transition between schemas 'MAIN' and 'B' for access to required field 'B:T.k<String>'.
                              Unable to satisfy the requirement '{ kU }' for lookup 'uByKU' in schema 'B'.
                                Unable to satisfy the requirement 'kU'.
                                  Unable to access the required field 'U.kU' on path 'MAIN:V.q<U>'.
                                    Unable to transition between schemas 'MAIN' and 'X' for access to required field 'X:U.kU<String>'.
                                      No lookups found for type 'U' in schema 'X'.
            """
        ]);
    }

    [Theory]
    [InlineData(true, "Mutation")]
    [InlineData(false, "Mutation")]
    [InlineData(true, "Subscription")]
    [InlineData(false, "Subscription")]
    public void Compose_Should_Succeed_When_RequirementIsReachedThroughViewerFallback(
        bool reRootedDeclaredFirst,
        string rootType)
    {
        // arrange
        // T.x is fetched by re-rooting doSomething.viewer at Query.viewer in B.
        var (first, second) = reRootedDeclaredFirst
            ? ("doSomething: DoSomethingPayload", "doOther: Other")
            : ("doOther: Other", "doSomething: DoSomethingPayload");

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                $$"""
                type Query {
                    tById(id: ID!): T @lookup @internal
                }

                type {{rootType}} {
                    {{first}}
                    {{second}}
                }

                type DoSomethingPayload {
                    something: Int
                    viewer: Viewer
                }

                type Other {
                    id: ID! @shareable
                    me: Viewer @shareable
                }

                type Viewer {
                    t: T @shareable
                }

                type T {
                    id: ID! @shareable
                    f(x: String @require(field: "x")): String
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    viewer: Viewer
                    otherById(id: ID!): Other @lookup @internal
                }

                type Other {
                    id: ID! @shareable
                    me: Viewer @shareable
                }

                type Viewer {
                    t: T @shareable
                }

                type T {
                    id: ID! @shareable
                    x: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Compose_Should_Fail_When_ViewerFieldIsPartialInQueryViewerSchema(bool otherSchemaLacksViewerField)
    {
        // arrange
        // Only B serves T.x, and doSomething.viewer is not re-rooted at Query.viewer in B, which has it as @external.
        var a = new SourceSchemaText(
            "A",
            """
            type Query {
                viewer: Viewer @shareable
                tById(id: ID!): T @lookup @internal
            }

            type Mutation {
                doSomething: DoSomethingPayload
            }

            type DoSomethingPayload {
                viewer: Viewer @shareable
            }

            type Viewer {
                vid: String @shareable
                t: T @shareable
            }

            type T {
                id: ID! @shareable
                f(x: String @require(field: "x")): String
            }
            """);
        var b = new SourceSchemaText(
            "B",
            """
            type Query {
                viewer: Viewer @shareable
                lastPayload: DoSomethingPayload @provides(fields: "viewer { vid }") @inaccessible
            }

            type DoSomethingPayload {
                viewer: Viewer @external
            }

            type Viewer {
                vid: String @external
                t: T @shareable
            }

            type T {
                id: ID! @shareable
                x: String
            }
            """);
        var c = new SourceSchemaText(
            "C",
            """
            type Query {
                viewer: Viewer @shareable
            }

            type Viewer {
                vid: String @shareable
            }
            """);
        SourceSchemaText[] sourceSchemas = otherSchemaLacksViewerField ? [a, b, c] : [a, b];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.False(result.IsSuccess);
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'T.f' on path 'A:Mutation.doSomething<DoSomethingPayload> -> A:DoSomethingPayload.viewer<Viewer> -> A:Viewer.t<T>'.
              Unable to satisfy the requirement '{ x }' on field 'A:T.f<String>'.
                Unable to satisfy the requirement 'x'.
                  Unable to access the required field 'T.x' on path 'A:Viewer.t<T>'.
                    Unable to transition between schemas 'A' and 'B' for access to required field 'B:T.x<String>'.
                      No lookups found for type 'T' in schema 'B'.
            """,
            """
            Unable to access the field 'T.x' on path 'A:Mutation.doSomething<DoSomethingPayload> -> A:DoSomethingPayload.viewer<Viewer> -> A:Viewer.t<T>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:T.x<String>'.
                No lookups found for type 'T' in schema 'B'.
            """
        ]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Compose_Should_Fail_When_RequirementWithoutHolderDataIsOnlyReachableOnOtherArrivalPath(
        bool xDeclaredFirst)
    {
        // arrange
        // T.r must be fetched without data from S through P.tk, which only G1 serves and only below x.
        var rootFields = xDeclaredFirst ? "x: X\n    y: Y" : "y: Y\n    x: X";

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "r",
                $$"""
                type Query {
                    {{rootFields}}
                }

                type X {
                    id: ID! @shareable
                }

                type Y {
                    id: ID! @shareable
                }
                """),
            new SourceSchemaText(
                "g1",
                """
                type Query {
                    xById(id: ID!): X @lookup @internal
                }

                type X {
                    id: ID! @shareable
                    p: P
                }

                type P {
                    sk: ID! @shareable
                    tk: ID! @shareable @inaccessible
                }
                """),
            new SourceSchemaText(
                "g2",
                """
                type Query {
                    yById(id: ID!): Y @lookup @internal
                }

                type Y {
                    id: ID! @shareable
                    p: P
                }

                type P {
                    sk: ID! @shareable
                }
                """),
            new SourceSchemaText(
                "s",
                """
                type Query {
                    pBySk(sk: ID!): P @lookup @internal
                }

                type P {
                    sk: ID! @shareable
                    fp: T @shareable
                }

                type T {
                    id: ID! @shareable
                    f(r: String @require(field: "r")): String
                }
                """),
            new SourceSchemaText(
                "tt",
                """
                type Query {
                    pByTk(tk: ID!): P @lookup @internal
                    tById(id: ID!): T @lookup @internal
                }

                type P {
                    tk: ID! @shareable @inaccessible
                    fp: T @shareable
                }

                type T {
                    id: ID! @shareable
                    r: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.False(result.IsSuccess);
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'T.f' on path 'R:Query.y<Y> -> G2:Y.p<P> -> S:P.fp<T>'.
              Unable to satisfy the requirement '{ r }' on field 'S:T.f<String>'.
                The field 'T.f' in schema 'S' requires data from another schema, but schema 'S' has no lookup for type 'T' that could resolve the field once the required data has been fetched.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Fail_When_RequirementIsOnlyProvidedBelowObjectHeldWithRequiringSchema()
    {
        // arrange
        // T.r must be fetched without data from S, and Q serves it only below X, which Q holds by a key of S.
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "s",
                """
                type Query {
                    x: X @shareable
                }

                type X {
                    id: ID! @shareable
                    p: P @shareable
                }

                type P {
                    id: ID! @shareable
                    t: T
                }

                type T {
                    id: ID! @shareable
                    f(r: String @require(field: "r")): String
                }
                """),
            new SourceSchemaText(
                "q",
                """
                type Query {
                    xById(id: ID!): X @lookup @internal
                    pById(id: ID!): P @lookup @internal
                }

                type X {
                    id: ID! @shareable
                    p: P @shareable @provides(fields: "t { id }")
                }

                type P {
                    id: ID! @shareable
                    t: T @external
                }

                type T {
                    id: ID! @external
                    r: String
                }
                """),
            new SourceSchemaText(
                "z",
                """
                type Query {
                    x: X @shareable
                }

                type X {
                    p: P @shareable
                }

                type P {
                    id: ID! @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.False(result.IsSuccess);
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'T.f' on path 'S:Query.x<X> -> S:X.p<P> -> S:P.t<T>'.
              Unable to satisfy the requirement '{ r }' on field 'S:T.f<String>'.
                The field 'T.f' in schema 'S' requires data from another schema, but schema 'S' has no lookup for type 'T' that could resolve the field once the required data has been fetched.
            """
        ]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Compose_Should_Fail_When_HoldingCallOfRequirementNeedsDataOfRequiringSchema(
        bool requiringFieldDeclaredFirst)
    {
        // arrange
        // T.r must be fetched without data from S, and H holds T only through a lookup keyed by data of S.
        var fields = requiringFieldDeclaredFirst
            ? "f(r: String @require(field: \"r\")): String\n    id: ID! @shareable"
            : "id: ID! @shareable\n    f(r: String @require(field: \"r\")): String";

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "s",
                $$"""
                type Query {
                    a: A
                    bById(id: ID!): B @lookup @internal
                }

                type A {
                    id: ID!
                    b: B
                }

                type B {
                    id: ID! @shareable
                    t: T @shareable
                }

                type T {
                    {{fields}}
                }
                """),
            new SourceSchemaText(
                "h",
                """
                type Query {
                    bById(id: ID!): B @lookup @internal
                }

                type B {
                    id: ID! @shareable
                    t: T @shareable
                }

                type T {
                    id: ID! @shareable
                    r: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.False(result.IsSuccess);
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'T.f' on path 'S:Query.a<A> -> S:A.b<B> -> S:B.t<T>'.
              Unable to satisfy the requirement '{ r }' on field 'S:T.f<String>'.
                The field 'T.f' in schema 'S' requires data from another schema, but schema 'S' has no lookup for type 'T' that could resolve the field once the required data has been fetched.
            """,
            """
            Unable to access the field 'T.f' on path 'S:Query.a<A> -> S:A.b<B> -> H:B.t<T>'.
              Unable to satisfy the requirement '{ r }' on field 'S:T.f<String>'.
                The field 'T.f' in schema 'S' requires data from another schema, but schema 'S' has no lookup for type 'T' that could resolve the field once the required data has been fetched.
            """
        ]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Compose_Should_Fail_When_ReRootedViewerCallTakesRequirementFromOtherReRootedCall(
        bool mutationPayloadDeclaredFirst)
    {
        // arrange
        // D holds T2 below doB only through the re-rooted doB.viewer, and T2.id comes from the call re-rooted in E.
        var payloadFields = mutationPayloadDeclaredFirst
            ? "ok: Boolean\n    viewer: Viewer"
            : "viewer: Viewer\n    ok: Boolean";

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "B",
                $$"""
                type Query {
                    ping: String @shareable
                }

                type Mutation {
                    doB: Payload
                }

                type Payload {
                    {{payloadFields}}
                }

                type Viewer {
                    vid: String @shareable
                }
                """),
            new SourceSchemaText(
                "D",
                """
                type Query {
                    viewer: Viewer @shareable
                }

                type Viewer {
                    vid: String @shareable
                    vt: T0 @shareable
                }

                type T0 {
                    f2: T2 @shareable
                }

                type T2 {
                    rqD(v: ID @require(field: "id")): String
                }
                """),
            new SourceSchemaText(
                "E",
                """
                type Query {
                    viewer: Viewer @shareable
                }

                type Viewer {
                    vid: String @shareable
                    vt: T0 @shareable
                }

                type T0 {
                    f2: T2 @shareable
                }

                type T2 {
                    id: ID! @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.False(result.IsSuccess);
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'T2.rqD' on path 'B:Mutation.doB<Payload> -> B:Payload.viewer<Viewer> -> D:Viewer.vt<T0> -> D:T0.f2<T2>'.
              Unable to satisfy the requirement '{ id }' on field 'D:T2.rqD<String>'.
                The field 'T2.rqD' in schema 'D' requires data from another schema, but schema 'D' has no lookup for type 'T2' that could resolve the field once the required data has been fetched.
            """,
            """
            Unable to access the field 'T2.rqD' on path 'B:Mutation.doB<Payload> -> B:Payload.viewer<Viewer> -> D:Viewer.vt<T0> -> E:T0.f2<T2>'.
              Unable to satisfy the requirement '{ id }' on field 'D:T2.rqD<String>'.
                The field 'T2.rqD' in schema 'D' requires data from another schema, but schema 'D' has no lookup for type 'T2' that could resolve the field once the required data has been fetched.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_ReRootedViewerCallTakesRequirementFromMutationCall()
    {
        // arrange
        // D holds T2 below doB only through the re-rooted doB.viewer, and the mutation call in B serves T2.id.
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "B",
                """
                type Query {
                    ping: String @shareable
                }

                type Mutation {
                    doB: Payload
                }

                type Payload {
                    ok: Boolean
                    viewer: Viewer
                }

                type Viewer {
                    vid: String @shareable
                    vt: T0 @shareable
                }

                type T0 {
                    f2: T2 @shareable
                }

                type T2 {
                    id: ID! @shareable
                }
                """),
            new SourceSchemaText(
                "D",
                """
                type Query {
                    viewer: Viewer @shareable
                }

                type Viewer {
                    vid: String @shareable
                    vt: T0 @shareable
                }

                type T0 {
                    f2: T2 @shareable
                }

                type T2 {
                    rqD(v: ID @require(field: "id")): String
                }
                """),
            new SourceSchemaText(
                "E",
                """
                type Query {
                    viewer: Viewer @shareable
                }

                type Viewer {
                    vid: String @shareable
                    vt: T0 @shareable
                }

                type T0 {
                    f2: T2 @shareable
                }

                type T2 {
                    id: ID! @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Compose_Should_Succeed_When_EventStreamObjectIsEnteredAgainThroughMessageKey(
        bool idDeclaredFirst)
    {
        // arrange
        // The message omits T3.s0, which A serves after entering T2 again with the id of the message.
        var fields = idDeclaredFirst ? "id: ID!\n    f2: T3" : "f2: T3\n    id: ID!";

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "a",
                $$"""
                type Query {
                    t2ById(id: ID!): T2 @lookup @internal
                }

                type Subscription {
                    ev: T2 @eventStream(topics: ["e"], message: "{ id f2 { __typename } }")
                }

                type T2 {
                    {{fields}}
                }

                type T3 {
                    s0: String
                }
                """),
            new SourceSchemaText(
                "b",
                """
                type Query {
                    ping: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_AncestorOfEventStreamObjectIsEnteredAgainThroughMessageKey()
    {
        // arrange
        // The message omits T3.s0, which A serves after entering T1 again with the id of the message.
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "a",
                """
                type Query {
                    t1ById(id: ID!): T1 @lookup @internal
                }

                type Subscription {
                    ev: T1 @eventStream(topics: ["e"], message: "{ id f1 { f2 { __typename } } }")
                }

                type T1 {
                    id: ID!
                    f1: T2
                }

                type T2 {
                    f2: T3
                }

                type T3 {
                    s0: String
                }
                """),
            new SourceSchemaText(
                "b",
                """
                type Query {
                    ping: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Fail_When_EventStreamMessageLacksLookupKey()
    {
        // arrange
        // The message omits T2.id, so A cannot enter T2 again to resolve the fields it omits.
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "a",
                """
                type Query {
                    t2ById(id: ID!): T2 @lookup @internal
                }

                type Subscription {
                    ev: T2 @eventStream(topics: ["e"], message: "{ f2 { __typename } }")
                }

                type T2 {
                    id: ID!
                    f2: T3
                }

                type T3 {
                    s0: String
                }
                """),
            new SourceSchemaText(
                "b",
                """
                type Query {
                    ping: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.False(result.IsSuccess);
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            "Unable to access the field 'T2.id' on path 'A:Subscription.ev<T2>'.",
            "Unable to access the field 'T3.s0' on path 'A:Subscription.ev<T2> -> A:T2.f2<T3>'."
        ]);
    }

    [Fact]
    public void Compose_Should_Fail_When_ReRootedViewerCallTakesRequirementThroughLookupKeyedByOtherReRootedCall()
    {
        // arrange
        // D holds T2 below doB only through the re-rooted doB.viewer, and T2.s comes from G, keyed by E's call.
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "B",
                """
                type Query {
                    ping: String @shareable
                }

                type Mutation {
                    doB: Payload
                }

                type Payload {
                    ok: Boolean
                    viewer: Viewer
                }

                type Viewer {
                    vid: String @shareable
                }
                """),
            new SourceSchemaText(
                "D",
                """
                type Query {
                    viewer: Viewer @shareable
                }

                type Viewer {
                    vid: String @shareable
                    vt: T0 @shareable
                }

                type T0 {
                    f2: T2 @shareable
                }

                type T2 {
                    id: ID! @shareable
                    rqD(v: String @require(field: "s")): String
                }
                """),
            new SourceSchemaText(
                "E",
                """
                type Query {
                    viewer: Viewer @shareable
                }

                type Viewer {
                    vid: String @shareable
                    vt: T0 @shareable
                }

                type T0 {
                    f2: T2 @shareable
                }

                type T2 {
                    id: ID! @shareable
                    s: String @shareable
                }
                """),
            new SourceSchemaText(
                "G",
                """
                type Query {
                    t2ById(id: ID!): T2 @lookup @internal
                }

                type T2 {
                    id: ID! @shareable
                    s: String @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.False(result.IsSuccess);
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'T2.rqD' on path 'B:Mutation.doB<Payload> -> B:Payload.viewer<Viewer> -> D:Viewer.vt<T0> -> D:T0.f2<T2>'.
              Unable to satisfy the requirement '{ s }' on field 'D:T2.rqD<String>'.
                The field 'T2.rqD' in schema 'D' requires data from another schema, but schema 'D' has no lookup for type 'T2' that could resolve the field once the required data has been fetched.
            """,
            """
            Unable to access the field 'T2.rqD' on path 'B:Mutation.doB<Payload> -> B:Payload.viewer<Viewer> -> D:Viewer.vt<T0> -> E:T0.f2<T2>'.
              Unable to satisfy the requirement '{ s }' on field 'D:T2.rqD<String>'.
                The field 'T2.rqD' in schema 'D' requires data from another schema, but schema 'D' has no lookup for type 'T2' that could resolve the field once the required data has been fetched.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Fail_When_MutationCallTakesRequirementFromReRootedCall()
    {
        // arrange
        // B holds T2 in the call of doB, and T2.s only comes from doB.viewer re-rooted at Query.viewer in E.
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "B",
                """
                type Query {
                    ping: String @shareable
                }

                type Mutation {
                    doB: Payload
                }

                type Payload {
                    ok: Boolean
                    viewer: Viewer
                }

                type Viewer {
                    vid: String @shareable
                    vt: T0 @shareable
                }

                type T0 {
                    f2: T2 @shareable
                }

                type T2 {
                    id: ID! @shareable
                    rqB(v: String @require(field: "s")): String
                }
                """),
            new SourceSchemaText(
                "E",
                """
                type Query {
                    viewer: Viewer @shareable
                }

                type Viewer {
                    vid: String @shareable
                    vt: T0 @shareable
                }

                type T0 {
                    f2: T2 @shareable
                }

                type T2 {
                    id: ID! @shareable
                    s: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.False(result.IsSuccess);
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'T2.rqB' on path 'E:Query.viewer<Viewer> -> E:Viewer.vt<T0> -> E:T0.f2<T2>'.
              Unable to transition between schemas 'E' and 'B' for access to field 'B:T2.rqB<String>'.
                No lookups found for type 'T2' in schema 'B'.
            """,
            """
            Unable to access the field 'T2.rqB' on path 'B:Mutation.doB<Payload> -> B:Payload.viewer<Viewer> -> B:Viewer.vt<T0> -> B:T0.f2<T2>'.
              Unable to satisfy the requirement '{ s }' on field 'B:T2.rqB<String>'.
                The field 'T2.rqB' in schema 'B' requires data from another schema, but schema 'B' has no lookup for type 'T2' that could resolve the field once the required data has been fetched.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_LookupKeyBelowRequirementSelectsRequiringField()
    {
        // arrange
        // g serves n.z, and c enters N by N.k, which requires N.w, so a check without a asks for T without a and d.
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "a",
                """
                type Query {
                    xById(id: ID!): X @lookup @internal
                }

                type X {
                    id: ID! @shareable
                    t: T @shareable
                }

                type T {
                    id: ID! @shareable
                    f(z: String @require(field: "n.z")): String
                }
                """),
            new SourceSchemaText(
                "b",
                """
                type Query {
                    x: X @shareable
                }

                type X {
                    id: ID! @shareable
                    t: T @shareable
                }

                type T {
                    id: ID! @shareable
                    n: N @shareable
                }

                type N {
                    id: ID! @shareable
                }
                """),
            new SourceSchemaText(
                "c",
                """
                type Query {
                    nByK(k: String!): N @lookup @internal
                }

                type N {
                    k: String! @shareable
                    z: String @shareable
                }
                """),
            new SourceSchemaText(
                "d",
                """
                type Query {
                    xById(id: ID!): X @lookup @internal
                }

                type X {
                    id: ID! @shareable
                    t: T @shareable
                }

                type T {
                    id: ID! @shareable
                    n: N @shareable
                }

                type N {
                    k(w: String @require(field: "w")): String! @shareable
                }
                """),
            new SourceSchemaText(
                "e",
                """
                type Query {
                    x: X @shareable
                }

                type X {
                    id: ID! @shareable
                    t: T @shareable
                }

                type T {
                    id: ID! @shareable
                    n: N @shareable
                }

                type N {
                    w: String
                }
                """),
            new SourceSchemaText(
                "g",
                """
                type Query {
                    nById(id: ID!): N @lookup @internal
                }

                type N {
                    id: ID! @shareable
                    z: String @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }
}
