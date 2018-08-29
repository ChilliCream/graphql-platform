using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using System.Linq;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Types
{
    internal class ObjectFieldDescriptor
        : InterfaceFieldDescriptor
        , IObjectFieldDescriptor
        , IDescriptionFactory<ObjectFieldDescription>
    {
        private readonly string _typeName;
        private bool _argumentsInitialized;

        public ObjectFieldDescriptor(string typeName, string fieldName)
            : base(new ObjectFieldDescription())
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
            FieldDescription.Name = fieldName;

        }

        public ObjectFieldDescriptor(string typeName, MemberInfo member, Type nativeFieldType)
            : base(new ObjectFieldDescription())
        {
            _typeName = typeName;
            FieldDescription.Member = member
                ?? throw new ArgumentNullException(nameof(member));
            FieldDescription.Name = member.GetGraphQLName();
            FieldDescription.TypeReference = new TypeReference(nativeFieldType);
        }

        protected new ObjectFieldDescription FieldDescription
            => (ObjectFieldDescription)base.FieldDescription;

        public new ObjectFieldDescription CreateDescription()
        {
            CompleteArguments();
            return FieldDescription;
        }

        protected void Ignore()
        {
            FieldDescription.Ignored = true;
        }

        protected void Resolver(FieldResolverDelegate fieldResolver)
        {
            FieldDescription.Resolver = fieldResolver;
        }

        protected void Resolver(FieldResolverDelegate fieldResolver, Type resultType)
        {
            FieldDescription.Resolver = fieldResolver;
            FieldDescription.TypeReference = FieldDescription.TypeReference
                .GetMoreSpecific(resultType);
        }

        private void CompleteArguments()
        {
            if (!_argumentsInitialized)
            {
                FieldDescription.Arguments = CreateArguments().ToList();
                _argumentsInitialized = true;
            }
        }

        private IEnumerable<ArgumentDescription> CreateArguments()
        {
            var descriptions = new Dictionary<string, ArgumentDescription>();

            foreach (ArgumentDescription descriptor in FieldDescription.Arguments)
            {
                descriptions[descriptor.Name] = descriptor;
            }

            if (FieldDescription.Member != null && FieldDescription.Member is MethodInfo m)
            {
                foreach (ParameterInfo parameter in m.GetParameters())
                {
                    string argumentName = parameter.GetGraphQLName();
                    if (!descriptions.ContainsKey(argumentName)
                        && IsArgumentType(parameter))
                    {
                        var argumentDescriptor =
                            new ArgumentDescriptor(argumentName,
                                parameter.ParameterType);
                        descriptions[argumentName] = argumentDescriptor
                            .CreateDescription();
                    }
                }
            }

            return descriptions.Values;
        }

        private bool IsArgumentType(ParameterInfo parameter)
        {
            return (ArgumentHelper
                .LookupKind(parameter, FieldDescription.Member.ReflectedType) ==
                    ArgumentKind.Argument);
        }

        #region IObjectFieldDescriptor

        IObjectFieldDescriptor IObjectFieldDescriptor.SyntaxNode(FieldDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Name(string name)
        {
            Name(name);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Description(string description)
        {
            Description(description);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.DeprecationReason(string deprecationReason)
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

        IObjectFieldDescriptor IObjectFieldDescriptor.Argument(string name, Action<IArgumentDescriptor> argument)
        {
            Argument(name, argument);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Ignore()
        {
            Ignore();
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Resolver(FieldResolverDelegate fieldResolver)
        {
            Resolver(fieldResolver);
            return this;
        }

        IObjectFieldDescriptor IObjectFieldDescriptor.Resolver(FieldResolverDelegate fieldResolver, Type resultType)
        {
            Resolver(fieldResolver, resultType);
            return this;
        }

        #endregion
    }
}
