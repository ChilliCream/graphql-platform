using System;
using System.Collections.Generic;

namespace Neo4jDemo
{
    public class Speaker
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }
        public int Age { get; set; }
        public List<Session> Sessions { get; set; }
    }
}
