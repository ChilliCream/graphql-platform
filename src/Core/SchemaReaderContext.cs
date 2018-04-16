using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    public abstract class SchemaReaderContext
    {
        private readonly Dictionary<string, INamedType> _types = new Dictionary<string, INamedType>();
        private readonly Dictionary<string, IsOfType> _ofTypes = new Dictionary<string, IsOfType>();
        private readonly Dictionary<string, ResolveType> _typeResolver = new Dictionary<string, ResolveType>();


        public void Register(INamedType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            _types.Add(type.Name, type);
        }


        public IOutputType GetOutputType(string name)
        {
            if (_types.TryGetValue(name, out var t)
                && t is IOutputType ot)
            {
                return ot;
            }
            throw new ArgumentException(
                "The specified type does not exist or is not an output type.");
        }
        public T GetOutputType<T>(string name)
            where T : IOutputType
        {
            if (_types.TryGetValue(name, out var t)
                && t is T ot)
            {
                return ot;
            }
            throw new ArgumentException(
                "The specified type does not exist or is " +
                "not of the specified type.");
        }

        public IInputType GetInputType(string name)
        {
            if (_types.TryGetValue(name, out var t)
                && t is IInputType it)
            {
                return it;
            }
            throw new ArgumentException(
                "The specified type does not exist or is not an output type.");
        }

        public FieldResolverDelegate CreateResolver(
            ObjectType objectType, Field field)
        {
            throw new NotImplementedException();
        }

        public IsOfType CreateIsOfType(string name)
        {
            if (_ofTypes.TryGetValue(name, out var ot))
            {
                return ot;
            }

            return new IsOfType((c, r) =>
                c.ObjectType.Name == r.GetType().Name);
        }

        public ResolveType CreateTypeResolver(string name)
        {
            if (_typeResolver.TryGetValue(name, out var rt))
            {
                return rt;
            }

            throw new NotImplementedException();
        }

    }
}