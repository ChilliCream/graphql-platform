namespace HotChocolate.Types.Analyzers;

public static class WellKnownTypes
{
    public const string SystemObject = "System.Object";
    public const string ObjectType = "HotChocolate.Types.ObjectType";
    public const string InterfaceType = "HotChocolate.Types.InterfaceType";
    public const string UnionType = "HotChocolate.Types.UnionType";
    public const string InputObjectType = "HotChocolate.Types.InputObjectType";
    public const string EnumType = "HotChocolate.Types.EnumType";
    public const string ScalarType = "HotChocolate.Types.ScalarType";
    public const string ObjectTypeExtension = "HotChocolate.Types.ObjectTypeExtension";
    public const string InterfaceTypeExtension = "HotChocolate.Types.InterfaceTypeExtension";
    public const string UnionTypeExtension = "HotChocolate.Types.UnionTypeExtension";
    public const string InputObjectTypeExtension = "HotChocolate.Types.InputObjectTypeExtension";
    public const string EnumTypeExtension = "HotChocolate.Types.EnumTypeExtension";
    public const string DataLoader = "GreenDonut.IDataLoader";
    public const string CancellationToken = "System.Threading.CancellationToken";
    public const string GlobalCancellationToken = "global::System.Threading.CancellationToken";
    public const string ReadOnlyList = "System.Collections.Generic.IReadOnlyList";
    public const string AsyncEnumerable = "System.Collections.Generic.IAsyncEnumerable";
    public const string Queryable = "System.Linq.IQueryable";
    public const string Dictionary = "System.Collections.Generic.Dictionary";
    public const string ReadOnlyDictionary = "System.Collections.Generic.IReadOnlyDictionary";
    public const string DictionaryInterface = "System.Collections.Generic.IDictionary";
    public const string Lookup = "System.Linq.ILookup";
    public const string Task = "System.Threading.Tasks.Task";
    public const string ValueTask = "System.Threading.Tasks.ValueTask";
    public const string RequestCoreMiddleware = $"HotChocolate.Execution.{nameof(RequestCoreMiddleware)}";
    public const string Schema = $"HotChocolate.{nameof(Schema)}";
    public const string RequestExecutorBuilder = "HotChocolate.Execution.Configuration.IRequestExecutorBuilder";
    public const string FieldResolverDelegate = "HotChocolate.Resolvers.FieldResolverDelegate";
    public const string ResolverContext = "HotChocolate.Resolvers.IResolverContext";
    public const string ParameterBinding = "HotChocolate.Internal.IParameterBinding";
    public const string MemoryMarshal = "System.Runtime.InteropServices.MemoryMarshal";
    public const string Unsafe = "System.Runtime.CompilerServices.Unsafe";
    public const string Object = "System.Object";
    public const string Executable = "HotChocolate.IExecutable";
    public const string ClaimsPrincipal = "System.Security.Claims.ClaimsPrincipal";
    public const string DocumentNode = "HotChocolate.Language.DocumentNode";
    public const string OutputField = "HotChocolate.Types.IOutputField";
    public const string ParameterBindingResolver = "HotChocolate.Internal.IParameterBindingResolver";
    public const string CustomAttributeData = "HotChocolate.Internal.GenCustomAttributeData";
    public const string ParameterInfo = "HotChocolate.Internal.GenParameterInfo";
    public const string CustomAttributeTypedArgument = "System.Reflection.CustomAttributeTypedArgument";
    public const string CustomAttributeNamedArgument = "System.Reflection.CustomAttributeNamedArgument";
    public const string BindingFlags = "System.Reflection.BindingFlags";
    public const string HttpContext = "Microsoft.AspNetCore.Http.HttpContext";
    public const string HttpRequest = "Microsoft.AspNetCore.Http.HttpRequest";
    public const string HttpResponse = "Microsoft.AspNetCore.Http.HttpResponse";
    public const string FieldNode = "HotChocolate.Language.FieldNode";
    public const string ArgumentKind = "HotChocolate.Internal.ArgumentKind";
    public const string SchemaException = "HotChocolate.SchemaException";
    public const string SchemaErrorBuilder = "HotChocolate.SchemaErrorBuilder";
    public const string InvalidOperationException = "System.InvalidOperationException";
    public const string FieldResolverDelegates = "HotChocolate.Resolvers.FieldResolverDelegates";
    public const string ListPostProcessor = "HotChocolate.Execution.ListPostProcessor";
    public const string EnumerableDefinition = "System.Collections.Generic.IEnumerable<>";
    public const string ServiceCollection = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    public const string DataLoaderServiceCollectionExtension = "Microsoft.Extensions.DependencyInjection.DataLoaderServiceCollectionExtensions";
    public const string Memory = "System.Memory";
    public const string Span = "System.Span";
    public const string Result = "GreenDonut.Result";
    public const string DataLoaderFetchContext = "GreenDonut.DataLoaderFetchContext";
    public const string Array = "System.Array";
    public const string PromiseCacheObserver = "GreenDonut.PromiseCacheObserver";
    public const string KeyValuePair = "System.Collections.Generic.KeyValuePair";
    public const string EnumerableExtensions = "System.Linq.Enumerable";
    public const string SelectorBuilder = "GreenDonut.Selectors.ISelectorBuilder";
    public const string PredicateBuilder = "GreenDonut.Predicates.IPredicateBuilder";
    public const string PagingArguments = "HotChocolate.Pagination.PagingArguments";

    public static HashSet<string> TypeClass { get; } =
    [
        ObjectType,
        InterfaceType,
        UnionType,
        InputObjectType,
        EnumType,
        ScalarType,
    ];

    public static HashSet<string> TypeExtensionClass { get; } =
    [
        ObjectTypeExtension,
        InterfaceTypeExtension,
        UnionTypeExtension,
        InputObjectTypeExtension,
        EnumTypeExtension,
    ];

    public static HashSet<string> SupportedListInterfaces { get; } =
        new()
        {
            "System.Collections.Generic.IReadOnlyCollection<>",
            "System.Collections.Generic.IReadOnlyList<>",
            "System.Collections.Generic.ICollection<>",
            "System.Collections.Generic.IList<>",
            "System.Collections.Generic.ISet<>",
            "System.Linq.IQueryable<>",
            "System.Collections.Generic.IAsyncEnumerable<>",
            "System.IObservable<>",
            "System.Collections.Generic.List<>",
            "System.Collections.ObjectModel.Collection<>",
            "System.Collections.Generic.Stack<>",
            "System.Collections.Generic.HashSet<>",
            "System.Collections.Generic.Queue<>",
            "System.Collections.Concurrent.ConcurrentBag<>",
            "System.Collections.Immutable.ImmutableArray<>",
            "System.Collections.Immutable.ImmutableList<>",
            "System.Collections.Immutable.ImmutableQueue<>",
            "System.Collections.Immutable.ImmutableStack<>",
            "System.Collections.Immutable.ImmutableHashSet<>",
            "HotChocolate.Execution.ISourceStream<>",
            "HotChocolate.IExecutable<>"
        };

    public static HashSet<string> TaskWrapper { get; } =
        new()
        {
            "System.Threading.Tasks.Task<>",
            "System.Threading.Tasks.ValueTask<>"
        };
}
