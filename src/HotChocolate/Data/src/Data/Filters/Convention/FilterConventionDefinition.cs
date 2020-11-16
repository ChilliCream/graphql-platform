using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public class FilterConventionDefinition : IHasScope
    {
        public static readonly string DefaultArgumentName = "where";

        public string? Scope { get; set; }

        public string ArgumentName { get; set; } = DefaultArgumentName;

        public Type? Provider { get; set; }

        public IFilterProvider? ProviderInstance { get; set; }

        public List<FilterOperationConventionDefinition> Operations { get; } =
            new List<FilterOperationConventionDefinition>();

        public IDictionary<Type, Type> Bindings { get; } = new Dictionary<Type, Type>();

        public IDictionary<ITypeReference, List<ConfigureFilterInputType>> Configurations { get; } =
            new Dictionary<ITypeReference, List<ConfigureFilterInputType>>(
                TypeReferenceComparer.Default);

        public List<IFilterProviderExtension> ProviderExtensions { get; } =
            new List<IFilterProviderExtension>();

        public List<Type> ProviderExtensionsTypes { get; } = new List<Type>();
    }
}
