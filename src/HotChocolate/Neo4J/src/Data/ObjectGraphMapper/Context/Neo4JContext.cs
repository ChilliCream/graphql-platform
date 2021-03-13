using System;
using System.Collections.Generic;
using System.Reflection;

#nullable enable

namespace HotChocolate.Data.Neo4J
{
    public partial class Neo4JContext
    {
        private readonly IDictionary<Type, Meta> _allTypes = new Dictionary<Type, Meta>();

        private readonly IDictionary<string, Meta> _allKeys = new Dictionary<string, Meta>();

        private Assembly Assembly { get; }

        public Neo4JContext(Assembly assembly)
        {
            Assembly = assembly;
            Init();
        }
        private void Init()
        {
            foreach (var t in Assembly.GetTypes())
            {
                if (!IsSupportedEntityType(t)) continue;

                var meta = CreateMeta(t);
                if (_allKeys.ContainsKey(meta.Key))
                {
                    throw _duplicateNodeEntityKey;
                }

                _allKeys.Add(meta.Key, meta);
                _allTypes.Add(t, meta);
            }

            foreach (var type in _allTypes)
            {
                if (!type.Value.HasRelationshipProperty()) continue;
                ValidateRelationship(type.Value);
            }
        }

        public Meta GetMetaData(Type t)
        {
            if (_allTypes.TryGetValue(t, out var val))
            {
                return val;
            }

            throw _unsupportedNodeEntityType;
        }

        private static bool IsSupportedEntityType(MemberInfo t)
        {
            return t.GetCustomAttribute(_nodeType) != null
                   || t.GetCustomAttribute(_relationshipType) != null;
        }

        private static Neo4JNodeAttribute GetEntityAttribute(MemberInfo t)
        {
            var att = t.GetCustomAttribute(_nodeType);
            if (att == null)
            {
                throw _illegalNodeEntityException;
            }

            return (Neo4JNodeAttribute) att;
        }

        private static Meta CreateMeta(Type t)
        {
            var props = t.GetProperties();
            var entityAttribute = GetEntityAttribute(t);
            var meta = new Meta
            {
                RawType = t,
                //Label = entityAttribute.Labels[0],
                Key = entityAttribute.Key,
            };

            var hasId = false;
            foreach (var prop in props)
            {
                if (prop.HasAttribute(_ignoredType))
                {
                    continue;
                }

                if (prop.HasAttribute(_nodeIdType))
                {
                    if (hasId)
                    {
                        throw _duplicateIdException;
                    }

                    if (!prop.IsNullableLong())
                    {
                        throw _illegalIdFormatException;
                    }

                    hasId = true;
                    meta.IdField = prop;
                    continue;
                }

                // relationship property
                Neo4JRelationshipAttribute? relationshipAtt = (Neo4JRelationshipAttribute) prop.GetCustomAttribute(_relationshipType);
                if (relationshipAtt != null)
                {
                    var rp = new RelationshipProperty(prop, relationshipAtt);
                    meta.RelationshipProperties.Add(rp);
                    continue;
                }

                // regular property
                var property = new RegularProperty(prop);
                meta.RegularProperties.Add(property);
            }

            if (!hasId)
            {
                throw _idIsMissingException;
            }

            return meta;
        }

        private void ValidateRelationship(Meta meta)
        {
            foreach (var metaProperty in meta.RelationshipProperties)
            {
                var t = metaProperty.Info.PropertyType;
                if (metaProperty.Info.IsCollection())
                {
                    t = t.GetTypeInfo().GenericTypeArguments[0];
                }

                if (!_allTypes.ContainsKey(t))
                {
                    throw _unsupportedRelationshipType;
                }
            }
        }
    }
}
