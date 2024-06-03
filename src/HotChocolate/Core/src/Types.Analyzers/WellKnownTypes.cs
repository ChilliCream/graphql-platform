using System.Data;

namespace HotChocolate.Types.Analyzers;

public static class WellKnownTypes
{
    public const string ModuleAttribute = "HotChocolate.ModuleAttribute";
    public const string DataLoaderDefaultsAttribute = "HotChocolate.DataLoaderDefaultsAttribute";
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
    public const string ReadOnlyDictionary = "System.Collections.Generic.IReadOnlyDictionary";
    public const string Lookup = "System.Linq.ILookup";
    public const string Task = "System.Threading.Tasks.Task";
    public const string ValueTask = "System.Threading.Tasks.ValueTask";
    public const string RequestCoreMiddleware = $"HotChocolate.Execution.{nameof(RequestCoreMiddleware)}";
    public const string Schema = $"HotChocolate.{nameof(Schema)}";
    public const string RequestExecutorBuilder = "HotChocolate.Execution.Configuration.IRequestExecutorBuilder";
    public const string FieldResolverDelegate = "HotChocolate.Resolvers.FieldResolverDelegate";
    public const string ResolverContext = "HotChocolate.Resolvers.IResolverContext";
    public const string PureResolverContext = "HotChocolate.Resolvers.IPureResolverContext";
    public const string ParameterBinding = "HotChocolate.Internal.IParameterBinding";
    public const string MemoryMarshal = "System.Runtime.InteropServices.MemoryMarshal";
    public const string Unsafe = "System.Runtime.CompilerServices.Unsafe";
    public const string Object = "System.Object";
    public const string Executable = "HotChocolate.IExecutable";
    public const string ClaimsPrincipal = "System.Security.Claims.ClaimsPrincipal";
    public const string DocumentNode = "HotChocolate.Language.DocumentNode";

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
}
