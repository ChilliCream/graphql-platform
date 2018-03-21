using System;
using System.Collections.Generic;
using System.Linq;
using Prometheus.Resolvers;

namespace Prometheus.Types
{
    public class UnionType
        : IOutputType
        , INamedType
        , INullableType
    {
        private readonly UnionTypeConfig _config;
        private readonly ResolveType _typeResolver;
        private Dictionary<string, ObjectType> _types;

        public UnionType(UnionTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "An type name must not be null or empty.",
                    nameof(config));
            }

            _config = config;
            _typeResolver = config.TypeResolver;
            Name = config.Name;
            Description = config.Description;
        }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyDictionary<string, ObjectType> Types
        {
            get
            {
                if (_types == null)
                {
                    var types = _config.Types();
                    if (types == null)
                    {
                        throw new InvalidOperationException(
                            "An union type must have at least two types.");
                    }
                    _types = types.ToDictionary(t => t.Name);
                }
                return _types;
            }
        }

        public ObjectType ResolveType(IResolverContext context, object resolverResult)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _typeResolver?.Invoke(context, resolverResult);
        }
    }

    public class UnionTypeConfig
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public Func<IEnumerable<ObjectType>> Types { get; set; }

        public ResolveType TypeResolver { get; set; }
    }
}