using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Stitching;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace Microsoft.Extensions.DependencyInjection
{
    public class StitchingTypeInterceptor : TypeInterceptor
    {
        public override void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            if (definition is ObjectTypeDefinition objectTypeDef)
            {
                foreach (ObjectFieldDefinition objectField in objectTypeDef.Fields)
                {
                    if (objectField.Directives.Any(IsDelegatedField))
                    {
                        FieldMiddleware handleDictionary =
                            FieldClassMiddlewareFactory.Create<DictionaryResultMiddleware>();
                        FieldMiddleware delegateToSchema =
                            FieldClassMiddlewareFactory.Create<DelegateToRemoteSchemaMiddleware>();

                        objectField.MiddlewareComponents.Insert(0, handleDictionary);
                        objectField.MiddlewareComponents.Insert(0, delegateToSchema);
                    }
                }
            }
        }

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition,
            IDictionary<string, object> contextData)
        {
            if (completionContext.Type is ObjectType objectType &&
                definition is ObjectTypeDefinition objectTypeDef)
            {
                IReadOnlyDictionary<NameString, ISet<NameString>> externalFieldLookup =
                    completionContext.GetExternalFieldLookup();
                if (externalFieldLookup.TryGetValue(objectType.Name, out ISet<NameString> external))
                {
                    foreach (ObjectFieldDefinition objectField in objectTypeDef.Fields)
                    {
                        if (external.Contains(objectField.Name))
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
