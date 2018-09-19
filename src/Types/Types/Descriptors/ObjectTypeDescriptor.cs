using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ObjectTypeDescriptor
        : IObjectTypeDescriptor
        , IDescriptionFactory<ObjectTypeDescription>
    {
        public ObjectTypeDescriptor(Type objectType)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            ObjectDescription.Name = objectType.GetGraphQLName();
        }

        public ObjectTypeDescriptor(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            ObjectDescription.Name = name;
        }

        protected List<ObjectFieldDescriptor> Fields { get; } =
            new List<ObjectFieldDescriptor>();

        protected ObjectTypeDescription ObjectDescription { get; } =
            new ObjectTypeDescription();

        public ObjectTypeDescription CreateDescription()
        {
            CompleteFields();
            return ObjectDescription;
        }

        protected virtual void CompleteFields()
        {
            var fields = new Dictionary<string, ObjectFieldDescription>();

            foreach (ObjectFieldDescriptor fieldDescriptor in Fields)
            {
                ObjectFieldDescription fieldDescription = fieldDescriptor
                    .CreateDescription();
                fields[fieldDescription.Name] = fieldDescription;
            }

            ObjectDescription.Fields.AddRange(fields.Values);
        }

        protected void SyntaxNode(ObjectTypeDefinitionNode syntaxNode)
        {
            ObjectDescription.SyntaxNode = syntaxNode;
        }

        protected void Name(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsTypeNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL type name.",
                    nameof(name));
            }

            ObjectDescription.Name = name;
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
                new TypeReference(typeof(TInterface)));
        }

        protected void Interface(NamedTypeNode type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ObjectDescription.Interfaces.Add(new TypeReference(type));
        }

        protected void IsOfType(IsOfType isOfType)
        {
            ObjectDescription.IsOfType = isOfType
                ?? throw new ArgumentNullException(nameof(isOfType));
        }

        protected ObjectFieldDescriptor Field(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The field name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsFieldNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL field name.",
                    nameof(name));
            }

            var fieldDescriptor = new ObjectFieldDescriptor(
                ObjectDescription.Name, name);
            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }


        #region IObjectTypeDescriptor

        IObjectTypeDescriptor IObjectTypeDescriptor.SyntaxNode(
            ObjectTypeDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IObjectTypeDescriptor IObjectTypeDescriptor.Name(string name)
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

        IObjectTypeDescriptor IObjectTypeDescriptor.IsOfType(IsOfType isOfType)
        {
            IsOfType(isOfType);
            return this;
        }

        IObjectFieldDescriptor IObjectTypeDescriptor.Field(string name)
        {
            return Field(name);
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
                var fieldDescriptor = new ObjectFieldDescriptor(
                    ObjectDescription.Name, ObjectDescription.ClrType,
                    member, member.GetReturnType());

                if (typeof(TResolver) != ObjectDescription.ClrType)
                {
                    fieldDescriptor.ResolverType(typeof(TResolver));
                }

                Fields.Add(fieldDescriptor);
                return fieldDescriptor;
            }

            throw new ArgumentException(
                "A field of an entity can only be a property or a method.",
                nameof(member));
        }

        protected override void CompleteFields()
        {
            base.CompleteFields();

            var descriptions = new Dictionary<string, ObjectFieldDescription>();
            var handledMembers = new List<MemberInfo>();

            AddExplicitFields(descriptions, handledMembers);

            if (ObjectDescription.FieldBindingBehavior ==
                BindingBehavior.Implicit)
            {
                Dictionary<MemberInfo, string> members =
                    GetPossibleImplicitFields(handledMembers);
                AddImplicitFields(descriptions, members);
            }

            ObjectDescription.Fields.Clear();
            ObjectDescription.Fields.AddRange(descriptions.Values);
        }

        private void AddExplicitFields(
            Dictionary<string, ObjectFieldDescription> descriptors,
            List<MemberInfo> handledMembers)
        {
            foreach (ObjectFieldDescription fieldDescription in
                ObjectDescription.Fields)
            {
                if (!fieldDescription.Ignored)
                {
                    descriptors[fieldDescription.Name] = fieldDescription;
                }

                if (fieldDescription.Member != null)
                {
                    handledMembers.Add(fieldDescription.Member);
                }
            }
        }

        private Dictionary<MemberInfo, string> GetPossibleImplicitFields(
            List<MemberInfo> handledMembers)
        {
            Dictionary<MemberInfo, string> members = GetMembers(
                ObjectDescription.ClrType);

            foreach (MemberInfo member in handledMembers)
            {
                members.Remove(member);
            }

            return members;
        }

        private void AddImplicitFields(
            Dictionary<string, ObjectFieldDescription> descriptors,
            Dictionary<MemberInfo, string> members)
        {
            foreach (KeyValuePair<MemberInfo, string> member in members)
            {
                if (!descriptors.ContainsKey(member.Value))
                {
                    Type returnType = member.Key.GetReturnType();
                    if (returnType != null)
                    {
                        var fieldDescriptor = new ObjectFieldDescriptor(
                            ObjectDescription.Name,
                            ObjectDescription.ClrType,
                            member.Key, returnType);

                        descriptors[member.Value] = fieldDescriptor
                            .CreateDescription();
                    }
                }
            }
        }

        private static Dictionary<MemberInfo, string> GetMembers(Type type)
        {
            var members = new Dictionary<MemberInfo, string>();

            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public)
                .Where(t => t.DeclaringType != typeof(object)))
            {
                members[property] = property.GetGraphQLName();
            }

            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public)
                .Where(m => !m.IsSpecialName
                    && m.DeclaringType != typeof(object)))
            {
                members[method] = method.GetGraphQLName();
            }

            return members;
        }

        #region IObjectTypeDescriptor<T>

        IObjectTypeDescriptor<T> IObjectTypeDescriptor<T>.Name(string name)
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

        IObjectFieldDescriptor IObjectTypeDescriptor<T>.Field<TResolver>(
            Expression<Func<TResolver, object>> propertyOrMethod)
        {
            return Field(propertyOrMethod);
        }

        #endregion
    }
}
