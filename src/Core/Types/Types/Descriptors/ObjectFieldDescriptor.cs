using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using System.Linq;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Types
{
    internal class ObjectFieldDescriptor
        : ObjectFieldDescriptorBase
        , IObjectFieldDescriptor
        , IDescriptionFactory<ObjectFieldDescription>
    {
        private bool _argumentsInitialized;

        public ObjectFieldDescriptor(NameString fieldName)
            : base(new ObjectFieldDescription())
        {
            FieldDescription.Name =
                fieldName.EnsureNotEmpty(nameof(fieldName));
        }

        public ObjectFieldDescriptor(MemberInfo member, Type sourceType)
            : base(new ObjectFieldDescription())
        {
            FieldDescription.ClrMember = member
                ?? throw new ArgumentNullException(nameof(member));

            FieldDescription.SourceType = sourceType;
            FieldDescription.Name = member.GetGraphQLName();
            FieldDescription.Description = member.GetGraphQLDescription();
            FieldDescription.TypeReference = member.GetOutputType();
            FieldDescription.AcquireNonNullStatus(member);
        }

        protected new ObjectFieldDescription FieldDescription
            => (ObjectFieldDescription)base.FieldDescription;

        public new ObjectFieldDescription CreateDescription()
        {
            CompleteArguments();
            FieldDescription.RewriteClrType(c => c.GetOutputType());
            return FieldDescription;
        }

        public void ResolverType(Type resolverType)
        {
            FieldDescription.ResolverType = resolverType;
        }

        protected void Ignore()
        {
            FieldDescription.Ignored = true;
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
            FieldDescription.TypeReference = FieldDescription.TypeReference
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

        IObjectFieldDescriptor IObjectFieldDescriptor.SyntaxNode(
            FieldDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Name(NameString name)
        {
            Name(name);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.DeprecationReason(
            string deprecationReason)
        {
            DeprecationReason(deprecationReason);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Type<TOutputType>()
        {
            Type<TOutputType>();
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Type(ITypeNode type)
        {
            Type(type);
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

        IObjectFieldDescriptor IObjectFieldDescriptor.Directive(
            string name,
            params ArgumentNode[] arguments)
        {
            FieldDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }

    internal static class FieldDescriptorUtilities
    {
        internal static void DiscoverArguments(
            ICollection<ArgumentDescription> arguments,
            MemberInfo member)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            var processed = new HashSet<NameString>();

            foreach (ArgumentDescription description in arguments)
            {
                processed.Add(description.Name);
            }

            if (member is MethodInfo method)
            {
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    string argumentName = parameter.GetGraphQLName();
                    if (IsArgumentType(method, parameter)
                        && processed.Add(argumentName))
                    {
                        var argumentDescriptor = new ArgumentDescriptor(
                            argumentName, parameter.ParameterType);

                        argumentDescriptor.Description(
                            parameter.GetGraphQLDescription());

                        arguments.Add(argumentDescriptor.CreateDescription());
                    }
                }
            }
        }

        private static bool IsArgumentType(
            MemberInfo member,
            ParameterInfo parameter)
        {
            return (ArgumentHelper
                .LookupKind(parameter, member.ReflectedType) ==
                    ArgumentKind.Argument);
        }
    }
}
