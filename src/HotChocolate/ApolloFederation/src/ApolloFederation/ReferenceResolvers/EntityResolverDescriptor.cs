using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.ApolloFederation
{
    public class EntityResolverDescriptor : DescriptorBase<EntityResolverDefinition>, IEntityResolverDescriptor
    {
        private readonly IObjectTypeDescriptor _typeDescriptor;

        public EntityResolverDescriptor(IObjectTypeDescriptor descriptor, Type? resolvedEntityType = null)
            : base(descriptor.Extend().Context)
        {
            _typeDescriptor = descriptor;

            _typeDescriptor
                .Extend()
                .OnBeforeCreate(OnCompleteDefinition);

            Definition.ResolvedEntityType = resolvedEntityType;
        }

        private void OnCompleteDefinition(ObjectTypeDefinition definition)
        {
            if (Definition.Resolver is not null)
            {
                definition.ContextData[WellKnownContextData.EntityResolver] =
                    Definition.Resolver;
            }
        }

        protected override EntityResolverDefinition Definition { get; set; } = new EntityResolverDefinition();

        public IObjectTypeDescriptor ResolveEntity(FieldResolverDelegate fieldResolver)
        {
            Definition.Resolver = fieldResolver ??
                                  throw new ArgumentNullException(nameof(fieldResolver));

            return _typeDescriptor;
        }

        public IObjectTypeDescriptor ResolveEntityWith<TResolver>(Expression<Func<TResolver, object>> method)
        {
            if (method is null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            MemberInfo member = method.TryExtractMember();

            if (member is MethodInfo m)
            {
                FieldResolver resolver =
                    ResolverCompiler.Resolve.Compile(
                        new ResolverDescriptor(
                            typeof(TResolver),
                            typeof(object),
                            new FieldMember("_", "_", m)));
                return ResolveEntity(resolver.Resolver);
            }

            throw new ArgumentException(
                FederationResources.EntityResolver_MustBeMethod,
                nameof(member));
        }

        public IObjectTypeDescriptor ResolveEntityWith<TResolver>() =>
            ResolveEntityWith(Context.TypeInspector.GetNodeResolverMethod(
                Definition.ResolvedEntityType ?? typeof(TResolver),
                typeof(TResolver)));

        public IObjectTypeDescriptor ResolveEntityWith(MethodInfo method)
        {
            if (method is null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            FieldResolver resolver =
                ResolverCompiler.Resolve.Compile(
                    new ResolverDescriptor(
                        method.DeclaringType ?? typeof(object),
                        typeof(object),
                        new FieldMember("_", "_", method)));
            return ResolveEntity(resolver.Resolver);
        }

        public IObjectTypeDescriptor ResolveEntityWith(Type type) =>
            ResolveEntityWith(Context.TypeInspector.GetNodeResolverMethod(
                Definition.ResolvedEntityType ?? type,
                type));
    }
}
