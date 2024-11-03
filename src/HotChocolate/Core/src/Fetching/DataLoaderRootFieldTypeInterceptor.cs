using GreenDonut;
using GreenDonut.DependencyInjection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fetching;

internal sealed class DataLoaderRootFieldTypeInterceptor : TypeInterceptor
{
    private IApplicationServiceProvider? _services;
    private HashSet<Type>? _dataLoaderValueTypes;
    private ObjectType _queryType = default!;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _services = context.Services.GetService<IApplicationServiceProvider>();
    }

    internal override bool IsEnabled(IDescriptorContext context)
        => context.Options.PublishRootFieldPagesToPromiseCache;

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition,
        OperationType operationType)
    {
        if (operationType == OperationType.Query)
        {
            _queryType = (ObjectType)completionContext.Type;
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (completionContext.Type == _queryType
            && definition is ObjectTypeDefinition typeDef)
        {
            if (_services is null)
            {
                return;
            }

            var dataLoaderValueTypes = GetDataLoaderValueTypes();

            if (dataLoaderValueTypes.Count == 0)
            {
                return;
            }

            foreach (var field in typeDef.Fields)
            {
                if (IsUsableFieldConnection(field))
                {
                    var resultType = completionContext.TypeInspector.GetType(field.ResultType!);
                    if (resultType.IsArrayOrList && dataLoaderValueTypes.Contains(resultType.ElementType.Type))
                    {
                        field.MiddlewareDefinitions.Insert(
                            0,
                            new FieldMiddlewareDefinition(
                                static next => context =>
                                {
                                    var options = context.RequestServices.GetRequiredService<DataLoaderOptions>();
                                    if (options.Cache is null)
                                    {
                                        return next(context);
                                    }

                                    var pagePublisher = new PromiseCachePagePublisher(options.Cache);
                                    context.RegisterPageObserver(pagePublisher);
                                    return next(context);
                                },
                                key: "PromiseCachePagePublisher"));
                    }
                }
            }
        }
    }

    private static bool IsUsableFieldConnection(ObjectFieldDefinition field)
    {
        var isConnection = (field.Flags & FieldFlags.Connection) == FieldFlags.Connection;
        var usesProjection = (field.Flags & FieldFlags.UsesProjections) == FieldFlags.UsesProjections;

        return isConnection
            && !usesProjection
            && field.ResultType != null
            && field.ResultType != typeof(object);
    }

    private HashSet<Type> GetDataLoaderValueTypes()
    {
        if (_dataLoaderValueTypes is not null)
        {
            return _dataLoaderValueTypes;
        }

        var dataLoaderValueTypes = new HashSet<Type>();

        if (_services is not null)
        {
            foreach (var registration in _services.GetServices<DataLoaderRegistration>())
            {
                if (registration.ServiceType.IsGenericType
                    && registration.ServiceType.GetGenericTypeDefinition() == typeof(IDataLoader<,>))
                {
                    dataLoaderValueTypes.Add(registration.ServiceType.GetGenericArguments()[1]);
                }
                else
                {
                    var interfaces = registration.ServiceType.GetInterfaces();
                    foreach (var interfaceType in interfaces)
                    {
                        if (interfaceType.IsGenericType
                            && interfaceType.GetGenericTypeDefinition() == typeof(IDataLoader<,>))
                        {
                            dataLoaderValueTypes.Add(interfaceType.GetGenericArguments()[1]);
                            break;
                        }
                    }
                }
            }
        }

        return _dataLoaderValueTypes = dataLoaderValueTypes;
    }

    private sealed class PromiseCachePagePublisher(IPromiseCache cache) : IPageObserver
    {
        public void OnAfterSliced<T>(ReadOnlySpan<T> items, IPageInfo pageInfo)
        {
            if (items.Length == 0)
            {
                return;
            }

            cache.PublishMany(items);
        }
    }
}
