using System.Collections.Generic;

namespace Neo4jDemo
{
    public class Speaker
    {
     
        public string Name { get; set; }

        public string Bio { get; set; }
        public int Age { get; set; }
        public List<Session> Sessions { get; set; }
    }
}
