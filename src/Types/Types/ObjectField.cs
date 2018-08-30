using System;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class ObjectField
        : InterfaceField
    {
        private readonly Type _sourceType;
        private readonly Type _resolverType;
        private readonly MemberInfo _member;

        internal ObjectField(string fieldName,
            Action<IObjectFieldDescriptor> configure)
            : this(() => ExecuteConfigure(fieldName, configure))
        {
        }

        internal ObjectField(Func<ObjectFieldDescription> descriptionFactory)
            : this(DescriptorHelpers.ExecuteFactory(descriptionFactory))
        {
        }

        internal ObjectField(ObjectFieldDescription fieldDescription)
            : base(fieldDescription)
        {
            _sourceType = fieldDescription.SourceType;
            _resolverType = fieldDescription.ResolverType;
            _member = fieldDescription.Member;

            Resolver = fieldDescription.Resolver;
        }

        private static ObjectFieldDescription ExecuteConfigure(
            string fieldName,
            Action<IObjectFieldDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var descriptor = new ObjectFieldDescriptor(null, fieldName);
            configure(descriptor);
            return descriptor.CreateDescription();
        }

        public FieldResolverDelegate Resolver { get; private set; }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            if (_member != null)
            {
                context.RegisterResolver(
                    _sourceType, _resolverType, Name, _member);
            }
        }

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            if (Resolver == null)
            {
                Resolver = context.GetResolver(Name);
                if (Resolver == null)
                {
                    context.ReportError(new SchemaError(
                        $"The field `{context.Type.Name}.{Name}` " +
                        "has no resolver.", context.Type));
                }
            }
        }
    }
}
