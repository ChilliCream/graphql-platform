using System.Linq;
using ServiceStack;

namespace HotChocolate.Data.Neo4J
{
    internal class EntityPropertyAccessor<T> : IEntityPropertyAccessor
    {
        private readonly SetMemberDelegate nodeIdPropertySetter;
        private readonly GetMemberDelegate nodeIdPropertyGetter;

        public EntityPropertyAccessor()
        {
            var allProperties = typeof(T).GetAllProperties();
            var nodeIdProperty = allProperties.SingleOrDefault(p => p.HasAttributeNamed(nameof(NodeIdAttribute)));
            if (nodeIdProperty == null) return;

            nodeIdPropertySetter = nodeIdProperty.CreateSetter();
            nodeIdPropertyGetter = nodeIdProperty.CreateGetter();
        }

        public long? GetNodeId(object instance)
        {
            return (long?) nodeIdPropertyGetter?.Invoke(instance);
        }

        public void SetNodeId(object instance, long id)
        {
            nodeIdPropertySetter?.Invoke(instance, id);
        }
    }
}
