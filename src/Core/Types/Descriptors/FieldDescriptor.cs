using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    internal class FieldDescriptor
        : IFieldDescriptor
        , IInterfaceFieldDescriptor
    {
        private readonly string _typeName;
        private bool _argumentsInitialized;

        public FieldDescriptor(string typeName, string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException(
                    "The field name cannot be null or empty.",
                    nameof(fieldName));
            }

            if (!ValidationHelper.IsFieldNameValid(fieldName))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL field name.",
                    nameof(fieldName));
            }

            _typeName = typeName;
            Name = fieldName;
        }

        public FieldDescriptor(string typeName, MemberInfo member, Type nativeFieldType)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            _typeName = typeName;
            Member = member;
            Name = member.GetGraphQLName();
            TypeReference = new TypeReference(nativeFieldType);
        }

        public FieldDefinitionNode SyntaxNode { get; protected set; }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public MemberInfo Member { get; protected set; }

        public TypeReference TypeReference { get; protected set; }

        public string DeprecationReason { get; protected set; }

        protected ImmutableList<ArgumentDescriptor> Arguments { get; set; }
            = ImmutableList<ArgumentDescriptor>.Empty;

        public FieldResolverDelegate Resolver { get; protected set; }

        public IEnumerable<ArgumentDescriptor> GetArguments()
        {
            if (!_argumentsInitialized)
            {
                _argumentsInitialized = true;
                Arguments = CreateArguments().ToImmutableList();
            }
            return Arguments;
        }

        private IEnumerable<ArgumentDescriptor> CreateArguments()
        {
            Dictionary<string, ArgumentDescriptor> descriptors =
                new Dictionary<string, ArgumentDescriptor>();

            foreach (ArgumentDescriptor descriptor in Arguments)
            {
                descriptors[descriptor.Name] = descriptor;
            }

            if (Member != null && Member is MethodInfo m)
            {
                foreach (ParameterInfo parameter in m.GetParameters())
                {
                    string argumentName = parameter.GetGraphQLName();
                    if (!descriptors.ContainsKey(argumentName)
                        && IsArgumentType(parameter.ParameterType))
                    {
                        ArgumentDescriptor argumentDescriptor =
                            new ArgumentDescriptor(
                                argumentName, parameter.ParameterType);
                        descriptors[argumentName] = argumentDescriptor;
                    }
                }
            }

            return descriptors.Values;
        }

        private bool IsArgumentType(Type argumentType)
        {
            return (FieldResolverArgumentDescriptor
                .LookupKind(argumentType, Member.ReflectedType) ==
                    FieldResolverArgumentKind.Argument);
        }

        private FieldResolverDelegate CreateResolver(
            IResolverRegistry resolverRegistry)
        {
            return Resolver ?? resolverRegistry.GetResolver(_typeName, Name);
        }

        #region IFieldDescriptor

        IFieldDescriptor IFieldDescriptor.SyntaxNode(FieldDefinitionNode syntaxNode)
        {
            SyntaxNode = syntaxNode;
            return this;
        }

        IFieldDescriptor IFieldDescriptor.Name(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsFieldNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL field name.",
                    nameof(name));
            }

            Name = name;
            return this;
        }

        IFieldDescriptor IFieldDescriptor.Description(string description)
        {
            Description = description;
            return this;
        }

        IFieldDescriptor IFieldDescriptor.Type<TOutputType>()
        {
            TypeReference = TypeReference.GetMoreSpecific(typeof(TOutputType));
            return this;
        }

        IFieldDescriptor IFieldDescriptor.Type(ITypeNode type)
        {
            TypeReference = TypeReference.GetMoreSpecific(type);
            return this;
        }

        IFieldDescriptor IFieldDescriptor.DeprecationReason(
            string deprecationReason)
        {
            DeprecationReason = deprecationReason;
            return this;
        }

        IFieldDescriptor IFieldDescriptor.Argument(
            string name, Action<IArgumentDescriptor> argument)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The argument name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsFieldNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL argument name.",
                    nameof(name));
            }

            if (argument == null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            ArgumentDescriptor descriptor = new ArgumentDescriptor(name);
            argument(descriptor);
            Arguments = Arguments.Add(descriptor);
            return this;
        }

        IFieldDescriptor IFieldDescriptor.Resolver(
            FieldResolverDelegate fieldResolver)
        {
            if (fieldResolver == null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            Resolver = fieldResolver;
            return this;
        }

        IFieldDescriptor IFieldDescriptor.Resolver(
            FieldResolverDelegate fieldResolver, Type resultType)
        {
            if (fieldResolver == null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            Resolver = fieldResolver;
            TypeReference = TypeReference.GetMoreSpecific(resultType);
            return this;
        }

        #endregion

        #region IInterfaceFieldDescriptor

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.SyntaxNode(FieldDefinitionNode syntaxNode)
        {
            ((IFieldDescriptor)this).SyntaxNode(syntaxNode);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Name(string name)
        {
            ((IFieldDescriptor)this).Name(name);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Description(string description)
        {
            ((IFieldDescriptor)this).Description(description);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.DeprecationReason(string deprecationReason)
        {
            ((IFieldDescriptor)this).DeprecationReason(deprecationReason);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Type<TOutputType>()
        {
            ((IFieldDescriptor)this).Type<TOutputType>();
            return this;
        }

        IFieldDescriptor IInterfaceFieldDescriptor.Type(ITypeNode type)
        {
            ((IFieldDescriptor)this).Type(type);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Argument(string name, Action<IArgumentDescriptor> argument)
        {
            ((IFieldDescriptor)this).Argument(name, argument);
            return this;
        }

        #endregion
    }
}
