using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Sorting;
using HotChocolate.Execution;
using Xunit;

 namespace HotChocolate.Data.Neo4J.Filtering;

 public class Neo4JFilterCombinatorTests
        : IClassFixture<Neo4JFixture>
 {
     private readonly Neo4JFixture _fixture;

     public Neo4JFilterCombinatorTests(Neo4JFixture fixture)
     {
         _fixture = fixture;
     }

     private const string _fooEntitiesCypher =
         @"CREATE (:FooBool {Bar: true}), (:FooBool {Bar: false})";

     [Fact]
     public async Task Create_Empty_Expression()
     {
         // arrange
         var tester =
             await _fixture.GetOrCreateSchema<FooBool, FooBoolFilterType>(_fooEntitiesCypher);

         // act
         // assert
         IExecutionResult res1 = await tester.ExecuteAsync(
             QueryRequestBuilder.New()
                 .SetQuery("{ root(where: { }){ bar }}")
                 .Create());

            res1.MatchDocumentSnapshot("ASC");
     }

     public class FooBool
     {
         public bool Bar { get; set; }
     }

     public class FooBoolFilterType : FilterInputType<FooBool>
     {
     }
 }
