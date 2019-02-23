using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using System.Linq;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Types.Descriptors
{
    internal class ObjectFieldDescriptor
        : IObjectFieldDescriptor
        , IDescriptionFactory<ObjectFieldDescription>
    {
        private bool _argumentsInitialized;

        public ObjectFieldDescriptor(NameString fieldName)
        {
            FieldDescription.Name =
                fieldName.EnsureNotEmpty(nameof(fieldName));
        }

        public ObjectFieldDescriptor(MemberInfo member, Type sourceType)
        {
            FieldDescription.Member = member
                ?? throw new ArgumentNullException(nameof(member));

            FieldDescription.Name = member.GetGraphQLName();
            FieldDescription.Description = member.GetGraphQLDescription();
            FieldDescription.Type = member.GetOutputType();
            FieldDescription.AcquireNonNullStatus(member);
        }

        protected ObjectFieldDescription FieldDescription { get; } =
            new ObjectFieldDescription();

        public new ObjectFieldDescription CreateDescription()
        {
            CompleteArguments();
            FieldDescription.RewriteClrType(c => c.GetOutputType());
            return FieldDescription;
        }



        protected void Resolver(FieldResolverDelegate fieldResolver)
        {
            FieldDescription.Resolver = fieldResolver;
        }

        protected void Resolver(
            FieldResolverDelegate fieldResolver,
            Type resultType)
        {
            FieldDescription.Resolver = fieldResolver;
            FieldDescription.Type = FieldDescription.Type
                .GetMoreSpecific(resultType, TypeContext.Output);
        }

        protected void Use(FieldMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            FieldDescription.MiddlewareComponents.Add(middleware);
        }

        private void CompleteArguments()
        {
            if (!_argumentsInitialized)
            {
                FieldDescriptorUtilities.DiscoverArguments(
                    FieldDescription.Arguments,
                    FieldDescription.ClrMember);
                _argumentsInitialized = true;
            }
        }

        #region IObjectFieldDescriptor

        public IObjectFieldDescriptor SyntaxNode(
            FieldDefinitionNode fieldDefinition)
        {
            FieldDescription.SyntaxNode = fieldDefinition;
            return this;
        }

        public IObjectFieldDescriptor Name(NameString value)
        {
            FieldDescription.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IObjectFieldDescriptor Description(
            string description)
        {
            FieldDescription.Description = description;
            return this;
        }

        public IObjectFieldDescriptor DeprecationReason(
            string deprecationReason)
        {
            FieldDescription.DeprecationReason = deprecationReason;
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Type<TOutputType>()
        {
            FieldDescription.Type = FieldDescription.SetMoreSpecificType(
                typeof(TInputType), TypeContext.Input);
            return this;
        }

        public IObjectFieldDescriptor Type<TOutputType>(
            TOutputType outputType)
            where TOutputType : class, IOutputType
        {
            if (outputType == null)
            {
                throw new ArgumentNullException(nameof(outputType));
            }
            FieldDescription.Type = new SchemaTypeReference(outputType);
            return this;
        }

        public IObjectFieldDescriptor Type(ITypeNode typeNode)
        {
            if (typeNode == null)
            {
                throw new ArgumentNullException(nameof(typeNode));
            }
            FieldDescription.SetMoreSpecificType(typeNode);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Argument(
            NameString name,
            Action<IArgumentDescriptor> argument)
        {
            Argument(name, argument);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Ignore()
        {
            Ignore();
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Resolver(
            FieldResolverDelegate fieldResolver)
        {
            Resolver(fieldResolver);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Resolver(
            FieldResolverDelegate fieldResolver,
            Type resultType)
        {
            Resolver(fieldResolver, resultType);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Use(
            FieldMiddleware middleware)
        {
            Use(middleware);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Directive<T>(T directive)
        {
            FieldDescription.Directives.AddDirective(directive);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Directive<T>()
        {
            FieldDescription.Directives.AddDirective(new T());
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            FieldDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        DescriptionBase IDescriptionFactory.CreateDescription()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
