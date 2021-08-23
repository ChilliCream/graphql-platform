using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Stitching.WellKnownContextData;

namespace HotChocolate.Stitching.Utilities
{
    internal class StitchingTypeInterceptor : TypeInterceptor
    {
        private readonly HashSet<(NameString, NameString)> _handledExternalFields =
            new HashSet<(NameString, NameString)>();

        public override void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is ObjectTypeDefinition objectTypeDef)
            {
                foreach (ObjectFieldDefinition objectField in objectTypeDef.Fields)
                {
                    if (objectField.GetDirectives().Any(IsDelegatedField))
                    {
                        FieldMiddleware handleDictionary =
                            FieldClassMiddlewareFactory.Create<DictionaryResultMiddleware>();
                        FieldMiddleware delegateToSchema =
                            FieldClassMiddlewareFactory.Create<DelegateToRemoteSchemaMiddleware>();

                        objectField.MiddlewareComponents.Insert(0, handleDictionary);
                        objectField.MiddlewareComponents.Insert(0, delegateToSchema);
                        _handledExternalFields.Add((objectTypeDef.Name, objectField.Name));
                    }
                }
            }

            if (definition is SchemaTypeDefinition)
            {
                if (discoveryContext.ContextData.TryGetValue(RemoteExecutors, out object? value))
                {
                    // we copy the remote executors that are stored only on the
                    // schema builder context to the schema context so that
                    // the stitching context can access these at runtime.
                    contextData.Add(RemoteExecutors, value);
                }

                contextData.Add(NameLookup, discoveryContext.GetNameLookup());
            }
        }

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (completionContext.Type is ObjectType objectType &&
                definition is ObjectTypeDefinition objectTypeDef)
            {
                IReadOnlyDictionary<NameString, ISet<NameString>> externalFieldLookup =
                    completionContext.GetExternalFieldLookup();
                if (externalFieldLookup.TryGetValue(
                    objectType.Name,
                    out ISet<NameString>? external))
                {
                    foreach (ObjectFieldDefinition objectField in objectTypeDef.Fields)
                    {
                        if (external.Contains(objectField.Name) &&
                            _handledExternalFields.Add((objectTypeDef.Name, objectField.Name)))
                        {
                            FieldMiddleware handleDictionary =
                                FieldClassMiddlewareFactory.Create<DictionaryResultMiddleware>();
                            objectField.MiddlewareComponents.Insert(0, handleDictionary);
                        }
                    }
                }
            }
        }

        private static bool IsDelegatedField(DirectiveDefinition directiveDef)
        {
            if (directiveDef.Reference is NameDirectiveReference nameRef &&
                nameRef.Name.Equals(DirectiveNames.Delegate))
            {
                return true;
            }

            if (directiveDef.Reference is ClrTypeDirectiveReference typeRef &&
                typeRef.ClrType == typeof(DelegateDirective))
            {
                return true;
            }

            return false;
        }
    }
}
