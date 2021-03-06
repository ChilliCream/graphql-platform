using System;
using System.Reflection;

namespace HotChocolate.Data.Neo4J
{
    public abstract class MetaProperty
    {
        public PropertyInfo Info { get; }

        protected MetaProperty(PropertyInfo info)
        {
            Info = info;
        }

        public string GetName()
        {
            return Info.Name;
        }

        public object GetValue(object instance)
        {
            return Info.GetValue(instance);
        }

        public void SetValue(object instance, object value)
        {
            Info.SetValue(instance, value);
        }
    }

    public class RegularProperty : MetaProperty
    {
        public bool IsCreatedAt { get; init; }

        public bool IsUpdatedAt { get; init; }

        public RegularProperty(PropertyInfo info) : base(info)
        {
        }
    }

    public class RelationshipProperty : MetaProperty
    {
        public bool IsCollection { get; }

        public Type EntityType { get; }

        public RelationshipDirection Direction { get; }

        public string RelationshipType { get; }

        public RelationshipProperty(PropertyInfo info, Neo4JRelationshipAttribute attribute) : base(info)
        {
            var t = Info.PropertyType;
            //IsCollection = Info.IsCollection();
            //EntityType = IsCollection ? t.GetTypeInfo().GenericTypeArguments[0] : t;
            //Direction = attribute.Direction;
            //RelationshipType = attribute.Type;
        }
    }
}
