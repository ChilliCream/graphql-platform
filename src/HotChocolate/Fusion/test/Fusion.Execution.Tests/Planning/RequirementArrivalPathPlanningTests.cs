using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class RequirementArrivalPathPlanningTests : FusionTestBase
{
    private const string LookupKeySchemaA =
        """
        # name: a
        type Query {
          good: X @shareable
          bad: X
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
        """;

    private const string LookupKeySchemaC =
        """
        # name: c
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
        """;

    private const string LookupKeySchemaD =
        """
        # name: d
        type Query {
          good: X @shareable
        }

        type X {
          p: P @shareable
        }

        type P {
          k: String @shareable
        }
        """;

    private const string ViewerSchemaB =
        """
        # name: b
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
        """;

    private const string PartialViewerSchemaA =
        """
        # name: A
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
        """;

    private const string PartialViewerSchemaB =
        """
        # name: B
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
        """;

    private const string PartialViewerSchemaC =
        """
        # name: C
        type Query {
          viewer: Viewer @shareable
        }

        type Viewer {
          vid: String @shareable
        }
        """;

    private const string ViewerRequirementSchemaB =
        """
        # name: B
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
        """;

    private const string ViewerRequirementSchemaD =
        """
        # name: D
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
        """;

    private const string ViewerRequirementSchemaE =
        """
        # name: E
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
        """;

    private static readonly string[] s_holderDataSchemas =
    [
        """
        # name: r
        type Query {
          x: X
          y: Y
        }

        type X {
          id: ID! @shareable
        }

        type Y {
          id: ID! @shareable
        }
        """,
        """
        # name: g1
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
        """,
        """
        # name: g2
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
        """,
        """
        # name: s
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
        """,
        """
        # name: tt
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
        """
    ];

    [Fact]
    public void Plan_Should_FetchLookupKeyByReTraversingPath_When_PathExistsInKeySchema()
    {
        // arrange
        var schema = ComposeLookupKeySchema();

        // act
        var plan = PlanOperation(schema, "{ good { p { t { f } } } }");

        // assert
        MatchSnapshot(plan);
    }

    [Theory]
    [InlineData("{ bad { p { t { f } } } }")]
    [InlineData("{ bad { p { t { q } } } }")]
    [InlineData("{ bad { p { k } } }")]
    public void Plan_Should_Fail_When_PathDoesNotExistInKeySchema(string operation)
    {
        // arrange
        var schema = ComposeLookupKeySchema();

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => PlanOperation(schema, operation));

        // assert
        Assert.Equal("No possible plan was found.", exception.Message);
    }

    [Fact]
    public void Plan_Should_FetchNestedLookupKeyByReTraversingPath_When_PathExistsInKeySchema()
    {
        // arrange
        var schema = ComposeNestedLookupKeySchema();

        // act
        var plan = PlanOperation(schema, "{ viaA { q { p { f } } } }");

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Fail_When_PathDoesNotExistInNestedKeySchema()
    {
        // arrange
        var schema = ComposeNestedLookupKeySchema();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(schema, "{ viaB { q { p { f } } } }"));

        // assert
        Assert.Equal("No possible plan was found.", exception.Message);
    }

    [Theory]
    [InlineData("Mutation", "mutation", "doSomething { something viewer { t { f } } }")]
    [InlineData("Mutation", "mutation", "doOther { me { t { f } } }")]
    [InlineData("Subscription", "subscription", "doSomething { something viewer { t { f } } }")]
    [InlineData("Subscription", "subscription", "doOther { me { t { f } } }")]
    public void Plan_Should_FetchRequirement_When_ViewerIsReRootedOrParentHasLookup(
        string rootType,
        string operationType,
        string selection)
    {
        // arrange
        var schema = ComposeSchema(
            _ => { },
            $$"""
            # name: a
            type Query {
              tById(id: ID!): T @lookup @internal
            }

            type {{rootType}} {
              doSomething: DoSomethingPayload
              doOther: Other
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
            """,
            ViewerSchemaB);

        // act
        var plan = PlanOperation(schema, $"{operationType} {{ {selection} }}");

        // assert
        new YamlOperationPlanFormatter().Format(plan).MatchSnapshot(
            postFix: $"{operationType}_{selection.Split(' ')[0]}",
            extension: ".yaml");
    }

    [Theory]
    [InlineData("mutation { doSomething { viewer { t { f } } } }", false)]
    [InlineData("mutation { doSomething { viewer { t { x } } } }", false)]
    [InlineData("mutation { doSomething { viewer { t { f } } } }", true)]
    [InlineData("mutation { doSomething { viewer { t { x } } } }", true)]
    public void Plan_Should_Fail_When_ViewerFieldIsPartialInQueryViewerSchema(
        string operation,
        bool otherSchemaLacksViewerField)
    {
        // arrange
        string[] sourceSchemas = otherSchemaLacksViewerField
            ? [PartialViewerSchemaA, PartialViewerSchemaB, PartialViewerSchemaC]
            : [PartialViewerSchemaA, PartialViewerSchemaB];
        var schema = ComposeSchema(
            options =>
            {
                const string path =
                    "A:Mutation.doSomething<DoSomethingPayload> -> A:DoSomethingPayload.viewer<Viewer> -> A:Viewer.t<T>";
                options.Satisfiability.IgnoredNonAccessibleFields["T.f"] = [path];
                options.Satisfiability.IgnoredNonAccessibleFields["T.x"] = [path];
            },
            sourceSchemas);

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => PlanOperation(schema, operation));

        // assert
        Assert.Equal("No possible plan was found.", exception.Message);
    }

    [Fact]
    public void Plan_Should_FetchRequirementWithoutHolderData_When_KeyOfOtherSchemaIsHeld()
    {
        // arrange
        var schema = ComposeHolderDataSchema();

        // act
        var plan = PlanOperation(schema, "{ x { p { fp { f } } } }");

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_Fail_When_RequirementNeedsHolderDataAndKeyOfOtherSchemaIsNotHeld()
    {
        // arrange
        var schema = ComposeHolderDataSchema();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(schema, "{ y { p { fp { f } } } }"));

        // assert
        Assert.Equal("No possible plan was found.", exception.Message);
    }

    [Fact]
    public void Plan_Should_Fail_When_RequirementIsOnlyProvidedBelowObjectHeldWithRequiringSchema()
    {
        // arrange
        var schema = ComposeSchema(
            options => options.Satisfiability.IgnoredNonAccessibleFields["T.f"] =
                ["S:Query.x<X> -> S:X.p<P> -> S:P.t<T>"],
            """
            # name: s
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
            """,
            """
            # name: q
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
            """,
            """
            # name: z
            type Query {
              x: X @shareable
            }

            type X {
              p: P @shareable
            }

            type P {
              id: ID! @shareable
            }
            """);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(schema, "{ x { p { t { f } } } }"));

        // assert
        Assert.Equal("No possible plan was found.", exception.Message);
    }

    [Fact]
    public void Plan_Should_Fail_When_HoldingCallOfRequirementNeedsDataOfRequiringSchema()
    {
        // arrange
        var schema = ComposeSchema(
            options => options.Satisfiability.IgnoredNonAccessibleFields["T.f"] =
            [
                "S:Query.a<A> -> S:A.b<B> -> S:B.t<T>",
                "S:Query.a<A> -> S:A.b<B> -> H:B.t<T>"
            ],
            """
            # name: s
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
              id: ID! @shareable
              f(r: String @require(field: "r")): String
            }
            """,
            """
            # name: h
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
            """);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(schema, "{ a { b { t { f } } } }"));

        // assert
        Assert.Equal("No possible plan was found.", exception.Message);
    }

    [Fact]
    public void Plan_Should_Fail_When_ReRootedViewerCallTakesRequirementFromOtherReRootedCall()
    {
        // arrange
        var schema = ComposeSchema(
            options => options.Satisfiability.IgnoredNonAccessibleFields["T2.rqD"] =
            [
                "B:Mutation.doB<Payload> -> B:Payload.viewer<Viewer> -> D:Viewer.vt<T0> -> D:T0.f2<T2>",
                "B:Mutation.doB<Payload> -> B:Payload.viewer<Viewer> -> D:Viewer.vt<T0> -> E:T0.f2<T2>"
            ],
            ViewerRequirementSchemaB,
            ViewerRequirementSchemaD,
            ViewerRequirementSchemaE);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(schema, "mutation { doB { viewer { vt { f2 { rqD } } } } }"));

        // assert
        Assert.Equal("No possible plan was found.", exception.Message);
    }

    [Fact]
    public void Plan_Should_FetchRequirementFromQueryRoot_When_ViewerIsNotReRooted()
    {
        // arrange
        var schema = ComposeSchema(
            options => options.Satisfiability.IgnoredNonAccessibleFields["T2.rqD"] =
            [
                "B:Mutation.doB<Payload> -> B:Payload.viewer<Viewer> -> D:Viewer.vt<T0> -> D:T0.f2<T2>",
                "B:Mutation.doB<Payload> -> B:Payload.viewer<Viewer> -> D:Viewer.vt<T0> -> E:T0.f2<T2>"
            ],
            ViewerRequirementSchemaB,
            ViewerRequirementSchemaD,
            ViewerRequirementSchemaE);

        // act
        var plan = PlanOperation(schema, "{ viewer { vt { f2 { rqD } } } }");

        // assert
        MatchInline(
            plan,
            """
            operation:
              - document: |
                  {
                    viewer {
                      vt {
                        f2 {
                          rqD
                          id @fusion__requirement
                        }
                      }
                    }
                  }
                hash: 123456789101112
                searchSpace: 2
                expandedNodes: 6
            nodes:
              - id: 1
                type: Operation
                schema: E
                operation: |
                  query Op_123456789101112_1 {
                    viewer {
                      vt {
                        f2 {
                          id
                        }
                      }
                    }
                  }
              - id: 2
                type: Operation
                schema: D
                operation: |
                  query Op_123456789101112_2($__fusion_1_v: ID) {
                    viewer {
                      vt {
                        f2 {
                          rqD(v: $__fusion_1_v)
                        }
                      }
                    }
                  }
                requirements:
                  - name: __fusion_1_v
                    selectionMap: >-
                      id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Plan_Should_FetchRequirementFromMutationCall_When_ViewerIsReRooted()
    {
        // arrange
        var schema = ComposeSchema(
            _ => { },
            """
            # name: B
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
            """,
            ViewerRequirementSchemaD,
            ViewerRequirementSchemaE);

        // act
        var plan = PlanOperation(schema, "mutation { doB { viewer { vt { f2 { rqD } } } } }");

        // assert
        MatchInline(
            plan,
            """
            operation:
              - document: |
                  mutation {
                    doB {
                      viewer {
                        vt {
                          f2 {
                            rqD
                            id @fusion__requirement
                          }
                        }
                      }
                    }
                  }
                hash: 123456789101112
                searchSpace: 1
                expandedNodes: 1
            nodes:
              - id: 1
                type: Operation
                schema: B
                operation: |
                  mutation Op_123456789101112_1 {
                    doB {
                      viewer {
                        vt {
                          f2 {
                            id
                          }
                        }
                      }
                    }
                  }
              - id: 2
                type: Operation
                schema: D
                operation: |
                  query Op_123456789101112_2($__fusion_1_v: ID) {
                    viewer {
                      vt {
                        f2 {
                          rqD(v: $__fusion_1_v)
                        }
                      }
                    }
                  }
                source: $.viewer
                target: $.doB.viewer.vt.f2
                requirements:
                  - name: __fusion_1_v
                    selectionMap: >-
                      id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Plan_Should_FetchRequiredDataThroughLookup_When_LookupKeyBelowRequirementSelectsRequiringField()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: a
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
            """,
            """
            # name: b
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
            """,
            """
            # name: c
            type Query {
              nByK(k: String!): N @lookup @internal
            }

            type N {
              k: String! @shareable
              z: String @shareable
            }
            """,
            """
            # name: d
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
            """,
            """
            # name: e
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
            """,
            """
            # name: g
            type Query {
              nById(id: ID!): N @lookup @internal
            }

            type N {
              id: ID! @shareable
              z: String @shareable
            }
            """);

        // act
        var plan = PlanOperation(schema, "{ x { t { f } } }");

        // assert
        MatchInline(
            plan,
            """
            operation:
              - document: |
                  {
                    x {
                      t {
                        f
                        n @fusion__requirement {
                          z @fusion__requirement
                          id @fusion__requirement
                        }
                      }
                      id @fusion__requirement
                    }
                  }
                hash: 123456789101112
                searchSpace: 3
                expandedNodes: 10
            nodes:
              - id: 2
                type: Operation
                schema: a
                operation: |
                  query Op_123456789101112_2($__fusion_1_id: ID!, $__fusion_2_z: String) {
                    xById(id: $__fusion_1_id) {
                      t {
                        f(z: $__fusion_2_z)
                      }
                    }
                  }
                source: $.xById
                target: $.x
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                  - name: __fusion_2_z
                    selectionMap: >-
                      n.z
                dependencies:
                  - id: 3
                  - id: 5
              - id: 3
                type: Operation
                schema: g
                operation: |
                  query Op_123456789101112_3($__fusion_3_id: ID!) {
                    nById(id: $__fusion_3_id) {
                      z
                    }
                  }
                source: $.nById
                target: $.x.t.n
                requirements:
                  - name: __fusion_3_id
                    selectionMap: >-
                      id
                dependencies:
                  - id: 5
              - id: 1
                type: Operation
                schema: b
                operation: |
                  query Op_123456789101112_1 {
                    x {
                      t {
                        n {
                          __typename
                        }
                      }
                      id
                    }
                  }
                batchingGroupId: 5
              - id: 4
                type: Operation
                schema: b
                operation: |
                  query Op_123456789101112_4 {
                    x {
                      t {
                        n {
                          id
                        }
                      }
                    }
                  }
                batchingGroupId: 5
            """);
    }

    [Fact]
    public void Plan_Should_EnterEventStreamObjectAgain_When_MessageHoldsLookupKey()
    {
        // arrange
        var schema = ComposeSchema(
            _ => { },
            """
            # name: a
            type Query {
              t2ById(id: ID!): T2 @lookup @internal
            }

            type Subscription {
              ev: T2 @eventStream(topics: ["e"], message: "{ id f2 { __typename } }")
            }

            type T2 {
              id: ID!
              f2: T3
            }

            type T3 {
              s0: String
            }
            """,
            """
            # name: b
            type Query {
              ping: String
            }
            """);

        // act
        var plan = PlanOperation(schema, "subscription { ev { f2 { s0 } } }");

        // assert
        MatchInline(
            plan,
            """
            operation:
              - document: |
                  subscription {
                    ev {
                      f2 {
                        s0
                      }
                      id @fusion__requirement
                    }
                  }
                hash: 123456789101112
                searchSpace: 1
                expandedNodes: 2
            nodes:
              - id: 1
                type: EventStream
                fieldName: ev
                resultSelectionSet: >-
                  { ev }
                eventStream:
                  schema: a
                  topics: [e]
                  message: { id f2 { __typename } }
              - id: 2
                type: Operation
                schema: a
                operation: |
                  query Op_123456789101112_2($__fusion_1_id: ID!) {
                    t2ById(id: $__fusion_1_id) {
                      f2 {
                        s0
                      }
                    }
                  }
                source: $.t2ById
                target: $.ev
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Plan_Should_EnterAncestorOfEventStreamObjectAgain_When_MessageHoldsLookupKey()
    {
        // arrange
        var schema = ComposeSchema(
            _ => { },
            """
            # name: a
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
            """,
            """
            # name: b
            type Query {
              ping: String
            }
            """);

        // act
        var plan = PlanOperation(schema, "subscription { ev { f1 { f2 { s0 } } } }");

        // assert
        MatchInline(
            plan,
            """
            operation:
              - document: |
                  subscription {
                    ev {
                      f1 {
                        f2 {
                          s0
                        }
                      }
                      id @fusion__requirement
                    }
                  }
                hash: 123456789101112
                searchSpace: 1
                expandedNodes: 2
            nodes:
              - id: 1
                type: EventStream
                fieldName: ev
                resultSelectionSet: >-
                  { ev }
                eventStream:
                  schema: a
                  topics: [e]
                  message: { id f1 { f2 { __typename } } }
              - id: 2
                type: Operation
                schema: a
                operation: |
                  query Op_123456789101112_2($__fusion_1_id: ID!) {
                    t1ById(id: $__fusion_1_id) {
                      f1 {
                        f2 {
                          s0
                        }
                      }
                    }
                  }
                source: $.t1ById
                target: $.ev
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Plan_Should_Fail_When_ReRootedViewerCallTakesRequirementThroughLookupKeyedByOtherReRootedCall()
    {
        // arrange
        var schema = ComposeSchema(
            options => options.Satisfiability.IgnoredNonAccessibleFields["T2.rqD"] =
            [
                "B:Mutation.doB<Payload> -> B:Payload.viewer<Viewer> -> D:Viewer.vt<T0> -> D:T0.f2<T2>",
                "B:Mutation.doB<Payload> -> B:Payload.viewer<Viewer> -> D:Viewer.vt<T0> -> E:T0.f2<T2>"
            ],
            ViewerRequirementSchemaB,
            """
            # name: D
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
            """,
            """
            # name: E
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
            """,
            """
            # name: G
            type Query {
              t2ById(id: ID!): T2 @lookup @internal
            }

            type T2 {
              id: ID! @shareable
              s: String @shareable
            }
            """);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(schema, "mutation { doB { viewer { vt { f2 { rqD } } } } }"));

        // assert
        Assert.Equal("No possible plan was found.", exception.Message);
    }

    [Fact]
    public void Plan_Should_Fail_When_MutationCallTakesRequirementFromReRootedCall()
    {
        // arrange
        var schema = ComposeSchema(
            options => options.Satisfiability.IgnoredNonAccessibleFields["T2.rqB"] =
            [
                "E:Query.viewer<Viewer> -> E:Viewer.vt<T0> -> E:T0.f2<T2>",
                "B:Mutation.doB<Payload> -> B:Payload.viewer<Viewer> -> B:Viewer.vt<T0> -> B:T0.f2<T2>",
                "B:Mutation.doB<Payload> -> B:Payload.viewer<Viewer> -> B:Viewer.vt<T0> -> E:T0.f2<T2>"
            ],
            """
            # name: B
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
            """,
            """
            # name: E
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
            """);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(schema, "mutation { doB { viewer { vt { f2 { rqB } } } } }"));

        // assert
        Assert.Equal("No possible plan was found.", exception.Message);
    }

    private static FusionSchemaDefinition ComposeLookupKeySchema()
        => ComposeSchema(
            options =>
            {
                var ignored = options.Satisfiability.IgnoredNonAccessibleFields;
                ignored["P.k"] = ["A:Query.bad<X> -> A:X.p<P>"];
                ignored["T.f"] = ["A:Query.bad<X> -> A:X.p<P> -> A:P.t<T>"];
                ignored["T.q"] = ["A:Query.bad<X> -> A:X.p<P> -> A:P.t<T>"];
            },
            LookupKeySchemaA,
            LookupKeySchemaC,
            LookupKeySchemaD);

    private static FusionSchemaDefinition ComposeNestedLookupKeySchema()
        => ComposeSchema(
            options => options.Satisfiability.IgnoredNonAccessibleFields["T.f"] =
                ["MAIN:Query.viaB<V> -> MAIN:V.q<U> -> MAIN:U.p<T>"],
            """
            # name: main
            type Query {
              viaA: V @shareable
              viaB: V
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
            """,
            """
            # name: a
            type Query {
              tByK(k: String!): T @lookup @internal
            }

            type T {
              k: String! @inaccessible @shareable
              x: String @inaccessible
            }
            """,
            """
            # name: b
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
            """,
            """
            # name: x
            type Query {
              viaA: V @shareable
            }

            type V {
              q: U @shareable
            }

            type U {
              kU: String! @inaccessible @shareable
            }
            """);

    private static FusionSchemaDefinition ComposeHolderDataSchema()
        => ComposeSchema(
            options => options.Satisfiability.IgnoredNonAccessibleFields["T.f"] =
                ["R:Query.y<Y> -> G2:Y.p<P> -> S:P.fp<T>"],
            s_holderDataSchemas);
}
