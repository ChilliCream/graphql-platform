using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public class ObjectField
        : InterfaceField
    {
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
                context.RegisterResolver(Name, _member);
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
