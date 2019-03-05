using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using System.Linq;
using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class ObjectFieldDescriptor
        : OutputFieldDescriptorBase<ObjectFieldDefinition>
        , IObjectFieldDescriptor
    {
        private bool _argumentsInitialized;

        public ObjectFieldDescriptor(NameString fieldName)
        {
            Definition.Name =
                fieldName.EnsureNotEmpty(nameof(fieldName));
        }

        public ObjectFieldDescriptor(MemberInfo member, Type sourceType)
        {
            Definition.Member = member
                ?? throw new ArgumentNullException(nameof(member));

            Definition.Name = member.GetGraphQLName();
            Definition.Description = member.GetGraphQLDescription();
            Definition.Type = member.GetOutputType();
            Definition.AcquireNonNullStatus(member);
        }

        protected override ObjectFieldDefinition Definition { get; } =
            new ObjectFieldDefinition();

        protected override void OnCreateDefinition(
            ObjectFieldDefinition definition)
        {
            CompleteArguments(definition);
            definition.RewriteClrType(c => c.GetOutputType());
        }

        private void CompleteArguments(ObjectFieldDefinition definition)
        {
            if (!_argumentsInitialized)
            {
                FieldDescriptorUtilities.DiscoverArguments(
                    definition.Arguments,
                    definition.Member);
                _argumentsInitialized = true;
            }
        }

        public new IObjectFieldDescriptor SyntaxNode(
            FieldDefinitionNode fieldDefinition)
        {
            base.SyntaxNode(fieldDefinition);
            return this;
        }

        public new IObjectFieldDescriptor Name(NameString value)
        {
            base.Name(value);
            return this;
        }

        public new IObjectFieldDescriptor Description(
            string value)
        {
            base.Description(value);
            return this;
        }

        public new IObjectFieldDescriptor DeprecationReason(
            string value)
        {
            base.DeprecationReason(value);
            return this;
        }

        public new IObjectFieldDescriptor Type<TOutputType>()
            where TOutputType : IOutputType
        {
            base.Type<TOutputType>();
            return this;
        }

        public new IObjectFieldDescriptor Type<TOutputType>(
            TOutputType outputType)
            where TOutputType : class, IOutputType
        {
            base.Type(outputType);
            return this;
        }

        public new IObjectFieldDescriptor Type(ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public new IObjectFieldDescriptor Argument(
            NameString name,
            Action<IArgumentDescriptor> argument)
        {
            base.Argument(name, argument);
            return this;
        }

        public new IObjectFieldDescriptor Ignore()
        {
            base.Ignore();
            return this;
        }

        public IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver)
        {
            if (fieldResolver == null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            Definition.Resolver = fieldResolver;
            return this;
        }

        public IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver,
            Type resultType)
        {
            if (fieldResolver == null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            Resolver(fieldResolver, resultType);
            return this;
        }

        public IObjectFieldDescriptor Use(FieldMiddleware middleware)
        {
            Use(middleware);
            return this;
        }

        public new IObjectFieldDescriptor Directive<T>(T directive)
            where T : class
        {
            base.Directive(directive);
            return this;
        }

        public new IObjectFieldDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        public new IObjectFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
