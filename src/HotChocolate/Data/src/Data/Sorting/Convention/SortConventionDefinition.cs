using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Sorting
{
    public class SortConventionDefinition : IHasScope
    {
        public static readonly string DefaultArgumentName = "order";

        public string? Scope { get; set; }

        public string ArgumentName { get; set; } = DefaultArgumentName;

        public Type? Provider { get; set; }

        public ISortProvider? ProviderInstance { get; set; }

        public Type? DefaultBinding { get; set; }

        public List<SortOperationConventionDefinition> Operations { get; } =
            new List<SortOperationConventionDefinition>();

        public IDictionary<Type, Type> Bindings { get; } = new Dictionary<Type, Type>();

        public IDictionary<ITypeReference, List<ConfigureSortInputType>> Configurations { get; } =
            new Dictionary<ITypeReference, List<ConfigureSortInputType>>(
                TypeReferenceComparer.Default);

        public IDictionary<ITypeReference, List<ConfigureSortEnumType>> EnumConfigurations { get; }
            = new Dictionary<ITypeReference, List<ConfigureSortEnumType>>(
                TypeReferenceComparer.Default);

        public List<ISortProviderExtension> ProviderExtensions { get; } =
            new List<ISortProviderExtension>();

        public List<Type> ProviderExtensionsTypes { get; } = new List<Type>();
    }
}
