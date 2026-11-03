using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityErrorExplanationTests
{
    [Fact]
    public void Validate_Should_CapTransitionExplanations_When_LookupKeysDependOnEachOther()
    {
        // arrange
        // fuzz seed 647: below the event stream, every lookup key needs data from another lookup
        var schema = CreateFuzzSeed647Schema(
            eventStreamMessage: "{ s0 s1 s2 f0 { __typename } }",
            requiringField: "");
        var log = new CompositionLog();
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.InRange(satisfiabilityValidator.ExplanationEntryCount, 1, 20_000);
        Assert.InRange(log.Sum(e => e.Message.Length), 1, 25_000);
        var message = GetMessage(log, "Unable to access the field 'T1.id' on path 'C:");
        message.MatchInlineSnapshot(
            """
            Unable to access the field 'T1.id' on path 'C:Subscription.ev<T1>'.
              Unable to transition between schemas 'C' and 'A' for access to field 'A:T1.id<ID>'.
                Unable to satisfy the requirement '{ id }' for lookup 'iById' in schema 'A'.
                  ... and 1 more
              Unable to transition between schemas 'C' and 'B' for access to field 'B:T1.id<ID>'.
                Unable to satisfy the requirement '{ f0 { id } }' for lookup 't1ByF0' in schema 'B'.
                  ... and 1 more
                Unable to satisfy the requirement '{ id }' for lookup 'iById' in schema 'B'.
                  ... and 1 more
              Unable to transition between schemas 'C' and 'E' for access to field 'E:T1.id<ID>'.
                No lookups found for type 'T1' in schema 'E'.
              Unable to transition between schemas 'C' and 'F' for access to field 'F:T1.id<ID>'.
                Unable to satisfy the requirement '{ id }' for lookup 't1ById' in schema 'F'.
                  ... and 1 more
                Unable to satisfy the requirement '{ id }' for lookup 'iById' in schema 'F'.
                  ... and 1 more
              Unable to transition between schemas 'C' and 'G' for access to field 'G:T1.id<ID>'.
                Unable to satisfy the requirement '{ f0 { id } }' for lookup 't1ByF0' in schema 'G'.
                  ... and 1 more
                Unable to satisfy the requirement '{ id }' for lookup 'iById' in schema 'G'.
                  ... and 1 more
            """);
    }

    [Fact]
    public void Validate_Should_CapRequirementExplanations_When_RequiredFieldNeedsDependentLookups()
    {
        // arrange
        // fuzz seed 647 with a field of T1 in C that requires the id, which only the lookups resolve
        var schema = CreateFuzzSeed647Schema(
            eventStreamMessage: "{ s0 s1 s2 rqC f0 { __typename } }",
            requiringField: "rqC(v: ID @require(field: \"id\")): String");
        var log = new CompositionLog();
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.InRange(satisfiabilityValidator.ExplanationEntryCount, 1, 20_000);
        var message = GetMessage(log, "Unable to access the field 'T1.rqC' on path 'C:");
        string.Join('\n', message.Split('\n').Take(6)).MatchInlineSnapshot(
            """
            Unable to access the field 'T1.rqC' on path 'C:Subscription.ev<T1>'.
              Unable to satisfy the requirement '{ id }' on field 'C:T1.rqC<String>'.
                Unable to satisfy the requirement 'id'.
                  Unable to access the required field 'T1.id' on path 'C:Subscription.ev<T1>'.
                    Unable to transition between schemas 'C' and 'A' for access to required field 'A:T1.id<ID>'.
                      Unable to satisfy the requirement '{ id }' for lookup 'iById' in schema 'A'.
            """);
    }

    [Fact]
    public void CreateError_Should_StopSearchAndKeepHoldingEntries_When_PruningBudgetExceedsFailures()
    {
        // arrange
        var budget = SatisfiabilityExplanationBudget.Pruning(maxEntries: 1, maxFailures: 2);
        var created = budget.CreateError("created");
        var cut = budget.CreateError("cut");
        var exhaustedAtLimit = budget.IsExhausted;

        // act
        var holder = budget.CreateError("holder", [created, cut]);

        // assert
        Assert.False(exhaustedAtLimit);
        Assert.True(budget.IsExhausted);
        Assert.Same(SatisfiabilityError.Unexplained, cut);
        holder.ToString().MatchInlineSnapshot(
            """
            holder
              created
              ... and 1 more
            """);
    }

    [Fact]
    public void ToString_Should_EndEachCutLevelWithAndMore_When_ExplanationExceedsCaps()
    {
        // arrange
        var error = new SatisfiabilityError(
            "root",
            [
                new SatisfiabilityError("a", [new("a1"), new("a2"), new("a3")]),
                new SatisfiabilityError("b", [new("b1"), SatisfiabilityError.Unexplained]),
                new SatisfiabilityError("c"),
                new SatisfiabilityError("d")
            ]);

        // act
        var message = error.ToString(maxLines: 6, maxEntriesPerLevel: 2);

        // assert
        message.MatchInlineSnapshot(
            """
            root
              a
                a1
                a2
                ... and 1 more
              b
                ... and 2 more
              ... and 2 more
            """);
    }

    [Fact]
    public void ToString_Should_RenderEveryEntry_When_ExplanationIsBelowCaps()
    {
        // arrange
        var error = new SatisfiabilityError(
            "root",
            [
                new SatisfiabilityError("a", [new("a1"), new("a2")]),
                new SatisfiabilityError("b")
            ]);

        // act
        var message = error.ToString();

        // assert
        message.MatchInlineSnapshot(
            """
            root
              a
                a1
                a2
              b
            """);
    }

    private static string GetMessage(CompositionLog log, string prefix)
        => log.Single(e => e.Message.StartsWith(prefix, StringComparison.Ordinal)).Message;

    private static MutableSchemaDefinition CreateFuzzSeed647Schema(
        string eventStreamMessage,
        string requiringField)
    {
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                type Query {
                  ping: String @shareable
                  q0: T0 @shareable
                  q1: T0 @shareable
                  iById(id: ID!): I @lookup
                }

                interface I {
                  id: ID!
                }

                type T0 {
                  id: ID! @shareable
                  s1: String @external
                  s2: String @shareable
                  f1: T1 @shareable
                  f2: T0 @shareable
                }

                type T1 implements I {
                  id: ID! @shareable
                  s2: String @shareable
                  f0: T0 @shareable
                  f1: I @shareable
                  f2: T1 @shareable
                }
                """,
                """
                type Query {
                  ping: String @shareable
                  q0: T0 @shareable
                  t1ByF0(id: ID! @is(field: "f0.id")): T1 @lookup
                  iById(id: ID!): I @lookup
                }

                interface I {
                  id: ID!
                }

                type T0 {
                  id: ID! @shareable
                  s1: String @shareable
                  s2: String @external
                  f0: T1 @shareable
                  f2: T0 @shareable
                  rqB(v: String @require(field: "s2")): String
                  xB: String
                }

                type T1 implements I {
                  id: ID! @shareable
                  s1: String @shareable
                  f0: T0 @shareable @provides(fields: "s2")
                  xB: String
                }
                """,
                $$"""
                type Query {
                  ping: String @shareable
                  q0: T0 @shareable
                  q1: T0 @shareable
                }

                type Subscription {
                  ev: T1 @eventStream(topics: ["e"], message: "{{eventStreamMessage}}")
                }

                type T0 {
                  id: ID! @shareable
                  s0: String @shareable
                  s1: String @shareable
                  s2: String @shareable
                  f1: T1 @shareable
                  f2: T0 @shareable
                }

                type T1 {
                  s0: String @shareable
                  s1: String @shareable
                  s2: String @shareable
                  f0: T0 @shareable
                  f2: T1 @shareable
                  {{requiringField}}
                }
                """,
                """
                type Query {
                  ping: String @shareable
                  q0: T0 @shareable
                  q1: T0 @shareable
                }

                type T0 {
                  id: ID! @shareable
                  s0: String @shareable
                  s1: String @shareable
                  s2: String @shareable
                  f1: T1 @shareable
                  f2: T0 @shareable
                }

                type T1 {
                  s0: String @shareable
                  s1: String @shareable
                  s2: String @shareable
                  f0: T0 @shareable
                  f2: T1 @shareable
                }
                """,
                """
                type Query {
                  ping: String @shareable
                  q0: T0 @shareable
                  q1: T0 @shareable
                }

                interface I {
                  id: ID!
                }

                type T0 {
                  id: ID! @shareable
                  s1: String @shareable
                  s2: String @shareable
                  f1: T1 @shareable
                  f2: T0 @shareable
                  rqE(v: String @require(field: "s1")): String
                }

                type T1 implements I {
                  id: ID! @shareable
                  s2: String @external
                  f0: T0 @shareable
                  f1: I @shareable
                  f2: T1 @shareable
                }
                """,
                """
                type Query {
                  ping: String @shareable
                  q1: T0 @shareable
                  q2: I @shareable
                  q3: I @shareable
                  t1ById(id: ID!): T1 @lookup
                  iById(id: ID!): I @lookup
                }

                interface I {
                  id: ID!
                }

                type T0 {
                  id: ID! @shareable
                  s2: String @shareable
                  f1: T1 @shareable
                  f2: T0 @shareable
                }

                type T1 implements I {
                  id: ID! @shareable
                  s0: String @external
                  s2: String @shareable
                  f1: I @shareable
                }
                """,
                """
                type Query {
                  ping: String @shareable
                  q0: T0 @shareable
                  q1: T0 @shareable
                  t1ByF0(id: ID! @is(field: "f0.id")): T1 @lookup
                  iById(id: ID!): I @lookup
                }

                interface I {
                  id: ID!
                }

                type T0 {
                  id: ID! @shareable
                  s1: String @shareable
                  s2: String @shareable
                  f1: T1 @shareable
                  f2: T0 @shareable
                }

                type T1 implements I {
                  id: ID! @shareable
                  s2: String @shareable
                  f0: T0 @shareable
                  f1: I @shareable
                  f2: T1 @shareable
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        return merger.Merge().Value;
    }
}
