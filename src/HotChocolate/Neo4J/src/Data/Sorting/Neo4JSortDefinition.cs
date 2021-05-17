﻿using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J.Sorting
{
    public class Neo4JSortDefinition
    {
        public Neo4JSortDefinition(string field, SortDirection direction)
        {
            Field = field;
            Direction = direction;
        }

        public string Field { get; set; }

        public SortDirection Direction { get; set; }

    }
}
