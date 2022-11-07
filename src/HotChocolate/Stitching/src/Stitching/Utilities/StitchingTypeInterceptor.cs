using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Resolvers.FieldClassMiddlewareFactory;
using static HotChocolate.Stitching.WellKnownContextData;

namespace HotChocolate.Stitching.Utilities
{
    internal class StitchingTypeInterceptor : TypeInterceptor
    {
        private readonly HashSet<(string, string)> _handledExternalFields =
            new HashSet<(string, string)>();

        public override void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is ObjectTypeDefinition objectTypeDef)
            {
                foreach (var objectField in objectTypeDef.Fields)
                {
                    if (objectField.GetDirectives().Any(IsDelegatedField))
                    {
                        var handleDictionary =
                            Create<DictionaryResultMiddleware>();
                        var delegateToSchema =
                            Create<DelegateToRemoteSchemaMiddleware>();

                        objectField.MiddlewareDefinitions.Insert(0, new(handleDictionary));
                        objectField.MiddlewareDefinitions.Insert(0, new(delegateToSchema));
                        _handledExternalFields.Add((objectTypeDef.Name, objectField.Name));
                    }
                }
            }

            if (definition is SchemaTypeDefinition)
            {
                if (discoveryContext.ContextData.TryGetValue(RemoteExecutors, out var value))
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
                var externalFieldLookup =
                    completionContext.GetExternalFieldLookup();
                if (externalFieldLookup.TryGetValue(objectType.Name,
                        out var external))
                {
                    foreach (var objectField in objectTypeDef.Fields)
                    {
                        if (external.Contains(objectField.Name) &&
                            _handledExternalFields.Add((objectTypeDef.Name, objectField.Name)))
                        {
                            if (objectField.Resolvers.HasResolvers)
                            {
                                var handleDictionary =
                                    Create<DictionaryResultMiddleware>();
                                objectField.MiddlewareDefinitions.Insert(0, new(handleDictionary));
                            }
                            else
                            {
                                objectField.Resolvers = new FieldResolverDelegates(
                                    pureResolver: RemoteFieldHelper.RemoteFieldResolver);
                            }
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
