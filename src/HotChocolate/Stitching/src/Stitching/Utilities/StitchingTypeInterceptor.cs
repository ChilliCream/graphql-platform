using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Processing;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Resolvers.FieldClassMiddlewareFactory;
using static HotChocolate.Stitching.WellKnownContextData;

namespace HotChocolate.Stitching.Utilities;

internal sealed class StitchingTypeInterceptor : TypeInterceptor
{
    private readonly HashSet<(NameString, NameString)> _handledExternalFields = new();

    public override void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
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
            if (completionContext.IsSubscriptionType ?? false)
            {
                foreach (ObjectFieldDefinition objectField in objectTypeDef.Fields)
                {
                    if (objectField.GetDirectives().Any(IsDelegatedField))
                    {
                        FieldMiddleware handleDictionary = Create<DictionaryResultMiddleware>();
                        FieldMiddleware handleQueryResult = Create<QueryResultMiddleware>();
                        FieldMiddleware copyResult = Create<CopyEventMessageMiddleware>();

                        objectField.MiddlewareDefinitions.Insert(0, new(handleDictionary));
                        objectField.MiddlewareDefinitions.Insert(0, new(handleQueryResult));
                        objectField.MiddlewareDefinitions.Insert(0, new(copyResult));
                        objectField.SubscribeResolver = DelegateSubscribe.SubscribeAsync;
                        _handledExternalFields.Add((objectTypeDef.Name, objectField.Name));
                    }
                }
            }
            else
            {
                foreach (ObjectFieldDefinition objectField in objectTypeDef.Fields)
                {
                    if (objectField.GetDirectives().Any(IsDelegatedField))
                    {
                        FieldMiddleware handleDictionary = Create<DictionaryResultMiddleware>();
                        FieldMiddleware handleQueryResult = Create<QueryResultMiddleware>();
                        FieldMiddleware delegateResolve = Create<DelegateResolve>();

                        objectField.MiddlewareDefinitions.Insert(0, new(handleDictionary));
                        objectField.MiddlewareDefinitions.Insert(0, new(handleQueryResult));
                        objectField.MiddlewareDefinitions.Insert(0, new(delegateResolve));
                        _handledExternalFields.Add((objectTypeDef.Name, objectField.Name));
                    }
                }
            }

            IReadOnlyDictionary<NameString, ISet<NameString>> externalFieldLookup =
                completionContext.GetExternalFieldLookup();
            if (externalFieldLookup.TryGetValue(objectType.Name, out ISet<NameString>? external))
            {
                foreach (ObjectFieldDefinition objectField in objectTypeDef.Fields)
                {
                    if (external.Contains(objectField.Name) &&
                        _handledExternalFields.Add((objectTypeDef.Name, objectField.Name)))
                    {
                        if (objectField.Resolvers.HasResolvers)
                        {
                            FieldMiddleware handleDictionary =
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
