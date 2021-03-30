using HotChocolate;
using HotChocolate.Data.Neo4J;
using HotChocolate.Types;
using Neo4jDemo.Models;

namespace Neo4jDemo.Schema
{
    [ExtendObjectType(Name = "Mutation")]
    public class Mutations
    {
        [UseNeo4JRepository]
        public Business CreateBusiness([ScopedService] Neo4JRepository repo, Business input)
        {
            Business entity = repo.Create(input);
            return entity;
        }

        [UseNeo4JRepository]
        public Business UpdateBusiness([ScopedService] Neo4JRepository repo, Business input)
        {
            Business entity = repo.Update(input);
            return entity;
        }

        [UseNeo4JRepository]
        public bool DeleteBusiness([ScopedService] Neo4JRepository repo, long id)
        {
            repo.DeleteById<Business>(id);
            return true;
        }
    }
}
