using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters
{
    public class FilterConventionDescriptor
        : IFilterConventionDescriptor
    {
        private readonly IServiceProvider _services;

        protected ICollection<FilterOperationConventionDescriptor> Operations { get; } =
            new List<FilterOperationConventionDescriptor>();

        protected FilterConventionDescriptor(IConventionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Definition.Scope = context.Scope;
            _services = context.Services;
        }

        protected FilterConventionDefinition Definition { get; set; } =
            new FilterConventionDefinition();

        public IFilterOperationConventionDescriptor Operation(int operation)
        {
            FilterOperationConventionDescriptor? descriptor =
                Operations.FirstOrDefault(x => x.Definition.Operation == operation);

            if (descriptor is null)
            {
                descriptor = new FilterOperationConventionDescriptor(operation);
                Operations.Add(descriptor);
            }

            return descriptor;
        }

        public FilterConventionDefinition CreateDefinition()
        {
            Definition.Operations = Operations.Select(x => x.CreateDefinition());
            return Definition;
        }

        public IFilterConventionDescriptor Binding<TRuntime, TInput>()
        {
            Definition.Bindings.Add(typeof(TRuntime), typeof(TInput));
            return this;
        }

        public static FilterConventionDescriptor New(IConventionContext context) =>
            new FilterConventionDescriptor(context);

        public IFilterConventionDescriptor Extension(
            NameString typeName,
            Action<IFilterInputTypeDescriptor> extension)
        {
            TypeReference? typeReference =
                TypeReference.Create(
                    typeName,
                    TypeContext.Input,
                    Definition.Scope);

            if (!Definition.Extensions.TryGetValue(
                    typeReference,
                    out List<Action<IFilterInputTypeDescriptor>>? descriptorList))
            {
                descriptorList = new List<Action<IFilterInputTypeDescriptor>>();
                Definition.Extensions[typeReference] = descriptorList;
            }

            descriptorList.Add(extension);
            return this;
        }

        public IFilterConventionDescriptor Extension<TFilterType>(
                Action<IFilterInputTypeDescriptor> extension)
            where TFilterType : FilterInputType
        {
            TypeReference? typeReference =
                TypeReference.Create<TFilterType>(
                    TypeContext.Input,
                    Definition.Scope);

            if (!Definition.Extensions.TryGetValue(
                    typeReference,
                    out List<Action<IFilterInputTypeDescriptor>>? descriptorList))
            {
                descriptorList = new List<Action<IFilterInputTypeDescriptor>>();
                Definition.Extensions[typeReference] = descriptorList;
            }

            descriptorList.Add(extension);
            return this;
        }

        public IFilterConventionDescriptor Extension<TFilterType, TType>(
                Action<IFilterInputTypeDescriptor<TType>> extension)
            where TFilterType : FilterInputType<TType>
        {
            TypeReference? typeReference =
                TypeReference.Create<TFilterType>(
                    TypeContext.Input,
                    Definition.Scope);

            if (!Definition.Extensions.TryGetValue(
                    typeReference,
                    out List<Action<IFilterInputTypeDescriptor>>? descriptorList))
            {
                descriptorList = new List<Action<IFilterInputTypeDescriptor>>();
                Definition.Extensions[typeReference] = descriptorList;
            }

            descriptorList.Add(descriptor =>
            {
                if (descriptor is IFilterInputTypeDescriptor<TType> descriptorOfT)
                {
                    extension.Invoke(descriptorOfT);
                }
            });

            return this;
        }

        public IFilterConventionDescriptor Provider<TProvider>()
            where TProvider : IFilterProvider
        {
            Definition.Provider = _services.GetService<TProvider>();
            return this;
        }
    }
}
