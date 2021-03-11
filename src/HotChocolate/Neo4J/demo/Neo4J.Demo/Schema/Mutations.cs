using System.Collections.Generic;
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
        public bool CreateBusinesses([ScopedService] Neo4JRepository repo)
        {
            var a = new Business() {Name = "A"};
            var r = new Review() {Text = "text"};
            a.Reviews = new List<Review>() {r};

            repo.Create(a);

            return true;
        }
        public bool UpdateBusinesses(List<Business> businesses) => true;
        public bool DeleteBusinesses(List<Business> businesses) => true;
    }
}
