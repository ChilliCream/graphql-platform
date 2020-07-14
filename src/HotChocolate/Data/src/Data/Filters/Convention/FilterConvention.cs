using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using System.Collections.Immutable;

namespace HotChocolate.Data.Filters
{
    public class FilterConvention :
        ConventionBase<FilterConventionDefinition>
        , IFilterConvention
    {
        public IImmutableDictionary<int, OperationConvention> Operations { get; private set; }

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
                .ToImmutableDictionary(x => x.Operation, x => new OperationConvention(x));
        }

        public NameString GetFieldDescription(IDescriptorContext context, MemberInfo member) =>
            context.Naming.GetMemberDescription(member, MemberKind.InputObjectField);

        public NameString GetFieldName(IDescriptorContext context, MemberInfo member) =>
            context.Naming.GetMemberName(member, MemberKind.InputObjectField);

        public ITypeReference GetFieldType(IDescriptorContext context, MemberInfo member) =>
            context.Inspector.GetInputReturnType(member);

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
            context.Naming.GetTypeName(entityType, TypeKind.InputObject);

        public bool TryCreateImplicitFilter(
            PropertyInfo property,
            [NotNullWhen(true)] out InputFieldDefinition? definition)
        {
            throw new NotImplementedException();
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
