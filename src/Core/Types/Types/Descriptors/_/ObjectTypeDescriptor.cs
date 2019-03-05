using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    internal class ObjectTypeDescriptor
        : DescriptorBase<ObjectTypeDefinition>
        , IObjectTypeDescriptor
    {
        public ObjectTypeDescriptor(Type clrType)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            ObjectDescription.Name = clrType.GetGraphQLName();
            ObjectDescription.Description = clrType.GetGraphQLDescription();
        }

        public ObjectTypeDescriptor(NameString name)
        {
            ObjectDescription.Name = name.EnsureNotEmpty(nameof(name));
        }

        protected override ObjectTypeDefinition Definition { get; } =
            new ObjectTypeDefinition();

        protected List<ObjectFieldDescriptor> Fields { get; } =
            new List<ObjectFieldDescriptor>();

        protected HashSet<Type> ResolverTypes { get; } =
            new HashSet<Type>();



        public ObjectTypeDescription CreateDescription()
        {
            CompleteFields();
            return ObjectDescription;
        }

        private void CompleteFields()
        {
            var fields = new Dictionary<string, ObjectFieldDescription>();
            var handledMembers = new HashSet<MemberInfo>();

            foreach (ObjectFieldDescriptor fieldDescriptor in Fields)
            {
                ObjectFieldDescription fieldDescription = fieldDescriptor
                    .CreateDescription();

                if (!fieldDescription.Ignored)
                {
                    fields[fieldDescription.Name] = fieldDescription;
                }

                if (fieldDescription.ClrMember != null)
                {
                    handledMembers.Add(fieldDescription.ClrMember);
                }
            }

            OnCompleteFields(fields, handledMembers);

            ObjectDescription.Fields.AddRange(fields.Values);
        }

        protected virtual void OnCompleteFields(
            IDictionary<string, ObjectFieldDescription> fields,
            ISet<MemberInfo> handledMembers)
        {
            AddResolverTypes(fields);
        }

        protected void Include(Type type)
        {
            if (typeof(IType).IsAssignableFrom(type))
            {
                throw new ArgumentException(
                    "Schema types cannot be used as resolver types.");
            }

            ResolverTypes.Add(type);
        }

        protected void IsOfType(IsOfType isOfType)
        {
            ObjectDescription.IsOfType = isOfType
                ?? throw new ArgumentNullException(nameof(isOfType));
        }

        protected ObjectFieldDescriptor Field(NameString name)
        {
            var fieldDescriptor = new ObjectFieldDescriptor(
                name.EnsureNotEmpty(nameof(name)));
            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        protected ObjectFieldDescriptor Field<TResolver>(
            Expression<Func<TResolver, object>> propertyOrMethod)
        {
            if (propertyOrMethod == null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            MemberInfo member = propertyOrMethod.ExtractMember();
            if (member is PropertyInfo || member is MethodInfo)
            {
                ObjectFieldDescriptor fieldDescriptor =
                    CreateFieldDescriptor(
                        ObjectDescription.ClrType,
                        typeof(TResolver), member);
                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
            }

            throw new ArgumentException(
                "A field of an entity can only be a property or a method.",
                nameof(member));
        }

        protected void AddResolverTypes(
            IDictionary<string, ObjectFieldDescription> fields)
        {
            var processed = new HashSet<string>();

            if (ObjectDescription.ClrType != null)
            {
                AddResolverTypes(
                    fields,
                    processed,
                    ObjectDescription.ClrType);
            }

            foreach (Type resolverType in ResolverTypes)
            {
                AddResolverType(
                    fields,
                    processed,
                    ObjectDescription.ClrType ?? typeof(object),
                    resolverType);
            }
        }

        protected static void AddResolverTypes(
            IDictionary<string, ObjectFieldDescription> fields,
            ISet<string> processed,
            Type sourceType)
        {
            if (sourceType.IsDefined(typeof(GraphQLResolverAttribute)))
            {
                foreach (Type resolverType in sourceType
                    .GetCustomAttributes(typeof(GraphQLResolverAttribute))
                    .OfType<GraphQLResolverAttribute>()
                    .SelectMany(attr => attr.ResolverTypes))
                {
                    AddResolverType(
                        fields,
                        processed,
                        sourceType,
                        resolverType);
                }
            }
        }

        protected internal static void AddResolverType(
            IDictionary<string, ObjectFieldDescription> fields,
            ISet<string> processed,
            Type sourceType,
            Type resolverType)
        {
            Dictionary<string, MemberInfo> members =
                ReflectionUtils.GetMembers(resolverType);

            foreach (KeyValuePair<string, MemberInfo> member in members)
            {
                if (IsResolverRelevant(sourceType, member.Value))
                {
                    ObjectFieldDescription description =
                        CreateFieldDescriptor(
                            sourceType, resolverType, member.Value)
                        .CreateDescription();

                    if (processed.Add(description.Name))
                    {
                        fields[description.Name] = description;
                    }
                }
            }
        }

        protected static ObjectFieldDescriptor CreateFieldDescriptor(
            Type sourceType, Type resolverType, MemberInfo member)
        {
            var fieldDescriptor = new ObjectFieldDescriptor(
                member, sourceType);

            if (resolverType != sourceType)
            {
                fieldDescriptor.ResolverType(resolverType);
            }

            return fieldDescriptor;
        }

        private static bool IsResolverRelevant(
            Type sourceType,
            MemberInfo resolver)
        {
            if (resolver is PropertyInfo)
            {
                return true;
            }

            if (resolver is MethodInfo m)
            {
                ParameterInfo parent = m.GetParameters()
                    .FirstOrDefault(t => t.IsDefined(typeof(ParentAttribute)));
                return parent == null
                    || parent.ParameterType.IsAssignableFrom(sourceType);
            }

            return false;
        }


        public IObjectTypeDescriptor SyntaxNode(
            ObjectTypeDefinitionNode objectTypeDefinitionNode)
        {
            Definition.SyntaxNode = objectTypeDefinitionNode;
            return this;
        }

        public IObjectTypeDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IObjectTypeDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }

        public IObjectTypeDescriptor Interface<TInterface>()
            where TInterface : InterfaceType
        {
            if (typeof(TInterface) == typeof(InterfaceType))
            {
                throw new ArgumentException(
                    "The interface type has to be inherited.");
            }

            Definition.Interfaces.Add(
                typeof(TInterface).GetOutputType());
            return this;
        }

        public IObjectTypeDescriptor Interface<TInterface>(
            TInterface interfaceType)
            where TInterface: InterfaceType
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ObjectDescription.Interfaces.Add(new TypeReference(type));
            return this;
        }

        public IObjectTypeDescriptor Interface(
            NamedTypeNode namedTypeNode)
        {
           if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ObjectDescription.Interfaces.Add(new TypeReference(type));
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.Include<TResolver>()
        {
            Include(typeof(TResolver));
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.IsOfType(IsOfType isOfType)
        {
            IsOfType(isOfType);
            return this;
        }

        IObjectFieldDescriptor IObjectTypeDescriptor.Field(NameString name)
        {
            return Field(name);
        }

        IObjectFieldDescriptor IObjectTypeDescriptor.Field<TResolver>(
            Expression<Func<TResolver, object>> propertyOrMethod)
        {
            return Field(propertyOrMethod);
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.Directive<T>(T directive)
        {
            ObjectDescription.Directives.AddDirective(directive);
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.Directive<T>()
        {
            ObjectDescription.Directives.AddDirective(new T());
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            ObjectDescription.Directives.AddDirective(name, arguments);
            return this;
        }
    }
}
