using GreenDonut;
using GreenDonut.DependencyInjection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fetching;

internal sealed class DataLoaderRootFieldTypeInterceptor : TypeInterceptor
{
    private IServiceProvider? _services;
    private HashSet<Type>? _dataLoaderValueTypes;
    private ObjectType _queryType = null!;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _services = context.Services.GetService<IRootServiceProviderAccessor>()?.ServiceProvider;
    }

    public override bool IsEnabled(IDescriptorContext context)
        => context.Options.PublishRootFieldPagesToPromiseCache;

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeConfiguration configuration,
        OperationType operationType)
    {
        if (operationType == OperationType.Query)
        {
            _queryType = (ObjectType)completionContext.Type;
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (completionContext.Type == _queryType
            && configuration is ObjectTypeConfiguration typeDef)
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
                        field.MiddlewareConfigurations.Insert(
                            0,
                            new FieldMiddlewareConfiguration(
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

    private static bool IsUsableFieldConnection(ObjectFieldConfiguration field)
    {
        var isConnection = (field.Flags & CoreFieldFlags.Connection) == CoreFieldFlags.Connection;
        var usesProjection = (field.Flags & CoreFieldFlags.UsesProjections) == CoreFieldFlags.UsesProjections;

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
