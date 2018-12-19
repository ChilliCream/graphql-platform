using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ObjectTypeDescriptor
        : IObjectTypeDescriptor
        , IDescriptionFactory<ObjectTypeDescription>
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

        protected List<ObjectFieldDescriptor> Fields { get; } =
            new List<ObjectFieldDescriptor>();

        protected HashSet<Type> ResolverTypes { get; } =
            new HashSet<Type>();
        protected ObjectTypeDescription ObjectDescription { get; } =
            new ObjectTypeDescription();

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

                if (fieldDescription.Member != null)
                {
                    handledMembers.Add(fieldDescription.Member);
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

        protected void SyntaxNode(ObjectTypeDefinitionNode syntaxNode)
        {
            ObjectDescription.SyntaxNode = syntaxNode;
        }

        protected void Name(NameString name)
        {
            ObjectDescription.Name = name.EnsureNotEmpty(nameof(name));
        }

        protected void Description(string description)
        {
            ObjectDescription.Description = description;
        }

        protected void Interface<TInterface>()
        {
            if (typeof(TInterface) == typeof(InterfaceType))
            {
                throw new ArgumentException(
                    "The interface type has to be inherited.");
            }

            ObjectDescription.Interfaces.Add(
                typeof(TInterface).GetOutputType());
        }

        protected void Interface(NamedTypeNode type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ObjectDescription.Interfaces.Add(new TypeReference(type));
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
                    CreateResolverDescriptor(
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
                        CreateResolverDescriptor(
                            sourceType, resolverType, member.Value)
                        .CreateDescription();

                    if (processed.Add(description.Name))
                    {
                        fields[description.Name] = description;
                    }
                }
            }
        }

        protected static ObjectFieldDescriptor CreateResolverDescriptor(
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


        #region IObjectTypeDescriptor

        IObjectTypeDescriptor IObjectTypeDescriptor.SyntaxNode(
            ObjectTypeDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.Name(NameString name)
        {
            Name(name);
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.Interface<TInterface>()
        {
            Interface<TInterface>();
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.Interface(
            NamedTypeNode type)
        {
            Interface(type);
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

        IObjectTypeDescriptor IObjectTypeDescriptor.Directive(
            string name,
            params ArgumentNode[] arguments)
        {
            ObjectDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }

    internal class ObjectTypeDescriptor<T>
        : ObjectTypeDescriptor
        , IObjectTypeDescriptor<T>
    {
        public ObjectTypeDescriptor()
            : base(typeof(T))
        {
            ObjectDescription.ClrType = typeof(T);
        }

        protected void BindFields(BindingBehavior bindingBehavior)
        {
            ObjectDescription.FieldBindingBehavior = bindingBehavior;
        }

        protected override void OnCompleteFields(
            IDictionary<string, ObjectFieldDescription> fields,
            ISet<MemberInfo> handledMembers)
        {
            if (ObjectDescription.FieldBindingBehavior ==
                BindingBehavior.Implicit)
            {
                AddImplicitFields(fields, handledMembers);
            }

            AddResolverTypes(fields);
        }

        private void AddImplicitFields(
            IDictionary<string, ObjectFieldDescription> fields,
            ISet<MemberInfo> handledMembers)
        {
            foreach (KeyValuePair<MemberInfo, string> member in
                GetAllMembers(handledMembers))
            {
                if (!fields.ContainsKey(member.Value))
                {
                    var fieldDescriptor = new ObjectFieldDescriptor(
                        member.Key,
                        ObjectDescription.ClrType);

                    fields[member.Value] = fieldDescriptor
                        .CreateDescription();
                }
            }
        }

        private Dictionary<MemberInfo, string> GetAllMembers(
            ISet<MemberInfo> handledMembers)
        {
            var members = new Dictionary<MemberInfo, string>();

            foreach (KeyValuePair<string, MemberInfo> member in
                ReflectionUtils.GetMembers(ObjectDescription.ClrType))
            {
                if (!handledMembers.Contains(member.Value))
                {
                    members[member.Value] = member.Key;
                }
            }

            return members;
        }

        #region IObjectTypeDescriptor<T>

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Name(NameString name)
        {
            Name(name);
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.BindFields(
            BindingBehavior bindingBehavior)
        {
            BindFields(bindingBehavior);
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Interface<TInterface>()
        {
            Interface<TInterface>();
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Include<TResolver>()
        {
            Include(typeof(TResolver));
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.IsOfType(
            IsOfType isOfType)
        {
            IsOfType(isOfType);
            return this;
        }

        IObjectFieldDescriptor IObjectTypeDescriptor<T>.Field(
            Expression<Func<T, object>> propertyOrMethod)
        {
            return Field(propertyOrMethod);
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Directive<TDirective>(
            TDirective directive)
        {
            ObjectDescription.Directives.AddDirective(directive);
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Directive<TDirective>()
        {
            ObjectDescription.Directives.AddDirective(new TDirective());
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            ObjectDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Directive(
            string name,
            params ArgumentNode[] arguments)
        {
            ObjectDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }
}
