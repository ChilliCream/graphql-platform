using System;

namespace HotChocolate.Data.Neo4J
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Neo4JNodeAttribute : Attribute
    {
        public string[] Labels { get; set; }

        public Neo4JNodeAttribute(params string[] labels)
        {
            Labels = labels;
        }
    }
}
