using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using System.Linq;
using System.Collections.Generic;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    public class FilterConvention
        : ConventionBase<FilterConventionDefinition>
        , IFilterConvention
    {
        private readonly Action<IFilterConventionDescriptor> _configure;

        protected FilterConvention()
        {
            _configure = Configure;
        }

        public FilterConvention(Action<IFilterConventionDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        public IReadOnlyDictionary<int, OperationConvention> Operations { get; private set; }
            = null!;

        public IReadOnlyDictionary<Type, Type> Bindings { get; private set; } = null!;

        public IReadOnlyDictionary<ITypeReference, Action<IFilterInputTypeDescriptor>[]> Extensions
        { get; private set; } = null!;

        protected override FilterConventionDefinition CreateDefinition(
            IConventionContext context)
        {
            var descriptor = FilterConventionDescriptor.New(context);
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(
            IFilterConventionDescriptor descriptor)
        {
        }

        protected override void OnComplete(
            IConventionContext context,
            FilterConventionDefinition? definition)
        {
            if (definition is { })
            {
                Operations = definition
                    .Operations
                    .ToDictionary(x => x.Operation, x => new OperationConvention(x));
                Bindings = definition.Bindings;
                Extensions = definition.Extensions.ToDictionary(x => x.Key, x => x.Value.ToArray());
            }
        }

        public NameString GetFieldDescription(IDescriptorContext context, MemberInfo member) =>
            context.Naming.GetMemberDescription(member, MemberKind.InputObjectField);

        public NameString GetFieldName(IDescriptorContext context, MemberInfo member) =>
            context.Naming.GetMemberName(member, MemberKind.InputObjectField);

        public ITypeReference GetFieldType(IDescriptorContext context, MemberInfo member)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (TryGetTypeOfMember(member, out Type? reflectedType))
            {
                return new ClrTypeReference(reflectedType, TypeContext.Input, Scope);
            }

            throw ThrowHelper.FilterConvention_TypeOfMemberIsUnknown(member);
        }

        public NameString GetOperationDescription(IDescriptorContext context, int operation)
        {
            if (Operations.TryGetValue(operation, out OperationConvention? operationConvention))
            {
                return operationConvention.Description;
            }
            return null;
        }

        public NameString GetOperationName(IDescriptorContext context, int operation)
        {
            if (Operations.TryGetValue(operation, out OperationConvention? operationConvention))
            {
                return operationConvention.Name;
            }
            throw ThrowHelper.FilterConvention_OperationNameNotFound(operation);
        }

        public NameString GetTypeDescription(IDescriptorContext context, Type entityType) =>
            context.Naming.GetTypeDescription(entityType, TypeKind.InputObject);

        public NameString GetTypeName(IDescriptorContext context, Type entityType) =>
            context.Naming.GetTypeName(entityType, TypeKind.Object) + "Filter";

        private bool TryGetTypeOfMember(
            MemberInfo member,
            [NotNullWhen(true)] out Type? type)
        {
            if (member is PropertyInfo p &&
                TryGetTypeOfRuntimeType(p.PropertyType, out type))
            {
                return true;
            }
            type = null;
            return false;
        }

        private bool TryGetTypeOfRuntimeType(
            Type runtimeType,
            [NotNullWhen(true)] out Type? type)
        {
            if (runtimeType.IsGenericType
                && System.Nullable.GetUnderlyingType(runtimeType) is { } nullableType)
            {
                runtimeType = nullableType;
            }

            if (Bindings.TryGetValue(runtimeType, out type))
            {
                return true;
            }

            if (DotNetTypeInfoFactory.IsListType(runtimeType))
            {
                if (!TypeInspector.Default.TryCreate(runtimeType, out Utilities.TypeInfo typeInfo))
                {
                    throw new ArgumentException(
                        string.Format("The type {0} is unknown", runtimeType.Name),
                        nameof(runtimeType));
                }

                if (TryGetTypeOfRuntimeType(typeInfo.ClrType, out Type? clrType))
                {
                    type = typeof(ListFilterInput<>).MakeGenericType(clrType);
                    return true;
                }
            }

            if (runtimeType.IsEnum)
            {
                type = typeof(EnumOperationInput<>).MakeGenericType(runtimeType);
                return true;
            }

            if (runtimeType.IsClass)
            {
                type = typeof(FilterInputType<>).MakeGenericType(runtimeType);
                return true;
            }

            type = null;
            return false;
        }

        internal static readonly IFilterConvention Default = TemporaryInitializer();

        //TODO: Replace with named conventions!
        internal static IFilterConvention TemporaryInitializer()
        {
            var convention = new FilterConvention(x => x.UseDefault());
            convention.Initialize(new ConventionContext(null, null));
            return convention;
        }

        public IEnumerable<Action<IFilterInputTypeDescriptor>> GetExtensions(
            ITypeReference reference)
        {
            if (!Extensions.TryGetValue(
                reference,
                out Action<IFilterInputTypeDescriptor>[]? extensions))
            {
                extensions = Array.Empty<Action<IFilterInputTypeDescriptor>>();
            }

            return extensions;
        }
    }
}
