// ReSharper disable BuiltInTypeReferenceStyle
// ReSharper disable RedundantNameQualifier
// ReSharper disable ArrangeObjectCreationWhenTypeEvident
// ReSharper disable UnusedType.Global
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedMethodReturnValue.Local
// ReSharper disable ConvertToAutoProperty
// ReSharper disable UnusedMember.Global
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable InconsistentNaming

// DroidEntity

// StrawberryShake.CodeGeneration.CSharp.Generators.EntityTypeGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class DroidEntity
    {
        public global::System.String Name { get; set; } = default!;
    }
}


// HumanEntity

// StrawberryShake.CodeGeneration.CSharp.Generators.EntityTypeGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class HumanEntity
    {
        public global::System.String Name { get; set; } = default!;
    }
}


// GetHeroResultFactory

// StrawberryShake.CodeGeneration.CSharp.Generators.ResultDataFactoryGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHeroResultFactory
        : global::StrawberryShake.IOperationResultDataFactory<GetHeroResult>
    {
        private readonly global::StrawberryShake.IEntityStore _entityStore;
        private readonly global::StrawberryShake.IEntityMapper<DroidEntity, GetHero_Hero_Droid> _getHero_Hero_DroidFromDroidEntityMapper;
        private readonly global::StrawberryShake.IEntityMapper<HumanEntity, GetHero_Hero_Human> _getHero_Hero_HumanFromHumanEntityMapper;

        public GetHeroResultFactory(
            global::StrawberryShake.IEntityStore entityStore,
            global::StrawberryShake.IEntityMapper<DroidEntity, GetHero_Hero_Droid> getHero_Hero_DroidFromDroidEntityMapper,
            global::StrawberryShake.IEntityMapper<HumanEntity, GetHero_Hero_Human> getHero_Hero_HumanFromHumanEntityMapper)
        {
            _entityStore = entityStore
                 ?? throw new global::System.ArgumentNullException(nameof(entityStore));
            _getHero_Hero_DroidFromDroidEntityMapper = getHero_Hero_DroidFromDroidEntityMapper
                 ?? throw new global::System.ArgumentNullException(nameof(getHero_Hero_DroidFromDroidEntityMapper));
            _getHero_Hero_HumanFromHumanEntityMapper = getHero_Hero_HumanFromHumanEntityMapper
                 ?? throw new global::System.ArgumentNullException(nameof(getHero_Hero_HumanFromHumanEntityMapper));
        }

        public GetHeroResult Create(global::StrawberryShake.IOperationResultDataInfo dataInfo)
        {
            if (dataInfo is GetHeroResultInfo info)
            {
                return new GetHeroResult(MapIGetHero_Hero(info.Hero));
            }

            throw new global::System.ArgumentException("GetHeroResultInfo expected.");
        }

        private IGetHero_Hero? MapIGetHero_Hero(global::StrawberryShake.EntityId? entityId)
        {
            if (entityId == default)
            {
                return null;
            }


            if (entityId.Value.Name.Equals(
                    "Droid",
                    global::System.StringComparison.Ordinal))
            {
                return _getHero_Hero_DroidFromDroidEntityMapper.Map(
                    _entityStore.GetEntity<DroidEntity>(entityId.Value)
                        ?? throw new global::StrawberryShake.GraphQLClientException());
            }

            if (entityId.Value.Name.Equals(
                    "Human",
                    global::System.StringComparison.Ordinal))
            {
                return _getHero_Hero_HumanFromHumanEntityMapper.Map(
                    _entityStore.GetEntity<HumanEntity>(entityId.Value)
                        ?? throw new global::StrawberryShake.GraphQLClientException());
            }
            throw new global::System.NotSupportedException();
        }
    }
}


// GetHeroResultInfo

// StrawberryShake.CodeGeneration.CSharp.Generators.ResultInfoGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHeroResultInfo
        : global::StrawberryShake.IOperationResultDataInfo
    {
        private readonly global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> _entityIds;
        private readonly global::System.UInt64 _version;

        public GetHeroResultInfo(
            global::StrawberryShake.EntityId? hero,
            global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> entityIds,
            global::System.UInt64 version)
        {
            Hero = hero;
            _entityIds = entityIds
                 ?? throw new global::System.ArgumentNullException(nameof(entityIds));
            _version = version;
        }

        public global::StrawberryShake.EntityId? Hero { get; }

        public global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> EntityIds => _entityIds;

        public global::System.UInt64 Version => _version;

        public global::StrawberryShake.IOperationResultDataInfo WithVersion(global::System.UInt64 version)
        {
            return new GetHeroResultInfo(
                Hero,
                _entityIds,
                version);
        }
    }
}


// GetHeroResult

// StrawberryShake.CodeGeneration.CSharp.Generators.ResultTypeGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHeroResult
        : IGetHeroResult
    {
        public GetHeroResult(IGetHero_Hero? hero)
        {
            Hero = hero;
        }

        public IGetHero_Hero? Hero { get; } = default!;
    }
}


// GetHero_Hero_DroidFromDroidEntityMapper

// StrawberryShake.CodeGeneration.CSharp.Generators.ResultFromEntityTypeMapperGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHero_Hero_DroidFromDroidEntityMapper
        : global::StrawberryShake.IEntityMapper<DroidEntity, GetHero_Hero_Droid>
    {
        public GetHero_Hero_Droid Map(DroidEntity entity)
        {
            return new GetHero_Hero_Droid(entity.Name);
        }
    }
}


// GetHero_Hero_Droid

// StrawberryShake.CodeGeneration.CSharp.Generators.ResultTypeGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHero_Hero_Droid
        : IGetHero_Hero_Droid
    {
        public GetHero_Hero_Droid(global::System.String name)
        {
            Name = name;
        }

        public global::System.String Name { get; }
    }
}


// GetHero_Hero_HumanFromHumanEntityMapper

// StrawberryShake.CodeGeneration.CSharp.Generators.ResultFromEntityTypeMapperGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHero_Hero_HumanFromHumanEntityMapper
        : global::StrawberryShake.IEntityMapper<HumanEntity, GetHero_Hero_Human>
    {
        public GetHero_Hero_Human Map(HumanEntity entity)
        {
            return new GetHero_Hero_Human(entity.Name);
        }
    }
}


// GetHero_Hero_Human

// StrawberryShake.CodeGeneration.CSharp.Generators.ResultTypeGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHero_Hero_Human
        : IGetHero_Hero_Human
    {
        public GetHero_Hero_Human(global::System.String name)
        {
            Name = name;
        }

        public global::System.String Name { get; }
    }
}


// IGetHeroResult

// StrawberryShake.CodeGeneration.CSharp.Generators.ResultInterfaceGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public interface IGetHeroResult
    {
        public IGetHero_Hero? Hero { get; }
    }
}


// IGetHero_Hero

// StrawberryShake.CodeGeneration.CSharp.Generators.ResultInterfaceGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public interface IGetHero_Hero
    {
        public global::System.String Name { get; }
    }
}


// IGetHero_Hero_Droid

// StrawberryShake.CodeGeneration.CSharp.Generators.ResultInterfaceGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public interface IGetHero_Hero_Droid
        : IGetHero_Hero
    {
    }
}


// IGetHero_Hero_Human

// StrawberryShake.CodeGeneration.CSharp.Generators.ResultInterfaceGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public interface IGetHero_Hero_Human
        : IGetHero_Hero
    {
    }
}


// GetHeroQueryDocument

// StrawberryShake.CodeGeneration.CSharp.Generators.OperationDocumentGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    /// <summary>
    /// Represents the operation service of the GetHero GraphQL operation
    /// <code>
    /// query GetHero {
    ///   hero(episode: NEW_HOPE) {
    ///     __typename
    ///     name
    ///     ... on Droid {
    ///       id
    ///     }
    ///     ... on Human {
    ///       id
    ///     }
    ///   }
    /// }
    /// </code>
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHeroQueryDocument
        : global::StrawberryShake.IDocument
    {
        private GetHeroQueryDocument()
        {
        }

        public static GetHeroQueryDocument Instance { get; } = new GetHeroQueryDocument();

        public global::StrawberryShake.OperationKind Kind => global::StrawberryShake.OperationKind.Query;

        public global::System.ReadOnlySpan<global::System.Byte> Body => new global::System.Byte[]{ 0x71, 0x75, 0x65, 0x72, 0x79, 0x20, 0x47, 0x65, 0x74, 0x48, 0x65, 0x72, 0x6f, 0x20, 0x7b, 0x20, 0x68, 0x65, 0x72, 0x6f, 0x28, 0x65, 0x70, 0x69, 0x73, 0x6f, 0x64, 0x65, 0x3a, 0x20, 0x4e, 0x45, 0x57, 0x5f, 0x48, 0x4f, 0x50, 0x45, 0x29, 0x20, 0x7b, 0x20, 0x5f, 0x5f, 0x74, 0x79, 0x70, 0x65, 0x6e, 0x61, 0x6d, 0x65, 0x20, 0x6e, 0x61, 0x6d, 0x65, 0x20, 0x2e, 0x2e, 0x2e, 0x20, 0x6f, 0x6e, 0x20, 0x44, 0x72, 0x6f, 0x69, 0x64, 0x20, 0x7b, 0x20, 0x69, 0x64, 0x20, 0x7d, 0x20, 0x2e, 0x2e, 0x2e, 0x20, 0x6f, 0x6e, 0x20, 0x48, 0x75, 0x6d, 0x61, 0x6e, 0x20, 0x7b, 0x20, 0x69, 0x64, 0x20, 0x7d, 0x20, 0x7d, 0x20, 0x7d };

        public global::StrawberryShake.DocumentHash Hash { get; } = new global::StrawberryShake.DocumentHash("sha1Hash", "95ca68547e3a55b9ff81efe79b33a417b2f0690b");

        public override global::System.String ToString()
        {
            return global::System.Text.Encoding.UTF8.GetString(Body);
        }
    }
}


// GetHeroQuery

// StrawberryShake.CodeGeneration.CSharp.Generators.OperationServiceGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    /// <summary>
    /// Represents the operation service of the GetHero GraphQL operation
    /// <code>
    /// query GetHero {
    ///   hero(episode: NEW_HOPE) {
    ///     __typename
    ///     name
    ///     ... on Droid {
    ///       id
    ///     }
    ///     ... on Human {
    ///       id
    ///     }
    ///   }
    /// }
    /// </code>
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHeroQuery
    {
        private readonly global::StrawberryShake.IOperationExecutor<IGetHeroResult> _operationExecutor;

        public GetHeroQuery(global::StrawberryShake.IOperationExecutor<IGetHeroResult> operationExecutor)
        {
            _operationExecutor = operationExecutor
                 ?? throw new global::System.ArgumentNullException(nameof(operationExecutor));
        }

        public async global::System.Threading.Tasks.Task<global::StrawberryShake.IOperationResult<IGetHeroResult>> ExecuteAsync(global::System.Threading.CancellationToken cancellationToken = default)
        {
            var request = CreateRequest();

            return await _operationExecutor
                .ExecuteAsync(
                    request,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public global::System.IObservable<global::StrawberryShake.IOperationResult<IGetHeroResult>> Watch(global::StrawberryShake.ExecutionStrategy? strategy = null)
        {
            var request = CreateRequest();
            return _operationExecutor.Watch(
                request,
                strategy);
        }

        private global::StrawberryShake.OperationRequest CreateRequest()
        {

            return new global::StrawberryShake.OperationRequest(
                id: GetHeroQueryDocument.Instance.Hash.Value,
                name: "GetHero",
                document: GetHeroQueryDocument.Instance,
                strategy: global::StrawberryShake.RequestStrategy.Default);
        }
    }
}


// GetHeroBuilder

// StrawberryShake.CodeGeneration.CSharp.Generators.JsonResultBuilderGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHeroBuilder
        : global::StrawberryShake.IOperationResultBuilder<global::System.Text.Json.JsonDocument, IGetHeroResult>
    {
        private readonly global::StrawberryShake.IEntityStore _entityStore;
        private readonly global::System.Func<global::System.Text.Json.JsonElement, global::StrawberryShake.EntityId> _extractId;
        private readonly global::StrawberryShake.IOperationResultDataFactory<IGetHeroResult> _resultDataFactory;
        private readonly global::StrawberryShake.Serialization.ILeafValueParser<global::System.String, global::System.String> _stringParser;

        public GetHeroBuilder(
            global::StrawberryShake.IEntityStore entityStore,
            global::System.Func<global::System.Text.Json.JsonElement, global::StrawberryShake.EntityId> extractId,
            global::StrawberryShake.IOperationResultDataFactory<IGetHeroResult> resultDataFactory,
            global::StrawberryShake.Serialization.ISerializerResolver serializerResolver)
        {
            _entityStore = entityStore
                 ?? throw new global::System.ArgumentNullException(nameof(entityStore));
            _extractId = extractId
                 ?? throw new global::System.ArgumentNullException(nameof(extractId));
            _resultDataFactory = resultDataFactory
                 ?? throw new global::System.ArgumentNullException(nameof(resultDataFactory));
            _stringParser = serializerResolver.GetLeafValueParser<global::System.String, global::System.String>("String")
                 ?? throw new global::System.ArgumentException("No serializer for type `String` found.");
        }

        public global::StrawberryShake.IOperationResult<IGetHeroResult> Build(global::StrawberryShake.Response<global::System.Text.Json.JsonDocument> response)
        {
            (IGetHeroResult Result, GetHeroResultInfo Info)? data = null;
            global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.IClientError>? errors = null;

            if (response.Body != null)
            {
                if (response.Body.RootElement.TryGetProperty("data", out global::System.Text.Json.JsonElement dataElement))
                {
                    data = BuildData(dataElement);
                }
                if (response.Body.RootElement.TryGetProperty("errors", out global::System.Text.Json.JsonElement errorsElement))
                {
                    errors = global::StrawberryShake.Json.JsonErrorParser.ParseErrors(errorsElement);
                }
            }

            return new global::StrawberryShake.OperationResult<IGetHeroResult>(
                data?.Result,
                data?.Info,
                _resultDataFactory,
                errors);
        }

        private (IGetHeroResult, GetHeroResultInfo) BuildData(global::System.Text.Json.JsonElement obj)
        {
            using global::StrawberryShake.IEntityUpdateSession session = _entityStore.BeginUpdate();
            var entityIds = new global::System.Collections.Generic.HashSet<global::StrawberryShake.EntityId>();

            global::StrawberryShake.EntityId? heroId = UpdateIGetHero_HeroEntity(
                global::StrawberryShake.Json.JsonElementExtensions.GetPropertyOrNull(
                    obj,
                    "hero"),
                entityIds);

            var resultInfo = new GetHeroResultInfo(
                heroId,
                entityIds,
                session.Version);

            return (
                _resultDataFactory.Create(resultInfo),
                resultInfo
            );
        }

        private global::StrawberryShake.EntityId? UpdateIGetHero_HeroEntity(
            global::System.Text.Json.JsonElement? obj,
            global::System.Collections.Generic.ISet<global::StrawberryShake.EntityId> entityIds)
        {
            if (!obj.HasValue)
            {
                return null;
            }

            global::StrawberryShake.EntityId entityId = _extractId(obj.Value);
            entityIds.Add(entityId);


            if (entityId.Name.Equals(
                    "Droid",
                    global::System.StringComparison.Ordinal))
            {
                DroidEntity entity = _entityStore.GetOrCreate<DroidEntity>(entityId);
                entity.Name = DeserializeNonNullableString(
                    global::StrawberryShake.Json.JsonElementExtensions.GetPropertyOrNull(
                        obj,
                        "name"));

                return entityId;
            }

            if (entityId.Name.Equals(
                    "Human",
                    global::System.StringComparison.Ordinal))
            {
                HumanEntity entity = _entityStore.GetOrCreate<HumanEntity>(entityId);
                entity.Name = DeserializeNonNullableString(
                    global::StrawberryShake.Json.JsonElementExtensions.GetPropertyOrNull(
                        obj,
                        "name"));

                return entityId;
            }

            throw new global::System.NotSupportedException();
        }

        private global::System.String DeserializeNonNullableString(global::System.Text.Json.JsonElement? obj)
        {
            if (!obj.HasValue)
            {
                throw new global::System.ArgumentNullException();
            }

            return _stringParser.Parse(obj.Value.GetString()!);
        }
    }
}


// StarWarsGetHeroClient

// StrawberryShake.CodeGeneration.CSharp.Generators.ClientGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    /// <summary>
    /// Represents the StarWarsGetHeroClient GraphQL client
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class StarWarsGetHeroClient
    {
        private readonly global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.GetHeroQuery _getHero;

        public StarWarsGetHeroClient(global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.GetHeroQuery getHero)
        {
            _getHero = getHero
                 ?? throw new global::System.ArgumentNullException(nameof(getHero));
        }

        public static global::System.String ClientName => "StarWarsGetHeroClient";

        public global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.GetHeroQuery GetHero => _getHero;
    }
}


// EntityIdFactory

// StrawberryShake.CodeGeneration.CSharp.Generators.EntityIdFactoryGenerator

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public static partial class EntityIdFactory
    {
        public static global::StrawberryShake.EntityId CreateEntityId(global::System.Text.Json.JsonElement obj)
        {
            global::System.String typeName = obj
                .GetProperty("__typename")
                .GetString()!;

            return typeName switch
            {
                "Droid" => CreateDroidEntityId(
                    obj,
                    typeName),
                "Human" => CreateHumanEntityId(
                    obj,
                    typeName),
                _ => throw new global::System.NotSupportedException()
            };
        }

        private static global::StrawberryShake.EntityId CreateDroidEntityId(
            global::System.Text.Json.JsonElement obj,
            global::System.String type)
        {
            return new global::StrawberryShake.EntityId(
                type,
                obj
                    .GetProperty("id")
                    .GetString()!);
        }

        private static global::StrawberryShake.EntityId CreateHumanEntityId(
            global::System.Text.Json.JsonElement obj,
            global::System.String type)
        {
            return new global::StrawberryShake.EntityId(
                type,
                obj
                    .GetProperty("id")
                    .GetString()!);
        }
    }
}


// StarWarsGetHeroClientServiceCollectionExtensions

// StrawberryShake.CodeGeneration.CSharp.Generators.DependencyInjectionGenerator

#nullable enable

namespace Microsoft.Extensions.DependencyInjection
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public static partial class StarWarsGetHeroClientServiceCollectionExtensions
    {
        public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddStarWarsGetHeroClient(
            this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services,
            global::StrawberryShake.ExecutionStrategy strategy = global::StrawberryShake.ExecutionStrategy.NetworkOnly)
        {
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(
                services,
                sp => 
                {
                    var serviceCollection = new global::Microsoft.Extensions.DependencyInjection.ServiceCollection();

                    ConfigureClient(
                        serviceCollection,
                        sp,
                        strategy);

                    return new ClientServiceProvider(
                        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(serviceCollection));
                });

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(
                services,
                sp => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.GetHeroQuery>(
                    global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ClientServiceProvider>(sp)));

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(
                services,
                sp => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.StarWarsGetHeroClient>(
                    global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ClientServiceProvider>(sp)));

            return services;
        }

        private static global::Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureClient(
            global::Microsoft.Extensions.DependencyInjection.IServiceCollection services,
            global::System.IServiceProvider parentServices,
            global::StrawberryShake.ExecutionStrategy strategy = global::StrawberryShake.ExecutionStrategy.NetworkOnly)
        {
            if (services is null)
            {
                throw new global::System.ArgumentNullException(nameof(services));
            }
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::System.Func<global::System.Text.Json.JsonElement, global::StrawberryShake.EntityId>>(
                services,
                global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.EntityIdFactory.CreateEntityId);
            global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<global::StrawberryShake.IEntityStore, global::StrawberryShake.EntityStore>(services);
            global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<global::StrawberryShake.IOperationStore>(
                services,
                sp => new global::StrawberryShake.OperationStore(
                    global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.IEntityStore>(sp)
                    .Watch()));
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(
                services,
                sp => 
                {
                    var clientFactory = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::System.Net.Http.IHttpClientFactory>(parentServices);
                    return new global::StrawberryShake.Transport.Http.HttpConnection(() => clientFactory.CreateClient("StarWarsGetHeroClient"));
                });

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.IEntityMapper<global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.DroidEntity, global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.GetHero_Hero_Droid>, global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.GetHero_Hero_DroidFromDroidEntityMapper>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.IEntityMapper<global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.HumanEntity, global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.GetHero_Hero_Human>, global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.GetHero_Hero_HumanFromHumanEntityMapper>(services);

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.StringSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.BooleanSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.ByteSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.ShortSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.IntSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.LongSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.FloatSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.DecimalSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.UrlSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.UuidSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.IdSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.DateTimeSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.DateSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.ByteArraySerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializer, global::StrawberryShake.Serialization.TimeSpanSerializer>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.Serialization.ISerializerResolver>(
                services,
                sp => new global::StrawberryShake.Serialization.SerializerResolver(
                    global::System.Linq.Enumerable.Concat(
                        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::System.Collections.Generic.IEnumerable<global::StrawberryShake.Serialization.ISerializer>>(
                            parentServices),
                        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::System.Collections.Generic.IEnumerable<global::StrawberryShake.Serialization.ISerializer>>(
                            sp))));

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.IOperationResultDataFactory<global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.IGetHeroResult>, global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.GetHeroResultFactory>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.IOperationResultBuilder<global::System.Text.Json.JsonDocument, global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.IGetHeroResult>, global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.GetHeroBuilder>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.IOperationExecutor<global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.IGetHeroResult>>(
                services,
                sp => new global::StrawberryShake.OperationExecutor<global::System.Text.Json.JsonDocument, global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.IGetHeroResult>(
                    global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.Transport.Http.HttpConnection>(sp),
                    () => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.IOperationResultBuilder<global::System.Text.Json.JsonDocument, global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.IGetHeroResult>>(sp),
                    global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.IOperationStore>(sp),
                    strategy));
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.GetHeroQuery>(services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWarsGetHero.StarWarsGetHeroClient>(services);
            return services;
        }

        private class ClientServiceProvider
            : System.IServiceProvider
            , System.IDisposable
        {
            private readonly System.IServiceProvider _provider;

            public ClientServiceProvider(System.IServiceProvider provider)
            {
                _provider = provider;
            }

            public object? GetService(System.Type serviceType)
            {
                return _provider.GetService(serviceType);
            }

            public void Dispose()
            {
                if (_provider is System.IDisposable d)
                {
                    d.Dispose();
                }
            }
        }
    }
}


