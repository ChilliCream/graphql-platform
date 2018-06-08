using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    internal class FieldDescriptor
        : IFieldDescriptor
        , IInterfaceFieldDescriptor
    {
        private readonly string _typeName;

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

        public FieldDescriptor(string typeName, MemberInfo member, Type nativeType)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            _typeName = typeName;
            Member = member;
            Name = member.GetGraphQLName();
            NativeType = nativeType;
        }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public MemberInfo Member { get; protected set; }

        public Type NativeType { get; protected set; }

        public string DeprecationReason { get; protected set; }

        public ImmutableList<ArgumentDescriptor> Arguments { get; protected set; }
            = ImmutableList<ArgumentDescriptor>.Empty;

        public FieldResolverDelegate Resolver { get; protected set; }

        public Field CreateField()
        {
            return new Field(new FieldConfig
            {
                Name = Name,
                Description = Description,
                DeprecationReason = DeprecationReason,
                Member = Member,
                Type = CreateType,
                NativeNamedType = TypeInspector.Default.ExtractNamedType(NativeType),
                Arguments = CreateArguments(),
                Resolver = CreateResolver
            });
        }

        private IOutputType CreateType(ITypeRegistry typeRegistry)
        {
            return TypeInspector.Default.CreateOutputType(
                typeRegistry, NativeType);
        }

        private IEnumerable<InputField> CreateArguments()
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

            return descriptors.Values.Select(t => t.CreateArgument());
        }

        private bool IsArgumentType(Type argumentType)
        {
            return (FieldResolverArgumentDescriptor
                .LookupKind(argumentType, Member.ReflectedType) ==
                    FieldResolverArgumentKind.Argument)
                && TypeInspector.Default.IsSupported(argumentType);
        }

        private FieldResolverDelegate CreateResolver(
            IResolverRegistry resolverRegistry)
        {
            return Resolver ?? resolverRegistry.GetResolver(_typeName, Name);
        }

        #region IFieldDescriptor

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
            NativeType = typeof(TOutputType);
            return this;
        }

        IFieldDescriptor IFieldDescriptor.Type(Type outputType, bool overwrite)
        {

            if (overwrite == true || NativeType == null)
            {
                if (TypeInspector.Default.IsSupported(outputType))
                {
                    NativeType = outputType;
                }
                else
                {
                    throw new ArgumentException(
                        "The specified is not a supported output type.");
                }
            }
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

        IFieldDescriptor IFieldDescriptor.Resolver(FieldResolverDelegate fieldResolver)
        {
            if (fieldResolver == null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            Resolver = fieldResolver;
            return this;
        }

        #endregion

        #region IInterfaceFieldDescriptor

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

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Argument(string name, Action<IArgumentDescriptor> argument)
        {
            ((IFieldDescriptor)this).Argument(name, argument);
            return this;
        }

        #endregion
    }
}
