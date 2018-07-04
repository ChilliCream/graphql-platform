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
            : this(ExecuteFactory(descriptionFactory))
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

            ObjectFieldDescriptor descriptor =
                new ObjectFieldDescriptor(null, fieldName);
            configure(descriptor);
            return descriptor.CreateFieldDescription();
        }

        public FieldResolverDelegate Resolver { get; private set; }

        internal override void OnRegisterDependencies(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            base.OnRegisterDependencies(schemaContext, reportError, parentType);

            if (_member != null)
            {
                schemaContext.Resolvers.RegisterResolver(
                    new MemberResolverBinding(parentType.Name, Name, _member));
            }
        }

        internal override void OnCompleteField(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            base.OnCompleteField(schemaContext, reportError, parentType);

            if (Resolver == null)
            {
                Resolver = schemaContext.Resolvers.GetResolver(parentType.Name, Name);
                if (Resolver == null)
                {
                    reportError(new SchemaError(
                        $"The field `{parentType.Name}.{Name}` " +
                        "has no resolver.", parentType));
                }
            }
        }
    }
}
