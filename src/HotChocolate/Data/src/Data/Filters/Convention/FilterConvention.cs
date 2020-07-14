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
    public class FilterConvention :
        ConventionBase<FilterConventionDefinition>
        , IFilterConvention
    {
        private readonly Action<IFilterConventionDescriptor> _configure;

        protected FilterConvention()
        {
            _configure = Configure;
        }

        public FilterConvention(Action<IFilterConventionDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        public IReadOnlyDictionary<int, OperationConvention> Operations { get; private set; }

        public IReadOnlyDictionary<Type, Type> Bindings { get; private set; }

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
            Operations = definition
                .Operations
                .ToDictionary(x => x.Operation, x => new OperationConvention(x));
            Bindings = definition.Bindings;
        }

        public NameString GetFieldDescription(IDescriptorContext context, MemberInfo member)
            => context.Naming.GetMemberDescription(member, MemberKind.InputObjectField);

        public NameString GetFieldName(IDescriptorContext context, MemberInfo member)
            => context.Naming.GetMemberName(member, MemberKind.InputObjectField);

        public ITypeReference GetFieldType(IDescriptorContext context, MemberInfo member)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (TryGetType(member, out Type? reflectedType))
            {
                return new ClrTypeReference(reflectedType, TypeContext.Input, Scope);
            }

            throw new SchemaException(
                SchemaErrorBuilder
                    .New()
                    .SetMessage(
                        "The type of the member {0} of the declaring type {1} is unknown",
                        member.Name,
                        member.DeclaringType.Name)
                    .Build());
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
            throw new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "Operation with identifier {0} has no name defined. Add a name to the " +
                        "filter convention",
                        operation)
                    .Build());
        }

        public NameString GetTypeDescription(IDescriptorContext context, Type entityType)
            => context.Naming.GetTypeDescription(entityType, TypeKind.InputObject);

        public NameString GetTypeName(IDescriptorContext context, Type entityType)
            => context.Naming.GetTypeName(entityType, TypeKind.InputObject);

        private bool TryGetType(
            MemberInfo member,
            [NotNullWhen(true)] out Type? type)
        {
            if (member is PropertyInfo p)
            {
                Type reflectedType = p.PropertyType;

                if (reflectedType.IsGenericType
                    && System.Nullable.GetUnderlyingType(reflectedType) is { } nullableType)
                {
                    reflectedType = nullableType;
                }

                if (Bindings.TryGetValue(reflectedType, out type))
                {
                    return true;
                }

                if (DotNetTypeInfoFactory.IsListType(reflectedType))
                {
                    if (!TypeInspector.Default.TryCreate(reflectedType, out Utilities.TypeInfo typeInfo))
                    {
                        throw new ArgumentException(
                            string.Format(
                                "The type {0} of the property {1} of the declaring type {2} is unknown",
                                p.PropertyType?.Name,
                                p.Name,
                                p.DeclaringType?.Name),
                            nameof(member));
                    }
                    type = typeInfo.ClrType;
                    return true;
                }

                if (reflectedType.IsClass)
                {
                    type = typeof(FilterInputType<>).MakeGenericType(reflectedType);
                    return true;
                }
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
    }
}
