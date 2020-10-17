using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Data;
using HotChocolate.Data.Neo4j;
using HotChocolate.Types;
using Neo4j.Driver;
using Neo4jDemo.Extensions;
using Neo4jMapper;
using System;

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
        public IExecutable<Speaker> Speakers([ScopedService] IAsyncSession session)
        {
            Node m = new Node("Speaker").Named("m");
            return new Cypher<Speaker>(session).Match(m).Return(m);
        }
    }
}
