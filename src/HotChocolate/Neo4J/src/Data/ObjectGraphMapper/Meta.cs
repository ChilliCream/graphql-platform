using System;
using System.Collections.Generic;
using System.Reflection;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public class Meta
    {
        /*
         * node entity type
         */
        public Type RawType { get; set; }

        public string Label { get; set; }

        public string Key { get; set; }

        /**
         * Normal properties
         */
        public List<RegularProperty> RegularProperties { get; } = new();

        public List<RelationshipProperty> RelationshipProperties { get; } = new();

        public PropertyInfo IdField { get; set; }

        public long? GetId(object entity)
        {
            return IdField.GetValue(entity).As<long?>();
        }

        public void SetId(object entity, long id)
        {
            IdField.SetValue(entity, id);
        }

        public bool HasRelationshipProperty()
        {
            return RelationshipProperties.Count > 0;
        }
    }
}
