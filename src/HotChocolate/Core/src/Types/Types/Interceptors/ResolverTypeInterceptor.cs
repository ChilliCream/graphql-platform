using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Interceptors
{
    internal sealed class ResolverTypeInterceptor : TypeInterceptor
    {
        private readonly Dictionary<NameString, ObjectFieldDefinition> _fields = new();
        private readonly List<FieldResolverConfig> _fieldResolvers;
        private ILookup<NameString, FieldResolverConfig> _configs = default!;

        public ResolverTypeInterceptor(List<FieldResolverConfig> fieldResolvers)
        {
            _fieldResolvers = fieldResolvers;
        }

        public override bool CanHandle(ITypeSystemObjectContext context)
            => _fieldResolvers.Count > 0;

        public override void OnAfterCompleteTypeNames()
        {
            _configs = _fieldResolvers.ToLookup(t => t.Field.TypeName);
        }

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (completionContext.Type is ObjectType type &&
                definition is ObjectTypeDefinition typeDefinition &&
                _configs.Contains(type.Name))
            {
                foreach (ObjectFieldDefinition field in typeDefinition.Fields)
                {
                    _fields[field.Name] = field;
                }

                foreach (FieldResolverConfig config in _configs[type.Name])
                {
                    if (_fields.TryGetValue(config.Field.FieldName, out var field))
                    {
                        field.Resolvers = config.ToFieldResolverDelegates();
                    }
                }

                _fields.Clear();
            }
        }
    }
}
