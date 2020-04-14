using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Sorting.Conventions
{
    public delegate NameString GetSortingTypeName(
        IDescriptorContext context,
        Type entityType);

    public delegate string GetSortingDescription(
        IDescriptorContext context,
        Type entityType);

    public delegate bool TryCreateImplicitSorting(
         IDescriptorContext context,
         Type type,
         PropertyInfo property,
         ISortingConvention filterConventions,
         [NotNullWhen(true)] out SortOperationDefintion? definition);

    public class SortingConvention : ISortingConvention
    {
        private readonly object _definitionLock = new { };
        private readonly Action<ISortingConventionDescriptor> _configure;
        private volatile SortingConventionDefinition? _definition;

        public SortingConvention()
        {
            _configure = Configure;
        }

        public SortingConvention(Action<ISortingConventionDescriptor> descriptor)
        {
            _configure = descriptor;
        }

        public NameString GetArgumentName()
        {
            return GetOrCreateConfiguration().ArgumentName;
        }

        public NameString GetOperationKindTypeName(
            IDescriptorContext context,
            Type entityType)
        {
            GetSortingTypeName? factory = GetOrCreateConfiguration().OperationKindTypeNameFactory;

            return factory == null ? (NameString)"" : factory(context, entityType);
        }

        public NameString GetTypeName(
            IDescriptorContext context,
            Type entityType)
        {
            GetSortingTypeName? factory = GetOrCreateConfiguration().TypeNameFactory;

            return factory == null ? (NameString)"" : factory(context, entityType);
        }

        public string GetDescription(
            IDescriptorContext context,
            Type entityType)
        {
            GetSortingDescription? factory = GetOrCreateConfiguration().DescriptionFactory;

            return factory == null ? "" : factory(context, entityType);
        }

        public NameString GetAscendingName()
            => GetOrCreateConfiguration().AscendingName;

        public NameString GetDescendingName()
            => GetOrCreateConfiguration().DescendingName;

        public IReadOnlyList<TryCreateImplicitSorting> GetImplicitFactories()
        {
            return GetOrCreateConfiguration().ImplicitSortingFactories;
        }

        protected virtual void Configure(
            ISortingConventionDescriptor descriptor)
        {
        }

        private SortingConventionDefinition CreateDefinition()
        {
            var descriptor = SortingConventionDescriptor.New();
            descriptor.UseDefault();
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected SortingConventionDefinition GetOrCreateConfiguration()
        {
            if (_definition == null)
            {
                lock (_definitionLock)
                {
                    if (_definition == null)
                    {
                        _definition = CreateDefinition();
                    }
                }
            }
            return _definition;
        }

        public async Task ApplySorting<T>(
            FieldDelegate next,
            ITypeConversion converter,
            IMiddlewareContext context)
        {
            if (GetOrCreateConfiguration().VisitorDefinition is { } definition)
            {
                await definition.ApplSorting<T>(this, next, converter, context)
                    .ConfigureAwait(false);
            }
            else
            {
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage("No visitor definiton found for this SortingConvention")
                        .Build());
            }
        }

        public readonly static ISortingConvention Default = new SortingConvention();
    }
}
