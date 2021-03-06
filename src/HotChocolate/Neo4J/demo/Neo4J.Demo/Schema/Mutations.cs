using System.Collections.Generic;
using HotChocolate.Types;
using Neo4jDemo.Models;

namespace Neo4jDemo.Schema
{
    [ExtendObjectType(Name = "Mutation")]
    public class Mutations
    {
        public bool CreateBusinesses(List<Business> businesses) => true;
        public bool UpdateBusinesses(List<Business> businesses) => true;
        public bool DeleteBusinesses(List<Business> businesses) => true;
    }
}
