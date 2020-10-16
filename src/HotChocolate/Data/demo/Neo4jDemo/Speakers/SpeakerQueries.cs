using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Data.Neo4j;
using HotChocolate.Types;
using Neo4j.Driver;
using Neo4jDemo.Extensions;
using Neo4jMapper;

namespace Neo4jDemo
{
    [ExtendObjectType(Name = "Query")]
    public class SpeakerQueries
    {
        //[UseSession]
        //[UseFiltering]
        //public async Task<List<Speaker>> Speakers(FieldNode fieldSelection, ISchema schema, DocumentNode document, [ScopedService] IAsyncSession session){

        //    IResultCursor cursor = await session.RunAsync(@"MATCH (speaker:Speaker {name: 'Bill Crosby'}) RETURN speaker");

        //    return await cursor.MapAsync<Speaker>();
        //}

        [UseSession]
        [UseFiltering]
        public async Task<List<Speaker>> Speakers([ScopedService] IAsyncSession session)
        {
            Node m = new Node("Speaker").Named("m");
            IResultCursor cursor = await new Cypher(session)
                                                        .Match(m)
                                                        .Return(m)
                                                        .ExecuteAsync();

            return await cursor.MapAsync<Speaker>();
        }
    }
}
