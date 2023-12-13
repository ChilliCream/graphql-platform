using CookieCrumble;
using HotChocolate.Fusion.Composition.Pipeline;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class UnionMergeTests(ITestOutputHelper output)
    : CompositionTestBase(
        output, 
        new UnionTypeMergeHandler(), 
        new ObjectTypeMergeHandler(), 
        new ScalarTypeMergeHandler())
{
    [Fact]
    public async Task Identical_Unions_Merge()
        => await Succeed(
                """
                union Abc = A | B

                type A { a: String }
                
                type B { b: String }
                """,
                """
                union Abc = A | B

                type A { a: String }
                
                type B { b: String }
                """)
            .MatchInlineSnapshotAsync(
                """"
                type A
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  a: String
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }
                
                type B
                  @source(subgraph: "A")
                  @source(subgraph: "B") {
                  b: String
                    @source(subgraph: "A")
                    @source(subgraph: "B")
                }
                
                union Abc
                  @source(subgraph: "A")
                  @source(subgraph: "B") = A | B
                """");
    
    [Fact]
    public async Task Union_Split_Across_Subgraphs_Merge()
        => await Succeed(
                """
                union Abc = A
                
                type A { a: String }
                """,
                """
                union Abc = B
                
                type B { b: String }
                """)
            .MatchInlineSnapshotAsync(
                """"
                type A
                  @source(subgraph: "A") {
                  a: String
                    @source(subgraph: "A")
                }
                
                type B
                  @source(subgraph: "B") {
                  b: String
                    @source(subgraph: "B")
                }
                
                union Abc
                  @source(subgraph: "A")
                  @source(subgraph: "B") = A | B
                """");
}