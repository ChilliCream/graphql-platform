using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Sorting;

public class SortConventionConfiguration : IHasScope
{
    public static readonly string DefaultArgumentName = "order";
    private string _argumentName = DefaultArgumentName;

    public string? Scope { get; set; }

    public string ArgumentName
    {
        get => _argumentName;
        set => _argumentName = value.EnsureGraphQLName();
    }

    public Type? Provider { get; set; }

    public ISortProvider? ProviderInstance { get; set; }

    public Type? DefaultBinding { get; set; }

    public IList<SortOperationConventionConfiguration> Operations { get; } = [];

    public IDictionary<Type, Type> Bindings { get; } = new Dictionary<Type, Type>();

    public IDictionary<TypeReference, List<ConfigureSortInputType>> Configurations { get; } =
        new Dictionary<TypeReference, List<ConfigureSortInputType>>(
            TypeReferenceComparer.Default);

    public IDictionary<TypeReference, List<ConfigureSortEnumType>> EnumConfigurations { get; }
        = new Dictionary<TypeReference, List<ConfigureSortEnumType>>(
            TypeReferenceComparer.Default);

    public IList<ISortProviderExtension> ProviderExtensions { get; } = [];

    public IList<Type> ProviderExtensionsTypes { get; } = [];
}
