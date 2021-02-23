// Code:
// CreateReviewResultFactory

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class CreateReviewResultFactory
        : global::StrawberryShake.IOperationResultDataFactory<CreateReviewResult>
    {
        private readonly global::StrawberryShake.IEntityStore _entityStore;

        public CreateReviewResultFactory(global::StrawberryShake.IEntityStore entityStore)
        {
            _entityStore = entityStore
                 ?? throw new global::System.ArgumentNullException(nameof(entityStore));
        }

        public CreateReviewResult Create(global::StrawberryShake.IOperationResultDataInfo dataInfo)
        {
            if (dataInfo is CreateReviewResultInfo info)
            {
                return new CreateReviewResult(MapNonNullableICreateReview_CreateReview(info.CreateReview));
            }

            throw new global::System.ArgumentException("CreateReviewResultInfo expected.");
        }

        private ICreateReview_CreateReview MapNonNullableICreateReview_CreateReview(global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWars.State.ReviewData data)
        {
            ICreateReview_CreateReview returnValue = default!;

            if (data.__typename.Equals("Review", global::System.StringComparison.Ordinal))
            {
                returnValue = new CreateReview_CreateReview_Review(data.Stars ?? throw new global::System.ArgumentNullException());
            }
            else {
                throw new global::System.NotSupportedException();
            }
            return returnValue;
        }
    }
}


// CreateReviewResultInfo

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class CreateReviewResultInfo
        : global::StrawberryShake.IOperationResultDataInfo
    {
        private readonly global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> _entityIds;
        private readonly ulong _version;

        public CreateReviewResultInfo(
            global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWars.State.ReviewData createReview,
            global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> entityIds,
            ulong version)
        {
            CreateReview = createReview;
            _entityIds = entityIds
                 ?? throw new global::System.ArgumentNullException(nameof(entityIds));
            _version = version;
        }

        public global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWars.State.ReviewData CreateReview { get; }

        public global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> EntityIds => _entityIds;

        public ulong Version => _version;

        public global::StrawberryShake.IOperationResultDataInfo WithVersion(ulong version)
        {
            return new CreateReviewResultInfo(
                CreateReview,
                _entityIds,
                _version);
        }
    }
}


// CreateReviewResult

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class CreateReviewResult
        : ICreateReviewResult
    {
        public CreateReviewResult(ICreateReview_CreateReview createReview)
        {
            CreateReview = createReview;
        }

        public ICreateReview_CreateReview CreateReview { get; }
    }
}


// CreateReview_CreateReview_Review

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class CreateReview_CreateReview_Review
        : ICreateReview_CreateReview_Review
    {
        public CreateReview_CreateReview_Review(global::System.Int32 stars)
        {
            Stars = stars;
        }

        public global::System.Int32 Stars { get; }
    }
}


// ICreateReviewResult

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public interface ICreateReviewResult
    {
        public ICreateReview_CreateReview CreateReview { get; }
    }
}


// ICreateReview_CreateReview

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public interface ICreateReview_CreateReview
    {
        public global::System.Int32 Stars { get; }
    }
}


// ICreateReview_CreateReview_Review

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public interface ICreateReview_CreateReview_Review
        : ICreateReview_CreateReview
    {
    }
}


// OnReviewResultFactory

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class OnReviewResultFactory
        : global::StrawberryShake.IOperationResultDataFactory<OnReviewResult>
    {
        private readonly global::StrawberryShake.IEntityStore _entityStore;

        public OnReviewResultFactory(global::StrawberryShake.IEntityStore entityStore)
        {
            _entityStore = entityStore
                 ?? throw new global::System.ArgumentNullException(nameof(entityStore));
        }

        public OnReviewResult Create(global::StrawberryShake.IOperationResultDataInfo dataInfo)
        {
            if (dataInfo is OnReviewResultInfo info)
            {
                return new OnReviewResult(MapNonNullableIOnReview_OnReview(info.OnReview));
            }

            throw new global::System.ArgumentException("OnReviewResultInfo expected.");
        }

        private IOnReview_OnReview MapNonNullableIOnReview_OnReview(global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWars.State.ReviewData data)
        {
            IOnReview_OnReview returnValue = default!;

            if (data.__typename.Equals("Review", global::System.StringComparison.Ordinal))
            {
                returnValue = new OnReview_OnReview_Review(data.Stars ?? throw new global::System.ArgumentNullException());
            }
            else {
                throw new global::System.NotSupportedException();
            }
            return returnValue;
        }
    }
}


// OnReviewResultInfo

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class OnReviewResultInfo
        : global::StrawberryShake.IOperationResultDataInfo
    {
        private readonly global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> _entityIds;
        private readonly ulong _version;

        public OnReviewResultInfo(
            global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWars.State.ReviewData onReview,
            global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> entityIds,
            ulong version)
        {
            OnReview = onReview;
            _entityIds = entityIds
                 ?? throw new global::System.ArgumentNullException(nameof(entityIds));
            _version = version;
        }

        public global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWars.State.ReviewData OnReview { get; }

        public global::System.Collections.Generic.IReadOnlyCollection<global::StrawberryShake.EntityId> EntityIds => _entityIds;

        public ulong Version => _version;

        public global::StrawberryShake.IOperationResultDataInfo WithVersion(ulong version)
        {
            return new OnReviewResultInfo(
                OnReview,
                _entityIds,
                _version);
        }
    }
}


// OnReviewResult

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class OnReviewResult
        : IOnReviewResult
    {
        public OnReviewResult(IOnReview_OnReview onReview)
        {
            OnReview = onReview;
        }

        public IOnReview_OnReview OnReview { get; }
    }
}


// OnReview_OnReview_Review

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class OnReview_OnReview_Review
        : IOnReview_OnReview_Review
    {
        public OnReview_OnReview_Review(global::System.Int32 stars)
        {
            Stars = stars;
        }

        public global::System.Int32 Stars { get; }
    }
}


// IOnReviewResult

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public interface IOnReviewResult
    {
        public IOnReview_OnReview OnReview { get; }
    }
}


// IOnReview_OnReview

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public interface IOnReview_OnReview
    {
        public global::System.Int32 Stars { get; }
    }
}


// IOnReview_OnReview_Review

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public interface IOnReview_OnReview_Review
        : IOnReview_OnReview
    {
    }
}


// CreateReviewMutationDocument

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class CreateReviewMutationDocument
        : global::StrawberryShake.IDocument
    {
        private const global::System.String _bodyString =
            @"mutation CreateReview($stars: Int!) {
  createReview(episode: EMPIRE, review: { stars: $stars, commentary: ""good"" }) {
    __typename
    stars
  }
}";
        private static readonly byte[] _body = global::System.Text.Encoding.UTF8.GetBytes(_bodyString);

        private CreateReviewMutationDocument()
        {
        }

        public static CreateReviewMutationDocument Instance { get; } = new CreateReviewMutationDocument();

        public global::StrawberryShake.OperationKind Kind => global::StrawberryShake.OperationKind.Mutation;

        public global::System.ReadOnlySpan<byte> Body => _body;

        public override string ToString()
        {
            return _bodyString;
        }
    }
}


// CreateReviewMutation

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class CreateReviewMutation
    {
        private readonly global::StrawberryShake.IOperationExecutor<ICreateReviewResult> _operationExecutor;
        private readonly global::StrawberryShake.Serialization.IInputValueFormatter _intFormatter;

        public CreateReviewMutation(
            global::StrawberryShake.IOperationExecutor<ICreateReviewResult> operationExecutor,
            global::StrawberryShake.Serialization.ISerializerResolver serializerResolver)
        {
            _operationExecutor = operationExecutor
                 ?? throw new global::System.ArgumentNullException(nameof(operationExecutor));
            _intFormatter = serializerResolver.GetInputValueFormatter("Int");
        }

        public async global::System.Threading.Tasks.Task<global::StrawberryShake.IOperationResult<ICreateReviewResult>> ExecuteAsync(
            global::System.Int32 stars,
            global::System.Threading.CancellationToken cancellationToken = default)
        {
            var request = CreateRequest(stars);

            return await _operationExecutor
                .ExecuteAsync(
                    request,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public global::System.IObservable<global::StrawberryShake.IOperationResult<ICreateReviewResult>> Watch(
            global::System.Int32 stars,
            global::StrawberryShake.ExecutionStrategy? strategy = null)
        {
            var request = CreateRequest(stars);
            return _operationExecutor.Watch(request, strategy);
        }

        private global::StrawberryShake.OperationRequest CreateRequest(global::System.Int32 stars)
        {
            var arguments = new global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object?>();
            arguments.Add("stars", FormatStars(stars));

            return new global::StrawberryShake.OperationRequest(
                "CreateReview",
                CreateReviewMutationDocument.Instance,
                arguments);
        }

        private global::System.Object? FormatStars(global::System.Int32 value)
        {
            if (value == default)
            {
                return null;
            }

            return _intFormatter.Format(value);
        }
    }
}


// OnReviewSubscriptionDocument

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class OnReviewSubscriptionDocument
        : global::StrawberryShake.IDocument
    {
        private const global::System.String _bodyString =
            @"subscription OnReview {
  onReview(episode: EMPIRE) {
    __typename
    stars
  }
}";
        private static readonly byte[] _body = global::System.Text.Encoding.UTF8.GetBytes(_bodyString);

        private OnReviewSubscriptionDocument()
        {
        }

        public static OnReviewSubscriptionDocument Instance { get; } = new OnReviewSubscriptionDocument();

        public global::StrawberryShake.OperationKind Kind => global::StrawberryShake.OperationKind.Subscription;

        public global::System.ReadOnlySpan<byte> Body => _body;

        public override string ToString()
        {
            return _bodyString;
        }
    }
}


// OnReviewSubscription

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class OnReviewSubscription
    {
        private readonly global::StrawberryShake.IOperationExecutor<IOnReviewResult> _operationExecutor;

        public OnReviewSubscription(global::StrawberryShake.IOperationExecutor<IOnReviewResult> operationExecutor)
        {
            _operationExecutor = operationExecutor
                 ?? throw new global::System.ArgumentNullException(nameof(operationExecutor));
        }

        public global::System.IObservable<global::StrawberryShake.IOperationResult<IOnReviewResult>> Watch(global::StrawberryShake.ExecutionStrategy? strategy = null)
        {
            var request = CreateRequest();
            return _operationExecutor.Watch(request, strategy);
        }

        private global::StrawberryShake.OperationRequest CreateRequest()
        {

            return new global::StrawberryShake.OperationRequest(
                "OnReview",
                OnReviewSubscriptionDocument.Instance);
        }
    }
}


// CreateReviewBuilder

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class CreateReviewBuilder
        : global::StrawberryShake.IOperationResultBuilder<global::System.Text.Json.JsonDocument, ICreateReviewResult>
    {
        private readonly global::StrawberryShake.IEntityStore _entityStore;
        private readonly global::System.Func<global::System.Text.Json.JsonElement, global::StrawberryShake.EntityId> _extractId;
        private readonly global::StrawberryShake.IOperationResultDataFactory<ICreateReviewResult> _resultDataFactory;
        private global::StrawberryShake.Serialization.ILeafValueParser<global::System.Int32, global::System.Int32> _int32Parser;

        public CreateReviewBuilder(
            global::StrawberryShake.IEntityStore entityStore,
            global::System.Func<global::System.Text.Json.JsonElement, global::StrawberryShake.EntityId> extractId,
            global::StrawberryShake.IOperationResultDataFactory<ICreateReviewResult> resultDataFactory,
            global::StrawberryShake.Serialization.ISerializerResolver serializerResolver)
        {
            _entityStore = entityStore
                 ?? throw new global::System.ArgumentNullException(nameof(entityStore));
            _extractId = extractId
                 ?? throw new global::System.ArgumentNullException(nameof(extractId));
            _resultDataFactory = resultDataFactory
                 ?? throw new global::System.ArgumentNullException(nameof(resultDataFactory));
            _int32Parser = serializerResolver.GetLeafValueParser<global::System.Int32, global::System.Int32>("Int")
                 ?? throw new global::System.ArgumentNullException(nameof(_int32Parser));
        }

        public global::StrawberryShake.IOperationResult<ICreateReviewResult> Build(global::StrawberryShake.Response<global::System.Text.Json.JsonDocument> response)
        {
            (ICreateReviewResult Result, CreateReviewResultInfo Info)? data = null;

            if (response.Body is not null
                && response.Body.RootElement.TryGetProperty("data", out global::System.Text.Json.JsonElement obj))
            {
                data = BuildData(obj);
            }

            return new global::StrawberryShake.OperationResult<ICreateReviewResult>(
                data?.Result,
                data?.Info,
                _resultDataFactory,
                null);
        }

        private (ICreateReviewResult, CreateReviewResultInfo) BuildData(global::System.Text.Json.JsonElement obj)
        {
            using global::StrawberryShake.IEntityUpdateSession session = _entityStore.BeginUpdate();
            var entityIds = new global::System.Collections.Generic.HashSet<global::StrawberryShake.EntityId>();


            var resultInfo = new CreateReviewResultInfo(
                DeserializeNonNullableICreateReview_CreateReview(global::StrawberryShake.Transport.Http.JsonElementExtensions.GetPropertyOrNull(obj, "createReview")),
                entityIds,
                session.Version);

            return (_resultDataFactory.Create(resultInfo), resultInfo);
        }

        private global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWars.State.ReviewData DeserializeNonNullableICreateReview_CreateReview(global::System.Text.Json.JsonElement? obj)
        {
            if (!obj.HasValue)
            {
                throw new global::System.ArgumentNullException();
            }

            var typename = obj.Value.GetProperty("__typename").GetString();

            if (typename?.Equals("Review", global::System.StringComparison.Ordinal) ?? false)
            {
                return new global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWars.State.ReviewData(
                    typename,
                    stars: DeserializeNonNullableInt32(global::StrawberryShake.Transport.Http.JsonElementExtensions.GetPropertyOrNull(obj.Value, "stars")));
            }

            throw new global::System.NotSupportedException();
        }

        private global::System.Int32 DeserializeNonNullableInt32(global::System.Text.Json.JsonElement? obj)
        {
            if (!obj.HasValue)
            {
                throw new global::System.ArgumentNullException();
            }

            return _int32Parser.Parse(obj.Value.GetInt32()!);
        }
    }
}


// OnReviewBuilder

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class OnReviewBuilder
        : global::StrawberryShake.IOperationResultBuilder<global::System.Text.Json.JsonDocument, IOnReviewResult>
    {
        private readonly global::StrawberryShake.IEntityStore _entityStore;
        private readonly global::System.Func<global::System.Text.Json.JsonElement, global::StrawberryShake.EntityId> _extractId;
        private readonly global::StrawberryShake.IOperationResultDataFactory<IOnReviewResult> _resultDataFactory;
        private global::StrawberryShake.Serialization.ILeafValueParser<global::System.Int32, global::System.Int32> _int32Parser;

        public OnReviewBuilder(
            global::StrawberryShake.IEntityStore entityStore,
            global::System.Func<global::System.Text.Json.JsonElement, global::StrawberryShake.EntityId> extractId,
            global::StrawberryShake.IOperationResultDataFactory<IOnReviewResult> resultDataFactory,
            global::StrawberryShake.Serialization.ISerializerResolver serializerResolver)
        {
            _entityStore = entityStore
                 ?? throw new global::System.ArgumentNullException(nameof(entityStore));
            _extractId = extractId
                 ?? throw new global::System.ArgumentNullException(nameof(extractId));
            _resultDataFactory = resultDataFactory
                 ?? throw new global::System.ArgumentNullException(nameof(resultDataFactory));
            _int32Parser = serializerResolver.GetLeafValueParser<global::System.Int32, global::System.Int32>("Int")
                 ?? throw new global::System.ArgumentNullException(nameof(_int32Parser));
        }

        public global::StrawberryShake.IOperationResult<IOnReviewResult> Build(global::StrawberryShake.Response<global::System.Text.Json.JsonDocument> response)
        {
            (IOnReviewResult Result, OnReviewResultInfo Info)? data = null;

            if (response.Body is not null
                && response.Body.RootElement.TryGetProperty("data", out global::System.Text.Json.JsonElement obj))
            {
                data = BuildData(obj);
            }

            return new global::StrawberryShake.OperationResult<IOnReviewResult>(
                data?.Result,
                data?.Info,
                _resultDataFactory,
                null);
        }

        private (IOnReviewResult, OnReviewResultInfo) BuildData(global::System.Text.Json.JsonElement obj)
        {
            using global::StrawberryShake.IEntityUpdateSession session = _entityStore.BeginUpdate();
            var entityIds = new global::System.Collections.Generic.HashSet<global::StrawberryShake.EntityId>();


            var resultInfo = new OnReviewResultInfo(
                DeserializeNonNullableIOnReview_OnReview(global::StrawberryShake.Transport.Http.JsonElementExtensions.GetPropertyOrNull(obj, "onReview")),
                entityIds,
                session.Version);

            return (_resultDataFactory.Create(resultInfo), resultInfo);
        }

        private global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWars.State.ReviewData DeserializeNonNullableIOnReview_OnReview(global::System.Text.Json.JsonElement? obj)
        {
            if (!obj.HasValue)
            {
                throw new global::System.ArgumentNullException();
            }

            var typename = obj.Value.GetProperty("__typename").GetString();

            if (typename?.Equals("Review", global::System.StringComparison.Ordinal) ?? false)
            {
                return new global::StrawberryShake.CodeGeneration.CSharp.Integration.StarWars.State.ReviewData(
                    typename,
                    stars: DeserializeNonNullableInt32(global::StrawberryShake.Transport.Http.JsonElementExtensions.GetPropertyOrNull(obj.Value, "stars")));
            }

            throw new global::System.NotSupportedException();
        }

        private global::System.Int32 DeserializeNonNullableInt32(global::System.Text.Json.JsonElement? obj)
        {
            if (!obj.HasValue)
            {
                throw new global::System.ArgumentNullException();
            }

            return _int32Parser.Parse(obj.Value.GetInt32()!);
        }
    }
}


// ReviewData

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars.State
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class ReviewData
    {
        public ReviewData(
            global::System.String typename,
            global::System.Int32? stars = null)
        {
            __typename = typename
                 ?? throw new global::System.ArgumentNullException(nameof(typename));
            Stars = stars;
        }

        public global::System.String __typename { get; }

        public global::System.Int32? Stars { get; }
    }
}


// StarWarsIntegrationClient

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class StarWarsIntegrationClient
    {
        private readonly CreateReviewMutation _createReviewMutation;
        private readonly OnReviewSubscription _onReviewSubscription;

        public StarWarsIntegrationClient(
            CreateReviewMutation createReviewMutation,
            OnReviewSubscription onReviewSubscription)
        {
            _createReviewMutation = createReviewMutation
                 ?? throw new global::System.ArgumentNullException(nameof(createReviewMutation));
            _onReviewSubscription = onReviewSubscription
                 ?? throw new global::System.ArgumentNullException(nameof(onReviewSubscription));
        }

        public CreateReviewMutation CreateReviewMutation => _createReviewMutation;

        public OnReviewSubscription OnReviewSubscription => _onReviewSubscription;
    }
}


// EntityIdFactory

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public static partial class EntityIdFactory
    {
        public static global::StrawberryShake.EntityId CreateEntityId(global::System.Text.Json.JsonElement obj)
        {
            global::System.String typeName = obj.GetProperty("__typename").GetString()!;

            return typeName switch
            {
                _ => throw new global::System.NotSupportedException()
            };
        }
    }
}


// StarWarsIntegrationClientServiceCollectionExtensions

#nullable enable

namespace StrawberryShake.CodeGeneration.CSharp.Integration.StarWars
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public static partial class StarWarsIntegrationClientServiceCollectionExtensions
    {
        public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddStarWarsIntegrationClient(
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
                sp => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<CreateReviewMutation>(
                    global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ClientServiceProvider>(sp)));
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(
                services,
                sp => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<OnReviewSubscription>(
                    global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ClientServiceProvider>(sp)));

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(
                services,
                sp => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<StarWarsIntegrationClient>(
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

            // register entity id factory

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::System.Func<global::System.Text.Json.JsonElement, global::StrawberryShake.EntityId>>(services, EntityIdFactory.CreateEntityId);

            // register stores

            global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<
                global::StrawberryShake.IEntityStore,
                global::StrawberryShake.EntityStore>(
                    services);
            global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddSingleton<
                global::StrawberryShake.IOperationStore>(
                    services,
                    sp => new global::StrawberryShake.OperationStore(
                        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<
                            global::StrawberryShake.IEntityStore
                            >(sp)
                        .Watch()
                        ));

            // register connections

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(
                services,
                sp =>
                {
                    var sessionPool =
                        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<
                            global::StrawberryShake.Transport.WebSockets.ISessionPool
                            >(parentServices);

                    return new global::StrawberryShake.Transport.WebSockets.WebSocketConnection(
                        () => sessionPool.CreateAsync("StarWarsIntegrationClient", default));
                });


            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(
                services,
                sp =>
                {
                    var clientFactory =
                        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<
                            global::System.Net.Http.IHttpClientFactory
                            >(parentServices);

                    return new global::StrawberryShake.Transport.Http.HttpConnection(
                        () => clientFactory.CreateClient("StarWarsIntegrationClient"));
                });

            // register mappers


            // register serializers

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

            // register operations

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<
                global::StrawberryShake.IOperationResultDataFactory<ICreateReviewResult>,
                CreateReviewResultFactory>(
                    services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<
                global::StrawberryShake.IOperationResultBuilder<global::System.Text.Json.JsonDocument, ICreateReviewResult>,
                CreateReviewBuilder>(
                    services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<
                global::StrawberryShake.IOperationExecutor<ICreateReviewResult>>(
                    services,
                    sp => new global::StrawberryShake.OperationExecutor<global::System.Text.Json.JsonDocument, ICreateReviewResult>(
                        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.Transport.Http.HttpConnection>(sp),
                        () => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.IOperationResultBuilder<global::System.Text.Json.JsonDocument, ICreateReviewResult>>(sp),
                        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.IOperationStore>(sp),
                        strategy));

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<CreateReviewMutation>(services);


            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<
                global::StrawberryShake.IOperationResultDataFactory<IOnReviewResult>,
                OnReviewResultFactory>(
                    services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<
                global::StrawberryShake.IOperationResultBuilder<global::System.Text.Json.JsonDocument, IOnReviewResult>,
                OnReviewBuilder>(
                    services);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<
                global::StrawberryShake.IOperationExecutor<IOnReviewResult>>(
                    services,
                    sp => new global::StrawberryShake.OperationExecutor<global::System.Text.Json.JsonDocument, IOnReviewResult>(
                        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.Transport.WebSockets.WebSocketConnection>(sp),
                        () => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.IOperationResultBuilder<global::System.Text.Json.JsonDocument, IOnReviewResult>>(sp),
                        global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::StrawberryShake.IOperationStore>(sp),
                        strategy));

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<OnReviewSubscription>(services);

            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<StarWarsIntegrationClient>(services);

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


