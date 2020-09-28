using System.Collections.Generic;
using HotChocolate.Data;

namespace StarWars
{
    public class Query
    {
        [UseFiltering]
        public IReadOnlyList<Person> GetPersons() => new[]
        {
            new Person { Name = "Pascal" },
            new Person { Name = "Rafael" },
            new Person { Name = "Michael" }
        };
    }
}
